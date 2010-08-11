// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Engine;
using Engine.UISystem;

namespace Game
{
	/// <summary>
	/// Defines a "Debug Draw Options" window.
	/// </summary>
	public class DebugDrawOptionsWindow : EControl
	{
		EControl window;

		protected override void OnAttach()
		{
			base.OnAttach();

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\DebugDrawOptionsWindow.gui" );
			Controls.Add( window );

			Type type = typeof( EngineDebugSettings );
			InitCheckBox( "StaticPhysics", type.GetProperty( "DrawStaticPhysics" ) );
			InitCheckBox( "DynamicPhysics", type.GetProperty( "DrawDynamicPhysics" ) );
			InitCheckBox( "SceneGraphInfo", type.GetProperty( "DrawSceneGraphInfo" ) );
			InitCheckBox( "Regions", type.GetProperty( "DrawRegions" ) );
			InitCheckBox( "MapObjectBounds", type.GetProperty( "DrawMapObjectBounds" ) );
			InitCheckBox( "SceneNodeBounds", type.GetProperty( "DrawSceneNodeBounds" ) );
			InitCheckBox( "StaticMeshObjectBounds", type.GetProperty( "DrawStaticMeshObjectBounds" ) );
			InitCheckBox( "ZonesPortalsOccluders", type.GetProperty( "DrawZonesPortalsOccluders" ) );
			InitCheckBox( "FrustumTest", type.GetProperty( "FrustumTest" ) );
			InitCheckBox( "Lights", type.GetProperty( "DrawLights" ) );
			InitCheckBox( "StaticGeometry", type.GetProperty( "DrawStaticGeometry" ) );
			InitCheckBox( "Models", type.GetProperty( "DrawModels" ) );
			InitCheckBox( "Effects", type.GetProperty( "DrawEffects" ) );
			InitCheckBox( "Gui", type.GetProperty( "DrawGui" ) );
			InitCheckBox( "Wireframe", type.GetProperty( "DrawWireframe" ) );
			InitCheckBox( "PostEffects", type.GetProperty( "DrawPostEffects" ) );
			InitCheckBox( "GameSpecificDebugGeometry", type.GetProperty( "DrawGameSpecificDebugGeometry" ) );

			( (EButton)window.Controls[ "Defaults" ] ).Click += Defaults_Click;

			( (EButton)window.Controls[ "Close" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};
		}

		void Defaults_Click( EButton sender )
		{
			foreach( EControl control in window.Controls )
			{
				ECheckBox checkBox = control as ECheckBox;
				if( checkBox == null )
					continue;

				PropertyInfo property = checkBox.UserData as PropertyInfo;
				if( property == null )
					continue;

				DefaultValueAttribute[] attributes = (DefaultValueAttribute[])property.
					GetCustomAttributes( typeof( DefaultValueAttribute ), true );

				if( attributes.Length == 0 )
					continue;

				checkBox.Checked = (bool)attributes[ 0 ].Value;
			}
		}

		void InitCheckBox( string name, PropertyInfo property )
		{
			ECheckBox checkBox = (ECheckBox)window.Controls[ name ];
			checkBox.UserData = property;

			checkBox.Checked = (bool)property.GetValue( null, null );

			checkBox.CheckedChange += delegate( ECheckBox sender )
			{
				PropertyInfo p = (PropertyInfo)sender.UserData;
				p.SetValue( null, !(bool)p.GetValue( null, null ), null );
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
