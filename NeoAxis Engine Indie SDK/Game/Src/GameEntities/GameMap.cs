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
	/// Defines the <see cref="GameMap"/> entity type.
	/// </summary>
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class GameMapType : MapType
	{
	}

	public class GameMap : Map
	{
		static GameMap instance;

		[FieldSerialize]
		[DefaultValue( GameMap.GameTypes.Action )]
		GameTypes gameType = GameTypes.Action;

		[FieldSerialize]
		string gameMusic = "Sounds\\Music\\Game.ogg";

		///////////////////////////////////////////

		public enum GameTypes
		{
			None,
			Action,
			RTS,
			TPSArcade,
			TurretDemo,
			JigsawPuzzleGame,

			//Put here your game type.
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			GameTypeToClient
		}

		///////////////////////////////////////////

		GameMapType _type = null; public new GameMapType Type { get { return _type; } }

		public GameMap()
		{
			instance = this;
		}

		public static new GameMap Instance
		{
			get { return instance; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate2(bool)"/>.</summary>
		protected override void OnPostCreate2( bool loaded )
		{
			base.OnPostCreate2( loaded );

			GameWorld gameWorld = Parent as GameWorld;
			if( gameWorld != null )
				gameWorld.DoActionsAfterMapCreated();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			instance = null;
		}

		[DefaultValue( GameMap.GameTypes.Action )]
		public GameTypes GameType
		{
			get { return gameType; }
			set
			{
				gameType = value;

				//send to clients
				if( EntitySystemWorld.Instance.IsServer() )
					Server_SendGameTypeToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
			}
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			//here you can to preload resources for your specific map.
			//
			//example:
			//EntityType entityType = EntityTypes.Instance.GetByName( "MyEntity" );
			//if( entityType != null )
			//entityType.PreloadResources();
		}

		[DefaultValue( "Sounds\\Music\\Game.ogg" )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string GameMusic
		{
			get { return gameMusic; }
			set { gameMusic = value; }
		}

		void Server_SendGameTypeToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( GameMap ),
				(ushort)NetworkMessages.GameTypeToClient );
			writer.WriteVariableUInt32( (uint)gameType );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.GameTypeToClient )]
		void Client_ReceiveGameType( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			GameTypes value = (GameTypes)reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;
			gameType = value;
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			//send gameType value to the connected world
			Server_SendGameTypeToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

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
