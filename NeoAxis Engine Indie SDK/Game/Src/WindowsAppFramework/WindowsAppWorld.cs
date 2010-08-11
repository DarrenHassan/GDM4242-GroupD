// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Engine;
using Engine.FileSystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;

namespace WindowsAppFramework
{
	public static class WindowsAppWorld
	{
		internal static List<RenderTargetUserControl> renderTargetUserControls =
			new List<RenderTargetUserControl>();

		static Control windowHandleControl;

		static bool duringWarningOrErrorMessageBox;

		//

		public static bool Init( Form mainApplicationForm, string logFileName )
		{
			if( !mainApplicationForm.IsHandleCreated )
				Log.Fatal( "WindowsAppWorld: Init: mainApplicationForm: Handle is not created." );

			if( !VirtualFileSystem.Init( logFileName, true, null, null, null ) )
				return false;
			Log.DumpToFile( string.Format( "Windows Application (NeoAxis Engine {0})\r\n",
				EngineVersionInformation.Version ) );

			Log.Handlers.WarningHandler += delegate( string text, ref bool handled )
			{
				handled = true;
				duringWarningOrErrorMessageBox = true;
				MessageBox.Show( text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				duringWarningOrErrorMessageBox = false;
			};

			Log.Handlers.ErrorHandler += delegate( string text, ref bool handled )
			{
				handled = true;
				duringWarningOrErrorMessageBox = true;
				MessageBox.Show( text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				duringWarningOrErrorMessageBox = false;
			};

			EngineApp.Init( new WindowsAppEngineApp() );

			windowHandleControl = new Control();
			windowHandleControl.Parent = mainApplicationForm;
			windowHandleControl.Location = new System.Drawing.Point( 0, 0 );
			windowHandleControl.Size = new System.Drawing.Size( 10, 10 );
			windowHandleControl.Visible = false;
			windowHandleControl.CreateControl();

			EngineApp.Instance.WindowHandle = windowHandleControl.Handle;
			if( !EngineApp.Instance.Create() )
				return false;

			return true;
		}

		public static void Shutdown()
		{
			while( renderTargetUserControls.Count != 0 )
				renderTargetUserControls[ 0 ].Destroy();

			EngineApp.Instance.Destroy();
			EngineApp.Shutdown();
			Log.DumpToFile( "Program END\r\n" );
			VirtualFileSystem.Shutdown();

			//bug fix for ".NET-BroadcastEventWindow" error
			Application.Exit();
		}

		public static bool MapLoad( string virtualFileName, bool runSimulation )
		{
			//Destroy old
			MapDestroy();

			//New
			if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationTypes.Single,
				EntitySystemWorld.Instance.DefaultWorldType ) )
			{
				Log.Error( "EntitySystemWorld: WorldCreate failed." );
				return false;
			}

			if( !MapSystemWorld.MapLoad( virtualFileName ) )
				return false;

			//run simulation
			EntitySystemWorld.Instance.Simulation = runSimulation;

			return true;
		}

		public static void MapDestroy()
		{
			MapSystemWorld.MapDestroy();
		}

		public static bool DuringWarningOrErrorMessageBox
		{
			get { return duringWarningOrErrorMessageBox; }
		}

	}
}
