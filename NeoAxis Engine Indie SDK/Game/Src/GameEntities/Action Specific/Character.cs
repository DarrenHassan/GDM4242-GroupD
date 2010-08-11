// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.SoundSystem;
using Engine.Utils;
using GameCommon;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Character"/> entity type.
	/// </summary>
	public class CharacterType : UnitType
	{
		const float heightDefault = 1.8f;
		[FieldSerialize]
		float height = heightDefault;

		const float radiusDefault = .4f;
		[FieldSerialize]
		float radius = radiusDefault;

		const float bottomRadiusDefault = .4f / 8;
		[FieldSerialize]
		float bottomRadius = bottomRadiusDefault;

		const float walkUpHeightDefault = .5f;
		[FieldSerialize]
		float walkUpHeight = walkUpHeightDefault;

		const float massDefault = 1;
		[FieldSerialize]
		float mass = massDefault;

		const float walkMaxVelocityDefault = 20;
		[FieldSerialize]
		float walkMaxVelocity = walkMaxVelocityDefault;

		const float walkForceDefault = 1500;
		[FieldSerialize]
		float walkForce = walkForceDefault;

		const float flyControlMaxVelocityDefault = 30;
		[FieldSerialize]
		float flyControlMaxVelocity = flyControlMaxVelocityDefault;

		const float flyControlForceDefault = 150;
		[FieldSerialize]
		float flyControlForce = flyControlForceDefault;

		const float jumpVelocityDefault = 4;
		[FieldSerialize]
		float jumpVelocity = jumpVelocityDefault;

		[FieldSerialize]
		string soundJump;

		float floorHeight;//height from center to floor

		//

		[DefaultValue( heightDefault )]
		public float Height
		{
			get { return height; }
			set { height = value; }
		}

		[DefaultValue( radiusDefault )]
		public float Radius
		{
			get { return radius; }
			set { radius = value; }
		}

		[DefaultValue( bottomRadiusDefault )]
		public float BottomRadius
		{
			get { return bottomRadius; }
			set { bottomRadius = value; }
		}

		[DefaultValue( walkUpHeightDefault )]
		public float WalkUpHeight
		{
			get { return walkUpHeight; }
			set { walkUpHeight = value; }
		}

		[DefaultValue( massDefault )]
		public float Mass
		{
			get { return mass; }
			set { mass = value; }
		}

		[DefaultValue( walkMaxVelocityDefault )]
		public float WalkMaxVelocity
		{
			get { return walkMaxVelocity; }
			set { walkMaxVelocity = value; }
		}

		[DefaultValue( walkForceDefault )]
		public float WalkForce
		{
			get { return walkForce; }
			set { walkForce = value; }
		}

		[DefaultValue( flyControlMaxVelocityDefault )]
		public float FlyControlMaxVelocity
		{
			get { return flyControlMaxVelocity; }
			set { flyControlMaxVelocity = value; }
		}

		[DefaultValue( flyControlForceDefault )]
		public float FlyControlForce
		{
			get { return flyControlForce; }
			set { flyControlForce = value; }
		}

		[DefaultValue( jumpVelocityDefault )]
		public float JumpVelocity
		{
			get { return jumpVelocity; }
			set { jumpVelocity = value; }
		}

		[DefaultValue( "" )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundJump
		{
			get { return soundJump; }
			set { soundJump = value; }
		}

		[Browsable( false )]
		public float FloorHeight
		{
			get { return floorHeight; }
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();
			floorHeight = ( height - walkUpHeight ) * .5f + walkUpHeight;
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			//it is not known how will be used this sound (2D or 3D?).
			//Sound will preloaded as 2D only here.
			if( !string.IsNullOrEmpty( SoundJump ) )
				SoundWorld.Instance.SoundCreate( SoundJump, 0 );
		}
	}

	/// <summary>
	/// Defines the physical characters.
	/// </summary>
	public class Character : Unit
	{
		Body mainBody;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float mainBodyGroundDistance = 1000;//from center of body
		Body groundBody;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float jumpInactiveTime;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float shouldJumpTime;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float onGroundTime;

		Vec3 turnToPosition;
		Radian horizontalDirectionForUpdateRotation;

		//moveVector
		int forceMoveVectorTimer;//if == 0 to disabled
		Vec2 forceMoveVector;

		Vec2 lastTickForceVector;

		bool noWakeBodies;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		Vec3 linearVelocityForSerialization;

		Vec3 groundRelativeVelocity;
		Vec3 server_sentGroundRelativeVelocity;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			JumpEventToClient,
			GroundRelativeVelocityToClient,
		}

		///////////////////////////////////////////

		CharacterType _type = null; public new CharacterType Type { get { return _type; } }

		public void SetForceMoveVector( Vec2 vec )
		{
			forceMoveVectorTimer = 2;
			forceMoveVector = vec;
		}

		[Browsable( false )]
		public Body MainBody
		{
			get { return mainBody; }
		}

		[Browsable( false )]
		public Vec3 TurnToPosition
		{
			get { return turnToPosition; }
		}

		public void SetTurnToPosition( Vec3 pos )
		{
			turnToPosition = pos;

			Vec3 diff = turnToPosition - Position;
			horizontalDirectionForUpdateRotation = MathFunctions.ATan( diff.Y, diff.X );

			UpdateRotation();
		}

		public void UpdateRotation()
		{
			float halfAngle = horizontalDirectionForUpdateRotation * .5f;
			Quat rot = new Quat( new Vec3( 0, 0, MathFunctions.Sin( halfAngle ) ),
				MathFunctions.Cos( halfAngle ) );
			noWakeBodies = true;
			Rotation = rot;
			noWakeBodies = false;

			//TPS arcade specific (camera observe)
			//No update OldRotation
			if( GameMap.Instance != null && GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade )
			{
				if( Intellect != null && PlayerIntellect.Instance == Intellect )
					return;
			}

			OldRotation = rot;
		}

		public bool IsOnGround()
		{
			return mainBodyGroundDistance - .05f < Type.FloorHeight && groundBody != null;
		}

		protected override void OnSave( TextBlock block )
		{
			if( mainBody != null )
				linearVelocityForSerialization = mainBody.LinearVelocity;

			base.OnSave( block );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			SetTurnToPosition( Position + Rotation.GetForward() * 100 );

			CreatePhysicsModel();

			Body body = PhysicsModel.CreateBody();
			mainBody = body;
			body.Name = "main";
			body.Position = Position;
			body.Rotation = Rotation;
			body.Sleepiness = 0;
			body.LinearVelocity = linearVelocityForSerialization;

			float length = Type.Height - Type.Radius * 2 - Type.WalkUpHeight;
			if( length < 0 )
			{
				Log.Error( "Length < 0" );
				return;
			}

			//create main capsule
			{
				CapsuleShape shape = body.CreateCapsuleShape();
				shape.Length = length;
				shape.Radius = Type.Radius;
				shape.ContactGroup = (int)ContactGroup.Dynamic;
				shape.StaticFriction = 0;
				shape.DynamicFriction = 0;
				shape.Bounciness = 0;
				shape.Hardness = 0;
				float r = shape.Radius;
				shape.Density = Type.Mass / ( MathFunctions.PI * r * r * shape.Length +
					( 4.0f / 3.0f ) * MathFunctions.PI * r * r * r );
				shape.SpecialLiquidDensity = .5f;
			}

			//create down capsule
			{
				CapsuleShape shape = body.CreateCapsuleShape();
				shape.Length = Type.Height - Type.BottomRadius * 2;
				shape.Radius = Type.BottomRadius;
				shape.Position = new Vec3( 0, 0,
					( Type.Height - Type.WalkUpHeight ) / 2 - Type.Height / 2 );
				shape.ContactGroup = (int)ContactGroup.Dynamic;
				shape.Bounciness = 0;
				shape.Hardness = 0;
				shape.Density = 0;
				shape.SpecialLiquidDensity = .5f;

				shape.StaticFriction = 0;
				shape.DynamicFriction = 0;
			}

			PhysicsModel.PushToWorld();

			AddTimer();
		}

		protected override void OnSuspendPhysicsDuringMapLoading( bool suspend )
		{
			base.OnSuspendPhysicsDuringMapLoading( suspend );

			//After loading a map, the physics simulate 5 seconds, that bodies have fallen asleep.
			//During this time we will disable physics for this entity.
			foreach( Body body in PhysicsModel.Bodies )
				body.Static = suspend;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			//we call this before OnTick for using old value of MainBody.LinearVelocity
			CalculateGroundRelativeVelocity();

			base.OnTick();

			TickMovement();

			if( Intellect != null )
				TickIntellect( Intellect );

			UpdateRotation();
			TickJump( false );

			if( IsOnGround() )
				onGroundTime += TickDelta;
			else
				onGroundTime = 0;

			if( forceMoveVectorTimer != 0 )
				forceMoveVectorTimer--;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.Client_OnTick()"/>.</summary>
		protected override void Client_OnTick()
		{
			//we call this before OnTick for using old value of MainBody.LinearVelocity
			CalculateGroundRelativeVelocity();

			base.Client_OnTick();

			Vec3 shouldAddForce;
			CalculateMainBodyGroundDistanceAndGroundBody( out shouldAddForce );

			if( IsOnGround() )
				onGroundTime += TickDelta;
			else
				onGroundTime = 0;
		}

		void TickIntellect( Intellect intellect )
		{
			Vec2 forceVec = Vec2.Zero;

			if( forceMoveVectorTimer != 0 )
			{
				forceVec = forceMoveVector;
			}
			else
			{
				Vec2 vec = Vec2.Zero;

				vec.X += intellect.GetControlKeyStrength( GameControlKeys.Forward );
				vec.X -= intellect.GetControlKeyStrength( GameControlKeys.Backward );
				vec.Y += intellect.GetControlKeyStrength( GameControlKeys.Left );
				vec.Y -= intellect.GetControlKeyStrength( GameControlKeys.Right );

				forceVec = ( new Vec3( vec.X, vec.Y, 0 ) * Rotation ).ToVec2();

				//TPS arcade specific (camera observe)
				//set forceVec depending on camera orientation
				if( GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade && forceVec != Vec2.Zero )
				{
					if( Intellect != null && PlayerIntellect.Instance == Intellect )
					{
						//!!!!!!not adapted for networking
						//using RendererWorld.Instance.DefaultCamera is bad

						Vec2 diff = Position.ToVec2() - RendererWorld.Instance.DefaultCamera.
							Position.ToVec2();
						Degree angle = new Radian( MathFunctions.ATan( diff.Y, diff.X ) );
						Degree vecAngle = new Radian( MathFunctions.ATan( -vec.Y, vec.X ) );
						Quat rot = new Angles( 0, 0, vecAngle - angle ).ToQuat();
						forceVec = ( rot * new Vec3( 1, 0, 0 ) ).ToVec2();
					}
				}

				if( forceVec != Vec2.Zero )
				{
					float length = forceVec.Length();
					if( length > 1 )
						forceVec /= length;
				}
			}

			if( forceVec != Vec2.Zero )
			{
				float velocityCoefficient = 1;
				if( FastMoveInfluence != null )
					velocityCoefficient = FastMoveInfluence.Type.Coefficient;

				float maxVelocity;
				float force;

				if( IsOnGround() )
				{
					maxVelocity = Type.WalkMaxVelocity;
					force = Type.WalkForce;
				}
				else
				{
					maxVelocity = Type.FlyControlMaxVelocity;
					force = Type.FlyControlForce;
				}

				maxVelocity *= forceVec.LengthFast();

				//velocityCoefficient
				maxVelocity *= velocityCoefficient;
				force *= velocityCoefficient;

				if( mainBody.LinearVelocity.LengthFast() < maxVelocity )
					mainBody.AddForce( ForceType.Global, 0, new Vec3( forceVec.X, forceVec.Y, 0 ) *
						force * TickDelta, Vec3.Zero );
			}

			lastTickForceVector = forceVec;
		}

		protected override void OnIntellectCommand( Intellect.Command command )
		{
			base.OnIntellectCommand( command );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( command.KeyPressed )
				{
					if( command.Key == GameControlKeys.Jump )
					{
						//TPS arcade specific (camera observe)
						//No jump
						if( GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade )
						{
							if( Intellect != null && PlayerIntellect.Instance == Intellect )
								return;
						}

						TryJump();
					}
				}
			}
		}

		//small distinction of different physics libraries.
		static bool physicsLibraryDetected;
		static bool isPhysX;
		bool IsPhysX()
		{
			if( !physicsLibraryDetected )
			{
				isPhysX = PhysicsWorld.Instance.DriverName.Contains( "PhysX" );
				physicsLibraryDetected = true;
			}
			return isPhysX;
		}

		void UpdateMainBodyDamping()
		{
			if( IsOnGround() && jumpInactiveTime == 0 )
			{
				//small distinction of different physics libraries.
				if( IsPhysX() )
					mainBody.LinearDamping = 9.3f;
				else
					mainBody.LinearDamping = 10;
			}
			else
				mainBody.LinearDamping = .15f;
		}

		void TickMovement()
		{
			if( groundBody != null && groundBody.IsDisposed )
				groundBody = null;

			if( mainBody.Sleeping && groundBody != null && !groundBody.Sleeping &&
				( groundBody.LinearVelocity.LengthSqr() > .3f ||
				groundBody.AngularVelocity.LengthSqr() > .3f ) )
			{
				mainBody.Sleeping = false;
			}

			if( mainBody.Sleeping && IsOnGround() )
				return;

			UpdateMainBodyDamping();

			if( IsOnGround() )
			{
				mainBody.AngularVelocity = Vec3.Zero;

				if( mainBodyGroundDistance + .05f < Type.FloorHeight && jumpInactiveTime == 0 )
				{
					noWakeBodies = true;
					Position = Position + new Vec3( 0, 0, Type.FloorHeight - mainBodyGroundDistance );
					noWakeBodies = false;
				}
			}

			Vec3 shouldAddForce;
			CalculateMainBodyGroundDistanceAndGroundBody( out shouldAddForce );

			//add force to body if need
			if( shouldAddForce != Vec3.Zero )
			{
				mainBody.AddForce( ForceType.GlobalAtLocalPos, TickDelta, shouldAddForce,
					Vec3.Zero );
			}

			//on dynamic ground velocity
			if( IsOnGround() && groundBody != null )
			{
				if( !groundBody.Static && !groundBody.Sleeping )
				{
					Vec3 groundVel = groundBody.LinearVelocity;

					Vec3 vel = mainBody.LinearVelocity;

					if( groundVel.X > 0 && vel.X >= 0 && vel.X < groundVel.X )
						vel.X = groundVel.X;
					else if( groundVel.X < 0 && vel.X <= 0 && vel.X > groundVel.X )
						vel.X = groundVel.X;

					if( groundVel.Y > 0 && vel.Y >= 0 && vel.Y < groundVel.Y )
						vel.Y = groundVel.Y;
					else if( groundVel.Y < 0 && vel.Y <= 0 && vel.Y > groundVel.Y )
						vel.Y = groundVel.Y;

					if( groundVel.Z > 0 && vel.Z >= 0 && vel.Z < groundVel.Z )
						vel.Z = groundVel.Z;
					else if( groundVel.Z < 0 && vel.Z <= 0 && vel.Z > groundVel.Z )
						vel.Z = groundVel.Z;

					mainBody.LinearVelocity = vel;

					//stupid anti damping
					mainBody.LinearVelocity += groundVel * .25f;
				}
			}

			//sleep if on ground and zero velocity

			bool needSleep = false;

			if( IsOnGround() )
			{
				bool groundStopped = groundBody.Sleeping ||
					( groundBody.LinearVelocity.LengthSqr() < .3f &&
					groundBody.AngularVelocity.LengthSqr() < .3f );

				if( groundStopped && mainBody.LinearVelocity.LengthSqr() < 1.0f )
					needSleep = true;
			}

			mainBody.Sleeping = needSleep;
		}

		void CalculateMainBodyGroundDistanceAndGroundBody( out Vec3 shouldAddForce )
		{
			shouldAddForce = Vec3.Zero;

			mainBodyGroundDistance = 1000;
			groundBody = null;

			for( int n = 0; n < 5; n++ )
			{
				Vec3 offset = Vec3.Zero;

				float step = Type.BottomRadius;

				switch( n )
				{
				case 0: offset = new Vec3( 0, 0, 0 ); break;
				case 1: offset = new Vec3( -step, -step, step/* * .1f*/ ); break;
				case 2: offset = new Vec3( step, -step, step/* * .1f*/ ); break;
				case 3: offset = new Vec3( step, step, step/* * .1f*/ ); break;
				case 4: offset = new Vec3( -step, step, step/* * .1f*/ ); break;
				}

				Vec3 pos = Position - new Vec3( 0, 0, Type.FloorHeight -
					Type.WalkUpHeight + .01f ) + offset;
				RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
					new Ray( pos, new Vec3( 0, 0, -Type.Height * 1.5f ) ),
					(int)mainBody.Shapes[ 0 ].ContactGroup );

				if( piercingResult.Length == 0 )
					continue;

				foreach( RayCastResult result in piercingResult )
				{
					if( result.Shape.Body == mainBody )
						continue;

					float dist = Position.Z - result.Position.Z;
					if( dist < mainBodyGroundDistance )
					{
						bool bigSlope = false;

						//max slope check
						const float maxSlopeCoef = .7f;// MathFunctions.Sin( new Degree( 60.0f ).InRadians() );
						if( result.Normal.Z < maxSlopeCoef )
						{
							Vec3 vector = new Vec3( result.Normal.X, result.Normal.Y, 0 );
							if( vector != Vec3.Zero )
							{
								bigSlope = true;

								//add force
								vector.Normalize();
								vector *= mainBody.Mass * 2;
								shouldAddForce += vector;
							}
						}

						if( !bigSlope )
						{
							mainBodyGroundDistance = dist;
							groundBody = result.Shape.Body;
						}
					}
				}
			}
		}

		protected virtual void OnJump()
		{
			SoundPlay3D( Type.SoundJump, .5f, true );
		}

		void TickJump( bool ignoreTicks )
		{
			if( !ignoreTicks )
			{
				if( shouldJumpTime != 0 )
				{
					shouldJumpTime -= TickDelta;
					if( shouldJumpTime < 0 )
						shouldJumpTime = 0;
				}

				if( jumpInactiveTime != 0 )
				{
					jumpInactiveTime -= TickDelta;
					if( jumpInactiveTime < 0 )
						jumpInactiveTime = 0;
				}
			}

			if( IsOnGround() && onGroundTime > TickDelta && jumpInactiveTime == 0 && shouldJumpTime != 0 )
			{
				Vec3 vel = mainBody.LinearVelocity;

				vel.Z = Type.JumpVelocity;
				mainBody.LinearVelocity = vel;
				Position += new Vec3( 0, 0, .05f );

				jumpInactiveTime = .2f;
				shouldJumpTime = 0;

				UpdateMainBodyDamping();

				OnJump();

				if( EntitySystemWorld.Instance.IsServer() )
					Server_SendJumpEventToAllClients();
			}
		}

		public void TryJump()
		{
			//cannot called on client.
			if( EntitySystemWorld.Instance.IsClientOnly() )
				Log.Fatal( "Character: TryJump: EntitySystemWorld.Instance.IsClientOnly()." );

			shouldJumpTime = .4f;
			TickJump( true );
		}

		[Browsable( false )]
		public Vec2 LastTickForceVector
		{
			get { return lastTickForceVector; }
		}

		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );

			if( PhysicsModel != null && !noWakeBodies )
			{
				foreach( Body body in PhysicsModel.Bodies )
					body.Sleeping = false;
			}
		}

		void CalculateGroundRelativeVelocity()
		{
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				//server or single mode
				if( IsOnGround() && groundBody.AngularVelocity.LengthSqr() < .3f )
					groundRelativeVelocity = mainBody.LinearVelocity - groundBody.LinearVelocity;
				else
					groundRelativeVelocity = Vec3.Zero;

				if( EntitySystemWorld.Instance.IsServer() )
				{
					if( !groundRelativeVelocity.Equals( server_sentGroundRelativeVelocity, .1f ) )
					{
						Server_SendGroundRelativeVelocityToClients(
							EntitySystemWorld.Instance.RemoteEntityWorlds, groundRelativeVelocity );
						server_sentGroundRelativeVelocity = groundRelativeVelocity;
					}
				}
			}
			else
			{
				//client

				//groundRelativeVelocity is updated from server, 
				//because body velocities are not synchronized via network.
			}
		}

		[Browsable( false )]
		public Vec3 GroundRelativeVelocity
		{
			get { return groundRelativeVelocity; }
		}

		void Server_SendJumpEventToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Character ),
				(ushort)NetworkMessages.JumpEventToClient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.JumpEventToClient )]
		void Client_ReceiveJumpEvent( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;
			OnJump();
		}

		protected override void Server_OnClientConnectedAfterPostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedAfterPostCreate( remoteEntityWorld );

			IList<RemoteEntityWorld> worlds = new RemoteEntityWorld[] { remoteEntityWorld };
			Server_SendGroundRelativeVelocityToClients( worlds, server_sentGroundRelativeVelocity );
		}

		void Server_SendGroundRelativeVelocityToClients( IList<RemoteEntityWorld> remoteEntityWorlds,
			Vec3 value )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Character ),
				(ushort)NetworkMessages.GroundRelativeVelocityToClient );
			writer.Write( value );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.GroundRelativeVelocityToClient )]
		void Client_ReceiveWeaponVerticalAngle( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			Vec3 value = reader.ReadVec3();
			if( !reader.Complete() )
				return;
			groundRelativeVelocity = value;
		}

	}
}
