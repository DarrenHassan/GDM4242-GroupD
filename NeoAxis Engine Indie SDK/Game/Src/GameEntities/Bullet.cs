// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using GameCommon;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Bullet"/> entity type.
	/// </summary>
	public class BulletType : DynamicType
	{
		[FieldSerialize]
		float velocity;

		[FieldSerialize]
		float maxDistance;

		[FieldSerialize]
		float damage;

		[FieldSerialize]
		float impulse;

		[FieldSerialize]
		float gravity;

		//

		public BulletType()
		{
			AllowEmptyName = true;
		}

		[DefaultValue( 0.0f )]
		public float Velocity
		{
			get { return velocity; }
			set { velocity = value; }
		}

		[DefaultValue( 0.0f )]
		public float MaxDistance
		{
			get { return maxDistance; }
			set { maxDistance = value; }
		}

		[DefaultValue( 0.0f )]
		public float Damage
		{
			get { return damage; }
			set { damage = value; }
		}

		[DefaultValue( 0.0f )]
		public float Impulse
		{
			get { return impulse; }
			set { impulse = value; }
		}

		[DefaultValue( 0.0f )]
		public float Gravity
		{
			get { return gravity; }
			set { gravity = value; }
		}

		//only for gravity bullets
		//-1 - will not get
		public float GetNeedFireAngleToPosition( float horizontalDistance, float verticalDistance )
		{
			float sh = horizontalDistance;
			float sv = verticalDistance;
			float v0 = velocity;

			//!!!!!currently only for big angle fire
			float approximationAngle = MathFunctions.DegToRad( 70 );
			float approximationStep = MathFunctions.DegToRad( 29.9f );

			float nearestsh = 0;

			const int iterationCount = 10;//!!!!!!for howitzers it is more. mb move to method parameter
			for( int iteration = 0; iteration < iterationCount; iteration++ )
			{
				double[] sidesh = new double[ 2 ];

				for( int iterside = 0; iterside < 2; iterside++ )
				{
					float angle = approximationAngle;
					if( iterside == 0 )
						angle += approximationStep;
					else
						angle -= approximationStep;

					if( angle > MathFunctions.PI / 2 )
						angle = MathFunctions.PI / 2 - .001f;

					//!!!!!ignore invalid angle (there is a way more correctly)
					if( angle < MathFunctions.DegToRad( 45 ) )
					{
						sidesh[ iterside ] = 100000.0f;
						continue;
					}

					double vh = MathFunctions.Cos16( angle ) * v0;
					double vv0 = MathFunctions.Sin16( angle ) * v0;

					double t;
					{
						double a = ( -gravity );
						double b = 2.0 * vv0;
						double c = -2.0 * sv;

						double d = b * b - 4.0 * a * c;

						if( d < 0 )
							Log.Warning( "BulletType.GetNeedAngleToPosition: d < 0 ({0})", d );

						double dsqrt = MathFunctions.Sqrt16( (float)d );

						double x1 = ( -b - dsqrt ) / ( 2.0 * a );
						double x2 = ( -b + dsqrt ) / ( 2.0 * a );

						if( x1 < 0 && x2 < 0 )
							Log.Warning( "BulletType.GetNeedAngleToPosition: x1 < 0 && x2 < 0" );

						t = Math.Max( x1, x2 );
					}

					double calcedsh = vh * t;

					sidesh[ iterside ] = calcedsh;
				}

				if( Math.Abs( sidesh[ 0 ] - sh ) < Math.Abs( sidesh[ 1 ] - sh ) )
				{
					approximationAngle += approximationStep;
					nearestsh = (float)sidesh[ 0 ];
				}
				else
				{
					approximationAngle -= approximationStep;
					nearestsh = (float)sidesh[ 1 ];
				}
				approximationStep *= .5f;
			}

			//!!!!admissible error
			if( Math.Abs( horizontalDistance - nearestsh ) > 5 )
				return -1;

			return approximationAngle;
		}

	}

	/// <summary>
	/// Defines the bullets.
	/// </summary>
	public class Bullet : Dynamic
	{
		[FieldSerialize]
		Vec3 velocity;

		[FieldSerialize]
		Unit sourceUnit;

		[FieldSerialize]
		float damageCoefficient = 1.0f;

		[FieldSerialize]
		Vec3 startPosition;

		[FieldSerialize]
		float flyDistance;

		bool firstTick = true;

		//

		BulletType _type = null; public new BulletType Type { get { return _type; } }

		public Unit SourceUnit
		{
			get { return sourceUnit; }
			set
			{
				if( sourceUnit != null )
					RemoveRelationship( sourceUnit );
				sourceUnit = value;
				if( sourceUnit != null )
					AddRelationship( sourceUnit );
			}
		}

		public Vec3 Velocity
		{
			get { return velocity; }
			set { velocity = value; }
		}

		public float DamageCoefficient
		{
			get { return damageCoefficient; }
			set { damageCoefficient = value; }
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			if( Type.Velocity != 0 )
				velocity = Rotation.GetForward() * Type.Velocity;
			startPosition = Position;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );
			if( sourceUnit == entity )
				sourceUnit = null;
		}

		protected virtual void OnHit( Shape shape, Vec3 normal, MapObject obj )
		{
			if( obj != null )
			{
				//impulse
				float impulse = Type.Impulse * DamageCoefficient;
				if( impulse != 0 && obj.PhysicsModel != null )
				{
					shape.Body.AddForce( ForceType.GlobalAtGlobalPos, 0,
						Rotation.GetForward() * impulse, Position );
				}

				//damage
				Dynamic dynamic = obj as Dynamic;
				if( dynamic != null )
				{
					float damage = Type.Damage * DamageCoefficient;
					if( damage != 0 )
						dynamic.DoDamage( this, Position, shape, damage, true );
				}
			}

			Die();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( Type.Velocity != 0 )
			{
				if( Type.Gravity != 0 )
					velocity.Z -= Type.Gravity * TickDelta;

				Vec3 offset = velocity * TickDelta;
				float distance = offset.LengthFast();

				bool deleteIfNoCollisions = false;
				if( Type.MaxDistance != 0 )
				{
					if( flyDistance + distance >= Type.MaxDistance )
					{
						distance = Type.MaxDistance - flyDistance;
						if( distance <= 0 )
							distance = .001f;
						offset = offset.GetNormalizeFast() * distance;
						deleteIfNoCollisions = true;
					}
				}

				Vec3 startPosition = Position;

				//back check (that did not fly by through moving towards objects)
				if( !firstTick )
					startPosition -= offset * .1f;

				Ray ray = new Ray( startPosition, offset );

				RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
					ray, (int)ContactGroup.CastOnlyContact );

				foreach( RayCastResult result in piercingResult )
				{
					MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

                    // Is the MapObject the ray hit the entity that fired the bullet
					if( obj != null )
					{
						Dynamic dynamic = obj as Dynamic;
						if( dynamic != null && sourceUnit != null &&
							dynamic.GetParentUnitHavingIntellect() == sourceUnit )
							continue;
					}

					Position = result.Position;
					OnHit( result.Shape, result.Normal, obj );
					CreateWaterPlaneSplash( new Ray( startPosition, Position - startPosition ) );
					goto end;
				}

				Position += offset;
				flyDistance += distance;

				//update rotation
				if( velocity != Vec3.Zero )
					Rotation = Quat.FromDirectionZAxisUp( velocity.GetNormalizeFast() );

				CreateWaterPlaneSplash( ray );

				if( deleteIfNoCollisions )
				{
					SetDeleted();
					return;
				}
			}
			else
			{
				const float checkPieceSize = 40.0f;

				Bounds checkBounds = SceneManager.Instance.GetTotalObjectsBounds();
				checkBounds.Expand( 200 );

				Vec3 dir = Rotation.GetForward();

				Vec3 pos = Position;
				while( checkBounds.IsContainsPoint( pos ) )
				{
					Vec3 offset = dir * checkPieceSize;
					float distance = offset.LengthFast();

					bool deleteIfNoCollisions = false;
					if( Type.MaxDistance != 0 )
					{
						if( flyDistance + distance >= Type.MaxDistance )
						{
							distance = Type.MaxDistance - flyDistance;
							if( distance <= 0 )
								distance = .001f;
							offset = offset.GetNormalizeFast() * distance;
							deleteIfNoCollisions = true;
						}
					}

					Ray ray = new Ray( pos, offset );

					RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
						ray, (int)ContactGroup.CastOnlyContact );

					foreach( RayCastResult result in piercingResult )
					{
						MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

						if( obj != null )
						{
							Dynamic dynamic = obj as Dynamic;
							if( dynamic != null && sourceUnit != null &&
								dynamic.GetParentUnitHavingIntellect() == sourceUnit )
								continue;
						}

						Position = result.Position;
						OnHit( result.Shape, result.Normal, obj );
						CreateWaterPlaneSplash( new Ray( ray.Origin, Position - ray.Origin ) );
						goto end;
					}

					pos += offset;
					flyDistance += distance;

					CreateWaterPlaneSplash( ray );

					if( deleteIfNoCollisions )
					{
						SetDeleted();
						return;
					}

				}
				SetDeleted();
			}

			end: ;

			firstTick = false;
		}

		protected override void OnDieObjectCreate( MapObjectCreateObject createObject,
			object objectCreated )
		{
			base.OnDieObjectCreate( createObject, objectCreated );

			MapObjectCreateMapObject createMapObject = createObject as MapObjectCreateMapObject;
			if( createMapObject != null )
			{
				MapObject mapObject = (MapObject)objectCreated;

				Explosion explosion = mapObject as Explosion;
				if( explosion != null )
				{
					explosion.DamageCoefficient = DamageCoefficient;
					explosion.SourceUnit = SourceUnit;
				}
			}
		}

		protected override void OnLifeTimeIsOver()
		{
			SetDeleted();
		}

		void CreateWaterPlaneSplash( Ray ray )
		{
			if( ray.Direction.Z >= 0 )
				return;

			foreach( WaterPlane waterPlane in WaterPlane.Instances )
			{
				//check by plane
				Plane plane = new Plane( Vec3.ZAxis, waterPlane.Position.Z );
				float scale;
				if( !plane.LineIntersection( ray.Origin, ray.Origin + ray.Direction, out scale ) )
					continue;
				Vec3 pos = ray.GetPointOnRay( scale );

				//check by bounds
				Rect bounds2 = new Rect( waterPlane.Position.ToVec2() );
				bounds2.Expand( waterPlane.Size * .5f );
				if( !bounds2.IsContainsPoint( pos.ToVec2() ) )
					continue;

				//create splash
				waterPlane.CreateSplash( WaterPlaneType.SplashTypes.Bullet, pos );
			}
		}
	}
}
