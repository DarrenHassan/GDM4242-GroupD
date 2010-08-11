// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.Networking;
using GameCommon;

namespace Game
{
	public class MultiplayerLoginWindow : EControl
	{
		[Config( "Multiplayer", "UserName" )]
		static string userName;

		[Config( "Multiplayer", "connectToAddress" )]
		static string connectToAddress = "127.0.0.1";

		EControl window;
		EEditBox editBoxUserName;
		EEditBox editBoxConnectTo;
		EButton buttonCreateServer;
		EButton buttonConnect;

		bool notDisposeClientOnDetach;

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			//disable check for disconnection
			GameEngineApp.Instance.Client_AllowCheckForDisconnection = false;

			//register config fields
			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			//create window
			window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\MultiplayerLoginWindow.gui" );
			Controls.Add( window );

			MouseCover = true;
			BackColor = new ColorValue( 0, 0, 0, .5f );

			//initialize controls

			buttonCreateServer = (EButton)window.Controls[ "CreateServer" ];
			buttonCreateServer.Click += CreateServer_Click;

			buttonConnect = (EButton)window.Controls[ "Connect" ];
			buttonConnect.Click += Connect_Click;

			( (EButton)window.Controls[ "Exit" ] ).Click += Exit_Click;

			//generate user name
			if( string.IsNullOrEmpty( userName ) )
			{
				EngineRandom random = new EngineRandom();
				userName = "Rabbit" + random.Next( 1000 ).ToString( "D03" );
			}

			editBoxUserName = (EEditBox)window.Controls[ "UserName" ];
			editBoxUserName.Text = userName;
			editBoxUserName.TextChange += editBoxUserName_TextChange;

			editBoxConnectTo = (EEditBox)window.Controls[ "ConnectTo" ];
			editBoxConnectTo.Text = connectToAddress;
			editBoxConnectTo.TextChange += editBoxConnectTo_TextChange;

			SetInfo( "", false );
		}

		protected override void OnDetach()
		{
			if( !notDisposeClientOnDetach )
				DisposeClient();

			//restore check for disconnection flag
			GameEngineApp.Instance.Client_AllowCheckForDisconnection = true;

			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.Escape )
			{
				SetShouldDetach();
				return true;
			}

			return false;
		}

		void editBoxUserName_TextChange( EControl sender )
		{
			userName = editBoxUserName.Text.Trim();
		}

		void editBoxConnectTo_TextChange( EControl sender )
		{
			connectToAddress = editBoxConnectTo.Text.Trim();
		}

		void CreateServer_Click( EButton sender )
		{
			if( string.IsNullOrEmpty( userName ) )
			{
				SetInfo( "Invalid user name.", true );
				return;
			}

			SetInfo( "Creating server...", false );

			GameNetworkServer server = new GameNetworkServer( "NeoAxis Server",
				EngineVersionInformation.Version, 128, true );

			int port = 56565;

			string error;
			if( !server.BeginListen( port, out error ) )
			{
				SetInfo( "Error: " + error, true );
				server.Dispose( "" );
				return;
			}

			//create user for server
			server.UserManagementService.CreateServerUser( userName );

			//close all windows
			foreach( EControl control in GameEngineApp.Instance.ControlManager.Controls )
				control.SetShouldDetach();
			//create lobby window
			MultiplayerLobbyWindow lobbyWindow = new MultiplayerLobbyWindow();
			GameEngineApp.Instance.ControlManager.Controls.Add( lobbyWindow );

			GameEngineApp.Instance.Server_OnCreateServer();
		}

		void Connect_Click( EButton sender )
		{
			if( string.IsNullOrEmpty( userName ) )
			{
				SetInfo( "Invalid user name.", true );
				return;
			}

			SetInfo( "Connecting to the server...", false );

			GameNetworkClient client = new GameNetworkClient( true );
			client.ConnectionStatusChanged += Client_ConnectionStatusChanged;

			int port = 56565;
			string password = "";

			string error;
			if( !client.BeginConnect( connectToAddress, port, EngineVersionInformation.Version,
				userName, password, out error ) )
			{
				Log.Error( error );
				DisposeClient();
				return;
			}

			editBoxUserName.Enable = false;
			editBoxConnectTo.Enable = false;
			buttonCreateServer.Enable = false;
			buttonConnect.Enable = false;
		}

		void RemoveEventsForClient()
		{
			if( GameNetworkClient.Instance != null )
				GameNetworkClient.Instance.ConnectionStatusChanged -= Client_ConnectionStatusChanged;
		}

		void DisposeClient()
		{
			RemoveEventsForClient();

			if( GameNetworkClient.Instance != null )
				GameNetworkClient.Instance.Dispose();

			editBoxUserName.Enable = true;
			editBoxConnectTo.Enable = true;
			buttonCreateServer.Enable = true;
			buttonConnect.Enable = true;
		}

		void Exit_Click( EButton sender )
		{
			SetShouldDetach();
		}

		void SetInfo( string text, bool error )
		{
			ETextBox textBoxInfo = (ETextBox)window.Controls[ "Info" ];

			textBoxInfo.Text = text;
			textBoxInfo.TextColor = error ? new ColorValue( 1, 0, 0 ) : new ColorValue( 1, 1, 1 );
		}

		void Client_ConnectionStatusChanged( NetworkClient sender, NetworkConnectionStatuses status )
		{
			switch( status )
			{
			case NetworkConnectionStatuses.Disconnected:
				{
					string text = "Unable to connect";
					if( sender.DisconnectionReason != "" )
						text += ". " + sender.DisconnectionReason;
					SetInfo( text, true );

					DisposeClient();
				}
				break;

			case NetworkConnectionStatuses.Connecting:
				SetInfo( "Connecting...", false );
				break;

			case NetworkConnectionStatuses.Connected:
				SetInfo( "Connected", false );

				//no work with client from this class anymore
				RemoveEventsForClient();
				notDisposeClientOnDetach = true;

				//close all windows
				foreach( EControl control in GameEngineApp.Instance.ControlManager.Controls )
					control.SetShouldDetach();
				//create lobby window
				MultiplayerLobbyWindow lobbyWindow = new MultiplayerLobbyWindow();
				GameEngineApp.Instance.ControlManager.Controls.Add( lobbyWindow );

				GameEngineApp.Instance.Client_OnConnectedToServer();

				break;
			}
		}
	}
}
