// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using WindowsAppFramework;

namespace WindowsAppExample
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			//NeoAxis initialization
			if( !WindowsAppWorld.Init( this, "user:Logs/WindowsAppExample.log" ) )
			{
				Close();
				return;
			}

			UpdateVolume();

			//load map
			WindowsAppWorld.MapLoad( "Maps\\WindowsAppExample\\Map.map", true );

			renderTargetUserControl1.AutomaticUpdateFPS = 60;
			renderTargetUserControl1.Render += renderTargetUserControl1_Render;
			renderTargetUserControl1.RenderUI += renderTargetUserControl1_RenderUI;
		}

		private void MainForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			//NeoAxis shutdown
			WindowsAppWorld.Shutdown();
		}

		private void buttonExit_Click( object sender, EventArgs e )
		{
			Close();
		}

		MapCamera GetMapCamera()
		{
			MapCamera mapCamera = null;
			foreach( Entity entity in Map.Instance.Children )
			{
				mapCamera = entity as MapCamera;
				if( mapCamera != null )
					break;
			}
			return mapCamera;
		}

		void renderTargetUserControl1_Render( Camera camera )
		{
			//update camera
			if( Map.Instance != null )
			{
				Vec3 position;
				Vec3 forward;
				Degree fov;

				MapCamera mapCamera = GetMapCamera();
				if( mapCamera != null )
				{
					position = mapCamera.Position;
					forward = mapCamera.Rotation * new Vec3( 1, 0, 0 );
					fov = mapCamera.Fov;
				}
				else
				{
					position = Map.Instance.EditorCameraPosition;
					forward = Map.Instance.EditorCameraDirection.GetVector();
					fov = Map.Instance.Fov;
				}

				if( fov == 0 )
					fov = Map.Instance.Fov;
				//if( fov == 0 )
				//   fov = Map.Instance.Type.DefaultFov;

				renderTargetUserControl1.CameraNearFarClipDistance = Map.Instance.NearFarClipDistance;
				renderTargetUserControl1.CameraFixedUp = Vec3.ZAxis;
				renderTargetUserControl1.CameraFov = fov;
				renderTargetUserControl1.CameraPosition = position;
				renderTargetUserControl1.CameraDirection = forward;
			}

			//update sound listener
			if( SoundWorld.Instance != null )
			{
				SoundWorld.Instance.SetListener( camera.Position, Vec3.Zero,
					camera.Direction, camera.Up );
			}

			RenderEntityOverCursor( camera );
		}

		void RenderEntityOverCursor( Camera camera )
		{
			Vec2 mouse = renderTargetUserControl1.GetFloatMousePosition();

			Ray ray = camera.GetCameraToViewportRay( mouse );

			MapObject mapObject = null;

			Map.Instance.GetObjects( ray, delegate( MapObject obj, float scale )
			{
				if( obj is StaticMesh )
					return true;
				mapObject = obj;
				return false;
			} );

			if( mapObject != null )
			{
				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
				camera.DebugGeometry.AddBounds( mapObject.MapBounds );
			}
		}

		void renderTargetUserControl1_RenderUI( GuiRenderer renderer )
		{
			string text = "NeoAxis Engine " + EngineVersionInformation.Version;
			renderer.AddText( text, new Vec2( .01f, .01f ), HorizontalAlign.Left,
				VerticalAlign.Top, new ColorValue( 1, 1, 1 ) );
		}

		private void buttonAdditionalForm_Click( object sender, EventArgs e )
		{
			AdditionalForm form = new AdditionalForm();
			form.ShowDialog();
		}

		private void buttonCreateBox_Click( object sender, EventArgs e )
		{
			if( Map.Instance != null )
			{
				MapObject box = (MapObject)Entities.Instance.Create( "Box", Map.Instance );
				box.Position = new Vec3( 1.6f, 18.0f, 10.0f );
				box.PostCreate();
			}
		}

		private void trackBarVolume_Scroll( object sender, EventArgs e )
		{
			UpdateVolume();
		}

		void UpdateVolume()
		{
			float volume = (float)trackBarVolume.Value / (float)trackBarVolume.Maximum;
			SoundWorld.Instance.MasterChannelGroup.Volume = volume;
		}

		private void buttonShowUI_Click( object sender, EventArgs e )
		{
			//close if already created
			foreach( EControl control in renderTargetUserControl1.ControlManager.Controls )
			{
				if( control is WindowsAppExampleHUD )
				{
					control.SetShouldDetach();
					return;
				}
			}

			//create
			WindowsAppExampleHUD window = new WindowsAppExampleHUD();
			renderTargetUserControl1.ControlManager.Controls.Add( window );
		}
	}
}