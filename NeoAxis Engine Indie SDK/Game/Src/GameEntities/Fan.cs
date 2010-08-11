// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Fan"/> entity type.
	/// </summary>
	public class FanType : DynamicType
	{
	}

	/// <summary>
	/// Defines a fans.
	/// </summary>
	/// <remarks>
	/// It is necessary that in physical model there 
	/// was a <see cref="Engine.PhysicsSystem.GearedMotor"/> with a name "bladesMotor". 
	/// </remarks>
	public class Fan : Dynamic
	{
		[FieldSerialize]
		float forceMaximum = 50;

		[FieldSerialize]
		Vec3 influenceRegionScale = new Vec3( 20, 3, 3 );

		[FieldSerialize]
		float throttle = 1;

		InfluenceRegion region;

		GearedMotor bladesMotor;

		float server_sentVelocityCoefficient;
		float client_velocityCoefficient;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			VelocityCoefficientToClient,
		}

		///////////////////////////////////////////

		FanType _type = null; public new FanType Type { get { return _type; } }

		/// <summary>
		/// Gets or sets the the maximal pushing force.
		/// </summary>
		[Description( "The the maximal pushing force." )]
		public float ForceMaximum
		{
			get { return forceMaximum; }
			set { forceMaximum = value; }
		}

		/// <summary>
		/// Gets or sets the current power.
		/// </summary>
		[Description( "The current power." )]
		public float Throttle
		{
			get { return throttle; }
			set
			{
				if( value < -1 || value > 1 )
					throw new InvalidOperationException( "Throttle need in [-1,1] interval" );
				throttle = value;
			}
		}

		[DefaultValue( typeof( Vec3 ), "20 3 3" )]
		public Vec3 InfluenceRegionScale
		{
			get { return influenceRegionScale; }
			set
			{
				influenceRegionScale = value;

				if( region != null )
					region.SetTransform( Position, Rotation, InfluenceRegionScale );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				const string regionTypeName = "ManualInfluenceRegion";
				InfluenceRegionType regionType = (InfluenceRegionType)EntityTypes.Instance.GetByName(
					regionTypeName );
				if( regionType == null )
				{
					regionType = (InfluenceRegionType)EntityTypes.Instance.ManualCreateType(
						regionTypeName,
						EntityTypes.Instance.GetClassInfoByEntityClassName( "InfluenceRegion" ) );
					regionType.NetworkType = EntityNetworkTypes.ServerOnly;
				}

				region = (InfluenceRegion)Entities.Instance.Create( regionType, Map.Instance );
				region.ShapeType = Region.ShapeTypes.Capsule;
				region.DistanceFunction = InfluenceRegion.DistanceFunctionType.NormalFadeAxisX;

				region.SetTransform( Position, Rotation, InfluenceRegionScale );
				region.PostCreate();
				region.AllowSave = false;
				region.EditorSelectable = false;

				bladesMotor = PhysicsModel.GetMotor( "bladesMotor" ) as GearedMotor;
			}

			AddTimer();

			UpdateParticlesForceCoefficient( 0 );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( region != null )
			{
				region.SetShouldDelete();
				region = null;
			}
			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnSetTransform(ref Vec3,ref Quat,ref Vec3)"/>.</summary>
		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );

			if( region != null )
				region.SetTransform( Position, Rotation, InfluenceRegionScale );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			bladesMotor.Throttle = throttle;

			float velocityCoefficient = CalculateVelocityCoefficient();

			region.ImpulsePerSecond = forceMaximum;
			region.Force = velocityCoefficient;

			UpdateParticlesForceCoefficient( velocityCoefficient );

			if( EntitySystemWorld.Instance.IsServer() )
			{
				if( Type.NetworkType == EntityNetworkTypes.Synchronized )
					Server_TickSendVelocityCoefficientToAllClients();
			}
		}

		float CalculateVelocityCoefficient()
		{
			if( bladesMotor == null )
				return 0;

			HingeJoint joint = bladesMotor.Joint as HingeJoint;
			Trace.Assert( joint != null );

			Radian jointVelocity = joint.Axis.Velocity;

			float velocityCoefficient = jointVelocity / bladesMotor.MaxVelocity;
			MathFunctions.Clamp( ref velocityCoefficient, -1.0f, 1.0f );

			return velocityCoefficient;
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();

			UpdateParticlesForceCoefficient( client_velocityCoefficient );
		}

		void UpdateParticlesForceCoefficient( float forceCoefficient )
		{
			//Set ForceCoefficient to particles
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedParticle attachedParticleObject =
					attachedObject as MapObjectAttachedParticle;
				if( attachedParticleObject != null )
					attachedParticleObject.ForceCoefficient = forceCoefficient;
			}
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendVelocityCoefficientToClients( new RemoteEntityWorld[] { remoteEntityWorld },
				server_sentVelocityCoefficient );
		}

		void Server_TickSendVelocityCoefficientToAllClients()
		{
			float velocityCoefficient = CalculateVelocityCoefficient();

			if( Math.Abs( velocityCoefficient - server_sentVelocityCoefficient ) > .05f )
			{
				Server_SendVelocityCoefficientToClients( EntitySystemWorld.Instance.RemoteEntityWorlds,
					velocityCoefficient );
				server_sentVelocityCoefficient = velocityCoefficient;
			}
		}

		void Server_SendVelocityCoefficientToClients( IList<RemoteEntityWorld> remoteEntityWorlds,
			float value )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Fan ),
				(ushort)NetworkMessages.VelocityCoefficientToClient );
			writer.WriteSignedSingle( value, 16 );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.VelocityCoefficientToClient )]
		void Client_ReceiveVelocityCoefficient( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			float value = reader.ReadSignedSingle( 16 );
			if( !reader.Complete() )
				return;
			client_velocityCoefficient = value;
		}

	}
}
