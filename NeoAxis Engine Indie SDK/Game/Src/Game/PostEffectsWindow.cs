// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Renderer;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a "PostEffects" window.
	/// </summary>
	public class PostEffectsWindow : EControl
	{
		static PostEffectsWindow instance;

		Viewport viewport;

		EControl window;

		EListBox listBox;

		ECheckBox checkBoxEnabled;
		EScrollBar[] scrollBarFloatParameters = new EScrollBar[ 7 ];
		ECheckBox[] checkBoxBoolParameters = new ECheckBox[ 1 ];

		bool noPostEffectUpdate;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			instance = this;

			viewport = RendererWorld.Instance.DefaultViewport;

			window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\PostEffectsWindow.gui" );
			Controls.Add( window );

			for( int n = 0; n < scrollBarFloatParameters.Length; n++ )
			{
				scrollBarFloatParameters[ n ] = (EScrollBar)window.Controls[
					"FloatParameter" + n.ToString() ];
				scrollBarFloatParameters[ n ].ValueChange += floatParameter_ValueChange;
			}
			for( int n = 0; n < checkBoxBoolParameters.Length; n++ )
			{
				checkBoxBoolParameters[ n ] = (ECheckBox)window.Controls[ "BoolParameter" + n.ToString() ];
				checkBoxBoolParameters[ n ].CheckedChange += boolParameter_CheckedChange;
			}

			listBox = (EListBox)window.Controls[ "List" ];
			listBox.Items.Add( "HDR" );
			listBox.Items.Add( "LDRBloom" );
			listBox.Items.Add( "Glass" );
			listBox.Items.Add( "OldTV" );
			listBox.Items.Add( "HeatVision" );
			listBox.Items.Add( "MotionBlur" );
			listBox.Items.Add( "RadialBlur" );
			listBox.Items.Add( "Blur" );
			listBox.Items.Add( "Grayscale" );
			listBox.Items.Add( "Invert" );
			listBox.Items.Add( "Tiling" );

			listBox.SelectedIndexChange += listBox_SelectedIndexChange;

			checkBoxEnabled = (ECheckBox)window.Controls[ "Enabled" ];
			checkBoxEnabled.Click += checkBoxEnabled_Click;

			for( int n = 0; n < listBox.Items.Count; n++ )
			{
				EButton itemButton = listBox.ItemButtons[ n ];
				ECheckBox checkBox = (ECheckBox)itemButton.Controls[ "CheckBox" ];

				string name = GetListCompositorItemName( (string)listBox.Items[ n ] );

				CompositorInstance compositorInstance = viewport.GetCompositorInstance( name );
				if( compositorInstance != null && compositorInstance.Enabled )
					checkBox.Checked = true;

				if( itemButton.Text == "HDR" )
					checkBox.Enable = false;

				checkBox.Click += listBoxCheckBox_Click;
			}

			listBox.SelectedIndex = 0;

			( (EButton)window.Controls[ "Close" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};

			//ApplyToTheScreenGUI
			{
				ECheckBox checkBox = (ECheckBox)window.Controls[ "ApplyToTheScreenGUI" ];
				checkBox.Checked = EngineApp.Instance.ScreenGuiRenderer.ApplyPostEffectsToScreenRenderer;
				checkBox.Click += delegate( ECheckBox sender )
				{
					EngineApp.Instance.ScreenGuiRenderer.ApplyPostEffectsToScreenRenderer = sender.Checked;
				};
			}

		}

		protected override void OnDetach()
		{
			base.OnDetach();
			instance = null;
		}

		public static PostEffectsWindow Instance
		{
			get { return instance; }
		}

		string GetListCompositorItemName( string itemName )
		{
			return itemName.Split( new char[] { ' ' } )[ 0 ];
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

		void listBox_SelectedIndexChange( EListBox sender )
		{
			UpdateCurrentPostEffectControls();
		}

		void listBoxCheckBox_Click( ECheckBox sender )
		{
			//set listBox current item
			for( int n = 0; n < listBox.Items.Count; n++ )
			{
				EButton itemButton = listBox.ItemButtons[ n ];
				if( itemButton.Controls[ "CheckBox" ] == sender )
					listBox.SelectedIndex = n;
			}

			checkBoxEnabled.Checked = sender.Checked;
			checkBoxEnabled.Enable = ( sender.Parent.Text != "HDR" );

			UpdateCurrentPostEffect();
		}

		void checkBoxEnabled_Click( ECheckBox sender )
		{
			EButton itemButton = listBox.ItemButtons[ listBox.SelectedIndex ];
			( (ECheckBox)itemButton.Controls[ "CheckBox" ] ).Checked = sender.Checked;

			UpdateCurrentPostEffect();
		}

		void floatParameter_ValueChange( EScrollBar sender )
		{
			window.Controls[ sender.Name + "Value" ].Text = sender.Value.ToString( "F2" );
			if( !noPostEffectUpdate )
				UpdateCurrentPostEffect();
		}

		void boolParameter_CheckedChange( ECheckBox sender )
		{
			if( !noPostEffectUpdate )
				UpdateCurrentPostEffect();
		}

		void UpdateCurrentPostEffectControls()
		{
			noPostEffectUpdate = true;

			string name = GetListCompositorItemName( listBox.SelectedItem.ToString() );

			//Hide controls
			{
				for( int n = 0; n < scrollBarFloatParameters.Length; n++ )
				{
					string s = "FloatParameter" + n.ToString();
					window.Controls[ s + "Text" ].Visible = false;
					window.Controls[ s ].Visible = false;
					window.Controls[ s ].Enable = true;
					window.Controls[ s + "Value" ].Visible = false;
					window.Controls[ s + "Value" ].Enable = true;
					window.Controls[ s + "Value" ].Text = "";
				}
				for( int n = 0; n < checkBoxBoolParameters.Length; n++ )
				{
					string s = "BoolParameter" + n.ToString();
					window.Controls[ s ].Visible = false;
					window.Controls[ s ].Enable = true;
				}
				window.Controls[ "Description" ].Text = "";
			}

			//Set post effect name
			window.Controls[ "Name" ].Text = name;

			//Update "Enabled" check box

			EButton itemButton = listBox.ItemButtons[ listBox.SelectedIndex ];
			checkBoxEnabled.Checked = ( (ECheckBox)itemButton.Controls[ "CheckBox" ] ).Checked;
			checkBoxEnabled.Enable = ( itemButton.Text != "HDR" );

			//Show need parameters

			//MotionBlur specific
			if( name == "MotionBlur" )
			{
				window.Controls[ "FloatParameter0Text" ].Visible = true;
				window.Controls[ "FloatParameter0Text" ].Text = "Blur";
				window.Controls[ "FloatParameter0Value" ].Visible = true;
				scrollBarFloatParameters[ 0 ].Visible = true;
				scrollBarFloatParameters[ 0 ].ValueRange = new Range( 0, .97f );
				scrollBarFloatParameters[ 0 ].Value = MotionBlurCompositorInstance.Blur;
			}

			//Blur specific
			if( name == "Blur" )
			{
				window.Controls[ "FloatParameter0Text" ].Visible = true;
				window.Controls[ "FloatParameter0Text" ].Text = "Fuzziness";
				window.Controls[ "FloatParameter0Value" ].Visible = true;
				scrollBarFloatParameters[ 0 ].Visible = true;
				scrollBarFloatParameters[ 0 ].ValueRange = new Range( 0, 15 );
				scrollBarFloatParameters[ 0 ].Value = BlurCompositorInstance.Fuzziness;
			}

			//HDR specific
			if( name == "HDR" )
			{
				//Adaptation enable
				window.Controls[ "BoolParameter0" ].Visible = true;
				window.Controls[ "BoolParameter0" ].Text = "Adaptation";
				checkBoxBoolParameters[ 0 ].Checked = HDRCompositorInstance.Adaptation;

				//AdaptationVelocity
				window.Controls[ "FloatParameter1Text" ].Visible = true;
				window.Controls[ "FloatParameter1Text" ].Text = "Adaptation velocity";
				window.Controls[ "FloatParameter1Value" ].Visible = true;
				scrollBarFloatParameters[ 1 ].Visible = true;
				scrollBarFloatParameters[ 1 ].ValueRange = new Range( .1f, 10 );
				scrollBarFloatParameters[ 1 ].Value = HDRCompositorInstance.AdaptationVelocity;

				//AdaptationMiddleBrightness
				window.Controls[ "FloatParameter2Text" ].Visible = true;
				window.Controls[ "FloatParameter2Text" ].Text = "Adaptation middle brightness";
				window.Controls[ "FloatParameter2Value" ].Visible = true;
				scrollBarFloatParameters[ 2 ].Visible = true;
				scrollBarFloatParameters[ 2 ].ValueRange = new Range( .1f, 2 );
				scrollBarFloatParameters[ 2 ].Value = HDRCompositorInstance.AdaptationMiddleBrightness;

				//AdaptationMinimum
				window.Controls[ "FloatParameter3Text" ].Visible = true;
				window.Controls[ "FloatParameter3Text" ].Text = "Adaptation minimum";
				window.Controls[ "FloatParameter3Value" ].Visible = true;
				scrollBarFloatParameters[ 3 ].Visible = true;
				scrollBarFloatParameters[ 3 ].ValueRange = new Range( .1f, 2 );
				scrollBarFloatParameters[ 3 ].Value = HDRCompositorInstance.AdaptationMinimum;

				//AdaptationMaximum
				window.Controls[ "FloatParameter4Text" ].Visible = true;
				window.Controls[ "FloatParameter4Text" ].Text = "Adaptation maximum";
				window.Controls[ "FloatParameter4Value" ].Visible = true;
				scrollBarFloatParameters[ 4 ].Visible = true;
				scrollBarFloatParameters[ 4 ].ValueRange = new Range( .1f, 2 );
				scrollBarFloatParameters[ 4 ].Value = HDRCompositorInstance.AdaptationMaximum;

				//BloomBrightThreshold
				window.Controls[ "FloatParameter5Text" ].Visible = true;
				window.Controls[ "FloatParameter5Text" ].Text = "Bloom bright threshold";
				window.Controls[ "FloatParameter5Value" ].Visible = true;
				scrollBarFloatParameters[ 5 ].Visible = true;
				scrollBarFloatParameters[ 5 ].ValueRange = new Range( .1f, 2.0f );
				scrollBarFloatParameters[ 5 ].Value = HDRCompositorInstance.BloomBrightThreshold;

				//BloomScale
				window.Controls[ "FloatParameter6Text" ].Visible = true;
				window.Controls[ "FloatParameter6Text" ].Text = "Bloom scale";
				window.Controls[ "FloatParameter6Value" ].Visible = true;
				scrollBarFloatParameters[ 6 ].Visible = true;
				scrollBarFloatParameters[ 6 ].ValueRange = new Range( 0, 5 );
				scrollBarFloatParameters[ 6 ].Value = HDRCompositorInstance.BloomScale;

				for( int n = 1; n <= 4; n++ )
					scrollBarFloatParameters[ n ].Enable = HDRCompositorInstance.Adaptation;

				window.Controls[ "Description" ].Text = "Use Configurator for enable/disable HDR.\n\n" +
					"Default values are set in \"Data\\Definitions\\Renderer.config\".";
			}

			//LDRBloom specific
			if( name == "LDRBloom" )
			{
				//BloomBrightThreshold
				window.Controls[ "FloatParameter0Text" ].Visible = true;
				window.Controls[ "FloatParameter0Text" ].Text = "Bloom bright threshold";
				window.Controls[ "FloatParameter0Value" ].Visible = true;
				scrollBarFloatParameters[ 0 ].Visible = true;
				scrollBarFloatParameters[ 0 ].ValueRange = new Range( .1f, 2.0f );
				scrollBarFloatParameters[ 0 ].Value = LDRBloomCompositorInstance.BloomBrightThreshold;

				//BloomScale
				window.Controls[ "FloatParameter1Text" ].Visible = true;
				window.Controls[ "FloatParameter1Text" ].Text = "Bloom scale";
				window.Controls[ "FloatParameter1Value" ].Visible = true;
				scrollBarFloatParameters[ 1 ].Visible = true;
				scrollBarFloatParameters[ 1 ].ValueRange = new Range( 0, 5 );
				scrollBarFloatParameters[ 1 ].Value = LDRBloomCompositorInstance.BloomScale;
			}

			noPostEffectUpdate = false;
		}

		void UpdateCurrentPostEffect()
		{
			string name = GetListCompositorItemName( listBox.SelectedItem.ToString() );

			bool enabled = checkBoxEnabled.Checked;
			CompositorInstance instance = viewport.GetCompositorInstance( name );

			if( enabled )
			{
				//Enable
				instance = viewport.AddCompositor( name );
				if( instance != null )
					instance.Enabled = true;
			}
			else
			{
				//Disable
				if( name == "MotionBlur" )
				{
					//MotionBlur game specific. No remove compositor. only disable.
					if( instance != null )
						instance.Enabled = false;
				}
				else
					viewport.RemoveCompositor( name );
			}

			if( enabled )
			{
				//MotionBlur specific
				if( name == "MotionBlur" )
				{
					//Update post effect parameters
					MotionBlurCompositorInstance.Blur = scrollBarFloatParameters[ 0 ].Value;
				}

				//Blur specific
				if( name == "Blur" )
				{
					//Update post effect parameters
					BlurCompositorInstance.Fuzziness = scrollBarFloatParameters[ 0 ].Value;
				}

				//HDR specific
				if( name == "HDR" )
				{
					//Update post effect parameters
					HDRCompositorInstance.Adaptation = checkBoxBoolParameters[ 0 ].Checked;
					HDRCompositorInstance.AdaptationVelocity = scrollBarFloatParameters[ 1 ].Value;
					HDRCompositorInstance.AdaptationMiddleBrightness = scrollBarFloatParameters[ 2 ].Value;
					HDRCompositorInstance.AdaptationMinimum = scrollBarFloatParameters[ 3 ].Value;
					HDRCompositorInstance.AdaptationMaximum = scrollBarFloatParameters[ 4 ].Value;
					HDRCompositorInstance.BloomBrightThreshold = scrollBarFloatParameters[ 5 ].Value;
					HDRCompositorInstance.BloomScale = scrollBarFloatParameters[ 6 ].Value;

					//Update controls
					for( int n = 1; n <= 4; n++ )
						scrollBarFloatParameters[ n ].Enable = HDRCompositorInstance.Adaptation;
				}

				//LDRBloom specific
				if( name == "LDRBloom" )
				{
					//Update post effect parameters
					LDRBloomCompositorInstance.BloomBrightThreshold = scrollBarFloatParameters[ 0 ].Value;
					LDRBloomCompositorInstance.BloomScale = scrollBarFloatParameters[ 1 ].Value;
				}
			}
		}
	}
}
