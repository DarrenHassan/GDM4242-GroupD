// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;

namespace Configurator
{
	public partial class MainForm : Form
	{
		//

		class RenderTechniqueItem
		{
			string name;
			string description;

			public RenderTechniqueItem( string name, string description )
			{
				this.name = name;
				this.description = description;
			}

			public string Name
			{
				get { return name; }
			}

			public string Description
			{
				get { return description; }
			}

			public override string ToString()
			{
				string text = description;
				if( name != "" )
					text += " (" + name + ")";
				return text;
			}
		}

		//

		public MainForm()
		{
			InitializeComponent();
		}

		string GetDefaultRenderSystemName()
		{
			string error;
			TextBlock block = TextBlockUtils.LoadFromVirtualFile( "Definitions/Renderer.config",
				out error );
			if( block != null )
				return block.GetAttribute( "defaultRenderSystemName" );
			return "";
		}

		string GetDefaultPhysicsSystemName()
		{
			string error;
			TextBlock block = TextBlockUtils.LoadFromVirtualFile( "Definitions/PhysicsSystem.config",
				out error );
			if( block != null )
				return block.GetAttribute( "defaultPhysicsSystemName" );
			return "";
		}

		string GetDefaultSoundSystemName()
		{
			string error;
			TextBlock block = TextBlockUtils.LoadFromVirtualFile( "Definitions/SoundSystem.config",
				out error );
			if( block != null )
				return block.GetAttribute( "defaultSoundSystemName" );
			return "";
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			//Engine.config parameters
			string renderSystemName = "";
			RendererWorld.MaxPixelShadersVersions maxPixelShaders =
				RendererWorld.MaxPixelShadersVersions.RecommendedSetting;
			RendererWorld.MaxVertexShadersVersions maxVertexShaders =
				RendererWorld.MaxVertexShadersVersions.RecommendedSetting;
			int fullSceneAntialiasing = 0;
			RendererWorld.FilteringModes filtering = RendererWorld.FilteringModes.RecommendedSetting;
			string renderTechnique = "";
			bool fullScreen = true;
			string videoMode = "";
			bool verticalSync = false;
			bool allowChangeDisplayFrequency = true;
			string physicsSystemName = "";
			string soundSystemName = "";
			string language = "English";
			{
				string error;
				TextBlock block = TextBlockUtils.LoadFromRealFile(
					VirtualFileSystem.GetRealPathByVirtual( "user:Configs/Engine.config" ),
					out error );
				if( block != null )
				{
					//Renderer
					TextBlock rendererBlock = block.FindChild( "Renderer" );
					if( rendererBlock != null )
					{
						renderSystemName = rendererBlock.GetAttribute( "renderSystemName" );

						if( rendererBlock.IsAttributeExist( "maxPixelShaders" ) )
						{
							try
							{
								maxPixelShaders = (RendererWorld.MaxPixelShadersVersions)
									Enum.Parse( typeof( RendererWorld.MaxPixelShadersVersions ),
									rendererBlock.GetAttribute( "maxPixelShaders" ) );
							}
							catch { }
						}

						if( rendererBlock.IsAttributeExist( "maxVertexShaders" ) )
						{
							try
							{
								maxVertexShaders = (RendererWorld.MaxVertexShadersVersions)
									Enum.Parse( typeof( RendererWorld.MaxVertexShadersVersions ),
									rendererBlock.GetAttribute( "maxVertexShaders" ) );
							}
							catch { }
						}

						if( rendererBlock.IsAttributeExist( "fullSceneAntialiasing" ) )
							fullSceneAntialiasing = int.Parse(
								rendererBlock.GetAttribute( "fullSceneAntialiasing" ) );

						if( rendererBlock.IsAttributeExist( "filtering" ) )
						{
							try
							{
								filtering = (RendererWorld.FilteringModes)
									Enum.Parse( typeof( RendererWorld.FilteringModes ),
									rendererBlock.GetAttribute( "filtering" ) );
							}
							catch { }
						}

						if( rendererBlock.IsAttributeExist( "renderTechnique" ) )
							renderTechnique = rendererBlock.GetAttribute( "renderTechnique" );

						if( rendererBlock.IsAttributeExist( "fullScreen" ) )
							fullScreen = bool.Parse( rendererBlock.GetAttribute( "fullScreen" ) );

						if( rendererBlock.IsAttributeExist( "videoMode" ) )
							videoMode = rendererBlock.GetAttribute( "videoMode" );

						if( rendererBlock.IsAttributeExist( "verticalSync" ) )
							verticalSync = bool.Parse( rendererBlock.GetAttribute( "verticalSync" ) );

						if( rendererBlock.IsAttributeExist( "allowChangeDisplayFrequency" ) )
							allowChangeDisplayFrequency = bool.Parse(
								rendererBlock.GetAttribute( "allowChangeDisplayFrequency" ) );
					}

					//Physics system
					TextBlock physicsSystemBlock = block.FindChild( "PhysicsSystem" );
					if( physicsSystemBlock != null )
						physicsSystemName = physicsSystemBlock.GetAttribute( "physicsSystemName" );

					//Sound system
					TextBlock soundSystemBlock = block.FindChild( "SoundSystem" );
					if( soundSystemBlock != null )
						soundSystemName = soundSystemBlock.GetAttribute( "soundSystemName" );

					//Localization
					TextBlock localizationBlock = block.FindChild( "Localization" );
					if( localizationBlock != null )
					{
						if( localizationBlock.IsAttributeExist( "language" ) )
							language = localizationBlock.GetAttribute( "language" );
					}
				}
			}

			//fill render system page
			{
				if( renderSystemName == "" )
					renderSystemName = GetDefaultRenderSystemName();

				List<string> fileNames = new List<string>();
				{
					string[] files = Directory.GetFiles(
						NativeLibraryManager.GetNativeLibrariesDirectory(), "rendersystem_*.dll" );

					foreach( string fileName in files )
					{
						string name = Path.GetFileName( fileName );

						//ignore null rendering system
						if( name.ToLower().Contains( "rendersystem_null" ) )
							continue;

						//Direct3D9 render system upper all
						if( name.ToLower().Contains( "rendersystem_direct3d9" ) )
							fileNames.Insert( 0, name );
						else
							fileNames.Add( name );
					}
				}

				foreach( string fileName in fileNames )
				{
					int itemId = comboBoxRenderSystems.Items.Add( fileName );
					if( string.Compare( renderSystemName, fileName, true ) == 0 )
					{
						comboBoxRenderSystems.SelectedIndex = itemId;

						//maxPixelShaders
						{
							EnumTypeConverter enumConverter = new EnumTypeConverter(
								typeof( RendererWorld.MaxPixelShadersVersions ) );
							for( int n = 0; n < comboBoxMaxPixelShaders.Items.Count; n++ )
							{
								string text = comboBoxMaxPixelShaders.Items[ n ].ToString();
								RendererWorld.MaxPixelShadersVersions s =
									(RendererWorld.MaxPixelShadersVersions)
									enumConverter.ConvertFromString( text );

								if( maxPixelShaders == s )
									comboBoxMaxPixelShaders.SelectedIndex = n;
							}
						}

						//maxVertexShaders
						{
							EnumTypeConverter enumConverter = new EnumTypeConverter(
								typeof( RendererWorld.MaxVertexShadersVersions ) );
							for( int n = 0; n < comboBoxMaxVertexShaders.Items.Count; n++ )
							{
								string text = comboBoxMaxVertexShaders.Items[ n ].ToString();
								RendererWorld.MaxVertexShadersVersions s =
									(RendererWorld.MaxVertexShadersVersions)
									enumConverter.ConvertFromString( text );

								if( maxVertexShaders == s )
									comboBoxMaxVertexShaders.SelectedIndex = n;
							}
						}

					}
				}

				if( comboBoxRenderSystems.Items.Count != 0 && comboBoxRenderSystems.SelectedIndex == -1 )
					comboBoxRenderSystems.SelectedIndex = 0;

				//fullSceneAntialiasing
				comboBoxAntialiasing.SelectedIndex = 0;
				if( fullSceneAntialiasing != 0 )
				{
					for( int n = 0; n < comboBoxAntialiasing.Items.Count; n++ )
					{
						if( comboBoxAntialiasing.Items[ n ].ToString() == fullSceneAntialiasing.ToString() )
						{
							comboBoxAntialiasing.SelectedIndex = n;
							break;
						}
					}
				}

				//filtering
				{
					Type enumType = typeof( RendererWorld.FilteringModes );
					EnumTypeConverter enumConverter = new EnumTypeConverter( enumType );

					RendererWorld.FilteringModes[] values =
						(RendererWorld.FilteringModes[])Enum.GetValues( enumType );
					for( int n = 0; n < values.Length; n++ )
					{
						RendererWorld.FilteringModes value = values[ n ];
						int index = comboBoxFiltering.Items.Add( enumConverter.ConvertToString( value ) );
						if( filtering == value )
							comboBoxFiltering.SelectedIndex = index;
					}
				}

				//renderTechnique
				{
					comboBoxRenderTechnique.Items.Add(
						new RenderTechniqueItem( "", "Recommended setting" ) );
					comboBoxRenderTechnique.Items.Add(
						new RenderTechniqueItem( "Standard", "Low Dynamic Range" ) );
					comboBoxRenderTechnique.Items.Add(
						new RenderTechniqueItem( "HDR", "64-bit High Dynamic Range" ) );

					for( int n = 0; n < comboBoxRenderTechnique.Items.Count; n++ )
					{
						string name = ( (RenderTechniqueItem)comboBoxRenderTechnique.Items[ n ] ).Name;
						if( string.Compare( name, renderTechnique, true ) == 0 )
							comboBoxRenderTechnique.SelectedIndex = n;
					}
					if( comboBoxRenderTechnique.SelectedIndex == -1 )
						comboBoxRenderTechnique.SelectedIndex = 0;
				}

				//video mode
				{
					comboBoxVideoMode.Items.Add( "Current screen resolution" );
					comboBoxVideoMode.SelectedIndex = 0;

					foreach( Vec2i mode in DisplaySettings.VideoModes )
					{
						if( mode.X < 640 )
							continue;
						comboBoxVideoMode.Items.Add( string.Format( "{0}x{1}", mode.X, mode.Y ) );
						if( mode.ToString() == videoMode )
							comboBoxVideoMode.SelectedIndex = comboBoxVideoMode.Items.Count - 1;
					}

					if( !string.IsNullOrEmpty( videoMode ) && comboBoxVideoMode.SelectedIndex == 0 )
					{
						try
						{
							Vec2i mode = Vec2i.Parse( videoMode );
							comboBoxVideoMode.Items.Add( string.Format( "{0}x{1}", mode.X, mode.Y ) );
							comboBoxVideoMode.SelectedIndex = comboBoxVideoMode.Items.Count - 1;
						}
						catch { }
					}
				}

				//full screen
				checkBoxFullScreen.Checked = fullScreen;

				//vertical sync
				checkBoxVerticalSync.Checked = verticalSync;

				//allowChangeDisplayFrequency
				checkBoxAllowChangeDisplayFrequency.Checked = allowChangeDisplayFrequency;
			}

			//fill physics system page
			{
				if( physicsSystemName == "" )
					physicsSystemName = GetDefaultPhysicsSystemName();

				List<string> fileNames = new List<string>();
				{
					string[] files = Directory.GetFiles( ".\\", "*physicssystem.dll" );
					foreach( string fileName in files )
					{
						string name = fileName.Substring( 2 );
						if( name.ToLower() == "physicssystem.dll" )
							continue;

						//ODE physics system upper all
						if( name.ToLower().Contains( "odephysicssystem" ) )
							fileNames.Insert( 0, name );
						else
							fileNames.Add( name );
					}
				}

				foreach( string fileName in fileNames )
				{
					int itemId = comboBoxPhysicsSystems.Items.Add( fileName );
					if( string.Compare( physicsSystemName, fileName, true ) == 0 )
						comboBoxPhysicsSystems.SelectedIndex = itemId;
				}

				if( comboBoxPhysicsSystems.SelectedIndex == -1 )
					comboBoxPhysicsSystems.SelectedIndex = 0;
			}

			//fill sound system page
			{
				if( soundSystemName == "" )
					soundSystemName = GetDefaultSoundSystemName();

				List<string> fileNames = new List<string>();
				{
					fileNames.Add( "Null" );
					string[] files = Directory.GetFiles( ".\\", "*soundsystem.dll" );
					foreach( string fileName in files )
					{
						string name = fileName.Substring( 2 );
						if( name.ToLower() == "soundsystem.dll" )
							continue;

						//DirectSound upper all
						if( name.ToLower().Contains( "directxsoundsystem" ) )
							fileNames.Insert( 1, name );
						else
							fileNames.Add( name );
					}
				}

				if( VirtualFileSystem.Deployed )
				{
					if( !string.IsNullOrEmpty( soundSystemName ) && soundSystemName.ToLower() != "null" )
					{
						if( !File.Exists( soundSystemName ) )
						{
							foreach( string fileName in fileNames )
							{
								string name = Path.GetFileName( fileName );
								if( name.ToLower() == "null" || name.ToLower() == "soundsystem.dll" )
									continue;

								soundSystemName = name;
								break;
							}
						}
					}
				}

				foreach( string fileName in fileNames )
				{
					int itemId = comboBoxSoundSystems.Items.Add( fileName );
					if( string.Compare( soundSystemName, fileName, true ) == 0 )
						comboBoxSoundSystems.SelectedIndex = itemId;
				}

				if( comboBoxSoundSystems.SelectedIndex == -1 )
					comboBoxSoundSystems.SelectedIndex = 0;
			}

			//fill localization page
			{
				List<string> languages = new List<string>();
				{
					languages.Add( "English" );

					string[] files = VirtualDirectory.GetFiles( LanguageManager.LanguagesDirectory,
						"*.language", SearchOption.TopDirectoryOnly );
					foreach( string fileName in files )
					{
						string lang = Path.GetFileNameWithoutExtension( fileName );
						if( string.Compare( lang, "English", true ) != 0 )
							languages.Add( lang );
					}
				}

				foreach( string lang in languages )
				{
					int itemId = comboBoxLanguages.Items.Add( lang );
					if( string.Compare( language, lang, true ) == 0 )
						comboBoxLanguages.SelectedIndex = itemId;
				}

				if( comboBoxLanguages.SelectedIndex == -1 )
					comboBoxLanguages.SelectedIndex = 0;
			}
		}

		protected override void OnFormClosing( FormClosingEventArgs e )
		{
			if( DialogResult == DialogResult.OK )
			{
				//save Engine.config

				TextBlock block = new TextBlock();

				//Renderer
				{
					TextBlock rendererBlock = block.AddChild( "Renderer" );

					string renderSystemName = "";
					if( comboBoxRenderSystems.SelectedIndex != -1 )
						renderSystemName = (string)comboBoxRenderSystems.SelectedItem;

					if( renderSystemName != "" )
						rendererBlock.SetAttribute( "renderSystemName", renderSystemName );

					//maxPixelShaders
					{
						EnumTypeConverter enumConverter = new EnumTypeConverter(
							typeof( RendererWorld.MaxPixelShadersVersions ) );
						string text = comboBoxMaxPixelShaders.SelectedItem.ToString();
						RendererWorld.MaxPixelShadersVersions maxPixelShaders =
							(RendererWorld.MaxPixelShadersVersions)enumConverter.ConvertFromString( text );
						rendererBlock.SetAttribute( "maxPixelShaders", maxPixelShaders.ToString() );
					}

					//maxVertexShaders
					{
						EnumTypeConverter enumConverter = new EnumTypeConverter(
							typeof( RendererWorld.MaxVertexShadersVersions ) );
						string text = comboBoxMaxVertexShaders.SelectedItem.ToString();
						RendererWorld.MaxVertexShadersVersions maxVertexShaders =
							(RendererWorld.MaxVertexShadersVersions)enumConverter.ConvertFromString( text );
						rendererBlock.SetAttribute( "maxVertexShaders", maxVertexShaders.ToString() );
					}

					//fullSceneAntialiasing
					{
						int fullSceneAntialiasing = 0;
						if( comboBoxAntialiasing.SelectedIndex > 0 )
							fullSceneAntialiasing = int.Parse( (string)comboBoxAntialiasing.SelectedItem );
						rendererBlock.SetAttribute( "fullSceneAntialiasing",
							fullSceneAntialiasing.ToString() );
					}

					//filtering
					{
						EnumTypeConverter enumConverter = new EnumTypeConverter(
							typeof( RendererWorld.FilteringModes ) );
						string text = comboBoxFiltering.SelectedItem.ToString();
						RendererWorld.FilteringModes filtering =
							(RendererWorld.FilteringModes)enumConverter.ConvertFromString( text );
						rendererBlock.SetAttribute( "filtering", filtering.ToString() );
					}

					//renderTechnique
					if( comboBoxRenderTechnique.SelectedIndex != -1 )
					{
						string renderTechnique = "";
						if( comboBoxRenderTechnique.SelectedIndex != 0 )
						{
							renderTechnique =
								( (RenderTechniqueItem)comboBoxRenderTechnique.SelectedItem ).Name;
						}
						rendererBlock.SetAttribute( "renderTechnique", renderTechnique );
					}

					//videoMode
					if( comboBoxVideoMode.SelectedIndex > 0 )
					{
						string[] strings = ( (string)comboBoxVideoMode.SelectedItem ).
							Split( new char[] { 'x' } );
						Vec2i videoMode = new Vec2i( int.Parse( strings[ 0 ] ),
							int.Parse( strings[ 1 ] ) );
						rendererBlock.SetAttribute( "videoMode", videoMode.ToString() );
					}

					//fullScreen
					rendererBlock.SetAttribute( "fullScreen", checkBoxFullScreen.Checked.ToString() );

					//vertical sync
					rendererBlock.SetAttribute( "verticalSync",
						checkBoxVerticalSync.Checked.ToString() );

					//allowChangeDisplayFrequency
					rendererBlock.SetAttribute( "allowChangeDisplayFrequency",
						checkBoxAllowChangeDisplayFrequency.Checked.ToString() );
				}

				//Physics system
				{
					string physicsSystemName = "";
					if( comboBoxPhysicsSystems.SelectedIndex != -1 )
						physicsSystemName = (string)comboBoxPhysicsSystems.SelectedItem;

					//physics system name
					TextBlock physicsSystemBlock = block.AddChild( "PhysicsSystem" );
					if( physicsSystemName != "" )
						physicsSystemBlock.SetAttribute( "physicsSystemName", physicsSystemName );
				}

				//Sound system
				{
					string soundSystemName = "";
					if( comboBoxSoundSystems.SelectedIndex != -1 )
						soundSystemName = (string)comboBoxSoundSystems.SelectedItem;

					TextBlock soundSystemBlock = block.AddChild( "SoundSystem" );
					if( soundSystemName != "" )
						soundSystemBlock.SetAttribute( "soundSystemName", soundSystemName );
				}

				//Localization
				{
					string language = "English";
					if( comboBoxLanguages.SelectedIndex != -1 )
						language = (string)comboBoxLanguages.SelectedItem;

					TextBlock localizationBlock = block.AddChild( "Localization" );
					localizationBlock.SetAttribute( "language", language );
				}


				//save file
				{
					string fileName = VirtualFileSystem.GetRealPathByVirtual(
						"user:Configs/Engine.config" );

					try
					{
						string directoryName = Path.GetDirectoryName( fileName );
						if( directoryName != "" && !Directory.Exists( directoryName ) )
							Directory.CreateDirectory( directoryName );
						using( StreamWriter writer = new StreamWriter( fileName ) )
						{
							writer.Write( block.DumpToString() );
						}
					}
					catch
					{
						string text = string.Format( "Saving file failed \"{0}\".", fileName );
						MessageBox.Show( text, "Configurator", MessageBoxButtons.OK,
							MessageBoxIcon.Warning );
						e.Cancel = true;
						return;
					}
				}
			}

			base.OnFormClosing( e );
		}

		private void buttonOK_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void buttonCancel_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void comboBoxRenderSystems_SelectedIndexChanged( object sender, EventArgs e )
		{
			if( comboBoxRenderSystems.SelectedIndex == -1 )
				return;

			//Update max shaders

			string renderSystemName = (string)comboBoxRenderSystems.SelectedItem;
			bool isDirect3D = renderSystemName.ToLower().Contains( "rendersystem_direct3d9" );

			//comboBoxMaxPixelShaders
			{
				int lastSelectedIndex = comboBoxMaxPixelShaders.SelectedIndex;

				comboBoxMaxPixelShaders.Items.Clear();

				Type enumType = typeof( RendererWorld.MaxPixelShadersVersions );
				EnumTypeConverter enumConverter = new EnumTypeConverter( enumType );

				RendererWorld.MaxPixelShadersVersions[] values =
					(RendererWorld.MaxPixelShadersVersions[])Enum.GetValues( enumType );
				for( int n = 0; n < values.Length; n++ )
				{
					if( !isDirect3D && n == 2 )
						break;
					comboBoxMaxPixelShaders.Items.Add( enumConverter.ConvertToString( values[ n ] ) );
				}

				if( lastSelectedIndex >= 0 && lastSelectedIndex < comboBoxMaxPixelShaders.Items.Count )
					comboBoxMaxPixelShaders.SelectedIndex = lastSelectedIndex;
				else
					comboBoxMaxPixelShaders.SelectedIndex = 0;
			}

			//comboBoxMaxVertexShaders
			{
				int lastSelectedIndex = comboBoxMaxVertexShaders.SelectedIndex;

				comboBoxMaxVertexShaders.Items.Clear();

				Type enumType = typeof( RendererWorld.MaxVertexShadersVersions );
				EnumTypeConverter enumConverter = new EnumTypeConverter( enumType );

				RendererWorld.MaxVertexShadersVersions[] values =
					(RendererWorld.MaxVertexShadersVersions[])Enum.GetValues( enumType );
				for( int n = 0; n < values.Length; n++ )
				{
					if( !isDirect3D && n == 2 )
						break;
					comboBoxMaxVertexShaders.Items.Add( enumConverter.ConvertToString( values[ n ] ) );
				}

				if( lastSelectedIndex >= 0 && lastSelectedIndex < comboBoxMaxVertexShaders.Items.Count )
					comboBoxMaxVertexShaders.SelectedIndex = lastSelectedIndex;
				else
					comboBoxMaxVertexShaders.SelectedIndex = 0;
			}
		}

		private void comboBoxPhysicsSystems_SelectedIndexChanged( object sender, EventArgs e )
		{
			if( comboBoxPhysicsSystems.SelectedIndex == -1 )
				return;

			string physicsSystemName = (string)comboBoxPhysicsSystems.SelectedItem;
			bool isPhysX = physicsSystemName.ToLower().Contains( "physx" );

			labelPhysXHardwareAcceleration.Visible = isPhysX;
		}

	}
}
