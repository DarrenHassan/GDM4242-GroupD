// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Networking;
using Engine.EntitySystem;
using Engine.Utils;
using Engine.MapSystem;

namespace GameCommon
{
	////////////////////////////////////////////////////////////////////////////////////////////////

	public class EntitySystemServerNetworkService : ServerNetworkService
	{
		MessageType[] entitySystemInternalMessageTypes = new MessageType[ 6 ];

		UserManagementServerNetworkService userManagementService;
		ServerEntitySystemNetworkingInterface networkingInterface;
		List<ClientRemoteEntityWorld> clientRemoteEntityWorlds = new List<ClientRemoteEntityWorld>();

		///////////////////////////////////////////

		public class ClientRemoteEntityWorld : RemoteEntityWorld
		{
			UserManagementServerNetworkService.UserInfo user;

			//

			internal ClientRemoteEntityWorld( UserManagementServerNetworkService.UserInfo user )
				: base( "Client remote entity world (User: \"" + user.ToString() + "\")" )
			{
				this.user = user;
			}

			public UserManagementServerNetworkService.UserInfo User
			{
				get { return user; }
			}
		}

		///////////////////////////////////////////

		class ServerEntitySystemNetworkingInterface : EntitySystemWorld.NetworkingInterface
		{
			EntitySystemServerNetworkService service;

			List<NetworkNode.ConnectedNode> tempConnectedNodesList =
				new List<NetworkNode.ConnectedNode>();//for OnBeginEntitySystemMessage()

			//

			public ServerEntitySystemNetworkingInterface( EntitySystemServerNetworkService service )
			{
				this.service = service;
			}

			protected override SendDataWriter OnBeginEntitySystemMessage(
				IList<RemoteEntityWorld> toRemoteEntityWorlds, int messageTypeIdentifier )
			{
				for( int n = 0; n < toRemoteEntityWorlds.Count; n++ )
				{
					ClientRemoteEntityWorld remoteEntityWorld =
						(ClientRemoteEntityWorld)toRemoteEntityWorlds[ n ];
					tempConnectedNodesList.Add( remoteEntityWorld.User.ConnectedNode );
				}

				SendDataWriter writer = service.BeginMessage( tempConnectedNodesList,
					service.entitySystemInternalMessageTypes[ messageTypeIdentifier ] );

				tempConnectedNodesList.Clear();

				return writer;
			}

			protected override void OnEndEntitySystemMessage()
			{
				service.EndMessage();
			}
		}

		///////////////////////////////////////////

		public EntitySystemServerNetworkService(
			UserManagementServerNetworkService userManagementService )
			: base( "EntitySystem", 4 )
		{
			this.userManagementService = userManagementService;

			//register message types
			for( int n = 0; n < 6; n++ )
			{
				entitySystemInternalMessageTypes[ n ] = RegisterMessageType(
					string.Format( "entitySystemInternal{0}", n ), (byte)( n + 1 ),
					ReceiveMessage_EntitySystemInternal );
			}
			RegisterMessageType( "worldCreateBeginToClient", 7 );
			RegisterMessageType( "worldCreateEndToClient", 8 );

			networkingInterface = new ServerEntitySystemNetworkingInterface( this );

			userManagementService.AddUserEvent += UserManagementService_AddUserEvent;
			userManagementService.RemoveUserEvent += UserManagementService_RemoveUserEvent;
		}

		protected override void Dispose()
		{
			if( EntitySystemWorld.Instance != null && networkingInterface != null )
			{
				while( clientRemoteEntityWorlds.Count != 0 )
				{
					ClientRemoteEntityWorld remoteEntityWorld = clientRemoteEntityWorlds[ 0 ];

					networkingInterface.DisconnectRemoteEntityWorld( remoteEntityWorld );
					clientRemoteEntityWorlds.Remove( remoteEntityWorld );
				}
			}

			if( userManagementService != null )
			{
				userManagementService.AddUserEvent -= UserManagementService_AddUserEvent;
				userManagementService.RemoveUserEvent -= UserManagementService_RemoveUserEvent;
			}

			networkingInterface = null;

			base.Dispose();
		}

		public EntitySystemWorld.NetworkingInterface NetworkingInterface
		{
			get { return networkingInterface; }
		}

		public ClientRemoteEntityWorld GetRemoteEntityWorld(
			UserManagementServerNetworkService.UserInfo user )
		{
			for( int n = 0; n < clientRemoteEntityWorlds.Count; n++ )
			{
				ClientRemoteEntityWorld remoteEntityWorld = clientRemoteEntityWorlds[ n ];
				if( remoteEntityWorld.User == user )
					return remoteEntityWorld;
			}
			return null;
		}

		ClientRemoteEntityWorld GetRemoteEntityWorld( NetworkNode.ConnectedNode connectedNode )
		{
			for( int n = 0; n < clientRemoteEntityWorlds.Count; n++ )
			{
				ClientRemoteEntityWorld remoteEntityWorld = clientRemoteEntityWorlds[ n ];
				if( remoteEntityWorld.User.ConnectedNode == connectedNode )
					return remoteEntityWorld;
			}
			return null;
		}

		void UserManagementService_AddUserEvent( UserManagementServerNetworkService service,
			UserManagementServerNetworkService.UserInfo user )
		{
			if( World.Instance != null )
				CreateClientRemoteEntityWorldAndSynchronizeWorld( user );
		}

		void UserManagementService_RemoveUserEvent( UserManagementServerNetworkService service,
			UserManagementServerNetworkService.UserInfo user )
		{
			ClientRemoteEntityWorld remoteEntityWorld = GetRemoteEntityWorld( user );
			if( remoteEntityWorld != null )
			{
				networkingInterface.DisconnectRemoteEntityWorld( remoteEntityWorld );
				clientRemoteEntityWorlds.Remove( remoteEntityWorld );
			}
		}

		bool ReceiveMessage_EntitySystemInternal( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			RemoteEntityWorld fromRemoteEntityWorld = GetRemoteEntityWorld( sender );
			if( fromRemoteEntityWorld == null )
			{
				//no such world already. as example World has been deleted.
				return true;
			}

			int entitySystemMessageIdentifier = messageType.Identifier - 1;

			return networkingInterface.ReceiveEntitySystemMessage( fromRemoteEntityWorld,
				entitySystemMessageIdentifier, reader, ref additionalErrorMessage );
		}

		void CreateClientRemoteEntityWorldAndSynchronizeWorld(
			UserManagementServerNetworkService.UserInfo user )
		{
			if( user.ConnectedNode != null )//check for local user
			{
				{
					MessageType messageType = GetMessageType( "worldCreateBeginToClient" );
					SendDataWriter writer = BeginMessage( user.ConnectedNode, messageType );

					writer.Write( World.Instance.Type.Name );

					if( Map.Instance != null )
						writer.Write( Map.Instance.VirtualFileName );
					else
						writer.Write( "" );

					EndMessage();
				}

				if( GetRemoteEntityWorld( user ) == null )//check for arealdy created
				{
					//create entity remote world
					ClientRemoteEntityWorld remoteEntityWorld = new ClientRemoteEntityWorld( user );
					clientRemoteEntityWorlds.Add( remoteEntityWorld );
					networkingInterface.ConnectRemoteEntityWorld( remoteEntityWorld );
				}

				{
					MessageType messageType = GetMessageType( "worldCreateEndToClient" );
					SendDataWriter writer = BeginMessage( user.ConnectedNode, messageType );
					EndMessage();
				}
			}
		}

		public void InformClientsAfterWorldCreated()
		{
			foreach( UserManagementServerNetworkService.UserInfo user in userManagementService.Users )
				CreateClientRemoteEntityWorldAndSynchronizeWorld( user );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class EntitySystemClientNetworkService : ClientNetworkService
	{
		MessageType[] entitySystemInternalMessageTypes = new MessageType[ 6 ];

		UserManagementClientNetworkService userManagementService;
		ClientEntitySystemNetworkingInterface networkingInterface;
		RemoteEntityWorld serverRemoteEntityWorld;

		///////////////////////////////////////////

		public delegate void WorldCreateBeginDelegate( EntitySystemClientNetworkService sender,
			WorldType worldType, string mapVirtualFileName );
		public event WorldCreateBeginDelegate WorldCreateBegin;
		public delegate void WorldCreateEndDelegate( EntitySystemClientNetworkService sender );
		public event WorldCreateEndDelegate WorldCreateEnd;

		///////////////////////////////////////////

		class ClientEntitySystemNetworkingInterface : EntitySystemWorld.NetworkingInterface
		{
			EntitySystemClientNetworkService service;

			//

			public ClientEntitySystemNetworkingInterface( EntitySystemClientNetworkService service )
			{
				this.service = service;
			}

			protected override SendDataWriter OnBeginEntitySystemMessage(
				IList<RemoteEntityWorld> toRemoteEntityWorlds, int messageTypeIdentifier )
			{
				if( toRemoteEntityWorlds.Count != 1 )
				{
					Log.Fatal( "EntitySystemClientNetworkService: " +
						"ClientEntitySystemNetworkingInterface: OnBeginEntitySystemMessage: " +
						"toRemoteEntityWorlds.Count != 1" );
				}
				if( toRemoteEntityWorlds[ 0 ] != service.serverRemoteEntityWorld )
				{
					Log.Fatal( "EntitySystemClientNetworkService: " +
						"ClientEntitySystemNetworkingInterface: OnBeginEntitySystemMessage: " +
						"toRemoteEntityWorlds[ 0 ] != service.serverRemoteEntityWorld" );
				}

				return service.BeginMessage(
					service.entitySystemInternalMessageTypes[ messageTypeIdentifier ] );
			}

			protected override void OnEndEntitySystemMessage()
			{
				service.EndMessage();
			}
		}

		///////////////////////////////////////////

		public EntitySystemClientNetworkService( UserManagementClientNetworkService userManagementService )
			: base( "EntitySystem", 4 )
		{
			this.userManagementService = userManagementService;

			//register message types
			for( int n = 0; n < 6; n++ )
			{
				entitySystemInternalMessageTypes[ n ] = RegisterMessageType(
					string.Format( "entitySystemInternal{0}", n ), (byte)( n + 1 ),
					ReceiveMessage_EntitySystemInternal );
			}
			RegisterMessageType( "worldCreateBeginToClient", 7,
				ReceiveMessage_WorldCreateBeginToClient );
			RegisterMessageType( "worldCreateEndToClient", 8, ReceiveMessage_WorldCreateEndToClient );

			networkingInterface = new ClientEntitySystemNetworkingInterface( this );
		}

		protected override void Dispose()
		{
			if( EntitySystemWorld.Instance != null && networkingInterface != null &&
				serverRemoteEntityWorld != null )
			{
				networkingInterface.DisconnectRemoteEntityWorld( serverRemoteEntityWorld );
			}
			serverRemoteEntityWorld = null;
			networkingInterface = null;

			base.Dispose();
		}

		public EntitySystemWorld.NetworkingInterface NetworkingInterface
		{
			get { return networkingInterface; }
		}

		bool ReceiveMessage_WorldCreateBeginToClient( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			string worldTypeName = reader.ReadString();
			string mapVirtualFileName = reader.ReadString();
			if( !reader.Complete() )
				return false;

			bool remoteWorldAlreadyExists = EntitySystemWorld.Instance.RemoteEntityWorlds.Contains(
				serverRemoteEntityWorld );

			if( !remoteWorldAlreadyExists )
			{
				serverRemoteEntityWorld = new RemoteEntityWorld( "Server remote entity world" );
				networkingInterface.ConnectRemoteEntityWorld( serverRemoteEntityWorld );
			}

			WorldType worldType = EntityTypes.Instance.GetByName( worldTypeName ) as WorldType;
			if( worldType == null )
			{
				Log.Fatal( "EntitySystemClientNetworkService: " +
					"ReceiveMessage_WorldCreateBeginToClient: World type \"{0}\" is not exists.",
					worldTypeName );
			}

			if( WorldCreateBegin != null )
				WorldCreateBegin( this, worldType, mapVirtualFileName );

			return true;
		}

		bool ReceiveMessage_WorldCreateEndToClient( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			if( !reader.Complete() )
				return false;

			if( WorldCreateEnd != null )
				WorldCreateEnd( this );

			return true;
		}

		bool ReceiveMessage_EntitySystemInternal( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			if( serverRemoteEntityWorld == null )
			{
				Log.Fatal( "EntitySystemClientNetworkService: ReceiveMessage_EntitySystemInternal: " +
					"serverRemoteEntityWorld == null." );
			}

			int entitySystemMessageIdentifier = messageType.Identifier - 1;

			return networkingInterface.ReceiveEntitySystemMessage( serverRemoteEntityWorld,
				entitySystemMessageIdentifier, reader, ref additionalErrorMessage );
		}
	}
}
