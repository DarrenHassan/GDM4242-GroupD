// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.Utils;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Minefield"/> entity type.
	/// </summary>
	public class MinefieldType : MapObjectType
	{
	}

	public class Minefield : MapObject
	{
		static List<Minefield> instances = new List<Minefield>();

		//

		MinefieldType _type = null; public new MinefieldType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			instances.Add( this );
			base.OnPostCreate( loaded );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			instances.Remove( this );
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnCalculateMapBounds(ref Bounds)"/>.</summary>
		protected override void OnCalculateMapBounds( ref Bounds bounds )
		{
			base.OnCalculateMapBounds( ref bounds );
			bounds.Add( GetBox().ToBounds() );
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnRender(Camera)"/>.</summary>
		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			bool editor = EntitySystemWorld.Instance.IsEditor();
			if( editor && EditorLayer.Visible || EngineDebugSettings.DrawGameSpecificDebugGeometry )
			{
				camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
				camera.DebugGeometry.AddBox( GetBox() );
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
			bool simpleGeometry  )
		{
			Box box = GetBox();
			box.Expand( bigBorder ? .2f : .1f );
			camera.DebugGeometry.AddBox( box );
		}

		public static Minefield GetMinefieldByPosition( Vec3 position )
		{
			//!!!!!slowly if many instances

			foreach( Minefield minefield in instances )
			{
				if( minefield.MapBounds.IsContainsPoint( position ) &&
					minefield.GetBox().IsContainsPoint( position ) )
				{
					return minefield;
				}
			}
			return null;
		}

	}
}
