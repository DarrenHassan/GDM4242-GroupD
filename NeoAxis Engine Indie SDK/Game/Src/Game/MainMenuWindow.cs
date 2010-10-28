// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.SoundSystem;
using GameCommon;
using GameEntities;
using Engine.FileSystem;

namespace Game
{
	/// <summary>
	/// Defines a main menu.
	/// </summary>
	public class MainMenuWindow : EControl
	{
		EControl window;
		ETextBox versionTextBox;
        EListBox listBox;

		Map mapInstance;

        //float MapPos= 0.0f;   
        float Phi = 0.0f; 
        bool Faction = false;//default faction red = false
        bool MapSelect = false;//default faction red = false
        bool Transition = false;
        float Theta = 0.0f;
        bool Transit = false;
        //float step = 0.02f;// MAKE SURE STEPSNCAN GET TO TARGET
        float step = 0.040f;// MAKE SURE STEPSNCAN GET TO TARGET
        float step_2 = 0.075f;
        string mapName = null;
		///////////////////////////////////////////

		/// <summary>
		/// Creates a window of the main menu and creates the background world.
		/// </summary>
		protected override void OnAttach()
		{
			base.OnAttach();

			//create main menu window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\AntMain.gui" );

            string[] mapList = VirtualDirectory.GetFiles("Maps\\AntRTS", "*.map", SearchOption.AllDirectories);

			window.ColorMultiplier = new ColorValue( 1, 1, 1, 0 );
			Controls.Add( window );

			//no shader model 2 warning
			//if( window.Controls[ "NoShaderModel2" ] != null )
				//window.Controls[ "NoShaderModel2" ].Visible = !RenderSystem.Instance.HasShaderModel2();

			//button handlers
			//( (EButton)window.Controls[ "Run" ] ).Click += Run_Click;

			//( (EButton)window.Controls[ "Multiplayer" ] ).Click += Multiplayer_Click;
            ((EButton)window.Controls["ToggleFaction"]).Click += ToggleFaction_Click;
            ((EButton)window.Controls["SelectFaction"]).Click += Faction_Click;
            ((EButton)window.Controls["Back"]).Click += Back_Click;
			//add version info control
			versionTextBox = new ETextBox();
			versionTextBox.TextHorizontalAlign = HorizontalAlign.Left;
			versionTextBox.TextVerticalAlign = VerticalAlign.Bottom;
			versionTextBox.Text = "Version " + EngineVersionInformation.Version;
			versionTextBox.ColorMultiplier = new ColorValue( 1, 1, 1, 0 );

			Controls.Add( versionTextBox );

            //maps listBox
            
                listBox = (EListBox)window.Controls["MapSelect"];

                foreach (string name in mapList)
                {
                    listBox.Items.Add(name);
                    if (Map.Instance != null)
                    {
                        if (string.Compare(name.Replace('/', '\\'),
                            Map.Instance.VirtualFileName.Replace('/', '\\'), true) == 0)
                            listBox.SelectedIndex = listBox.Items.Count - 1;
                    }
                }

                listBox.SelectedIndexChange += listBox_SelectedIndexChanged;
                if (listBox.Items.Count != 0 && listBox.SelectedIndex == -1)
                    listBox.SelectedIndex = 0;
                if (listBox.Items.Count != 0)
                    listBox_SelectedIndexChanged(null);

                listBox.ItemMouseDoubleClick += delegate(object sender, EListBox.ItemMouseEventArgs e)
                {
                    GameEngineApp.Instance.SetNeedMapLoad((string)e.Item);
                };
            
           

            ((EButton)window.Controls["Run"]).Click += delegate(EButton sender)
            {
                if (listBox.SelectedIndex != -1)
                    GameEngineApp.Instance.SetNeedMapLoad((string)listBox.SelectedItem);
            };

			//play background music
			GameMusic.MusicPlay( "Sounds\\Music\\Bumps.ogg", true );

			//update sound listener
			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			//create the background world
			CreateMap();

			ResetTime();
		}
        void Faction_Click( EButton sender )
        {
            //GameEntities.RTS_Specific.GenericAntCharacter.
            //GameEntities.RTSUnit.Equals["Warrior_1"];
            MapSelect = true;
            Transition = true;
            window.Controls["SelectFaction"].Enable = false;
            foreach (Entity entity in Map.Instance.Children)
            {
                GameEntities.RTS_Specific.GenericAntCharacter warrioir = entity as GameEntities.RTS_Specific.GenericAntCharacter;
                if (warrioir != null)
                {
                    warrioir.fighting = true;

                }
            }
            //UpdateBaseAnimation("Attack", true, true, 1);
        }
        void Back_Click(EButton sender)
        {
            MapSelect = false;
            Transition = true;
            window.Controls["Back"].Enable = false;
        }

		//void Run_Click( EButton sender )
		//{
        //    GameEngineApp.Instance.SetNeedMapLoad("Maps\\SmallAntMap\\Map.map");
		//}
        void ToggleFaction_Click(EButton sender)
        {
            Faction = !Faction;
            Transit = true;
            window.Controls["SelectFaction"].Enable = false;
            window.Controls["ToggleFaction"].Enable = false;
            window.Controls["Run"].Enable = false;


        }
        void listBox_SelectedIndexChanged(object sender)
        {
            Texture texture = null;

            if (listBox.SelectedIndex != -1)
            {
                string mapName = (string)listBox.SelectedItem;
                    string mapDirectory = Path.GetDirectoryName(mapName);
                    string textureName = mapDirectory + "\\Description\\Preview";

                    string textureFileName = null;

                    bool found = false;

                    string[] extensions = new string[] { "dds", "tga", "png", "jpg" };
                    foreach (string extension in extensions)
                    {
                        textureFileName = textureName + "." + extension;
                        if (VirtualFile.Exists(textureFileName))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        texture = TextureManager.Instance.Load(textureFileName);
                
            }

            window.Controls["Preview"].Controls["TexturePlacer"].BackTexture = texture;
        }
        /*
		void Multiplayer_Click( EButton sender )
		{
			if( EngineApp.Instance.WebPlayerMode )
			{
				Log.Warning( "Networking is not supported for web player at this time." );
				return;
			}

			Controls.Add( new MultiplayerLoginWindow() );
		}
        */

		/// <summary>
		/// Destroys the background world at closing the main menu.
		/// </summary>
        /// 
		protected override void OnDetach()
		{
			//destroy the background world
			DestroyMap();

			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			/*if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.Escape )
			{
				Controls.Add( new MenuWindow() );
				return true;
			}

			return false;*/
            if( base.OnKeyDown( e ) )
                return true;

            if( e.Key == EKeys.Escape )
            {
                Controls.Add(new AntMenuWindow());
                return true;
            }

            return false;
     
        }

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//Change window transparency
			{
				float alpha = 0;

				if( Time > 2 && Time <= 4 )
					alpha = ( Time - 2 ) / 2;
				else if( Time > 4 )
					alpha = 1;

				window.ColorMultiplier = new ColorValue( 1, 1, 1, alpha );
				versionTextBox.ColorMultiplier = new ColorValue( 1, 1, 1, alpha );
			}

			//Change pictures
			/*{
				const int imagePageCount = 7;
				float period = 6 * imagePageCount;

				float t = Time % period;

				for( int n = 1; ; n++ )
				{
					EControl control = window.Controls[ "Picture" + n.ToString() ];
					if( control == null )
						break;

					float a = 3 + t / 2 - n * 3;
					MathFunctions.Clamp( ref a, 0, 1 );
					if( t > period - 2 )
					{
						float a2 = ( period - t ) / 2;
						a = Math.Min( a, a2 );

						if( window.Controls[ "Picture" + ( n + 1 ).ToString() ] != null )
							a = 0;
					}
					control.BackColor = new ColorValue( 1, 1, 1, a );
				}
			}*/

			//update sound listener
			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			//Tick a background world
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.Tick();
		}

		protected override void OnRender()
		{
			base.OnRender();






            //RTSUnit ControlledObject = ;
            
            //ControlledObject.Name = "Warrior_1";

            //RTSUnit controlledObj = ControlledObject;


            //RTSUnit ControlledObj = ;
            //GameEntities.RTS_Specific.GenericAntCharacter warrior = ControlledObj as GameEntities.RTS_Specific.GenericAntCharacter;// = controlledObj as GenericAntCharacter;
            //warrior.fighting = true; 
                //.GenericAntCharacter warrior = controlledObj as GenericAntCharacter;  
            //GameEntities.RTSUnit.OnUpdateBaseAnimation();
			//Update camera orientation
			if( Map.Instance != null )
			{
			/*	float dir = Time / 10.0f;

				Vec3 from = new Vec3(
					MathFunctions.Cos( dir ) * 29.0f / 1.5f,
					MathFunctions.Sin( dir * 1.50f ) * 10.0f / 1.5f,
					( MathFunctions.Cos( dir * 1.2f ) + 1.4f ) * 17.0f / 1.5f );
				Vec3 to = Vec3.Zero;
				float fov = 80;

				Camera camera = RendererWorld.Instance.DefaultCamera;
				camera.NearClipDistance = Map.Instance.NearFarClipDistance.Minimum;
				camera.FarClipDistance = Map.Instance.NearFarClipDistance.Maximum;
				camera.FixedUp = Vec3.ZAxis;
				camera.Fov = fov;
				camera.Position = from;
				camera.LookAt( to );
			*/
                //blue camera position
                float X = 3.5f;
                float Y = 2;
                float Z = 3;
                EControl Redcontrol = window.Controls[ "RedInfo" ];
                EControl Bluecontrol = window.Controls["BlueInfo"];
                EControl Factioncontrol = window.Controls["SelectFaction"];
                EControl Backcontrol = window.Controls["Back"];
                EControl Togglecontrol = window.Controls["ToggleFaction"];
                EControl Mapcontrol = window.Controls["MapSelect"];
                EControl Previewcontrol = window.Controls["Preview"];
                EControl FactionHeadcontrol = window.Controls["FactionHead"];
                EControl MapHeadcontrol = window.Controls["MapHead"];
                EControl Runcontrol = window.Controls["Run"];
                //theta = 0 for red and pi for blue faction = 0 for red 1 for blue, fuck green
                if(Transit){
                    if(!Faction){
                        Theta -= step;
                        if (Theta <= 0.0f)
                        {
                            Theta = 0.0f;
                            Transit = false;
                            window.Controls["ToggleFaction"].Enable = true;
                            window.Controls["SelectFaction"].Enable = true;
                            window.Controls["Run"].Enable = true;
                        }
                    }
                    if (Faction){
                        Theta += step;
                        if (Theta >= 3.14f)
                        {
                            Theta = 3.1416f;
                            Transit = false;
                            window.Controls["ToggleFaction"].Enable = true;
                            window.Controls["SelectFaction"].Enable = true;
                            window.Controls["Run"].Enable = true;
                        }
                    }



                    //control.BackColor = new ColorValue(1, 1, 1, 1);
                    //Vec2 pos = new Vec2(0.0f,0.0f);
                    //ScaleValue posit = new ScaleValue(ScaleType.Parent, pos);
                //control.Position = posit;
                    //control.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.0f, 0.0f));
                //window.Controls["RedInfo"].Position
                }


                if (Transition)
                {
                    if (!MapSelect)
                    {
                        Phi -= step_2;
                        if (Phi <= 0.0f)
                        {
                            Phi = 0.0f;
                            Transition = false;
                            window.Controls["ToggleFaction"].Enable = true;
                            window.Controls["SelectFaction"].Enable = true;
                            window.Controls["Run"].Enable = true;
                            window.Controls["SelectFaction"].Enable = true;
                        }
                    }
                    if (MapSelect)
                    {
                        Phi += step_2;
                        if (Phi >= 3.14f)
                        {
                            Phi = 3.1416f;
                            Transition = false;
                            window.Controls["ToggleFaction"].Enable = true;
                            window.Controls["SelectFaction"].Enable = true;
                            window.Controls["Run"].Enable = true;
                            window.Controls["Back"].Enable = true;
                            foreach (Entity entity in Map.Instance.Children)
                            {
                                GameEntities.RTS_Specific.GenericAntCharacter warrioir = entity as GameEntities.RTS_Specific.GenericAntCharacter;
                                if (warrioir != null)
                                {
                                    warrioir.fighting = false;

                                }
                            }
                        }
                    }
                }

                if (Theta < 1.57f)
                {
                    window.Controls["RedInfo"].Visible = true;
                    window.Controls["BlueInfo"].Visible = false;
                }
                if (Theta > 1.57f)
                {
                    window.Controls["RedInfo"].Visible = false;
                    window.Controls["BlueInfo"].Visible = true;
                }
                //(1-.5-cos(x*2)*.5).^2)
                //change power and scale to make this faster/slower to make this faster 
                Redcontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + (1.0f - (0.5f + MathFunctions.Cos(Phi) * 0.5f)) + 2.0f * MathFunctions.Pow(1.0f - (0.5f + MathFunctions.Cos(2.0f * Theta) * 0.5f), 2f), 0.0f));
                Bluecontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + (1.0f - (0.5f + MathFunctions.Cos(Phi) * 0.5f)) + 2.0f * MathFunctions.Pow(1.0f - (0.5f + MathFunctions.Cos(2.0f * Theta) * 0.5f), 2f), 0.0f));
                Factioncontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + (1.0f - (0.5f + MathFunctions.Cos(Phi) * 0.5f)) + 2.0f * MathFunctions.Pow(1.0f - (0.5f + MathFunctions.Cos(2.0f * Theta) * 0.5f), 2f), 0.667f));
                Backcontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + ((0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.667f));
                Togglecontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + (1.0f - (0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.7291667f));

                Mapcontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + ((0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.1432292f));
                Previewcontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.6445313f + ((0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.4817708f));
                
                FactionHeadcontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(-1.0f + ((0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.0f));
                MapHeadcontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.0f -((0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.0f));

                Runcontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + ((0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.7291666f));
                //Redcontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + 2.0f * (1.0f - (0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.0f));
                //Bluecontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + 2.0f * (1.0f - (0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.0f));
                //Factioncontrol.Position = new ScaleValue(ScaleType.Parent, new Vec2(0.625f + 2.0f * (1.0f - (0.5f + MathFunctions.Cos(Phi) * 0.5f)), 0.667f));

                float ang_0 = 0.1206f;//6.911f;//deg in rad is 0.1206

                float cam_ang = 1.5708f*(0.5f+MathFunctions.Cos(Theta)*0.5f);
                    
               
                //float Rfrom = 16.62f;//20.0f - X;
                //float Rfrom = 16.62f - 7f * MathFunctions.Pow(0.2f,(0.5f + MathFunctions.Sin(Theta)*0.5f));

                //float Rfrom = 16.62f + 7f * MathFunctions.Pow(0.2f, 1.0f - (0.5f + MathFunctions.Cos(2.0f * Theta) * 0.5f));
                
                //change from plus or minus radius
                //float Rfrom = 16.62f + 10f * MathFunctions.Pow(1.0f - (0.5f + MathFunctions.Cos(2.0f * Theta) * 0.5f),0.2f);
                float Rfrom = 16.62f + 10f * MathFunctions.Pow(1.0f - (0.5f + MathFunctions.Cos(2.0f * Theta) * 0.5f), 0.2f);
          
                //(1-(.5*cos(x*2)+.5)).^.2

                float Rto = 20.0f;
                

                Vec3 from = new Vec3(
                    20-Rfrom*MathFunctions.Cos(ang_0+cam_ang),
                    Rfrom * MathFunctions.Sin(ang_0 + cam_ang),
                    Z);

                Vec3 to = new Vec3(
                    20 - Rto * MathFunctions.Cos(cam_ang),
                    Rto * MathFunctions.Sin(cam_ang),
                    1);
                float fov = 80;

                Camera camera = RendererWorld.Instance.DefaultCamera;
                camera.NearClipDistance = Map.Instance.NearFarClipDistance.Minimum;
                camera.FarClipDistance = Map.Instance.NearFarClipDistance.Maximum;
                camera.FixedUp = Vec3.ZAxis;
                camera.Fov = fov;
                camera.Position = from;
                camera.LookAt(to);


             }
		}

		/// <summary>
		/// Creates the background world.
		/// </summary>
		void CreateMap()
		{
			WorldType worldType = EntityTypes.Instance.GetByName( "SimpleWorld" ) as WorldType;
			if( worldType == null )
				Log.Fatal( "MainMenuWindow: CreateMap: \"SimpleWorld\" type is not exists." );

			if( !GameEngineApp.Instance.ServerOrSingle_MapLoad( "Maps\\AntMainMenu\\Map.map", worldType, true ) )
				return;

			mapInstance = Map.Instance;

			EntitySystemWorld.Instance.Simulation = true;
		}

		/// <summary>
		/// Destroys the background world.
		/// </summary>
		void DestroyMap()
		{
			if( mapInstance == Map.Instance )
			{
				MapSystemWorld.MapDestroy();
				EntitySystemWorld.Instance.WorldDestroy();
			}
		}
	}


}
