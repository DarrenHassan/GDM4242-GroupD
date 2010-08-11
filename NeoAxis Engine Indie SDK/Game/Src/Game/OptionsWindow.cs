// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a window of options.
	/// </summary>
	public class OptionsWindow : EControl
	{
		EControl window;
		EComboBox comboBoxResolution;
		EComboBox comboBoxInputDevices;

		///////////////////////////////////////////

		public class ShadowTechniqueItem
		{
			ShadowTechniques technique;
			string text;

			public ShadowTechniqueItem( ShadowTechniques technique, string text )
			{
				this.technique = technique;
				this.text = text;
			}

			public ShadowTechniques Technique
			{
				get { return technique; }
			}

			public override string ToString()
			{
				return text;
			}
		}

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			EComboBox comboBox;
			EScrollBar scrollBar;
			ECheckBox checkBox;

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\OptionsWindow.gui" );
			Controls.Add( window );

			( (EButton)window.Controls[ "Options" ].Controls[ "Quit" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};

			//pageVideo
			{
				EControl pageVideo = window.Controls[ "TabControl" ].Controls[ "Video" ];

				Vec2i currentMode = EngineApp.Instance.VideoMode;

				//screenResolutionComboBox
				comboBox = (EComboBox)pageVideo.Controls[ "ScreenResolution" ];
				comboBox.Enable = !EngineApp.Instance.WebPlayerMode;
				comboBoxResolution = comboBox;

				foreach( Vec2i mode in DisplaySettings.VideoModes )
				{
					if( mode.X < 640 )
						continue;

					comboBox.Items.Add( mode.X.ToString() + "x" + mode.Y.ToString() );

					if( mode == currentMode )
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
				}

				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					ChangeVideoMode();
				};

				//gamma
				scrollBar = (EScrollBar)pageVideo.Controls[ "Gamma" ];
				scrollBar.Value = GameEngineApp._Gamma;
				scrollBar.Enable = !EngineApp.Instance.WebPlayerMode;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					float value = float.Parse( sender.Value.ToString( "F1" ) );
					GameEngineApp._Gamma = value;
					pageVideo.Controls[ "GammaValue" ].Text = value.ToString( "F1" );
				};
				pageVideo.Controls[ "GammaValue" ].Text = GameEngineApp._Gamma.ToString( "F1" );

				//MaterialScheme
				{
					comboBox = (EComboBox)pageVideo.Controls[ "MaterialScheme" ];
					foreach( MaterialSchemes materialScheme in
						Enum.GetValues( typeof( MaterialSchemes ) ) )
					{
						comboBox.Items.Add( materialScheme.ToString() );

						if( GameEngineApp.MaterialScheme == materialScheme )
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
					}
					comboBox.SelectedIndexChange += delegate( EComboBox sender )
					{
						if( sender.SelectedIndex != -1 )
							GameEngineApp.MaterialScheme = (MaterialSchemes)sender.SelectedIndex;
					};
				}

				//ShadowTechnique
				{
					comboBox = (EComboBox)pageVideo.Controls[ "ShadowTechnique" ];

					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.None,
						"None" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapLow,
						"Low" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapMedium,
						"Medium (shaders 3.0 only)" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapHigh,
						"High (shaders 3.0 only)" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.Stencil, 
						"Stencil" ) );

					for( int n = 0; n < comboBox.Items.Count; n++ )
					{
						ShadowTechniqueItem item = (ShadowTechniqueItem)comboBox.Items[ n ];
						if( item.Technique == GameEngineApp.ShadowTechnique )
							comboBox.SelectedIndex = n;
					}

					comboBox.SelectedIndexChange += delegate( EComboBox sender )
					{
						if( sender.SelectedIndex != -1 )
						{
							ShadowTechniqueItem item = (ShadowTechniqueItem)sender.SelectedItem;
							GameEngineApp.ShadowTechnique = item.Technique;
						}
						UpdateShadowControlsEnable();
					};
					UpdateShadowControlsEnable();
				}

				//ShadowUseMapSettings
				{
					checkBox = (ECheckBox)pageVideo.Controls[ "ShadowUseMapSettings" ];
					checkBox.Checked = GameEngineApp.ShadowUseMapSettings;
					checkBox.CheckedChange += delegate( ECheckBox sender )
					{
						GameEngineApp.ShadowUseMapSettings = sender.Checked;
						if( sender.Checked && Map.Instance != null )
						{
							GameEngineApp.ShadowFarDistance = Map.Instance.InitialShadowFarDistance;
							GameEngineApp.ShadowColor = Map.Instance.InitialShadowColor;
						}

						UpdateShadowControlsEnable();

						if( sender.Checked )
						{
							( (EScrollBar)pageVideo.Controls[ "ShadowFarDistance" ] ).Value =
								GameEngineApp.ShadowFarDistance;

							pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
								( (int)GameEngineApp.ShadowFarDistance ).ToString();

							ColorValue color = GameEngineApp.ShadowColor;
							( (EScrollBar)pageVideo.Controls[ "ShadowColor" ] ).Value =
								( color.Red + color.Green + color.Blue ) / 3;
						}
					};
				}

				//ShadowFarDistance
				scrollBar = (EScrollBar)pageVideo.Controls[ "ShadowFarDistance" ];
				scrollBar.Value = GameEngineApp.ShadowFarDistance;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					GameEngineApp.ShadowFarDistance = sender.Value;
					pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
						( (int)GameEngineApp.ShadowFarDistance ).ToString();
				};
				pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
					( (int)GameEngineApp.ShadowFarDistance ).ToString();

				//ShadowColor
				scrollBar = (EScrollBar)pageVideo.Controls[ "ShadowColor" ];
				scrollBar.Value = ( GameEngineApp.ShadowColor.Red + GameEngineApp.ShadowColor.Green +
					GameEngineApp.ShadowColor.Blue ) / 3;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					float color = sender.Value;
					GameEngineApp.ShadowColor = new ColorValue( color, color, color, color );
				};

				//Shadow2DTextureSize
				comboBox = (EComboBox)pageVideo.Controls[ "Shadow2DTextureSize" ];
				comboBox.Items.Add( 256 );
				comboBox.Items.Add( 512 );
				comboBox.Items.Add( 1024 );
				comboBox.Items.Add( 2048 );
				if( GameEngineApp.Shadow2DTextureSize == 256 )
					comboBox.SelectedIndex = 0;
				if( GameEngineApp.Shadow2DTextureSize == 512 )
					comboBox.SelectedIndex = 1;
				else if( GameEngineApp.Shadow2DTextureSize == 1024 )
					comboBox.SelectedIndex = 2;
				else if( GameEngineApp.Shadow2DTextureSize == 2048 )
					comboBox.SelectedIndex = 3;
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.Shadow2DTextureSize = (int)sender.SelectedItem;
				};

				//Shadow2DTextureCount
				comboBox = (EComboBox)pageVideo.Controls[ "Shadow2DTextureCount" ];
				for( int n = 0; n < 3; n++ )
				{
					int count = n + 1;
					comboBox.Items.Add( count );
					if( count == GameEngineApp.Shadow2DTextureCount )
						comboBox.SelectedIndex = n;
				}
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.Shadow2DTextureCount = (int)sender.SelectedItem;
				};

				//ShadowCubicTextureSize
				comboBox = (EComboBox)pageVideo.Controls[ "ShadowCubicTextureSize" ];
				comboBox.Items.Add( 256 );
				comboBox.Items.Add( 512 );
				comboBox.Items.Add( 1024 );
				comboBox.Items.Add( 2048 );
				if( GameEngineApp.ShadowCubicTextureSize == 256 )
					comboBox.SelectedIndex = 0;
				if( GameEngineApp.ShadowCubicTextureSize == 512 )
					comboBox.SelectedIndex = 1;
				else if( GameEngineApp.ShadowCubicTextureSize == 1024 )
					comboBox.SelectedIndex = 2;
				else if( GameEngineApp.ShadowCubicTextureSize == 2048 )
					comboBox.SelectedIndex = 3;
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.ShadowCubicTextureSize = (int)sender.SelectedItem;
				};

				//ShadowCubicTextureCount
				comboBox = (EComboBox)pageVideo.Controls[ "ShadowCubicTextureCount" ];
				for( int n = 0; n < 3; n++ )
				{
					int count = n + 1;
					comboBox.Items.Add( count );
					if( count == GameEngineApp.ShadowCubicTextureCount )
						comboBox.SelectedIndex = n;
				}
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.ShadowCubicTextureCount = (int)sender.SelectedItem;
				};

				//fullScreenCheckBox
				checkBox = (ECheckBox)pageVideo.Controls[ "FullScreen" ];
				checkBox.Enable = !EngineApp.Instance.WebPlayerMode;
				checkBox.Checked = EngineApp.Instance.FullScreen;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					EngineApp.Instance.FullScreen = sender.Checked;
				};

				//waterReflectionLevel
				comboBox = (EComboBox)pageVideo.Controls[ "WaterReflectionLevel" ];
				foreach( WaterPlane.ReflectionLevels level in Enum.GetValues(
					typeof( WaterPlane.ReflectionLevels ) ) )
				{
					comboBox.Items.Add( level );
					if( GameEngineApp.WaterReflectionLevel == level )
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
				}
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.WaterReflectionLevel = (WaterPlane.ReflectionLevels)sender.SelectedItem;
				};

				//showDecorativeObjects
				checkBox = (ECheckBox)pageVideo.Controls[ "ShowDecorativeObjects" ];
				checkBox.Checked = GameEngineApp.ShowDecorativeObjects;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					GameEngineApp.ShowDecorativeObjects = sender.Checked;
				};

				//showSystemCursorCheckBox
				checkBox = (ECheckBox)pageVideo.Controls[ "ShowSystemCursor" ];
				checkBox.Checked = GameEngineApp._ShowSystemCursor;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					GameEngineApp._ShowSystemCursor = sender.Checked;
					sender.Checked = GameEngineApp._ShowSystemCursor;
				};

				//showFPSCheckBox
				checkBox = (ECheckBox)pageVideo.Controls[ "ShowFPS" ];
				checkBox.Checked = GameEngineApp._DrawFPS;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					GameEngineApp._DrawFPS = sender.Checked;
					sender.Checked = GameEngineApp._DrawFPS;
				};

			}

			//pageSound
			{
				EControl pageSound = window.Controls[ "TabControl" ].Controls[ "Sound" ];

				//soundVolumeCheckBox
				scrollBar = (EScrollBar)pageSound.Controls[ "SoundVolume" ];
				scrollBar.Value = GameEngineApp.SoundVolume;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					GameEngineApp.SoundVolume = sender.Value;
				};

				//musicVolumeCheckBox
				scrollBar = (EScrollBar)pageSound.Controls[ "MusicVolume" ];
				scrollBar.Value = GameEngineApp.MusicVolume;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					GameEngineApp.MusicVolume = sender.Value;
				};
			}

			//pageControls
			{
				EControl pageControls = window.Controls[ "TabControl" ].Controls[ "Controls" ];

				//MouseHSensitivity
				scrollBar = (EScrollBar)pageControls.Controls[ "MouseHSensitivity" ];
				scrollBar.Value = GameControlsManager.Instance.MouseSensitivity.X;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					value.X = sender.Value;
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//MouseVSensitivity
				scrollBar = (EScrollBar)pageControls.Controls[ "MouseVSensitivity" ];
				scrollBar.Value = Math.Abs( GameControlsManager.Instance.MouseSensitivity.Y );
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					bool invert = ( (ECheckBox)pageControls.Controls[ "MouseVInvert" ] ).Checked;
					value.Y = sender.Value * ( invert ? -1.0f : 1.0f );
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//MouseVInvert
				checkBox = (ECheckBox)pageControls.Controls[ "MouseVInvert" ];
				checkBox.Checked = GameControlsManager.Instance.MouseSensitivity.Y < 0;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					value.Y =
						( (EScrollBar)pageControls.Controls[ "MouseVSensitivity" ] ).Value *
						( sender.Checked ? -1.0f : 1.0f );
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//Devices
				comboBox = (EComboBox)pageControls.Controls[ "InputDevices" ];
				comboBoxInputDevices = comboBox;
				comboBox.Items.Add( "Keyboard/Mouse" );
				if( InputDeviceManager.Instance != null )
				{
					foreach( InputDevice device in InputDeviceManager.Instance.Devices )
						comboBox.Items.Add( device );
				}
				comboBox.SelectedIndex = 0;

				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					UpdateBindedInputControlsTextBox();
				};

				//Controls
				UpdateBindedInputControlsTextBox();
			}
		}

		void UpdateShadowControlsEnable()
		{
			EControl pageVideo = window.Controls[ "TabControl" ].Controls[ "Video" ];

			bool textureShadows =
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapLow ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapMedium ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHigh;
			bool allowShadowColor = GameEngineApp.ShadowTechnique != ShadowTechniques.None;

			pageVideo.Controls[ "ShadowColor" ].Enable =
				!GameEngineApp.ShadowUseMapSettings && allowShadowColor;
			pageVideo.Controls[ "ShadowFarDistance" ].Enable =
				!GameEngineApp.ShadowUseMapSettings &&
				GameEngineApp.ShadowTechnique != ShadowTechniques.None;
			pageVideo.Controls[ "Shadow2DTextureSize" ].Enable = textureShadows;
			pageVideo.Controls[ "Shadow2DTextureCount" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowCubicTextureSize" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowCubicTextureCount" ].Enable = textureShadows;
		}

		void ChangeVideoMode()
		{
			Vec2i size;
			{
				size = EngineApp.Instance.VideoMode;

				if( comboBoxResolution.SelectedIndex != -1 )
				{
					string s = (string)( comboBoxResolution ).SelectedItem;
					s = s.Replace( "x", " " );
					size = Vec2i.Parse( s );
				}
			}

			EngineApp.Instance.VideoMode = size;
		}

		void UpdateBindedInputControlsTextBox()
		{
			EControl pageControls = window.Controls[ "TabControl" ].Controls[ "Controls" ];

			//!!!!temp

			string text = "Configuring of custom controls is not implemented\n";
			text += "\n";

			InputDevice inputDevice = comboBoxInputDevices.SelectedItem as InputDevice;

			text += "Binded keys:\n\n";

			foreach( GameControlsManager.GameControlItem item in
				GameControlsManager.Instance.Items )
			{
				string valueStr = "";

				//keys and mouse buttons
				if( inputDevice == null )
				{
					foreach( GameControlsManager.SystemKeyboardMouseValue value in
						item.DefaultKeyboardMouseValues )
					{
						switch( value.Type )
						{
						case GameControlsManager.SystemKeyboardMouseValue.Types.Key:
							valueStr += string.Format( "Key: {0}", value.Key );
							break;

						case GameControlsManager.SystemKeyboardMouseValue.Types.MouseButton:
							valueStr += string.Format( "MouseButton: {0}", value.MouseButton );
							break;
						}
					}
				}

				//joystick
				JoystickInputDevice joystickInputDevice = inputDevice as JoystickInputDevice;
				if( joystickInputDevice != null )
				{
					foreach( GameControlsManager.SystemJoystickValue value in
						item.DefaultJoystickValues )
					{
						switch( value.Type )
						{
						case GameControlsManager.SystemJoystickValue.Types.Button:
							if( joystickInputDevice.GetButtonByName( value.Button ) != null )
								valueStr += string.Format( "Button: {0}", value.Button );
							break;

						case GameControlsManager.SystemJoystickValue.Types.Axis:
							if( joystickInputDevice.GetAxisByName( value.Axis ) != null )
								valueStr += string.Format( "Axis: {0}({1})", value.Axis, value.AxisFilter );
							break;

						case GameControlsManager.SystemJoystickValue.Types.POV:
							if( joystickInputDevice.GetPOVByName( value.POV ) != null )
								valueStr += string.Format( "POV: {0}({1})", value.POV, value.POVDirection );
							break;
						}
					}
				}

				if( valueStr != "" )
					text += string.Format( "{0} - {1}\n", item.ControlKey.ToString(), valueStr );
			}

			pageControls.Controls[ "Controls" ].Text = text;
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
