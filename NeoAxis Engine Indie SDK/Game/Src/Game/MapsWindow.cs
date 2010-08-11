// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine;
using Engine.UISystem;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.MapSystem;

namespace Game
{
	/// <summary>
	/// Defines a window of map choice.
	/// </summary>
	public class MapsWindow : EControl
	{
		const string dynamicMapExampleText = "[Dynamic created map example]";

		EListBox listBox;
		EControl window;
		EComboBox comboBoxAutorunMap;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\MapsWindow.gui" );
			Controls.Add( window );

			string[] mapList = VirtualDirectory.GetFiles( "", "*.map", SearchOption.AllDirectories );

			//maps listBox
			{
				listBox = (EListBox)window.Controls[ "List" ];

				//dynamic map example
				listBox.Items.Add( dynamicMapExampleText );

				foreach( string name in mapList )
				{
					listBox.Items.Add( name );
					if( Map.Instance != null )
					{
						if( string.Compare( name.Replace( '/', '\\' ),
							Map.Instance.VirtualFileName.Replace( '/', '\\' ), true ) == 0 )
							listBox.SelectedIndex = listBox.Items.Count - 1;
					}
				}

				listBox.SelectedIndexChange += listBox_SelectedIndexChanged;
				if( listBox.Items.Count != 0 && listBox.SelectedIndex == -1 )
					listBox.SelectedIndex = 0;
				if( listBox.Items.Count != 0 )
					listBox_SelectedIndexChanged( null );

				listBox.ItemMouseDoubleClick += delegate( object sender, EListBox.ItemMouseEventArgs e )
				{
					RunMap( (string)e.Item );
				};
			}

			//autorunMap
			comboBoxAutorunMap = (EComboBox)window.Controls[ "autorunMap" ];
			if( comboBoxAutorunMap != null )
			{
				comboBoxAutorunMap.Items.Add( "(None)" );
				comboBoxAutorunMap.SelectedIndex = 0;
				foreach( string name in mapList )
				{
					comboBoxAutorunMap.Items.Add( name );
					if( string.Compare( GameEngineApp.autorunMapName, name, true ) == 0 )
						comboBoxAutorunMap.SelectedIndex = comboBoxAutorunMap.Items.Count - 1;
				}

				comboBoxAutorunMap.SelectedIndexChange += delegate( EComboBox sender )
				{
					if( sender.SelectedIndex != 0 )
						GameEngineApp.autorunMapName = (string)sender.SelectedItem;
					else
						GameEngineApp.autorunMapName = "";
				};
			}

			//Run button event handler
			( (EButton)window.Controls[ "Run" ] ).Click += delegate( EButton sender )
			{
				if( listBox.SelectedIndex != -1 )
					RunMap( (string)listBox.SelectedItem );
			};

			//Quit button event handler
			( (EButton)window.Controls[ "Quit" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};
		}

		void listBox_SelectedIndexChanged( object sender )
		{
			Texture texture = null;

			if( listBox.SelectedIndex != -1 )
			{
				string mapName = (string)listBox.SelectedItem;
				if( mapName != dynamicMapExampleText )
				{
					string mapDirectory = Path.GetDirectoryName( mapName );
					string textureName = mapDirectory + "\\Description\\Preview";

					string textureFileName = null;

					bool found = false;

					string[] extensions = new string[] { "dds", "tga", "png", "jpg" };
					foreach( string extension in extensions )
					{
						textureFileName = textureName + "." + extension;
						if( VirtualFile.Exists( textureFileName ) )
						{
							found = true;
							break;
						}
					}

					if( found )
						texture = TextureManager.Instance.Load( textureFileName );
				}
			}

			window.Controls[ "Preview" ].Controls[ "TexturePlacer" ].BackTexture = texture;
		}

		void RunMap( string name )
		{
			if( name == dynamicMapExampleText )
				GameEngineApp.Instance.SetNeedMapCreateForDynamicMapExample();
			else
				GameEngineApp.Instance.SetNeedMapLoad( name );
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
