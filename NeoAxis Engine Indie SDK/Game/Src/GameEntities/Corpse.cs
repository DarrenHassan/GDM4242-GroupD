// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using GameCommon;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Corpse"/> entity type.
	/// </summary>
	public class CorpseType : DynamicType
	{
		[FieldSerialize]
		string deathAnimationName = "death";
		[FieldSerialize]
		string deadAnimationName = "dead";

		/// <summary>
		/// Gets or sets the name of animation when the object died. 
		/// The given animation is play after <see cref="DeadAnimationName"/>.
		/// </summary>
		[Description( "The name of animation when the object died. The given animation is play after \"DeadAnimationName\"." )]
		[DefaultValue( "death" )]
		public string DeathAnimationName
		{
			get { return deathAnimationName; }
			set { deathAnimationName = value; }
		}

		/// <summary>
		/// Gets or sets the name of animation when the object dies.
		/// </summary>
		[Description( "The name of animation when the object dies." )]
		[DefaultValue( "dead" )]
		public string DeadAnimationName
		{
			get { return deadAnimationName; }
			set { deadAnimationName = value; }
		}
	}

	/// <summary>
	/// Gives an opportunity to create corpses. A difference of a corpse from usual 
	/// object that he changes the orientation depending on a surface. 
	/// Also the class operates animations.
	/// </summary>
	public class Corpse : Dynamic
	{
		[FieldSerialize]
		bool duringDeathAnimation;
		[FieldSerialize]
		bool deadAnimation;
		[FieldSerialize]
		int deathAnimationNumber;

		//

		CorpseType _type = null; public new CorpseType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( PhysicsModel != null )
			{
				foreach( Body body in PhysicsModel.Bodies )
				{
					body.AngularVelocity = Vec3.Zero;

					Angles angles = Rotation.ToAngles();
					if( Math.Abs( angles.Roll ) > 30 || Math.Abs( angles.Pitch ) > 30 )
					{
						Quat oldRotation = body.OldRotation;
						body.Rotation = new Angles( 0, 0, angles.Yaw ).ToQuat();
						body.OldRotation = oldRotation;
					}
				}
			}
		}

		protected override void OnUpdateBaseAnimation()
		{
			base.OnUpdateBaseAnimation();

			//check for end of death animation
			if( duringDeathAnimation )
			{
				if( !CurrentAnimationIsEnabled() )
				{
					duringDeathAnimation = false;
					deadAnimation = true;
				}
			}

			//death animation
			if( !deadAnimation )
			{
				//Choose animation number: death, death2, death3
				if( deathAnimationNumber == 0 )
					deathAnimationNumber = GetRandomAnimationNumber( Type.DeathAnimationName, false );

				string animationName = Type.DeathAnimationName;
				if( deathAnimationNumber != 1 )
					animationName += deathAnimationNumber.ToString();
				UpdateBaseAnimation( animationName, false, false, 1 );

				duringDeathAnimation = true;

				return;
			}

			//dead animation
			if( deadAnimation )
			{
				string animationName = Type.DeadAnimationName;
				if( deathAnimationNumber != 1 )
					animationName += deathAnimationNumber.ToString();
				UpdateBaseAnimation( animationName, false, true, 1 );
				return;
			}
		}
	}
}
