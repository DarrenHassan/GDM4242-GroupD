// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Renderer;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a about us window.
	/// </summary>
	public class AboutWindow : EControl
	{
		protected override void OnAttach()
		{
			base.OnAttach();

			EControl window = ControlDeclarationManager.Instance.CreateControl( 
				"Gui\\AboutWindow.gui" );
			Controls.Add( window );

			window.Controls[ "Version" ].Text = EngineVersionInformation.Version;
			window.Controls[ "Copyright" ].Text = EngineVersionInformation.Copyright;
			window.Controls[ "WWW" ].Text = EngineVersionInformation.WWW;

			( (EButton)window.Controls[ "Quit" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};
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
