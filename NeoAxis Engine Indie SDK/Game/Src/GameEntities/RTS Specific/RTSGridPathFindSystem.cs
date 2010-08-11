// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="RTSGridPathFindSystem"/> entity type.
	/// </summary>
	public class RTSGridPathFindSystemType : GridPathFindSystemType
	{
	}

	public class RTSGridPathFindSystem : GridPathFindSystem
	{
		[TypeField]
		RTSGridPathFindSystemType __type = null;
		/// <summary>
		/// Gets the entity type.
		/// </summary>
		public new RTSGridPathFindSystemType Type { get { return __type; } }

		protected override void OnGetObjectBounds( MapObject obj, List<Rect> rectangles )
		{
			//RTSCharacter
			RTSCharacter character = obj as RTSCharacter;
			if( character != null )
			{
				float radius = character.Type.Radius;
				rectangles.Add( new Rect( 
					obj.Position.ToVec2() - new Vec2( radius, radius ),
					obj.Position.ToVec2() + new Vec2( radius, radius ) ) );
				return;
			}

			//all other objects
			if( obj.PhysicsModel != null )
			{
				foreach( Body body in obj.PhysicsModel.Bodies )
				{
					foreach( Shape shape in body.Shapes )
					{
						if( shape.ContactGroup == (int)ContactGroup.NoContact )
							continue;

						Bounds bounds = shape.GetGlobalBounds();
						rectangles.Add( new Rect( bounds.Minimum.ToVec2(), bounds.Maximum.ToVec2() ) );
					}
				}
				return;
			}
			
			//base.OnGetObjectBounds( obj, rectangles );
		}
	}
}
