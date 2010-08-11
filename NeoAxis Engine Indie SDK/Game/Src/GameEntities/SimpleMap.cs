// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.EntitySystem;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;
using GameCommon;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="SimpleMap"/> entity type. 
	/// Used for preview types in the Resource Editor.
	/// </summary>
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class SimpleMapType : MapType
	{
	}

	public class SimpleMap : Map
	{
		///////////////////////////////////////////

		SimpleMapType _type = null; public new SimpleMapType Type { get { return _type; } }

		protected override void OnSceneManagementGetObjectsForShadowGeneration( Camera camera,
			RenderLight light, Set<SceneNode> outSceneNodes,
			Set<StaticMeshObject> outStaticMeshObjects )
		{
			float farClipDistance = NearFarClipDistance.Maximum;

			LowLevelSceneManagement.WalkForShadowGeneration( camera, light, farClipDistance,
				outSceneNodes, outStaticMeshObjects );
		}

		protected override bool OnSceneManagementIsAllowPortalSystem( Camera camera )
		{
			return true;
		}

		protected override void OnSceneManagementGetObjectsForCamera( Camera camera,
			Set<SceneNode> outSceneNodes, Set<StaticMeshObject> outStaticMeshObjects )
		{
			float farClipDistance = NearFarClipDistance.Maximum;

			LowLevelSceneManagement.WalkForCamera( camera, farClipDistance,
				outSceneNodes, outStaticMeshObjects );
		}
	}

}
