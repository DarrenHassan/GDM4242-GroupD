// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using GameCommon;
using WindowsAppFramework;
using Engine;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.Networking;
using GameEntities;

namespace DedicatedServer
{
	public partial class MainForm : Form
	{
		[Config( "DedicatedServer", "lastMapName" )]
		static string lastMapName = "Maps\\JigsawPuzzleGame\\Map.map";//Jigsaw puzzle by default

		[Config( "DedicatedServer", "loadMapAtStartup" )]
		static bool loadMapAtStartup;

		//

		public MainForm()
		{
			InitializeComponent();
		}

		private void buttonClose_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void buttonCreate_Click( object sender, EventArgs e )
		{
			Create();
		}

		private void buttonDestroy_Click( object sender, EventArgs e )
		{
			Destroy();
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			//NeoAxis initialization
			EngineApp.ConfigName = "user:Configs/DedicatedServer.config";
			EngineApp.ForceRenderSystemName = "RenderSystem_NULL.dll";
			EngineApp.DisableAllSound = true;
			if( !WindowsAppWorld.Init( this, "user:Logs/DedicatedServer.log" ) )
			{
				Close();
				return;
			}
			WindowsAppEngineApp.Instance.AutomaticTicks = false;

			Engine.Log.Handlers.InfoHandler += delegate( string text )
			{
				Log( "Log: " + text );
			};

			Engine.Log.Handlers.ErrorHandler += delegate( string text, ref bool handled )
			{
				handled = true;
				timer1.Stop();
				MessageBox.Show( text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				timer1.Start();
			};

			Engine.Log.Handlers.FatalHandler += delegate( string text, ref bool handled )
			{
				handled = true;
				timer1.Stop();
				MessageBox.Show( text, "Fatal", MessageBoxButtons.OK, MessageBoxIcon.Warning );
			};

			//register config fields of this class
			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			//generate map list
			{
				string[] mapList = VirtualDirectory.GetFiles( "", "*.map", SearchOption.AllDirectories );
				foreach( string mapName in mapList )
				{
					//check for network support
					if( VirtualFile.Exists( string.Format( "{0}\\NoNetworkSupport.txt",
						Path.GetDirectoryName( mapName ) ) ) )
					{
						continue;
					}

					comboBoxMaps.Items.Add( mapName );
					if( mapName == lastMapName )
						comboBoxMaps.SelectedIndex = comboBoxMaps.Items.Count - 1;
				}

				comboBoxMaps.SelectedIndexChanged += comboBoxMaps_SelectedIndexChanged;
			}

			checkBoxLoadMapAtStartup.Checked = loadMapAtStartup;

			if( loadMapAtStartup && comboBoxMaps.SelectedItem != null )
				Create();
		}

		private void MainForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			Destroy();

			//NeoAxis shutdown
			WindowsAppWorld.Shutdown();
		}

		void Create()
		{
			if( GameNetworkServer.Instance != null )
			{
				Log( "Error: Server already created" );
				return;
			}

			string mapName = comboBoxMaps.SelectedItem as string;
			if( string.IsNullOrEmpty( mapName ) )
			{
				Log( "Error: You should choose a start map" );
				return;
			}

			GameNetworkServer server = new GameNetworkServer( "NeoAxis Game Server",
				EngineVersionInformation.Version, 128, true );

			server.UserManagementService.AddUserEvent += UserManagementService_AddUserEvent;
			server.UserManagementService.RemoveUserEvent += UserManagementService_RemoveUserEvent;
			server.ChatService.ReceiveText += ChatService_ReceiveText;

			int port = 56565;

			string error;
			if( !server.BeginListen( port, out error ) )
			{
				Log( "Error: " + error );
				Destroy();
				return;
			}

			Log( "Server has been created" );
			Log( "Listening port {0}...", port );

			buttonCreate.Enabled = false;
			buttonDestroy.Enabled = true;
			comboBoxMaps.Enabled = false;

			//load a map
			Log( "Loading map \"{0}\"...", mapName );
			MapLoad( mapName );
		}

		bool MapLoad( string fileName )
		{
			MapDestroy();

			WorldType worldType = EntitySystemWorld.Instance.DefaultWorldType;

			GameNetworkServer server = GameNetworkServer.Instance;
			if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationTypes.DedicatedServer,
				worldType, server.EntitySystemService.NetworkingInterface ) )
			{
				Log( "Error: EntitySystemWorld.Instance.WorldCreate failed." );
				return false;
			}

			if( !MapSystemWorld.MapLoad( fileName ) )
			{
				MapDestroy();
				return false;
			}

			//run simulation
			EntitySystemWorld.Instance.Simulation = true;

			Log( "Map loaded" );

			return true;
		}

		void MapDestroy()
		{
			if( Map.Instance != null )
			{
				MapSystemWorld.MapDestroy();
				Log( "Map destroyed" );
			}
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.WorldDestroy();
		}

		void Destroy()
		{
			MapDestroy();

			if( GameNetworkServer.Instance != null )
			{
				GameNetworkServer.Instance.Dispose( "The server has been destroyed" );

				buttonCreate.Enabled = true;
				buttonDestroy.Enabled = false;
				comboBoxMaps.Enabled = true;
				listBoxUsers.Items.Clear();

				Log( "Server destroyed" );
			}
		}

		void Log( string text, params object[] args )
		{
			while( listBoxLog.Items.Count > 300 )
				listBoxLog.Items.RemoveAt( 0 );
			int index = listBoxLog.Items.Add( string.Format( text, args ) );
			listBoxLog.SelectedIndex = index;
		}

		private void timer1_Tick( object sender, EventArgs e )
		{
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
				server.Update();

			if( WindowsAppEngineApp.Instance != null )
				WindowsAppEngineApp.Instance.DoTick();
		}

		void UserManagementService_AddUserEvent( UserManagementServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo user )
		{
			Log( "User connected: " + user.ToString() );
			listBoxUsers.Items.Add( user );
		}

		void UserManagementService_RemoveUserEvent( UserManagementServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo user )
		{
			listBoxUsers.Items.Remove( user );
			Log( "User disconnected: " + user.ToString() );
		}

		void ChatService_ReceiveText( ChatServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo fromUser, string text,
			UserManagementServerNetworkService.UserInfo privateToUser )
		{
			string userName = fromUser != null ? fromUser.Name : "(null)";
			string toUserName = privateToUser != null ? privateToUser.Name : "All";
			Log( "Chat: {0} -> {1}: {2}", userName, toUserName, text );
		}

		void comboBoxMaps_SelectedIndexChanged( object sender, EventArgs e )
		{
			lastMapName = comboBoxMaps.SelectedItem as string;
		}

		private void buttonDoSomething_Click( object sender, EventArgs e )
		{
			MessageBox.Show( "You can write code for testing here.", "Warning" );

			//example
			//MapObject mapObject = (MapObject)Entities.Instance.Create( "Box", Map.Instance );
			//mapObject.Position = new Vec3( 0, 0, 30 );
			//mapObject.PostCreate();
		}

		private void checkBoxLoadMapAtStartup_CheckedChanged( object sender, EventArgs e )
		{
			loadMapAtStartup = checkBoxLoadMapAtStartup.Checked;
		}

	}
}
