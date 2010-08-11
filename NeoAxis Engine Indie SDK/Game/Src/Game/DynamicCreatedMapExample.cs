// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.MathEx;
using Engine.UISystem;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using GameCommon;
using GameEntities;

namespace Game
{
	static class DynamicCreatedMapExample
	{
		public static bool ServerOrSingle_MapCreate()
		{
			GameNetworkServer server = GameNetworkServer.Instance;

			EControl mapLoadingWindow = null;

			//show map loading window
			{
				mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\MapLoadingWindow.gui" );
				if( mapLoadingWindow != null )
				{
					mapLoadingWindow.Text = "[Dynamic map creating]";
					GameEngineApp.Instance.ControlManager.Controls.Add( mapLoadingWindow );
				}
				EngineApp.Instance.RenderScene();
			}

			//delete all GameWindow's
			GameEngineApp.Instance.DeleteAllGameWindows();

			MapSystemWorld.MapDestroy();

			//create world if need
			WorldType worldType = EntitySystemWorld.Instance.DefaultWorldType;

			if( World.Instance == null || World.Instance.Type != worldType )
			{
				WorldSimulationTypes worldSimulationType;
				EntitySystemWorld.NetworkingInterface networkingInterface = null;

				if( server != null )
				{
					worldSimulationType = WorldSimulationTypes.ServerAndClient;
					networkingInterface = server.EntitySystemService.NetworkingInterface;
				}
				else
					worldSimulationType = WorldSimulationTypes.Single;

				if( !EntitySystemWorld.Instance.WorldCreate( worldSimulationType, worldType,
					networkingInterface ) )
				{
					Log.Fatal( "EntitySystemWorld.Instance.WorldCreate failed." );
				}
			}

			//create map
			GameMapType gameMapType = EntityTypes.Instance.GetByName( "GameMap" ) as GameMapType;
			if( gameMapType == null )
			{
				Log.Fatal( "GameEngineApp: MapCreateForDynamicMapExample: \"GameMap\" type " +
					"is not defined." );
			}
			GameMap gameMap = (GameMap)Entities.Instance.Create( gameMapType, World.Instance );
			gameMap.ShadowFarDistance = 60;

			//create MapObjects
			ServerOrSingle_CreateEntities();

			//post create map
			gameMap.PostCreate();

			//inform clients about world created
			if( server != null )
				server.EntitySystemService.InformClientsAfterWorldCreated();

			//Error
			foreach( EControl control in GameEngineApp.Instance.ControlManager.Controls )
			{
				if( control is MessageBoxWindow && !control.IsShouldDetach() )
					return false;
			}

			GameEngineApp.Instance.CreateGameWindowForMap();

			//play music
			if( GameMap.Instance != null )
				GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );

			return true;
		}

		static void CreateEntitiesWhichNotSynchronizedViaNetwork()
		{
			//ground
			{
				StaticMesh staticMesh = (StaticMesh)Entities.Instance.Create(
					"StaticMesh", Map.Instance );
				staticMesh.SplitGeometry = true;
				staticMesh.SplitGeometryPieceSize = new Vec3( 10, 10, 10 );
				staticMesh.MeshName = "Models\\DefaultBox\\DefaultBox.mesh";
				staticMesh.ForceMaterial = "Ball";
				staticMesh.Position = new Vec3( 0, 0, -.5f );
				staticMesh.Scale = new Vec3( 50, 50, 1 );
				staticMesh.CastDynamicShadows = false;
				staticMesh.PostCreate();
			}

			//SkyBox
			{
				Entity skyBox = Entities.Instance.Create( "SkyBox", Map.Instance );
				skyBox.PostCreate();
			}

			//Light
			{
				Light light = (Light)Entities.Instance.Create( "Light", Map.Instance );
				light.LightType = RenderLightType.Directional;
				light.SpecularColor = new ColorValue( 1, 1, 1 );
				light.Position = new Vec3( 0, 0, 10 );
				light.Rotation = new Angles( 120, 50, 330 ).ToQuat();
				light.PostCreate();
			}
		}

		static void ServerOrSingle_CreateEntities()
		{
			CreateEntitiesWhichNotSynchronizedViaNetwork();

			//SpawnPoint (server or single only)
			{
				SpawnPoint spawnPoint = (SpawnPoint)Entities.Instance.Create(
					"SpawnPoint", Map.Instance );
				spawnPoint.Position = new Vec3( -20, 0, 1 );
				spawnPoint.PostCreate();
			}

			//Boxes (synchronized via network)
			{
				for( int n = 0; n < 10; n++ )
				{
					Vec3 position = new Vec3(
						World.Instance.Random.NextFloatCenter() * 10,
						World.Instance.Random.NextFloatCenter() * 10,
						40 + (float)n * 1.1f );

					MapObject mapObject = (MapObject)Entities.Instance.Create( "Box", Map.Instance );
					mapObject.Position = position;
					mapObject.Rotation = new Angles(
						World.Instance.Random.NextFloat() * 360,
						World.Instance.Random.NextFloat() * 360,
						World.Instance.Random.NextFloat() * 360 ).ToQuat();
					mapObject.PostCreate();
				}
			}
		}

		public static void Client_CreateEntities()
		{
			CreateEntitiesWhichNotSynchronizedViaNetwork();
		}

	}
}
