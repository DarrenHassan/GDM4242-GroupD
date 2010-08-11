// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Firework"/> entity type.
	/// </summary>
	public class FireworkType : DynamicType
	{
	}

	public class Firework : Dynamic
	{
		FireworkType _type = null; public new FireworkType Type { get { return _type; } }

		BulletType fireworkBulletType;

		float fireTimeRemaining;

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			fireworkBulletType = (BulletType)EntityTypes.Instance.GetByName( "FireworkBullet" );

			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			fireTimeRemaining -= TickDelta;

			if( fireTimeRemaining <= 0 )
			{
				fireTimeRemaining = .1f;
				Fire();
			}
		}

		void Fire()
		{
			if( !Visible )
				return;

			if( fireworkBulletType == null )
				return;

			Bullet bullet = (Bullet)Entities.Instance.Create( fireworkBulletType, Map.Instance );

			bullet.Position = GetInterpolatedPosition() + new Vec3( 0, 0, .1f );

			EngineRandom random = World.Instance.Random;
			Quat rot = new Angles( random.NextFloatCenter() * 25, 90 + random.NextFloatCenter() * 25, 0 ).ToQuat();

			bullet.Rotation = rot;

			bullet.PostCreate();

			foreach( MapObjectAttachedObject attachedObject in bullet.AttachedObjects )
			{
				MapObjectAttachedRibbonTrail attachedRibbonTrail = attachedObject as MapObjectAttachedRibbonTrail;
				if( attachedRibbonTrail == null )
					continue;

				ColorValue color;
				switch( random.Next( 4 ) )
				{
				case 0: color = new ColorValue( 1, 0, 0 ); break;
				case 1: color = new ColorValue( 0, 1, 0 ); break;
				case 2: color = new ColorValue( 0, 0, 1 ); break;
				case 3: color = new ColorValue( 1, 1, 0 ); break;
				default: color = new ColorValue( 0, 0, 0 ); break;
				}

				if( attachedRibbonTrail.RibbonTrail != null )
					attachedRibbonTrail.RibbonTrail.Chains[ 0 ].InitialColor = color;
			}
		}

	}
}
