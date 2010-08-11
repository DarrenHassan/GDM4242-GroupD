// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MapSystem;
using Engine.EntitySystem;
using Engine.MathEx;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a system game menu.
	/// </summary>
	public class MenuWindow : EControl
	{
		protected override void OnAttach()
		{
			base.OnAttach();

			EControl window = ControlDeclarationManager.Instance.CreateControl( "Gui\\MenuWindow.gui" );
			Controls.Add( window );

			( (EButton)window.Controls[ "Maps" ] ).Click += mapsButton_Click;
			( (EButton)window.Controls[ "LoadSave" ] ).Click += loadSaveButton_Click;
			( (EButton)window.Controls[ "Options" ] ).Click += optionsButton_Click;
			( (EButton)window.Controls[ "PostEffects" ] ).Click += postEffectsButton_Click;
			( (EButton)window.Controls[ "Debug" ] ).Click += debugButton_Click;
			( (EButton)window.Controls[ "About" ] ).Click += aboutButton_Click;
			( (EButton)window.Controls[ "ExitToMainMenu" ] ).Click += exitToMainMenuButton_Click;
			( (EButton)window.Controls[ "Exit" ] ).Click += exitButton_Click;
			( (EButton)window.Controls[ "Resume" ] ).Click += resumeButton_Click;

			if( GameWindow.Instance == null )
				window.Controls[ "ExitToMainMenu" ].Enable = false;

			if( GameNetworkServer.Instance != null || GameNetworkClient.Instance != null )
			{
				window.Controls[ "Maps" ].Enable = false;
				window.Controls[ "LoadSave" ].Enable = false;
			}

			MouseCover = true;

			BackColor = new ColorValue( 0, 0, 0, .5f );
		}

		void mapsButton_Click( object sender )
		{
			foreach( EControl control in Controls )
				control.Visible = false;
			Controls.Add( new MapsWindow() );
		}

		void loadSaveButton_Click( object sender )
		{
			foreach( EControl control in Controls )
				control.Visible = false;
			Controls.Add( new WorldLoadSaveWindow() );
		}

		void optionsButton_Click( object sender )
		{
			foreach( EControl control in Controls )
				control.Visible = false;
			Controls.Add( new OptionsWindow() );
		}

		void postEffectsButton_Click( object sender )
		{
			foreach( EControl control in Controls )
				control.Visible = false;
			Controls.Add( new PostEffectsWindow() );
		}

		void debugButton_Click( object sender )
		{
			foreach( EControl control in Controls )
				control.Visible = false;
			Controls.Add( new DebugDrawOptionsWindow() );
		}

		void aboutButton_Click( object sender )
		{
			foreach( EControl control in Controls )
				control.Visible = false;
			Controls.Add( new AboutWindow() );
		}

		protected override void OnControlDetach( EControl control )
		{
			base.OnControlDetach( control );

			if( ( control as OptionsWindow ) != null ||
				( control as PostEffectsWindow ) != null ||
				( control as DebugDrawOptionsWindow ) != null ||
				( control as MapsWindow ) != null ||
				( control as WorldLoadSaveWindow ) != null ||
				( control as AboutWindow ) != null )
			{
				foreach( EControl c in Controls )
					c.Visible = true;
			}
		}

		void exitToMainMenuButton_Click( object sender )
		{
			MapSystemWorld.MapDestroy();
			EntitySystemWorld.Instance.WorldDestroy();

			GameEngineApp.Instance.Server_DestroyServer( "The server has been destroyed" );
			GameEngineApp.Instance.Client_DisconnectFromServer();

			//close all windows
			foreach( EControl control in GameEngineApp.Instance.ControlManager.Controls )
				control.SetShouldDetach();
			//create main menu
			GameEngineApp.Instance.ControlManager.Controls.Add( new MainMenuWindow() );
		}

		void exitButton_Click( object sender )
		{
			EngineApp.Instance.SetNeedExit();
		}

		void resumeButton_Click( object sender )
		{
			SetShouldDetach();
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

	}
}
