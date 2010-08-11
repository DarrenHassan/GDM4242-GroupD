// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="DynamicCollision"/> entity type.
	/// </summary>
	public class DynamicCollisionType : MapObjectType
	{
	}

	/// <summary>
	/// Represents creation of dynamic obstacles. 
	/// By means of this class it is possible to set limiting area of movings for map objects.
	/// </summary>
	public class DynamicCollision : MapObject
	{
		[FieldSerialize]
		bool active = true;

		//

		DynamicCollisionType _type = null; public new DynamicCollisionType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			UpdatePhysicsModel();
		}

		[DefaultValue( true )]
		public bool Active
		{
			get { return active; }
			set
			{
				active = value;
				UpdatePhysicsModel();
			}
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnSetTransform(ref Vec3,ref Quat,ref Vec3)"/>.</summary>
		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );
			UpdatePhysicsModel();
		}

		void UpdatePhysicsModel()
		{
			DestroyPhysicsModel();

			if( active )
			{
				CreatePhysicsModel();

				Body body = PhysicsModel.CreateBody();
				body.Static = true;
				body.Position = Position;
				body.Rotation = Rotation;

				BoxShape shape = body.CreateBoxShape();
				shape.ContactGroup = (int)ContactGroup.Dynamic;// Static;
				shape.Dimensions = Scale;

				body.PushedToWorld = true;
			}
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnRender(Camera)"/>.</summary>
		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( EntitySystemWorld.Instance.IsEditor() && EditorLayer.Visible )
			{
				if( PhysicsModel != null )
				{
					PhysicsWorld.Instance.SetForceDebugRenderColor( new ColorValue( 1, 0, 0 ) );
					foreach( Body body in PhysicsModel.Bodies )
						body.DebugRender( camera.DebugGeometry, 0, 1, true );
					PhysicsWorld.Instance.ResetForceDebugRenderColor();
				}
			}
		}

		protected override bool OnGetEditorSelectionByRay( Ray ray, out Vec3 pos, ref float priority )
		{
			float scale1, scale2;
			bool ret = GetBox().RayIntersection( ray, out scale1, out scale2 );
			if( ret )
				pos = ray.GetPointOnRay( Math.Min( scale1, scale2 ) );
			else
				pos = Vec3.Zero;
			return ret;
		}

		protected override void OnEditorSelectionDebugRender( Camera camera, bool bigBorder, 
			bool simpleGeometry )
		{
			Box box = GetBox();
			box.Expand( bigBorder ? .2f : .1f );
			camera.DebugGeometry.AddBox( box );
		}

	}
}
