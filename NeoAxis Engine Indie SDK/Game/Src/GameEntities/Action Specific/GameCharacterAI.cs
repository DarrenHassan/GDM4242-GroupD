// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="GameCharacterAI"/> entity type.
	/// </summary>
	public class GameCharacterAIType : AIType
	{
	}

	public class GameCharacterAI : AI
	{
		//optimization
		List<Weapon> initialWeapons;

		float updateTaskTimer;

		[FieldSerialize]
		TaskMoveValue taskMove = new TaskMoveValue( null );
		[FieldSerialize]
		Dynamic taskAttack;

		[FieldSerialize]
		TaskMoveValue forceTaskMove = new TaskMoveValue( null );
		[FieldSerialize]
		Dynamic forceTaskAttack;

		///////////////////////////////////////////

		public struct TaskMoveValue
		{
			[FieldSerialize]
			internal Vec3 position;
			[FieldSerialize]
			internal Dynamic dynamic;

			public Vec3 Position
			{
				get { return position; }
			}

			public Dynamic Dynamic
			{
				get { return dynamic; }
			}

			public TaskMoveValue( Vec3 position )
			{
				this.position = position;
				this.dynamic = null;
			}

			public TaskMoveValue( Dynamic dynamic )
			{
				this.position = new Vec3( float.NaN, float.NaN, float.NaN );
				this.dynamic = dynamic;
			}

			public bool IsInitialized
			{
				get { return !float.IsNaN( position.X ) || dynamic != null; }
			}
		}

		///////////////////////////////////////////

		GameCharacterAIType _type = null; public new GameCharacterAIType Type { get { return _type; } }

		public GameCharacterAI()
		{
			updateTaskTimer = World.Instance.Random.NextFloat() * 3;
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

			if( taskMove.Dynamic == entity )
				taskMove.dynamic = null;
			if( taskAttack == entity )
				taskAttack = null;
			if( forceTaskMove.Dynamic == entity )
				forceTaskMove.dynamic = null;
			if( forceTaskAttack == entity )
				ForceTaskAttack = null;
		}

		protected float GetTaskMoveObjectPriority( Unit obj )
		{
			return 0;
		}

		protected float GetTaskAttackObjectPriority( Unit obj )
		{
			if( ControlledObject == obj )
				return 0;

			if( obj.Intellect != null )
			{
				if( Faction != obj.Intellect.Faction )
				{
					Vec3 distance = obj.Position - ControlledObject.Position;
					float len = distance.LengthFast();
					return 1.0f / len + 1.0f;
				}
			}
			return 0;
		}

		protected bool UpdateTasks()
		{
			//!!!!!!!slowly

			GameCharacter controlledObj = ControlledObject;

			if( controlledObj == null )
				return false;

			TaskMoveValue newTaskMove = new TaskMoveValue( null );
			Dynamic newTaskAttack = null;

			float moveObjectPriority = 0;
			float attackObjectPriority = 0;

			Vec3 controlledObjPos = controlledObj.Position;
			float radius = controlledObj.ViewRadius;

			Map.Instance.GetObjects( new Sphere( controlledObjPos, radius ),
				GameFilterGroups.UnitFilterGroup, delegate( MapObject mapObject )
			{
				Unit obj = (Unit)mapObject;

				Vec3 objpos = obj.Position;

				//check distance
				Vec3 diff = objpos - controlledObjPos;
				float objDistance = diff.LengthFast();
				if( objDistance > radius )
					return;

				float priority;

				//Move task
				{
					priority = GetTaskMoveObjectPriority( obj );
					if( priority != 0 && priority > moveObjectPriority )
					{
						moveObjectPriority = priority;
						newTaskMove = new TaskMoveValue( obj );
					}
				}

				//Attack task
				{
					if( initialWeapons.Count != 0 )
					{
						priority = GetTaskAttackObjectPriority( obj );
						if( priority != 0 && priority > attackObjectPriority )
						{
							attackObjectPriority = priority;
							newTaskAttack = obj;
						}
					}
				}
			} );

			//Move task
			{
				if( !newTaskMove.IsInitialized && newTaskAttack != null )
					newTaskMove = new TaskMoveValue( newTaskAttack );

				if( taskMove.Position != newTaskMove.Position || taskMove.Dynamic != newTaskMove.Dynamic )
					TaskMove = newTaskMove;
			}

			//Attack task
			{
				if( taskAttack != newTaskAttack )
					TaskAttack = newTaskAttack;
			}

			return taskMove.IsInitialized || taskAttack != null;
		}

		[Browsable( false )]
		public new GameCharacter ControlledObject
		{
			//!!!!!!slowly
			get { return (GameCharacter)base.ControlledObject; }
		}

		void UpdateInitialWeapons()
		{
			GameCharacter controlledObj = ControlledObject;

			initialWeapons = new List<Weapon>();

			foreach( MapObjectAttachedObject attachedObject in controlledObj.AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject != null )
				{
					Weapon weapon = attachedMapObject.MapObject as Weapon;
					if( weapon != null )
					{
						initialWeapons.Add( weapon );
					}
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			GameCharacter controlledObj = ControlledObject;
			if( controlledObj != null )
			{
				if( initialWeapons == null )
					UpdateInitialWeapons();

				if( !forceTaskMove.IsInitialized && forceTaskAttack == null )
				{
					updateTaskTimer -= TickDelta;
					if( updateTaskTimer <= 0 )
					{
						updateTaskTimer += 1.0f;
						if( !UpdateTasks() )
							updateTaskTimer += .5f;
					}
				}

				TickTasks();
			}

			if( taskAttack != null )
			{
				foreach( Weapon weapon in initialWeapons )
				{
					Vec3 pos = taskAttack.Position;
					Gun gun = weapon as Gun;
					if( gun != null )
						gun.GetAdvanceAttackTargetPosition( false, taskAttack, false, out pos );
					weapon.SetForceFireRotationLookTo( pos );
				}
			}
		}

		protected void TickTasks()
		{
			GameCharacter controlledObj = ControlledObject;

			float distanceAttack;
			if( taskAttack != null )
				distanceAttack = ( taskAttack.Position - controlledObj.Position ).LengthFast();
			else
				distanceAttack = 0;

			if( taskMove.IsInitialized )
			{
				Vec3 taskPos;
				if( taskMove.Dynamic != null )
					taskPos = taskMove.Dynamic.Position;
				else
					taskPos = taskMove.Position;

				Vec2 diff = taskPos.ToVec2() - controlledObj.Position.ToVec2();
				float len;

				Vec2 vec = diff;
				len = vec.NormalizeFast();

				if( taskAttack != null && taskAttack == taskMove.Dynamic )
				{
					Range optimalAttackDistanceRange = controlledObj.Type.OptimalAttackDistanceRange;

					if( distanceAttack < optimalAttackDistanceRange.Minimum )
						vec *= -1.0f;
					else if( distanceAttack <= optimalAttackDistanceRange.Maximum )
						vec = Vec2.Zero;
				}
				else
				{
					if( len > .3f )
					{
						if( len < 1.5f )
						{
							vec *= len;

							//come
							if( taskAttack == null )
							{
								if( taskMove.Dynamic == null )
								{
									if( taskMove.dynamic == forceTaskMove.dynamic &&
										taskMove.position == forceTaskMove.position )
										ForceTaskMove = new TaskMoveValue( null );

									TaskMove = new TaskMoveValue( null );
								}
								else
									vec = Vec2.Zero;
							}
						}
					}
					else
						vec = Vec2.Zero;
				}

				if( !controlledObj.ForceAnimationIsEnabled() )
				{
					if( vec != Vec2.Zero )
						controlledObj.SetForceMoveVector( vec );
				}

				if( taskAttack == null || taskAttack == taskMove.Dynamic )
					controlledObj.SetTurnToPosition( taskPos );
			}

			if( taskAttack != null )
			{
				foreach( Weapon weapon in initialWeapons )
				{
					if( !weapon.Ready )
						continue;

					//weapon.SetForceFireRotationLookTo( true, taskAttack.Position );

					Range range;

					range = weapon.Type.WeaponNormalMode.UseDistanceRange;
					if( distanceAttack >= range.Minimum && distanceAttack <= range.Maximum )
						weapon.TryFire( false );

					range = weapon.Type.WeaponAlternativeMode.UseDistanceRange;
					if( distanceAttack >= range.Minimum && distanceAttack <= range.Maximum )
						weapon.TryFire( true );
				}

				if( taskAttack != taskMove.Dynamic )
					controlledObj.SetTurnToPosition( taskAttack.Position );
			}
		}

		protected override void OnControlledObjectRender( Camera camera )
		{
			base.OnControlledObjectRender( camera );

			if( camera != RendererWorld.Instance.DefaultCamera )
				return;

			if( EngineDebugSettings.DrawGameSpecificDebugGeometry )
			{
				GameCharacter controlledObj = ControlledObject;

				if( controlledObj != null )
				{
					if( taskMove.IsInitialized )
					{
						Vec3 pos;
						if( taskMove.Dynamic != null )
							pos = taskMove.Dynamic.Position;
						else
							pos = taskMove.Position;

						bool ignore = false;
						if( taskAttack != null && taskAttack == taskMove.Dynamic )
							ignore = true;

						if( !ignore )
						{
							camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .5f );
							camera.DebugGeometry.AddLine( controlledObj.Position, pos );
							camera.DebugGeometry.AddSphere( new Sphere( pos, 1 ) );
						}
					}

					if( taskAttack != null )
					{
						Vec3 pos = taskAttack.Position;
						camera.DebugGeometry.Color = new ColorValue( 1, 0, 0, .5f );
						camera.DebugGeometry.AddLine( controlledObj.Position, pos );
						camera.DebugGeometry.AddSphere( new Sphere( pos, 1 ) );
					}
				}
			}
		}

		[Browsable( false )]
		public TaskMoveValue ForceTaskMove
		{
			get { return forceTaskMove; }
			set
			{
				if( forceTaskMove.Dynamic != null )
					RemoveRelationship( forceTaskMove.Dynamic );
				forceTaskMove = value;
				if( forceTaskMove.Dynamic != null )
					AddRelationship( forceTaskMove.Dynamic );

				if( forceTaskMove.IsInitialized )
					TaskMove = forceTaskMove;
			}
		}

		[Browsable( false )]
		public Dynamic ForceTaskAttack
		{
			get { return forceTaskAttack; }
			set
			{
				if( forceTaskAttack != null )
					RemoveRelationship( forceTaskAttack );
				forceTaskAttack = value;
				if( forceTaskAttack != null )
					AddRelationship( forceTaskAttack );

				TaskAttack = forceTaskAttack;
			}
		}

		[Browsable( false )]
		public TaskMoveValue TaskMove
		{
			get { return taskMove; }
			set
			{
				if( taskMove.Dynamic != null )
					RemoveRelationship( taskMove.Dynamic );
				taskMove = value;
				if( taskMove.Dynamic != null )
					AddRelationship( taskMove.Dynamic );
			}
		}

		[Browsable( false )]
		public Dynamic TaskAttack
		{
			get { return taskAttack; }
			set
			{
				if( taskAttack != null )
					RemoveRelationship( taskAttack );
				taskAttack = value;
				if( taskAttack != null )
					AddRelationship( taskAttack );
			}
		}

	}
}
