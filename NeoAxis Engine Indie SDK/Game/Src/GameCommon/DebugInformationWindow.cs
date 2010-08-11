// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.PhysicsSystem;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.SoundSystem;
using Engine.Networking;

namespace GameCommon
{
	/// <summary>
	/// Defines a debug information window class.
	/// </summary>
	public class DebugInformationWindow : EControl
	{
		static DebugInformationWindow instance;

		[Config( "DebugInformationWindow", "background" )]
		static bool background;

		EControl window;
		ECheckBox backgroundCheckBox;
		List<Page> pages = new List<Page>();

		///////////////////////////////////////////

		public static DebugInformationWindow Instance
		{
			get { return instance; }
		}

		public bool Background
		{
			get { return background; }
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			instance = this;

			window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\DebugInformationWindow.gui" );
			Controls.Add( window );

			backgroundCheckBox = (ECheckBox)window.Controls[ "Background" ];
			backgroundCheckBox.Checked = background;
			backgroundCheckBox.CheckedChange += Background_CheckedChange;

			( (EButton)window.Controls[ "Close" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};

			EControl tabControl = window.Controls[ "TabControl" ];

			pages.Add( new GeneralPage( tabControl.Controls[ "General" ] ) );
			pages.Add( new RenderPage( tabControl.Controls[ "Render" ] ) );
			pages.Add( new PhysicsPage( tabControl.Controls[ "Physics" ] ) );
			pages.Add( new SoundPage( tabControl.Controls[ "Sound" ] ) );
			pages.Add( new EntitiesPage( tabControl.Controls[ "Entities" ] ) );
			pages.Add( new MemoryPage( tabControl.Controls[ "Memory" ] ) );
			pages.Add( new DLLPage( tabControl.Controls[ "DLL" ] ) );
			pages.Add( new NetworkPage( tabControl.Controls[ "Network" ] ) );

			UpdateColorMultiplier();
		}

		protected override void OnDetach()
		{
			foreach( Page page in pages )
				page.OnDestroy();
			pages.Clear();

			instance = null;

			base.OnDetach();
		}

		void Background_CheckedChange( ECheckBox sender )
		{
			background = backgroundCheckBox.Checked;
			UpdateColorMultiplier();
		}

		void UpdateColorMultiplier()
		{
			if( background )
				ColorMultiplier = new ColorValue( 1, 1, 1, .5f );
			else
				ColorMultiplier = new ColorValue( 1, 1, 1 );
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			ETabControl tabControl = (ETabControl)window.Controls[ "TabControl" ];

			if( e.Key == EKeys.B )
				backgroundCheckBox.Checked = !backgroundCheckBox.Checked;

			return false;
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			foreach( Page page in pages )
			{
				if( page.PageControl.Visible )
					page.OnUpdate();
			}
		}

		///////////////////////////////////////////

		abstract class Page
		{
			EControl pageControl;

			public Page( EControl pageControl )
			{
				this.pageControl = pageControl;
			}

			public EControl PageControl
			{
				get { return pageControl; }
			}

			public abstract void OnUpdate();
			public virtual void OnDestroy() { }
		}

		///////////////////////////////////////////

		class GeneralPage : Page
		{
			class StatisticsInfoItem
			{
				public string cameraInformation;
				public int ownerCameraIdentifier;

				public override string ToString()
				{
					return cameraInformation;
				}
			}

			///////////////

			public GeneralPage( EControl pageControl )
				: base( pageControl )
			{
			}

			public override void OnUpdate()
			{
				RenderStatisticsInfo statistics = RendererWorld.Instance.Statistics;

				PageControl.Controls[ "Triangles" ].Text = statistics.Triangles.ToString( "N0" );
				PageControl.Controls[ "Batches" ].Text = statistics.Batches.ToString( "N0" );
				PageControl.Controls[ "Lights" ].Text = statistics.Lights.ToString( "N0" );

				//performance counter
				{
					float otherTime = 0;

					foreach( PerformanceCounter.Counter counter in PerformanceCounter.Counters )
					{
						PerformanceCounter.TimeCounter timeCounter =
							counter as PerformanceCounter.TimeCounter;

						if( timeCounter != null )
						{
							string counterNameWithoutSpaces = counter.Name.Replace( " ", "" );

							EControl timeControl = PageControl.Controls[
								counterNameWithoutSpaces + "Time" ];
							EControl fpsControl = PageControl.Controls[
								counterNameWithoutSpaces + "FPS" ];

							if( timeControl != null )
							{
								timeControl.Text = ( timeCounter.CalculatedValue * 1000.0f ).
									ToString( "F2" );
							}
							if( fpsControl != null )
								fpsControl.Text = ( 1.0f / timeCounter.CalculatedValue ).ToString( "F1" );

							if( !counter.InnerCounter )
							{
								if( counter == PerformanceCounter.TotalTimeCounter )
									otherTime += timeCounter.CalculatedValue;
								else
									otherTime -= timeCounter.CalculatedValue;
							}
						}
					}

					{
						ETextBox timeControl = PageControl.Controls[ "OtherTime" ] as ETextBox;
						ETextBox fpsControl = PageControl.Controls[ "OtherFPS" ] as ETextBox;

						if( timeControl != null )
							timeControl.Text = ( otherTime * 1000.0f ).ToString( "F2" );
						if( fpsControl != null )
							fpsControl.Text = ( 1.0f / otherTime ).ToString( "F1" );
					}
				}

				//cameras
				{
					EListBox camerasListBox = (EListBox)PageControl.Controls[ "Cameras" ];

					//update cameras list
					{
						StatisticsInfoItem lastSelectedItem = camerasListBox.SelectedItem as
							StatisticsInfoItem;

						camerasListBox.Items.Clear();

						foreach( RenderStatisticsInfo.CameraStatistics cameraStatistics in
							statistics.CamerasStatistics )
						{
							StatisticsInfoItem item = new StatisticsInfoItem();
							item.cameraInformation = cameraStatistics.CameraInformation;
							item.ownerCameraIdentifier = cameraStatistics.OwnerCameraIdentifier;
							camerasListBox.Items.Add( item );

							if( lastSelectedItem != null )
							{
								if( item.cameraInformation == lastSelectedItem.cameraInformation &&
									item.ownerCameraIdentifier == lastSelectedItem.ownerCameraIdentifier )
								{
									camerasListBox.SelectedIndex = camerasListBox.Items.Count - 1;
								}
							}
						}

						camerasListBox.Items.Add( "Total" );
						if( lastSelectedItem != null && lastSelectedItem.cameraInformation == "Total" )
							camerasListBox.SelectedIndex = camerasListBox.Items.Count - 1;

						if( camerasListBox.SelectedIndex == -1 )
							camerasListBox.SelectedIndex = camerasListBox.Items.Count - 1;
					}

					//update camera info
					if( camerasListBox.SelectedIndex == camerasListBox.Items.Count - 1 )
					{
						//total statistics

						int staticMeshObjects = 0;
						int sceneNodes = 0;
						int guiRenderers = 0;
						int guiBatches = 0;
						int triangles = 0;
						int batches = 0;

						foreach( RenderStatisticsInfo.CameraStatistics cameraStatistics in
							statistics.CamerasStatistics )
						{
							staticMeshObjects += cameraStatistics.StaticMeshObjects;
							sceneNodes += cameraStatistics.SceneNodes;
							guiRenderers += cameraStatistics.GuiRenderers;
							guiBatches += cameraStatistics.GuiBatches;
							triangles += cameraStatistics.Triangles;
							batches += cameraStatistics.Batches;
						}

						PageControl.Controls[ "CameraOutdoorWalks" ].Text = "No camera";
						PageControl.Controls[ "CameraPortalsPassed" ].Text = "No camera";
						PageControl.Controls[ "CameraZonesPassed" ].Text = "No camera";

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text =
							staticMeshObjects.ToString( "N0" );
						PageControl.Controls[ "CameraSceneNodes" ].Text = sceneNodes.ToString( "N0" );
						PageControl.Controls[ "CameraGuiRenderers" ].Text = guiRenderers.ToString( "N0" );
						PageControl.Controls[ "CameraGuiBatches" ].Text = guiBatches.ToString( "N0" );
						PageControl.Controls[ "CameraTriangles" ].Text = triangles.ToString( "N0" );
						PageControl.Controls[ "CameraBatches" ].Text = batches.ToString( "N0" );

					}
					else if( camerasListBox.SelectedIndex != -1 )
					{
						//selected camera statistics

						RenderStatisticsInfo.CameraStatistics activeCameraStatistics =
							statistics.CamerasStatistics[ camerasListBox.SelectedIndex ];

						RenderStatisticsInfo.CameraStatistics.PortalSystemInfo portalSystemInfo =
							activeCameraStatistics.PortalSystem;
						if( portalSystemInfo != null )
						{
							PageControl.Controls[ "CameraOutdoorWalks" ].Text =
								portalSystemInfo.OutdoorWalks.ToString( "N0" );
							PageControl.Controls[ "CameraPortalsPassed" ].Text =
								portalSystemInfo.PortalsPassed.ToString( "N0" );
							PageControl.Controls[ "CameraZonesPassed" ].Text =
								portalSystemInfo.ZonesPassed.ToString( "N0" );
						}
						else
						{
							PageControl.Controls[ "CameraOutdoorWalks" ].Text = "No zones";
							PageControl.Controls[ "CameraPortalsPassed" ].Text = "No zones";
							PageControl.Controls[ "CameraZonesPassed" ].Text = "No zones";
						}

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text =
							activeCameraStatistics.StaticMeshObjects.ToString( "N0" );
						PageControl.Controls[ "CameraSceneNodes" ].Text =
							activeCameraStatistics.SceneNodes.ToString( "N0" );
						PageControl.Controls[ "CameraGuiRenderers" ].Text =
							activeCameraStatistics.GuiRenderers.ToString( "N0" );
						PageControl.Controls[ "CameraGuiBatches" ].Text =
							activeCameraStatistics.GuiBatches.ToString( "N0" );
						PageControl.Controls[ "CameraTriangles" ].Text =
							activeCameraStatistics.Triangles.ToString( "N0" );
						PageControl.Controls[ "CameraBatches" ].Text =
							activeCameraStatistics.Batches.ToString( "N0" );
					}
					else
					{
						//no camera selected

						PageControl.Controls[ "CameraOutdoorWalks" ].Text = "";
						PageControl.Controls[ "CameraPortalsPassed" ].Text = "";
						PageControl.Controls[ "CameraZonesPassed" ].Text = "";

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text = "";
						PageControl.Controls[ "CameraSceneNodes" ].Text = "";
						PageControl.Controls[ "CameraGuiRenderers" ].Text = "";
						PageControl.Controls[ "CameraGuiBatches" ].Text = "";
						PageControl.Controls[ "CameraTriangles" ].Text = "";
						PageControl.Controls[ "CameraBatches" ].Text = "";
					}
				}
			}
		}

		///////////////////////////////////////////

		class RenderPage : Page
		{
			public RenderPage( EControl pageControl )
				: base( pageControl )
			{
				PageControl.Controls[ "Library" ].Text = string.Format( "{0} ({1})",
					RenderSystem.Instance.Name, RenderSystem.Instance.DllFileName );

				string gpuSyntaxes = "";
				foreach( string gpuSyntax in GpuProgramManager.Instance.SupportedSyntaxes )
				{
					if( gpuSyntaxes != "" )
						gpuSyntaxes += " ";
					gpuSyntaxes += gpuSyntax;
				}
				PageControl.Controls[ "GPUSyntaxes" ].Text = gpuSyntaxes;
			}

			public override void OnUpdate()
			{
				//Textures
				{
					uint totalCount;
					uint totalSize;
					uint loadedCount;
					uint loadedSize;
					uint compressedLoadedCount;
					uint compressedLoadedSize;
					uint uncompressedLoadedCount;
					uint uncompressedLoadedSize;
					uint manuallyCreatedCount;
					uint manuallyCreatedSize;
					uint renderTargetCount;
					uint renderTargetSize;

					TextureManager.Instance.GetStatistics(
						out totalCount, out totalSize,
						out loadedCount, out loadedSize,
						out compressedLoadedCount, out compressedLoadedSize,
						out uncompressedLoadedCount, out uncompressedLoadedSize,
						out manuallyCreatedCount, out manuallyCreatedSize,
						out renderTargetCount, out renderTargetSize );

					PageControl.Controls[ "TexturesTotalCount" ].Text = totalCount.ToString();
					PageControl.Controls[ "TexturesTotalSize" ].Text =
						( (double)totalSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesLoadedCount" ].Text = loadedCount.ToString();
					PageControl.Controls[ "TexturesLoadedSize" ].Text =
						( (double)loadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesCompressedLoadedCount" ].Text =
						compressedLoadedCount.ToString();
					PageControl.Controls[ "TexturesCompressedLoadedSize" ].Text =
						( (double)compressedLoadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesUncompressedLoadedCount" ].Text =
						uncompressedLoadedCount.ToString();
					PageControl.Controls[ "TexturesUncompressedLoadedSize" ].Text =
						( (double)uncompressedLoadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesManuallyCreatedCount" ].Text =
						manuallyCreatedCount.ToString();
					PageControl.Controls[ "TexturesManuallyCreatedSize" ].Text =
						( (double)manuallyCreatedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesRenderTargetCount" ].Text =
						renderTargetCount.ToString();
					PageControl.Controls[ "TexturesRenderTargetSize" ].Text =
						( (double)renderTargetSize / 1024 / 1024 ).ToString( "F2" );
				}

				//Meshes
				{
					uint count;
					uint size;

					MeshManager.Instance.GetStatistics( out count, out size );

					PageControl.Controls[ "MeshesCount" ].Text = count.ToString();
					PageControl.Controls[ "MeshesSize" ].Text =
						( (double)size / 1024 / 1024 ).ToString( "F2" );
				}

				//GPU programs
				{
					PageControl.Controls[ "GPUPrograms" ].Text =
						GpuProgramManager.Instance.Programs.Count.ToString();

					int generatedAtRuntime = 0;
					int loadedFromCache = 0;
					foreach( GpuProgram gpuProgram in GpuProgramManager.Instance.Programs )
					{
						if( gpuProgram.HasLoadedFromShaderCache() )
							loadedFromCache++;
						else
							generatedAtRuntime++;
					}

					PageControl.Controls[ "GPUProgramsGeneratedAtRuntime" ].Text =
						generatedAtRuntime.ToString( "N0" );
					PageControl.Controls[ "GPUProgramsLoadedFromCache" ].Text =
						loadedFromCache.ToString( "N0" );

					if( RenderSystem.Instance.HasShaderModel2() && loadedFromCache == 0 )
					{
						PageControl.Controls[ "GPUProgramsLoadedFromCache" ].ColorMultiplier =
							new ColorValue( 1, 0, 0 );
					}
				}

				PageControl.Controls[ "HighLevelMaterials" ].Text =
					HighLevelMaterialManager.Instance.Materials.Count.ToString();

				PageControl.Controls[ "SceneGraph" ].Text =
					SceneManager.Instance._SceneGraph.Type.ToString();
				PageControl.Controls[ "SceneNodes" ].Text =
					SceneManager.Instance.SceneNodes.Count.ToString( "N0" );
				PageControl.Controls[ "StaticMeshObjects" ].Text =
					SceneManager.Instance.StaticMeshObjects.Count.ToString( "N0" );

				//Shader cache
				{
					OnlyForLoadShaderCacheManager manager = RendererWorld.ShaderCacheManager as
						OnlyForLoadShaderCacheManager;
					if( manager != null )
					{
						PageControl.Controls[ "ShaderCacheFileName" ].Text =
							Path.GetFileName( manager.UsedFileName );
						PageControl.Controls[ "ShaderCacheProgramCount" ].Text =
							manager.ProgramCount.ToString( "N0" );
					}
					else
					{
						PageControl.Controls[ "ShaderCacheFileName" ].Text = "";
						PageControl.Controls[ "ShaderCacheProgramCount" ].Text = "";
					}
				}
			}
		}

		///////////////////////////////////////////

		class PhysicsPage : Page
		{
			public PhysicsPage( EControl pageControl )
				: base( pageControl )
			{
				PageControl.Controls[ "Library" ].Text = string.Format( "{0} ({1})",
					PhysicsWorld.Instance.DriverName, PhysicsWorld.Instance.DriverAssemblyFileName );

				ECheckBox hardwareAccelecatedCheckBox = (ECheckBox)PageControl.Controls[
					"HardwareAccelerated" ];
				hardwareAccelecatedCheckBox.Enable = false;
				hardwareAccelecatedCheckBox.Checked = PhysicsWorld.Instance.HardwareAccelerated;
			}

			public override void OnUpdate()
			{
				PageControl.Controls[ "Bodies" ].Text =
					PhysicsWorld.Instance.Bodies.Count.ToString( "N0" );
				PageControl.Controls[ "Joints" ].Text =
					PhysicsWorld.Instance.Joints.Count.ToString( "N0" );
				PageControl.Controls[ "Motors" ].Text =
					PhysicsWorld.Instance.Motors.Count.ToString( "N0" );
			}
		}

		///////////////////////////////////////////

		class SoundPage : Page
		{
			public SoundPage( EControl pageControl )
				: base( pageControl )
			{
				EComboBox comboBox;

				string libraryText = SoundWorld.Instance.DriverName;
				if( SoundWorld.Instance.DriverName != "NULL" )
					libraryText += string.Format( " ({0})", SoundWorld.Instance.DriverAssemblyFileName );
				PageControl.Controls[ "Library" ].Text = libraryText;

				comboBox = (EComboBox)PageControl.Controls[ "InformationType" ];
				comboBox.Items.Add( "Loaded sounds" );
				comboBox.Items.Add( "Virtual channels" );
				comboBox.Items.Add( "Real channels" );
				comboBox.SelectedIndex = 0;
			}

			public override void OnUpdate()
			{
				int informationTypeIndex = ( (EComboBox)PageControl.Controls[ "InformationType" ] ).
					SelectedIndex;

				StringBuilder text = new StringBuilder( "" );

				if( informationTypeIndex == 0 )
				{
					//loaded sounds

					text.AppendFormat( "Count: {0}\n", SoundWorld.Instance.Sounds.Count );
					text.Append( "\n" );

					foreach( Sound sound in SoundWorld.Instance.Sounds )
						text.AppendFormat( "{0}\n", sound );
				}
				else if( informationTypeIndex == 1 )
				{
					//virtual channels

					int activeChannelCount = SoundWorld.Instance.ActiveVirtual2DChannels.Count +
						SoundWorld.Instance.ActiveVirtual3DChannels.Count;

					text.AppendFormat( "Active channels: {0}\n", activeChannelCount );
					text.Append( "\n" );

					for( int nChannels = 0; nChannels < 2; nChannels++ )
					{
						IEnumerable<VirtualChannel> activeChannels = nChannels == 0 ?
							SoundWorld.Instance.ActiveVirtual2DChannels :
							SoundWorld.Instance.ActiveVirtual3DChannels;

						foreach( VirtualChannel virtualChannel in activeChannels )
						{
							if( virtualChannel.CurrentRealChannel != null )
								text.Append( "Real - " );
							else
								text.Append( "Virtual - " );

							string soundName;

							if( virtualChannel.CurrentSound.Name != null )
								soundName = virtualChannel.CurrentSound.Name;
							else
								soundName = "DataBuffer";
							text.AppendFormat( "{0}  Volume {1}\n", soundName,
								virtualChannel.GetTotalVolume().ToString( "F3" ) );
						}
					}
				}
				else
				{
					//real channels

					int freeCount = 0;
					int activeCount = 0;

					for( int nRealChannels = 0; nRealChannels < 2; nRealChannels++ )
					{
						IEnumerable<RealChannel> realChannels = nRealChannels == 0 ?
							SoundWorld.Instance.Real2DChannels : SoundWorld.Instance.Real3DChannels;
						foreach( RealChannel realChannel in realChannels )
						{
							if( realChannel.CurrentVirtualChannel == null )
								freeCount++;
							else
								activeCount++;
						}
					}

					text.AppendFormat( "Free channels: {0}\n", freeCount );
					text.AppendFormat( "Active channels: {0}\n", activeCount );
					text.Append( "\n" );

					bool last3d = false;

					for( int nRealChannels = 0; nRealChannels < 2; nRealChannels++ )
					{
						IEnumerable<RealChannel> realChannels = nRealChannels == 0 ?
							SoundWorld.Instance.Real2DChannels : SoundWorld.Instance.Real3DChannels;

						foreach( RealChannel realChannel in realChannels )
						{
							VirtualChannel virtualChannel = realChannel.CurrentVirtualChannel;

							if( !last3d && realChannel.Is3D )
							{
								last3d = true;
								text.Append( "\n" );
							}

							text.AppendFormat( "{0}: ", realChannel.Is3D ? "3D" : "2D" );

							if( virtualChannel != null )
							{
								string soundName;

								if( virtualChannel.CurrentSound.Name != null )
									soundName = virtualChannel.CurrentSound.Name;
								else
									soundName = "DataBuffer";

								text.AppendFormat( "{0}  Volume {1}\n", soundName,
									virtualChannel.GetTotalVolume().ToString( "F3" ) );
							}
							else
								text.Append( "Free\n" );
						}
					}
				}

				PageControl.Controls[ "Information" ].Text = text.ToString();
			}
		}

		///////////////////////////////////////////

		class EntitiesPage : Page
		{
			public EntitiesPage( EControl pageControl )
				: base( pageControl )
			{
			}

			public override void OnUpdate()
			{
				if( Entities.Instance != null )
				{
					PageControl.Controls[ "EntityClasses" ].Text =
						EntityTypes.Instance.Classes.Count.ToString( "N0" );
					PageControl.Controls[ "EntityTypes" ].Text =
						EntityTypes.Instance.Types.Count.ToString( "N0" );
					PageControl.Controls[ "Entities" ].Text =
						Entities.Instance.EntitiesCollection.Count.ToString( "N0" );
				}
				else
				{
					PageControl.Controls[ "EntityClasses" ].Text = "";
					PageControl.Controls[ "EntityTypes" ].Text = "";
					PageControl.Controls[ "Entities" ].Text = "";
				}
			}
		}

		///////////////////////////////////////////

		class MemoryPage : Page
		{
			public MemoryPage( EControl pageControl )
				: base( pageControl )
			{
			}

			public override void OnUpdate()
			{
				PageControl.Controls[ "NetMemory" ].Text = GC.GetTotalMemory( false ).ToString( "N0" );

				int totalAllocatedMemory = 0;
				int totalAllocationCount = 0;

				for( int n = 0; n < (int)NativeMemoryAllocationType.Count; n++ )
				{
					NativeMemoryAllocationType allocationType = (NativeMemoryAllocationType)n;

					int allocatedMemory;
					int allocationCount;
					NativeMemoryManager.GetStatistics( allocationType, out allocatedMemory,
						out allocationCount );

					string typeString = allocationType.ToString();

					PageControl.Controls[ typeString + "Allocations" ].Text =
						allocationCount.ToString( "N0" );
					PageControl.Controls[ typeString + "Memory" ].Text =
						allocatedMemory.ToString( "N0" );

					totalAllocatedMemory += allocatedMemory;
					totalAllocationCount += allocationCount;
				}

				PageControl.Controls[ "TotalAllocations" ].Text = totalAllocationCount.ToString( "N0" );
				PageControl.Controls[ "TotalMemory" ].Text = totalAllocatedMemory.ToString( "N0" );

				int crtAllocatedMemory;
				int crtAllocationCount;
				NativeMemoryManager.GetCRTStatistics( out crtAllocatedMemory, out crtAllocationCount );

				PageControl.Controls[ "CRTAllocations" ].Text = crtAllocationCount.ToString( "N0" );
				PageControl.Controls[ "CRTMemory" ].Text = crtAllocatedMemory.ToString( "N0" );
			}
		}

		///////////////////////////////////////////

		class DLLPage : Page
		{
			bool updated;
			EComboBox comboBox;

			//

			public DLLPage( EControl pageControl )
				: base( pageControl )
			{
				comboBox = (EComboBox)PageControl.Controls[ "InformationType" ];
				comboBox.Items.Add( "Managed assemblies" );
				comboBox.Items.Add( "DLLs" );
				comboBox.SelectedIndex = 0;
				comboBox.SelectedIndexChange += ComboBox_SelectedIndexChange;
			}

			public override void OnDestroy()
			{
				comboBox.SelectedIndexChange -= ComboBox_SelectedIndexChange;

				base.OnDestroy();
			}

			void ComboBox_SelectedIndexChange( EComboBox sender )
			{
				updated = false;
			}

			public override void OnUpdate()
			{
				if( updated )
					return;
				updated = true;

				int informationTypeIndex = ( (EComboBox)PageControl.Controls[ "InformationType" ] ).
					SelectedIndex;

				EListBox listBox = (EListBox)PageControl.Controls[ "Information" ];

				int lastSelectedIndex = listBox.SelectedIndex;

				listBox.Items.Clear();

				if( informationTypeIndex == 0 )
				{
					//managed assemblies

					Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

					List<AssemblyName> resultAssemblyNames = new List<AssemblyName>( assemblies.Length );
					{
						List<Assembly> remainingAssemblies = new List<Assembly>( assemblies );

						while( true )
						{
							Assembly notReferencedAssembly = null;
							{
								foreach( Assembly assembly in remainingAssemblies )
								{
									AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();

									foreach( Assembly a in remainingAssemblies )
									{
										if( assembly == a )
											continue;

										AssemblyName aName = a.GetName();

										foreach( AssemblyName referencedAssembly in referencedAssemblies )
										{
											if( referencedAssembly.Name == aName.Name )
												goto nextAssembly;
										}
									}

									notReferencedAssembly = assembly;
									break;

									nextAssembly: ;
								}
							}

							if( notReferencedAssembly != null )
							{
								remainingAssemblies.Remove( notReferencedAssembly );
								resultAssemblyNames.Add( notReferencedAssembly.GetName() );
							}
							else
							{
								//no exists not referenced assemblies
								foreach( Assembly assembly in remainingAssemblies )
									resultAssemblyNames.Add( assembly.GetName() );
								break;
							}
						}
					}

					foreach( AssemblyName assemblyName in resultAssemblyNames )
					{
						string text = string.Format( "{0}, {1}", assemblyName.Name,
							assemblyName.Version );
						listBox.Items.Add( text );
					}
				}
				else if( informationTypeIndex == 1 )
				{
					//dlls
					string[] names = EngineApp.Instance.GetNativeModuleNames();

					ArrayUtils.SelectionSort( names, delegate( string s1, string s2 )
					{
						return string.Compare( s1, s2, true );
					} );

					foreach( string name in names )
					{
						string text = string.Format( "{0} - {1}", Path.GetFileName( name ), name );
						listBox.Items.Add( text );
					}
				}

				if( lastSelectedIndex >= 0 && lastSelectedIndex < listBox.Items.Count )
					listBox.SelectedIndex = lastSelectedIndex;
				if( listBox.Items.Count != 0 && listBox.SelectedIndex == -1 )
					listBox.SelectedIndex = 0;
			}
		}

		///////////////////////////////////////////

		class NetworkPage : Page
		{
			public NetworkPage( EControl pageControl )
				: base( pageControl )
			{
			}

			void GetConnectedNodeData( NetworkNode.ConnectedNode connectedNode, StringBuilder text )
			{
				if( connectedNode.Status == NetworkConnectionStatuses.Connected )
				{
					text.AppendFormat( "- Connection with {0}\n", connectedNode.RemoteEndPoint.Address );

					NetworkNode.ConnectedNode.StatisticsData statistics = connectedNode.Statistics;

					text.AppendFormat(
						"-   Send: Total: {0} kb, Speed: {1} b/s\n",
						statistics.GetBytesSent( true ) / 1024,
						(long)statistics.GetBytesSentPerSecond( true ) );

					text.AppendFormat(
						"-   Receive: Total: {0} kb, Speed: {1} b/s\n",
						statistics.GetBytesReceived( true ) / 1024,
						(long)statistics.GetBytesReceivedPerSecond( true ) );

					text.AppendFormat( "-   Ping: {0} ms\n",
						(int)( connectedNode.AverageRoundtripTime * 1000 ) );
				}
			}

			public override void OnUpdate()
			{
				StringBuilder text = new StringBuilder( "" );

				//GameNetworkServer
				GameNetworkServer server = GameNetworkServer.Instance;
				if( server != null )
				{
					text.Append( "Server:\n" );
					foreach( NetworkNode.ConnectedNode connectedNode in server.ConnectedNodes )
						GetConnectedNodeData( connectedNode, text );
					text.Append( "\n" );
				}

				//GameNetworkClient
				GameNetworkClient client = GameNetworkClient.Instance;
				if( client != null && client.Status == NetworkConnectionStatuses.Connected )
				{
					text.Append( "Client:\n" );
					GetConnectedNodeData( client.ServerConnectedNode, text );
					text.Append( "\n" );
				}

				if( text.Length == 0 )
					text.Append( "No connections" );

				//EntitySystem statistics
				if( EntitySystemWorld.Instance != null )
				{
					EntitySystemWorld.NetworkingInterface networkingInterface =
						EntitySystemWorld.Instance._GetNetworkingInterface();
					if( networkingInterface != null )
					{
						string[] lines = networkingInterface.GetStatisticsAsText();
						foreach( string line in lines )
							text.AppendFormat( "-   {0}\n", line );
					}
				}

				PageControl.Controls[ "Data" ].Text = text.ToString();
			}
		}
	}
}
