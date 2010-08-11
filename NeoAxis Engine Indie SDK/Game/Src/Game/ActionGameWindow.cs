// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.FileSystem;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a game window for FPS and TPS games.
	/// </summary>
	public class ActionGameWindow : GameWindow
	{
		enum CameraType
		{
			FPS,
			TPS,
			Free,

			Count,
		}
		[Config( "Camera", "cameraType" )]
		static CameraType cameraType;

		[Config( "Camera", "tpsCameraDistance" )]
		static float tpsCameraDistance = 4;
		[Config( "Camera", "tpsCameraCenterOffset" )]
		static float tpsCameraCenterOffset = 1.6f;

		[Config( "Camera", "tpsVehicleCameraDistance" )]
		static float tpsVehicleCameraDistance = 8.7f;
		[Config( "Camera", "tpsVehicleCameraCenterOffset" )]
		static float tpsVehicleCameraCenterOffset = 3.8f;

		//For management of pressing of the player on switches and management ingame GUI
		const float playerUseDistance = 3;
		const float playerUseDistanceTPS = 10;
		//Current ingame GUI which with which the player can cooperate
		MapObjectAttachedGui currentAttachedGuiObject;
		//Which player can switch the current switch
		GameEntities.Switch currentSwitch;

		//For an opportunity to change an active unit and for work with float switches
		bool switchUsing;

		//HUD screen
		EControl hudControl;

		//Data for an opportunity of the player to control other objects. (for Example: Turret control)
		Unit currentSeeUnitAllowPlayerControl;

		//For optimization of search of the nearest point on a map curve.
		//only for GetNearestPointToMapCurve()
		MapCurve observeCameraMapCurvePoints;
		List<Vec3> observeCameraMapCurvePointsList = new List<Vec3>();

		//The list of ObserveCameraArea's for faster work
		List<ObserveCameraArea> observeCameraAreas = new List<ObserveCameraArea>();

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			//To load the HUD screen
			hudControl = ControlDeclarationManager.Instance.CreateControl( "Gui\\ActionHUD.gui" );
			//Attach the HUD screen to the this window
			Controls.Add( hudControl );

			//CutSceneManager specific
			if( CutSceneManager.Instance != null )
			{
				CutSceneManager.Instance.CutSceneEnableChange += delegate( CutSceneManager manager )
				{
					if( manager.CutSceneEnable )
					{
						//Cut scene activated. All keys and buttons need to reset.
						EngineApp.Instance.KeysAndMouseButtonUpAll();
						GameControlsManager.Instance.DoKeyUpAll();
					}
				};
			}

			//fill observeCameraRegions list
			foreach( Entity entity in Map.Instance.Children )
			{
				ObserveCameraArea area = entity as ObserveCameraArea;
				if( area != null )
					observeCameraAreas.Add( area );
			}

			FreeCameraEnabled = cameraType == CameraType.Free;

			//add game specific console command
			if( EngineConsole.Instance != null )
			{
				EngineConsole.Instance.AddCommand( "movePlayerUnitToCamera",
					ConsoleCommand_MovePlayerUnitToCamera );
			}

			//accept commands of the player
			GameControlsManager.Instance.GameControlsEvent += GameControlsManager_GameControlsEvent;

			//MotionBlur for a player unit contusion
			{
				Compositor compositor = CompositorManager.Instance.GetByName( "MotionBlur" );
				if( compositor != null && compositor.IsSupported() )
					RendererWorld.Instance.DefaultViewport.AddCompositor( "MotionBlur" );
			}
		}

		protected override void OnDetach()
		{
			//MotionBlur for a player unit contusion
			RendererWorld.Instance.DefaultViewport.SetCompositorEnabled( "MotionBlur", false );

			//accept commands of the player
			GameControlsManager.Instance.GameControlsEvent -= GameControlsManager_GameControlsEvent;

			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnKeyDown( e );

			//currentAttachedGuiObject
			if( currentAttachedGuiObject != null )
			{
				if( currentAttachedGuiObject.ControlManager.DoKeyDown( e ) )
					return true;
			}

			//change camera type
			if( e.Key == EKeys.F7 )
			{
				cameraType = (CameraType)( (int)cameraType + 1 );
				if( cameraType == CameraType.Count )
					cameraType = (CameraType)0;

				if( GetPlayerUnit() == null )
					cameraType = CameraType.Free;

				FreeCameraEnabled = cameraType == CameraType.Free;

				return true;
			}

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation )
			{
				if( GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() )
				{
					if( GameControlsManager.Instance.DoKeyDown( e ) )
						return true;
				}
			}

			return base.OnKeyDown( e );
		}

		protected override bool OnKeyPress( KeyPressEvent e )
		{
			//currentAttachedGuiObject
			if( currentAttachedGuiObject != null )
			{
				currentAttachedGuiObject.ControlManager.DoKeyPress( e );
				return true;
			}

			return base.OnKeyPress( e );
		}

		protected override bool OnKeyUp( KeyEvent e )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnKeyUp( e );

			//currentAttachedGuiObject
			if( currentAttachedGuiObject != null )
				currentAttachedGuiObject.ControlManager.DoKeyUp( e );

			//GameControlsManager
			GameControlsManager.Instance.DoKeyUp( e );

			return base.OnKeyUp( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDown( button );

			//currentAttachedGuiObject
			if( currentAttachedGuiObject != null )
			{
				currentAttachedGuiObject.ControlManager.DoMouseDown( button );
				return true;
			}

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation )
			{
				if( GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() )
				{
					if( GameControlsManager.Instance.DoMouseDown( button ) )
						return true;
				}
			}

			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseUp( button );

			//currentAttachedGuiObject
			if( currentAttachedGuiObject != null )
				currentAttachedGuiObject.ControlManager.DoMouseUp( button );

			//GameControlsManager
			GameControlsManager.Instance.DoMouseUp( button );

			return base.OnMouseUp( button );
		}

		protected override bool OnMouseDoubleClick( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDoubleClick( button );

			//currentAttachedGuiObject
			if( currentAttachedGuiObject != null )
			{
				currentAttachedGuiObject.ControlManager.DoMouseDoubleClick( button );
				return true;
			}

			return base.OnMouseDoubleClick( button );
		}

		protected override bool OnMouseMove()
		{
			bool ret = base.OnMouseMove();

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return ret;

			//ignore mouse move events if DebugInformationWindow enabled without background mode
			if( DebugInformationWindow.Instance != null && !DebugInformationWindow.Instance.Background )
				return ret;

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation && EngineApp.Instance.MouseRelativeMode )
			{
				if( GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() )
				{
					Vec2 mouseOffset = MousePosition;

					//zoom for Turret
					if( EngineApp.Instance.IsMouseButtonPressed( EMouseButtons.Right ) )
						if( GetPlayerUnit() != null && GetPlayerUnit() is Turret )
							if( GetRealCameraType() == CameraType.TPS )
								mouseOffset /= 3;

					GameControlsManager.Instance.DoMouseMoveRelative( mouseOffset );
				}
			}

			return ret;
		}

		protected override bool OnMouseWheel( int delta )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseWheel( delta );

			//currentAttachedGuiObject
			if( currentAttachedGuiObject != null )
			{
				currentAttachedGuiObject.ControlManager.DoMouseWheel( delta );
				return true;
			}

			return base.OnMouseWheel( delta );
		}

		protected override bool OnJoystickEvent( JoystickInputEvent e )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnJoystickEvent( e );

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation )
			{
				if( GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() )
				{
					if( GameControlsManager.Instance.DoJoystickEvent( e ) )
						return true;
				}
			}

			return base.OnJoystickEvent( e );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//NeedWorldDestroy
			if( GameWorld.Instance.NeedWorldDestroy )
			{
				if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
					EntitySystemWorld.Instance.Simulation = false;
				MapSystemWorld.MapDestroy();
				EntitySystemWorld.Instance.WorldDestroy();

				GameEngineApp.Instance.Server_DestroyServer( "The server has been destroyed" );
				GameEngineApp.Instance.Client_DisconnectFromServer();

				//close all windows
				foreach( EControl control in GameEngineApp.Instance.ControlManager.Controls )
					control.SetShouldDetach();
				//create main menu
				GameEngineApp.Instance.ControlManager.Controls.Add( new MainMenuWindow() );
				return;
			}

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return;

			if( GameEngineApp.Instance.ControlManager != null &&
				GameEngineApp.Instance.ControlManager.IsControlFocused() )
				return;

			//update mouse relative mode
			{
				bool relative = EngineApp.Instance.MouseRelativeMode;

				if( GetRealCameraType() == CameraType.Free && !FreeCameraMouseRotating )
					relative = false;

				if( EntitySystemWorld.Instance.Simulation && GetRealCameraType() != CameraType.Free )
					relative = true;

				if( DebugInformationWindow.Instance != null && !DebugInformationWindow.Instance.Background )
					relative = false;

				EngineApp.Instance.MouseRelativeMode = relative;
			}

			bool activeConsole = EngineConsole.Instance != null && EngineConsole.Instance.Active;

			if( GetRealCameraType() == CameraType.TPS && !IsCutSceneEnabled() && !activeConsole )
			{
				Range distanceRange = new Range( 2, 200 );
				Range centerOffsetRange = new Range( 0, 10 );

				float cameraDistance;
				float cameraCenterOffset;

				if( IsPlayerUnitVehicle() )
				{
					cameraDistance = tpsVehicleCameraDistance;
					cameraCenterOffset = tpsVehicleCameraCenterOffset;
				}
				else
				{
					cameraDistance = tpsCameraDistance;
					cameraCenterOffset = tpsCameraCenterOffset;
				}

				if( EngineApp.Instance.IsKeyPressed( EKeys.PageUp ) )
				{
					cameraDistance -= delta * distanceRange.Size() / 20.0f;
					if( cameraDistance < distanceRange[ 0 ] )
						cameraDistance = distanceRange[ 0 ];
				}

				if( EngineApp.Instance.IsKeyPressed( EKeys.PageDown ) )
				{
					cameraDistance += delta * distanceRange.Size() / 20.0f;
					if( cameraDistance > distanceRange[ 1 ] )
						cameraDistance = distanceRange[ 1 ];
				}

				if( EngineApp.Instance.IsKeyPressed( EKeys.Home ) )
				{
					cameraCenterOffset += delta * centerOffsetRange.Size() / 4.0f;
					if( cameraCenterOffset > centerOffsetRange[ 1 ] )
						cameraCenterOffset = centerOffsetRange[ 1 ];
				}

				if( EngineApp.Instance.IsKeyPressed( EKeys.End ) )
				{
					cameraCenterOffset -= delta * centerOffsetRange.Size() / 4.0f;
					if( cameraCenterOffset < centerOffsetRange[ 0 ] )
						cameraCenterOffset = centerOffsetRange[ 0 ];
				}

				if( IsPlayerUnitVehicle() )
				{
					tpsVehicleCameraDistance = cameraDistance;
					tpsVehicleCameraCenterOffset = cameraCenterOffset;
				}
				else
				{
					tpsCameraDistance = cameraDistance;
					tpsCameraCenterOffset = cameraCenterOffset;
				}
			}

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation )
			{
				if( GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() )
					GameControlsManager.Instance.DoTick( delta );
			}
		}

		/// <summary>
		/// Updates objects on which the player can to operate.
		/// Such as which the player can supervise switches, ingameGUI or control units.
		/// </summary>
		void UpdateCurrentPlayerUseObjects()
		{
			Camera camera = RendererWorld.Instance.DefaultCamera;

			Unit playerUnit = GetPlayerUnit();

			float maxDistance = ( GetRealCameraType() == CameraType.FPS ) ?
				playerUseDistance : playerUseDistanceTPS;

			Ray ray = camera.GetCameraToViewportRay( new Vec2( .5f, .5f ) );
			ray.Direction = ray.Direction.GetNormalize() * maxDistance;

			//currentAttachedGuiObject
			{
				MapObjectAttachedGui attachedGuiObject = null;
				Vec2 screenPosition = Vec2.Zero;

				if( GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() &&
					EntitySystemWorld.Instance.Simulation )
				{
					Map.Instance.GetObjectsAttachedGuiObject( ray,
						out attachedGuiObject, out screenPosition );
				}

				//ignore empty gui objects
				if( attachedGuiObject != null )
				{
					In3dControlManager manager = attachedGuiObject.ControlManager;

					if( manager.Controls.Count == 0 ||
						( manager.Controls.Count == 1 && !manager.Controls[ 0 ].Enable ) )
					{
						attachedGuiObject = null;
					}
				}

				if( attachedGuiObject != currentAttachedGuiObject )
				{
					if( currentAttachedGuiObject != null )
						currentAttachedGuiObject.ControlManager.LostManagerFocus();
					currentAttachedGuiObject = attachedGuiObject;
				}

				if( currentAttachedGuiObject != null )
					currentAttachedGuiObject.ControlManager.DoMouseMove( screenPosition );
			}

			//currentFloatSwitch
			{
				GameEntities.Switch overSwitch = null;

				Map.Instance.GetObjects( ray, delegate( MapObject obj, float scale )
				{
					GameEntities.Switch s = obj as GameEntities.Switch;
					if( s != null )
					{
						if( s.UseAttachedMesh != null )
						{
							Bounds bounds = ( (MapObjectAttachedMesh)s.UseAttachedMesh ).SceneNode.
								GetWorldBounds();

							if( bounds.RayIntersection( ray ) )
							{
								overSwitch = s;
								return false;
							}
						}
						else
						{
							overSwitch = s;
							return false;
						}
					}

					return true;
				} );

				//draw border
				if( overSwitch != null )
				{
					camera.DebugGeometry.Color = new ColorValue( 1, 1, 1 );
					if( overSwitch.UseAttachedMesh != null )
					{
						camera.DebugGeometry.AddBounds( overSwitch.UseAttachedMesh.SceneNode.
							GetWorldBounds() );
					}
					else
						camera.DebugGeometry.AddBounds( overSwitch.MapBounds );
				}

				if( overSwitch != currentSwitch )
				{
					FloatSwitch floatSwitch = currentSwitch as FloatSwitch;
					if( floatSwitch != null )
						floatSwitch.UseEnd();

					currentSwitch = overSwitch;
				}
			}

			//Use player control unit
			if( playerUnit != null )
			{
				currentSeeUnitAllowPlayerControl = null;

				if( PlayerIntellect.Instance != null &&
					PlayerIntellect.Instance.MainNotActiveUnit == null &&
					GetRealCameraType() != CameraType.Free )
				{
					Ray unitFindRay = ray;

					//special ray for TPS camera
					if( GetRealCameraType() == CameraType.TPS )
					{
						unitFindRay = new Ray( playerUnit.Position,
							playerUnit.Rotation * new Vec3( playerUseDistance, 0, 0 ) );
					}

					Map.Instance.GetObjects( unitFindRay, delegate( MapObject obj, float scale )
					{
						Dynamic dynamic = obj as Dynamic;

						if( dynamic == null )
							return true;

						if( !dynamic.Visible )
							return true;

						Unit u = dynamic.GetParentUnit();
						if( u == null )
							return true;

						if( u == GetPlayerUnit() )
							return true;

						if( !u.Type.AllowPlayerControl )
							return true;

						if( u.Intellect != null )
							return true;

						if( !u.MapBounds.RayIntersection( unitFindRay ) )
							return true;

						currentSeeUnitAllowPlayerControl = u;

						return false;
					} );
				}

				//draw border
				if( currentSeeUnitAllowPlayerControl != null )
				{
					camera.DebugGeometry.Color = new ColorValue( 1, 1, 1 );
					camera.DebugGeometry.AddBounds( currentSeeUnitAllowPlayerControl.MapBounds );
				}
			}

			//draw "Press Use" text
			if( currentSwitch != null || currentSeeUnitAllowPlayerControl != null )
			{
				ColorValue color;
				if( ( Time % 2 ) < 1 )
					color = new ColorValue( 1, 1, 0 );
				else
					color = new ColorValue( 0, 1, 0 );

				string text;
				{
					text = "Press \"Use\"";

					//get binded keyboard key or mouse button
					GameControlsManager.GameControlItem controlItem = GameControlsManager.Instance.
						GetItemByControlKey( GameControlKeys.Use );
					if( controlItem != null && controlItem.DefaultKeyboardMouseValues.Length != 0 )
					{
						GameControlsManager.SystemKeyboardMouseValue value =
							controlItem.DefaultKeyboardMouseValues[ 0 ];
						text += string.Format( " ({0})", value.ToString() );
					}
				}

				EngineApp.Instance.ScreenGuiRenderer.AddText( text, new Vec2( .5f, .9f ),
					HorizontalAlign.Center, VerticalAlign.Center, color );
			}

		}

		protected override void OnRender()
		{
			base.OnRender();

			UpdateHUD();
			UpdateCurrentPlayerUseObjects();
			UpdatePlayerContusionMotionBlur();
		}

		void UpdatePlayerContusionMotionBlur()
		{
			//disable updating if post effects windows activated
			if( PostEffectsWindow.Instance != null )
				return;

			PlayerCharacter playerCharacter = GetPlayerUnit() as PlayerCharacter;

			//calculate blur factor
			float blur = 0;
			if( playerCharacter != null && GetRealCameraType() == CameraType.FPS &&
				EntitySystemWorld.Instance.Simulation )
			{
				blur = playerCharacter.ContusionTimeRemaining;
				if( blur > .8f )
					blur = .8f;
			}

			//update compositor
			MotionBlurCompositorInstance instance = (MotionBlurCompositorInstance)
				RendererWorld.Instance.DefaultViewport.GetCompositorInstance( "MotionBlur" );
			if( instance != null )
			{
				instance.Enabled = blur != 0;
				MotionBlurCompositorInstance.Blur = blur;
			}
		}

		void UpdateHUDControlIcon( EControl control, string iconName )
		{
			if( !string.IsNullOrEmpty( iconName ) )
			{
				string fileName = string.Format( "Gui\\HUD\\Icons\\{0}.png", iconName );

				bool needUpdate = false;

				if( control.BackTexture != null )
				{
					string current = control.BackTexture.Name;
					current = current.Replace( '/', '\\' );

					if( string.Compare( fileName, current, true ) != 0 )
						needUpdate = true;
				}
				else
					needUpdate = true;

				if( needUpdate )
				{
					if( VirtualFile.Exists( fileName ) )
						control.BackTexture = TextureManager.Instance.Load( fileName, Texture.Type.Type2D, 0 );
					else
						control.BackTexture = null;
				}
			}
			else
				control.BackTexture = null;
		}

		/// <summary>
		/// Updates HUD screen
		/// </summary>
		void UpdateHUD()
		{
			Unit playerUnit = GetPlayerUnit();

			hudControl.Visible = EngineDebugSettings.DrawGui;

			//Game

			hudControl.Controls[ "Game" ].Visible = GetRealCameraType() != CameraType.Free &&
				!IsCutSceneEnabled();

			//Player
			string playerTypeName = playerUnit != null ? playerUnit.Type.Name : "";

			UpdateHUDControlIcon( hudControl.Controls[ "Game/PlayerIcon" ], playerTypeName );
			hudControl.Controls[ "Game/Player" ].Text = playerTypeName;

			//HealthBar
			{
				float coef = 0;
				if( playerUnit != null )
					coef = playerUnit.Life / playerUnit.Type.LifeMax;

				EControl healthBar = hudControl.Controls[ "Game/HealthBar" ];
				Vec2 originalSize = new Vec2( 256, 32 );
				Vec2 interval = new Vec2( 117, 304 );
				float sizeX = ( 117 - 82 ) + coef * ( interval[ 1 ] - interval[ 0 ] );
				healthBar.Size = new ScaleValue( ScaleType.Texture, new Vec2( sizeX, originalSize.Y ) );
				healthBar.BackTextureCoord = new Rect( 0, 0, sizeX / originalSize.X, 1 );
			}

			//EnergyBar
			{
				float coef = .3f;

				EControl energyBar = hudControl.Controls[ "Game/EnergyBar" ];
				Vec2 originalSize = new Vec2( 256, 32 );
				Vec2 interval = new Vec2( 117, 304 );
				float sizeX = ( 117 - 82 ) + coef * ( interval[ 1 ] - interval[ 0 ] );
				energyBar.Size = new ScaleValue( ScaleType.Texture, new Vec2( sizeX, originalSize.Y ) );
				energyBar.BackTextureCoord = new Rect( 0, 0, sizeX / originalSize.X, 1 );
			}

			//Weapon
			{
				string weaponName = "";
				string magazineCountNormal = "";
				string bulletCountNormal = "";
				string bulletCountAlternative = "";

				Weapon weapon = null;
				{
					//PlayerCharacter specific
					PlayerCharacter playerCharacter = playerUnit as PlayerCharacter;
					if( playerCharacter != null )
						weapon = playerCharacter.ActiveWeapon;

					//Turret specific
					Turret turret = playerUnit as Turret;
					if( turret != null )
						weapon = turret.MainGun;

					//Tank specific
					Tank tank = playerUnit as Tank;
					if( tank != null )
						weapon = tank.MainGun;
				}

				if( weapon != null )
				{
					weaponName = weapon.Type.FullName;

					Gun gun = weapon as Gun;
					if( gun != null )
					{
						if( gun.Type.NormalMode.BulletType != null )
						{
							//magazineCountNormal
							if( gun.Type.NormalMode.MagazineCapacity != 0 )
							{
								magazineCountNormal = gun.NormalMode.BulletMagazineCount.ToString() + "/" +
									gun.Type.NormalMode.MagazineCapacity.ToString();
							}
							//bulletCountNormal
							if( gun.Type.NormalMode.BulletExpense != 0 )
							{
								bulletCountNormal = ( gun.NormalMode.BulletCount -
									gun.NormalMode.BulletMagazineCount ).ToString() + "/" +
									gun.Type.NormalMode.BulletCapacity.ToString();
							}
						}

						if( gun.Type.AlternativeMode.BulletType != null )
						{
							//bulletCountAlternative
							if( gun.Type.AlternativeMode.BulletExpense != 0 )
								bulletCountAlternative = gun.AlternativeMode.BulletCount.ToString() + "/" +
									gun.Type.AlternativeMode.BulletCapacity.ToString();
						}
					}
				}

				hudControl.Controls[ "Game/Weapon" ].Text = weaponName;
				hudControl.Controls[ "Game/WeaponMagazineCountNormal" ].Text = magazineCountNormal;
				hudControl.Controls[ "Game/WeaponBulletCountNormal" ].Text = bulletCountNormal;
				hudControl.Controls[ "Game/WeaponBulletCountAlternative" ].Text = bulletCountAlternative;

				UpdateHUDControlIcon( hudControl.Controls[ "Game/WeaponIcon" ], weaponName );
			}

			//CutScene
			{
				hudControl.Controls[ "CutScene" ].Visible = IsCutSceneEnabled();

				if( CutSceneManager.Instance != null )
				{
					//CutSceneFade
					float fadeCoef = 0;
					if( CutSceneManager.Instance != null )
						fadeCoef = CutSceneManager.Instance.GetFadeCoefficient();
					hudControl.Controls[ "CutSceneFade" ].BackColor = new ColorValue( 0, 0, 0, fadeCoef );

					//Message
					{
						string text;
						ColorValue color;
						CutSceneManager.Instance.GetMessage( out text, out color );
						if( text == null )
							text = "";

						ETextBox textBox = (ETextBox)hudControl.Controls[ "CutScene/Message" ];
						textBox.Text = text;
						textBox.TextColor = color;
					}
				}
			}
		}

		/// <summary>
		/// Draw a target at center of screen
		/// </summary>
		/// <param name="renderer"></param>
		void DrawTarget( GuiRenderer renderer )
		{
			Unit playerUnit = GetPlayerUnit();

			Weapon weapon = null;
			{
				//PlayerCharacter specific
				PlayerCharacter playerCharacter = playerUnit as PlayerCharacter;
				if( playerCharacter != null )
					weapon = playerCharacter.ActiveWeapon;

				//Turret specific
				Turret turret = playerUnit as Turret;
				if( turret != null )
					weapon = turret.MainGun;

				//Tank specific
				Tank tank = playerUnit as Tank;
				if( tank != null )
					weapon = tank.MainGun;
			}

			//draw quad
			if( weapon != null || currentAttachedGuiObject != null || currentSwitch != null )
			{
				Texture texture = TextureManager.Instance.Load( "Cursors/Target.png" );
				float size = .02f;
				float aspect = RendererWorld.Instance.DefaultCamera.AspectRatio;
				Rect rectangle = new Rect(
					.5f - size, .5f - size * aspect,
					.5f + size, .5f + size * aspect );
				renderer.AddQuad( rectangle, new Rect( 0, 0, 1, 1 ), texture );
			}

			//Tank specific
			DrawTankGunTarget( renderer );
		}

		//Tank specific
		void DrawTankGunTarget( GuiRenderer renderer )
		{
			Tank tank = GetPlayerUnit() as Tank;
			if( tank == null )
				return;

			Gun gun = tank.MainGun;
			if( gun == null )
				return;

			Vec3 gunPosition = gun.GetInterpolatedPosition();
			Vec3 gunDirection = gun.GetInterpolatedRotation() * new Vec3( 1, 0, 0 );

			RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
				new Ray( gunPosition, gunDirection * 1000 ),
				(int)ContactGroup.CastOnlyContact );

			bool finded = false;
			Vec3 pos = Vec3.Zero;

			foreach( RayCastResult result in piercingResult )
			{
				bool ignore = false;

				MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

				Dynamic dynamic = obj as Dynamic;
				if( dynamic != null && dynamic.GetParentUnit() == tank )
					ignore = true;

				if( !ignore )
				{
					finded = true;
					pos = result.Position;
					break;
				}
			}

			if( !finded )
				pos = gunPosition + gunDirection * 1000;

			Vec2 screenPos;
			RendererWorld.Instance.DefaultCamera.ProjectToScreenCoordinates( pos, out screenPos );

			//draw quad
			{
				Texture texture = TextureManager.Instance.Load( "Cursors/Target.png" );
				float size = .015f;
				float aspect = RendererWorld.Instance.DefaultCamera.AspectRatio;
				Rect rectangle = new Rect(
					screenPos.X - size, screenPos.Y - size * aspect,
					screenPos.X + size, screenPos.Y + size * aspect );
				renderer.AddQuad( rectangle, new Rect( 0, 0, 1, 1 ), texture,
					new ColorValue( 0, 1, 0 ) );
			}
		}

		/// <summary>
		/// To draw some information of a player
		/// </summary>
		/// <param name="renderer"></param>
		void DrawPlayerInformation( GuiRenderer renderer )
		{
			if( GetRealCameraType() == CameraType.Free )
				return;

			if( IsCutSceneEnabled() )
				return;

			//debug draw an influences.
			{
				float posy = .8f;

				foreach( Entity entity in GetPlayerUnit().Children )
				{
					Influence influence = entity as Influence;
					if( influence == null )
						continue;

					renderer.AddText( influence.Type.Name, new Vec2( .7f, posy ),
						HorizontalAlign.Left, VerticalAlign.Center );

					int count = (int)( (float)influence.RemainingTime * 2.5f );
					if( count > 50 )
						count = 50;
					string str = "";
					for( int n = 0; n < count; n++ )
						str += "I";

					renderer.AddText( str, new Vec2( .85f, posy ),
						HorizontalAlign.Left, VerticalAlign.Center );

					posy -= .025f;
				}
			}
		}

		void DrawPlayersStatistics( GuiRenderer renderer )
		{
			if( IsCutSceneEnabled() )
				return;

			if( PlayerManager.Instance == null )
				return;

			renderer.AddQuad( new Rect( .1f, .2f, .9f, .8f ), new ColorValue( 0, 0, 1, .5f ) );

			renderer.AddText( "Players statistics", new Vec2( .5f, .25f ),
				HorizontalAlign.Center, VerticalAlign.Center );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				float posy = .3f;

				foreach( PlayerManager.ServerOrSingle_Player player in
					PlayerManager.Instance.ServerOrSingle_Players )
				{
					string text = string.Format( "{0},   Frags: {1},   Ping: {2} ms", player.Name,
						player.Frags, (int)( player.Ping * 1000 ) );
					renderer.AddText( text, new Vec2( .2f, posy ), HorizontalAlign.Left,
						VerticalAlign.Center );

					posy += .025f;
				}
			}

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				float posy = .3f;

				foreach( PlayerManager.Client_Player player in PlayerManager.Instance.Client_Players )
				{
					string text = string.Format( "{0},   Frags: {1},   Ping: {2} ms", player.Name,
						player.Frags, (int)( player.Ping * 1000 ) );
					renderer.AddText( text, new Vec2( .2f, posy ), HorizontalAlign.Left,
						VerticalAlign.Center );

					posy += .025f;
				}
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			//Draw some HUD information
			if( GetPlayerUnit() != null )
			{
				if( GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() &&
					GetActiveObserveCameraArea() == null )
				{
					DrawTarget( renderer );
				}

				DrawPlayerInformation( renderer );

				bool activeConsole = EngineConsole.Instance != null && EngineConsole.Instance.Active;

				if( EngineApp.Instance.IsKeyPressed( EKeys.Tab ) && !activeConsole )
					DrawPlayersStatistics( renderer );

				if( GameNetworkServer.Instance != null || GameNetworkClient.Instance != null )
				{
					renderer.AddText( "\"Tab\" for players statistics", new Vec2( .01f, .1f ),
						HorizontalAlign.Left, VerticalAlign.Top, new ColorValue( 1, 1, 1, .5f ) );
				}
			}

			//Game is paused on server
			if( EntitySystemWorld.Instance.IsClientOnly() && !EntitySystemWorld.Instance.Simulation )
			{
				renderer.AddText( "Game is paused on server", new Vec2( .5f, .5f ),
					HorizontalAlign.Center, VerticalAlign.Center, new ColorValue( 1, 0, 0 ) );
			}
		}

		CameraType GetRealCameraType()
		{
			//Replacement the camera type depending on a current unit.
			Unit playerUnit = GetPlayerUnit();
			if( playerUnit != null )
			{
				//Turret specific
				if( playerUnit as Turret != null )
				{
					if( cameraType == CameraType.FPS )
						return CameraType.TPS;
				}

				//Crane specific
				if( playerUnit as Crane != null )
				{
					if( cameraType == CameraType.TPS )
						return CameraType.FPS;
				}

				//Tank specific
				if( playerUnit as Tank != null )
				{
					if( cameraType == CameraType.FPS )
						return CameraType.TPS;
				}
			}

			return cameraType;
		}

		Unit GetPlayerUnit()
		{
			if( PlayerIntellect.Instance == null )
				return null;
			return PlayerIntellect.Instance.ControlledObject;
		}

		bool SwitchUseStart()
		{
			if( switchUsing )
				return false;

			if( currentSwitch == null )
				return false;

			FloatSwitch floatSwitch = currentSwitch as FloatSwitch;
			if( floatSwitch != null )
				floatSwitch.UseStart();

			GameEntities.BooleanSwitch booleanSwitch = currentSwitch as GameEntities.BooleanSwitch;
			if( booleanSwitch != null )
				booleanSwitch.Press();

			switchUsing = true;

			return true;
		}

		void SwitchUseEnd()
		{
			switchUsing = false;

			if( currentSwitch == null )
				return;

			FloatSwitch floatSwitch = currentSwitch as FloatSwitch;
			if( floatSwitch != null )
				floatSwitch.UseEnd();
		}

		bool CurrentUnitAllowPlayerControlUse()
		{
			if( PlayerIntellect.Instance != null )
			{
				//change player unit
				if( currentSeeUnitAllowPlayerControl != null )
				{
					PlayerIntellect.Instance.TryToChangeMainControlledUnit(
						currentSeeUnitAllowPlayerControl );
					return true;
				}

				//restore player unit
				if( PlayerIntellect.Instance.MainNotActiveUnit != null )
				{
					PlayerIntellect.Instance.TryToRestoreMainControlledUnit();
					return true;
				}
			}
			return false;
		}

		bool IsCutSceneEnabled()
		{
			return CutSceneManager.Instance != null && CutSceneManager.Instance.CutSceneEnable;
		}

		protected override void OnGetCameraTransform( out Vec3 position, out Vec3 forward,
			out Vec3 up, ref Degree cameraFov )
		{
			position = Vec3.Zero;
			forward = Vec3.XAxis;
			up = Vec3.ZAxis;

			Unit unit = GetPlayerUnit();
			if( unit == null )
				return;

			PlayerIntellect.Instance.FPSCamera = false;

			//To use data about orientation the camera if the cut scene is switched on
			if( IsCutSceneEnabled() )
				if( CutSceneManager.Instance.GetCamera( out position, out forward, out up, out cameraFov ) )
					return;

			//To receive orientation the camera if the player is in a observe camera area
			if( GetActiveObserveCameraAreaCameraOrientation( out position, out forward, out up, ref cameraFov ) )
				return;

			Vec3 cameraLookDir = PlayerIntellect.Instance.LookDirection.GetVector();

			switch( GetRealCameraType() )
			{

			case CameraType.TPS:
				{
					float cameraDistance;
					float cameraCenterOffset;

					if( IsPlayerUnitVehicle() )
					{
						cameraDistance = tpsVehicleCameraDistance;
						cameraCenterOffset = tpsVehicleCameraCenterOffset;
					}
					else
					{
						cameraDistance = tpsCameraDistance;
						cameraCenterOffset = tpsCameraCenterOffset;
					}

					PlayerIntellect.Instance.UpdateTransformBeforeCameraPositionCalculation();

					//To calculate orientation of a TPS camera.
					Vec3 lookAt = unit.GetInterpolatedPosition() + new Vec3( 0, 0, cameraCenterOffset );
					Vec3 cameraPos = lookAt - cameraLookDir * cameraDistance;

					RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
						new Ray( lookAt, cameraPos - lookAt ), (int)ContactGroup.CastOnlyContact );
					foreach( RayCastResult result in piercingResult )
					{
						bool ignore = false;

						MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

						if( obj == unit )
							ignore = true;

						if( ( lookAt - result.Position ).LengthSqr() < .001f )
							ignore = true;

						//cut ignore objects here
						//..

						if( !ignore )
						{
							cameraPos = result.Position;
							break;
						}
					}

					position = cameraPos;
					forward = ( lookAt - position ).GetNormalize();
				}
				break;

			case CameraType.FPS:
				{
					//To calculate orientation of a FPS camera.

					PlayerIntellect.Instance.UpdateTransformBeforeCameraPositionCalculation();

					if( unit is Turret )
					{
						//Turret specific
						Gun mainGun = ( (Turret)unit ).MainGun;
						position = mainGun.GetInterpolatedPosition();
						position += unit.Type.FPSCameraOffset * mainGun.GetInterpolatedRotation();
					}
					else if( unit is Tank )
					{
						//Tank specific
						Gun mainGun = ( (Tank)unit ).MainGun;
						position = mainGun.GetInterpolatedPosition();
						position += unit.Type.FPSCameraOffset * mainGun.GetInterpolatedRotation();
					}
					else
					{
						//Characters, etc
						position = unit.GetInterpolatedPosition();
						position += unit.Type.FPSCameraOffset * unit.GetInterpolatedRotation();
					}
					forward = cameraLookDir;
				}
				break;
			}

			//To update data in player intellect about type of the camera
			PlayerIntellect.Instance.FPSCamera = GetRealCameraType() == CameraType.FPS;

			float cameraOffset;
			if( IsPlayerUnitVehicle() )
				cameraOffset = tpsVehicleCameraCenterOffset;
			else
				cameraOffset = tpsCameraCenterOffset;

			PlayerIntellect.Instance.TPSCameraCenterOffset = cameraOffset;

			//zoom for Turret
			if( EngineApp.Instance.IsMouseButtonPressed( EMouseButtons.Right ) )
			{
				if( GetPlayerUnit() as Turret != null )
					if( GetRealCameraType() == CameraType.TPS )
						cameraFov /= 3;
			}
		}

		/// <summary>
		/// Finds observe area in which there is a player.
		/// </summary>
		/// <returns><b>ObserveCameraArea</b>if the player is in area; otherwise <b>null</b>.</returns>
		ObserveCameraArea GetActiveObserveCameraArea()
		{
			Unit unit = GetPlayerUnit();
			if( unit == null )
				return null;

			foreach( ObserveCameraArea area in observeCameraAreas )
			{
				//check invalid area
				if( area.MapCamera == null && area.MapCurve == null )
					continue;

				if( area.GetBox().IsContainsPoint( unit.Position ) )
					return area;
			}
			return null;
		}

		/// <summary>
		/// Finds the nearest point to a map curve.
		/// </summary>
		/// <param name="destPos">The point to which is searched the nearest.</param>
		/// <param name="mapCurve">The map curve.</param>
		/// <returns>The nearest point to a map curve.</returns>
		Vec3 GetNearestPointToMapCurve( Vec3 destPos, MapCurve mapCurve )
		{
			//Calculate cached points
			if( observeCameraMapCurvePoints != mapCurve )
			{
				observeCameraMapCurvePoints = mapCurve;

				observeCameraMapCurvePointsList.Clear();

				float curveLength = 0;
				{
					ReadOnlyCollection<MapCurvePoint> points = mapCurve.Points;
					for( int n = 0; n < points.Count - 1; n++ )
						curveLength += ( points[ n ].Position - points[ n + 1 ].Position ).LengthFast();
				}

				float step = 1.0f / curveLength / 100;
				for( float c = 0; c < 1; c += step )
					observeCameraMapCurvePointsList.Add( mapCurve.CalculateCurvePointByCoefficient( c ) );
			}

			//calculate nearest point
			Vec3 nearestPoint = Vec3.Zero;
			float nearestDistanceSqr = float.MaxValue;

			foreach( Vec3 point in observeCameraMapCurvePointsList )
			{
				float distanceSqr = ( point - destPos ).LengthSqr();
				if( distanceSqr < nearestDistanceSqr )
				{
					nearestPoint = point;
					nearestDistanceSqr = distanceSqr;
				}
			}
			return nearestPoint;
		}

		/// <summary>
		/// Receives orientation of the camera in the observe area of in which there is a player.
		/// </summary>
		/// <param name="position">The camera position.</param>
		/// <param name="forward">The forward vector.</param>
		/// <param name="up">The up vector.</param>
		/// <param name="cameraFov">The camera FOV.</param>
		/// <returns><b>true</b>if the player is in any area; otherwise <b>false</b>.</returns>
		bool GetActiveObserveCameraAreaCameraOrientation( out Vec3 position, out Vec3 forward,
			out Vec3 up, ref Degree cameraFov )
		{
			position = Vec3.Zero;
			forward = Vec3.XAxis;
			up = Vec3.ZAxis;

			ObserveCameraArea area = GetActiveObserveCameraArea();
			if( area == null )
				return false;

			Unit unit = GetPlayerUnit();

			if( area.MapCurve != null )
			{
				Vec3 unitPos = unit.GetInterpolatedPosition();
				Vec3 nearestPoint = GetNearestPointToMapCurve( unitPos, area.MapCurve );

				position = nearestPoint;
				forward = ( unit.GetInterpolatedPosition() - position ).GetNormalize();
				up = Vec3.ZAxis;

				if( area.MapCamera != null && area.MapCamera.Fov != 0 )
					cameraFov = area.MapCamera.Fov;
			}

			if( area.MapCamera != null )
			{
				position = area.MapCamera.Position;
				forward = area.MapCamera.Rotation * new Vec3( 1, 0, 0 );
				up = area.MapCamera.Rotation * new Vec3( 0, 0, 1 );

				if( area.MapCamera.Fov != 0 )
					cameraFov = area.MapCamera.Fov;
			}

			return true;
		}

		bool IsPlayerUnitVehicle()
		{
			Unit playerUnit = GetPlayerUnit();

			//Tank specific
			if( playerUnit as Tank != null )
				return true;

			return false;
		}

		static void ConsoleCommand_MovePlayerUnitToCamera( string arguments )
		{
			if( Map.Instance == null )
				return;
			if( PlayerIntellect.Instance == null )
				return;

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				Log.Warning( "You cannot to do it on the client." );
				return;
			}

			Unit unit = PlayerIntellect.Instance.ControlledObject;
			if( unit == null )
				return;

			Ray lookRay = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
				new Vec2( .5f, .5f ) );

			RayCastResult result = PhysicsWorld.Instance.RayCast(
				lookRay, (int)ContactGroup.CastOnlyContact );

			if( result.Shape != null )
				unit.Position = result.Position + new Vec3( 0, 0, unit.MapBounds.GetSize().Z );
		}

		void GameControlsManager_GameControlsEvent( GameControlsEventData e )
		{
			//GameControlsKeyDownEventData
			{
				GameControlsKeyDownEventData evt = e as GameControlsKeyDownEventData;
				if( evt != null )
				{
					//"Use" control key
					if( evt.ControlKey == GameControlKeys.Use )
					{
						//currentAttachedGuiObject
						if( currentAttachedGuiObject != null )
						{
							currentAttachedGuiObject.ControlManager.DoMouseDown( EMouseButtons.Left );
							return;
						}

						//key down for switch use
						if( SwitchUseStart() )
							return;

						if( CurrentUnitAllowPlayerControlUse() )
							return;
					}

					return;
				}
			}

			//GameControlsKeyUpEventData
			{
				GameControlsKeyUpEventData evt = e as GameControlsKeyUpEventData;
				if( evt != null )
				{
					//"Use" control key
					if( evt.ControlKey == GameControlKeys.Use )
					{
						//currentAttachedGuiObject
						if( currentAttachedGuiObject != null )
							currentAttachedGuiObject.ControlManager.DoMouseUp( EMouseButtons.Left );

						//key up for switch use
						SwitchUseEnd();
					}

					return;
				}
			}
		}

	}
}
