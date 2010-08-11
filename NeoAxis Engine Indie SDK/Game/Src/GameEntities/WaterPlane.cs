// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.SoundSystem;
using Engine.Utils;
using GameCommon;

namespace GameEntities
{
	public class WaterPlaneType : MapGeneralObjectType
	{
		[FieldSerialize( "physicsDensity" )]
		float physicsDensity = 1;

		[FieldSerialize( "splashes" )]
		List<SplashItem> splashes = new List<SplashItem>();

		///////////////////////////////////////////

		public enum SplashTypes
		{
			None,
			Bullet,
			Body,
			Explosion,
		}

		///////////////////////////////////////////

		public class SplashItem
		{
			[FieldSerialize( "splashType" )]
			SplashTypes splashType;

			[FieldSerialize( "soundName" )]
			string soundName = "";

			[FieldSerialize( "particles" )]
			List<ParticleItem> particles = new List<ParticleItem>();

			///////////////

			public class ParticleItem
			{
				[FieldSerialize( "particleName" )]
				string particleName = "";

				[FieldSerialize( "scale" )]
				float scale = 1;

				[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
				public string ParticleName
				{
					get { return particleName; }
					set { particleName = value; }
				}

				[DefaultValue( 1.0f )]
				public float Scale
				{
					get { return scale; }
					set { scale = value; }
				}

				public override string ToString()
				{
					if( string.IsNullOrEmpty( particleName ) )
						return "(not initialized)";
					return particleName;
				}
			}

			///////////////

			[DefaultValue( SplashTypes.None )]
			public SplashTypes SplashType
			{
				get { return splashType; }
				set { splashType = value; }
			}

			[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
			public string SoundName
			{
				get { return soundName; }
				set { soundName = value; }
			}

			[TypeConverter( typeof( CollectionTypeConverter ) )]
			public List<ParticleItem> Particles
			{
				get { return particles; }
			}

			public override string ToString()
			{
				string text = splashType.ToString() + ": ";

				for( int n = 0; n < particles.Count; n++ )
				{
					if( n != 0 )
						text += ", ";
					text += particles[ n ].ParticleName;
				}

				return text;
			}
		}

		///////////////////////////////////////////

		[DefaultValue( 1.0f )]
		public float PhysicsDensity
		{
			get { return physicsDensity; }
			set
			{
				if( value < 0 )
				{
					Log.Warning( "Invalid value." );
					return;
				}

				physicsDensity = value;
			}
		}

		[TypeConverter( typeof( CollectionTypeConverter ) )]
		public List<SplashItem> Splashes
		{
			get { return splashes; }
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			foreach( SplashItem splashItem in splashes )
			{
				if( !string.IsNullOrEmpty( splashItem.SoundName ) )
					SoundWorld.Instance.SoundCreate( splashItem.SoundName, SoundMode.Mode3D );
			}
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[Browsable( false )]
	public class WaterPlaneHighLevelMaterial : HighLevelMaterial
	{
		WaterPlane owner;
		TextureUnitState reflectionMapState;

		internal void Init( WaterPlane owner )
		{
			this.owner = owner;
			SceneManager.Instance.FogChanged += SceneManager_FogChanged;
			SceneManager.Instance.ShadowSettingsChanged += SceneManager_ShadowSettingsChanged;
		}

		public override void Dispose()
		{
			SceneManager.Instance.ShadowSettingsChanged -= SceneManager_ShadowSettingsChanged;
			SceneManager.Instance.FogChanged -= SceneManager_FogChanged;

			base.Dispose();
		}

		static void SetProgramAutoConstants( GpuProgramParameters parameters )
		{
			//Matrix
			parameters.SetNamedAutoConstant( "worldViewProjMatrix",
				GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
			parameters.SetNamedAutoConstant( "worldViewMatrix",
				GpuProgramParameters.AutoConstantType.WorldViewMatrix );
			parameters.SetNamedAutoConstant( "cameraPositionObjectSpace",
				GpuProgramParameters.AutoConstantType.CameraPositionObjectSpace );

			//Fog
			parameters.SetNamedAutoConstant( "fogParams",
				GpuProgramParameters.AutoConstantType.FogParams );
			parameters.SetNamedAutoConstant( "fogColor",
				GpuProgramParameters.AutoConstantType.FogColor );

			//Time
			//parameters.SetNamedAutoConstantFloat( "timeValue",
			//   GpuProgramParameters.AutoConstantType.Time01, 20.0f );
			parameters.SetNamedAutoConstantFloat( "time0X",
				GpuProgramParameters.AutoConstantType.Time0X, 1000.0f );

			parameters.SetNamedAutoConstant( "renderTargetFlipping",
				GpuProgramParameters.AutoConstantType.RenderTargetFlipping );
		}

		void CreateDefaultTechnique( out bool notSupported )
		{
			notSupported = false;

			string sourceFile = "Materials\\Common\\Water.cg_hlsl";

			string vertexSyntax;
			string fragmentSyntax;
			{
				if( RenderSystem.Instance.IsDirect3D() )
				{
					if( RenderSystem.Instance.HasShaderModel3() )
					{
						vertexSyntax = "vs_3_0";
						fragmentSyntax = "ps_3_0";
					}
					else
					{
						vertexSyntax = "vs_2_0";
						fragmentSyntax = "ps_2_0";
					}
				}
				else
				{
					vertexSyntax = "arbvp1";
					fragmentSyntax = "arbfp1";
				}
			}

			if( !RenderSystem.Instance.HasShaderModel2() ||
				!RenderSystem.Instance.Capabilities.UserClipPlanes )
			{
				notSupported = true;
				return;
			}

			BaseMaterial.ReceiveShadows = false;

			Technique tecnhique = BaseMaterial.CreateTechnique();
			Pass pass = tecnhique.CreatePass();

			//generate compileArguments and bind textures
			StringBuilder compileArguments = new StringBuilder( 128 );

			//transparent surface
			if( owner.DeepColor.Alpha != 1 || owner.ShallowColor.Alpha != 1 )
			{
				pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
				pass.DestBlendFactor = SceneBlendFactor.OneMinusSourceAlpha;
				pass.DepthWrite = false;

				compileArguments.Append( " -DTRANSPARENT" );
			}

			//disable Direct3D standard fog features
			pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

			//bool direct3d = RenderSystem.Instance.Name.Contains( "Direct3D" );
			//compileArguments.Append( direct3d ? " -DDIRECT3D" : " -DOPENGL" );

			//Fog
			FogMode fogMode = SceneManager.Instance.GetFogMode();
			if( fogMode != FogMode.None )
			{
				compileArguments.Append( " -DFOG_ENABLED" );
				compileArguments.Append( " -DFOG_" + fogMode.ToString().ToUpper() );
			}

			//noiseMap
			{
				TextureUnitState state = pass.CreateTextureUnitState();
				state.SetTextureName( "Types\\Special\\WaterPlane\\WaterNoise.dds", Texture.Type.Type2D );
			}

			//reflectionMap
			if( owner.ReflectionLevel != WaterPlane.ReflectionLevels.None )
			{
				compileArguments.Append( " -DREFLECTION_MAP" );

				reflectionMapState = pass.CreateTextureUnitState();
				if( owner.ReflectionTexture != null )
					reflectionMapState.SetTextureName( owner.ReflectionTexture.Name );
				reflectionMapState.SetTextureAddressingMode( TextureAddressingMode.Clamp );
				reflectionMapState.SetTextureFiltering( FilterOptions.Linear,
					FilterOptions.Linear, FilterOptions.None );
			}

			//vertex program
			{
				string errorString;

				GpuProgram program = GpuProgramCacheManager.Instance.AddProgram(
					"WaterPlane_Vertex_", GpuProgramType.VertexProgram, sourceFile,
					"main_vp", vertexSyntax, compileArguments.ToString(), out errorString );

				if( !string.IsNullOrEmpty( errorString ) )
					Log.Fatal( errorString );

				if( program != null )
				{
					GpuProgramParameters parameters = program.DefaultParameters;
					SetProgramAutoConstants( parameters );
					pass.VertexProgramName = program.Name;
				}
			}

			//fragment program
			{
				string errorString;

				GpuProgram program = GpuProgramCacheManager.Instance.AddProgram(
					"WaterPlane_Fragment_", GpuProgramType.FragmentProgram, sourceFile,
					"main_fp", fragmentSyntax, compileArguments.ToString(), out errorString );

				if( !string.IsNullOrEmpty( errorString ) )
					Log.Fatal( errorString );

				if( program != null )
				{
					SetProgramAutoConstants( program.DefaultParameters );
					pass.FragmentProgramName = program.Name;
				}
			}
		}

		void CreateFixedPipelineTechnique()
		{
			BaseMaterial.ReceiveShadows = false;

			Technique tecnhique = BaseMaterial.CreateTechnique();
			Pass pass = tecnhique.CreatePass();
			pass.Ambient = owner.FixedPipelineColor;
			pass.Diffuse = owner.FixedPipelineColor;
			pass.Specular = new ColorValue( 0, 0, 0 );
			pass.CreateTextureUnitState( owner.FixedPipelineMap );
		}

		protected override bool OnInitBaseMaterial()
		{
			if( !base.OnInitBaseMaterial() )
				return false;

			bool notSupported;
			CreateDefaultTechnique( out notSupported );
			if( notSupported )
				CreateFixedPipelineTechnique();

			return true;
		}

		protected override void OnClearBaseMaterial()
		{
			reflectionMapState = null;

			//clear material
			BaseMaterial.RemoveAllTechniques();

			base.OnClearBaseMaterial();
		}

		void SceneManager_FogChanged( bool fogModeChanged )
		{
			if( fogModeChanged && IsBaseMaterialInitialized() )
				UpdateBaseMaterial();
		}

		void SceneManager_ShadowSettingsChanged( bool shadowTechniqueChanged )
		{
			if( shadowTechniqueChanged && IsBaseMaterialInitialized() )
				UpdateBaseMaterial();
		}

		void SetGpuNamedConstant( string name, Vec4 value )
		{
			foreach( Technique technique in BaseMaterial.Techniques )
			{
				foreach( Pass pass in technique.Passes )
				{
					GpuProgramParameters parameters;

					parameters = pass.VertexProgramParameters;
					if( parameters != null )
						parameters.SetNamedConstant( name, value );

					parameters = pass.FragmentProgramParameters;
					if( parameters != null )
						parameters.SetNamedConstant( name, value );
				}
			}
		}

		public void RemoveReflectionMapFromMaterial()
		{
			if( reflectionMapState != null )
				reflectionMapState.SetTextureName( "" );
		}

		internal void UpdateDynamicGpuParameters()
		{
			SetGpuNamedConstant( "deepColor", owner.DeepColor.ToVec4() );
			SetGpuNamedConstant( "shallowColor", owner.ShallowColor.ToVec4() );
			SetGpuNamedConstant( "reflectionColor", owner.ReflectionColor.ToVec4() );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class WaterPlane : MapGeneralObject
	{
		static List<WaterPlane> instances = new List<WaterPlane>();

		[FieldSerialize( "size" )]
		Vec2 size = new Vec2( 1000, 1000 );

		[FieldSerialize( "position" )]
		Vec3 position;

		[FieldSerialize( "segments" )]
		Vec2i segments = new Vec2i( 20, 20 );

		[FieldSerialize( "renderQueueGroup" )]
		RenderQueueGroupID renderQueueGroup = RenderQueueGroupID.Queue3;

		[FieldSerialize( "reflectionLevel" )]
		ReflectionLevels reflectionLevel = ReflectionLevels.OnlyModels;

		[FieldSerialize( "physicsHeight" )]
		float physicsHeight;

		[FieldSerialize( "deepColor" )]
		ColorValue deepColor = new ColorValue( 0, .3f, .5f );

		[FieldSerialize( "shallowColor" )]
		ColorValue shallowColor = new ColorValue( 0, 1, 1 );

		[FieldSerialize( "reflectionColor" )]
		ColorValue reflectionColor = new ColorValue( 1, 1, 1 );

		[FieldSerialize( "reflectionTextureSize" )]
		ReflectionTextureSizes reflectionTextureSize = ReflectionTextureSizes.HalfOfFrameBuffer;

		[FieldSerialize( "visible" )]
		bool visible = true;

		[FieldSerialize( "fixedPipelineMap" )]
		string fixedPipelineMap = "Types\\Special\\WaterPlane\\WaterFixedPipeline.jpg";

		[FieldSerialize( "fixedPipelineMapTiling" )]
		float fixedPipelineMapTiling = 10;

		[FieldSerialize( "fixedPipelineColor" )]
		ColorValue fixedPipelineColor = new ColorValue( 0, .3f, .5f );

		bool needUpdatePlane;
		Mesh meshPlane;
		MeshObject meshObject;
		SceneNode sceneNode;
		Body volumeBody;

		WaterPlaneHighLevelMaterial material;

		//Reflection
		Texture reflectionTexture;
		RenderTexture reflectionRenderTexture;
		Camera reflectionCamera;
		Viewport reflectionViewport;
		ReflectionRenderTargetListener renderTargetListener;

		//Key: splash type, Value: available items for this alias
		Dictionary<WaterPlaneType.SplashTypes, WaterPlaneType.SplashItem[]> splashItemsCache;
		List<SplashOffItem> bodiesSplashOffTime = new List<SplashOffItem>();

		//only for optimization
		ListAllocator<SubmergedCheckItem> listSubmergedCheckItemAllocator =
			new ListAllocator<SubmergedCheckItem>();

		bool server_shouldSendPropertiesToClients;

		///////////////////////////////////////////

		public enum ReflectionLevels
		{
			None,
			OnlySky,
			OnlyStaticGeometry,
			OnlyModels,
			ReflectAll,
		}

		///////////////////////////////////////////

		public enum ReflectionTextureSizes
		{
			EqualToFrameBuffer,
			HalfOfFrameBuffer,
			QuarterOfFrameBuffer,
		}

		///////////////////////////////////////////

		struct SplashOffItem
		{
			public Body body;
			public float remainingTime;
		}

		///////////////////////////////////////////

		struct SubmergedCheckItem
		{
			public float coef;
			public Vec3 center;

			public SubmergedCheckItem( float coef, Vec3 center )
			{
				this.coef = coef;
				this.center = center;
			}
		}

		///////////////////////////////////////////

		class ReflectionRenderTargetListener : RenderTargetListener
		{
			WaterPlane owner;

			bool saveDrawDecorativeObjects;
			Dictionary<HeightmapTerrain, bool> saveHeightmapTerrainsSimpleRenderingState =
				new Dictionary<HeightmapTerrain, bool>();

			public ReflectionRenderTargetListener( WaterPlane owner )
			{
				this.owner = owner;
			}

			protected override void OnPreRenderTargetUpdate( RenderTargetEvent evt )
			{
				base.OnPreRenderTargetUpdate( evt );

				Camera camera = owner.reflectionCamera;
				Camera defaultCamera = RendererWorld.Instance.DefaultCamera;

				camera.NearClipDistance = defaultCamera.NearClipDistance;
				camera.FarClipDistance = defaultCamera.FarClipDistance;
				camera.AspectRatio = defaultCamera.AspectRatio;
				camera.Fov = defaultCamera.Fov;
				camera.Position = defaultCamera.Position;
				camera.FixedUp = defaultCamera.FixedUp;
				camera.Direction = defaultCamera.Direction;

				Plane reflectionPlane = new Plane( Vec3.ZAxis, owner.Position.Z );
				camera.DisableReflection();
				camera.EnableReflection( reflectionPlane );

				//set clip planes
				{
					Plane clipPlane = new Plane( Vec3.ZAxis, owner.Position.Z );
					Plane[] clipPlanes = new Plane[ 5 ];
					clipPlanes[ 0 ] = clipPlane;

					Vec3 reflectedCameraPosition = camera.GetReflectionMatrix() * camera.Position;

					Bounds bounds = new Bounds( owner.Position );
					bounds.Expand( new Vec3( owner.Size.X, owner.Size.Y, 0 ) * .5f );
					Vec3 p0 = new Vec3( bounds.Minimum.X, bounds.Minimum.Y, owner.Position.Z );
					Vec3 p1 = new Vec3( bounds.Maximum.X, bounds.Minimum.Y, owner.Position.Z );
					Vec3 p2 = new Vec3( bounds.Maximum.X, bounds.Maximum.Y, owner.Position.Z );
					Vec3 p3 = new Vec3( bounds.Minimum.X, bounds.Maximum.Y, owner.Position.Z );
					clipPlanes[ 1 ] = Plane.FromPoints( reflectedCameraPosition, p0, p1 );
					clipPlanes[ 2 ] = Plane.FromPoints( reflectedCameraPosition, p1, p2 );
					clipPlanes[ 3 ] = Plane.FromPoints( reflectedCameraPosition, p2, p3 );
					clipPlanes[ 4 ] = Plane.FromPoints( reflectedCameraPosition, p3, p0 );

					camera.SetClipPlanesForAllGeometry( clipPlanes );
				}

				//set reflection level settings
				if( DecorativeObjectManager.Instance != null )
					saveDrawDecorativeObjects = DecorativeObjectManager.Instance.Visible;

				camera.DrawStaticGeometry = true;
				camera.DrawModels = true;
				camera.DrawEffects = true;

				if( (int)owner.ReflectionLevel < (int)ReflectionLevels.OnlyStaticGeometry )
					camera.DrawStaticGeometry = false;
				if( (int)owner.ReflectionLevel < (int)ReflectionLevels.OnlyModels )
					camera.DrawModels = false;
				if( (int)owner.ReflectionLevel < (int)ReflectionLevels.ReflectAll )
				{
					camera.DrawEffects = false;
					if( DecorativeObjectManager.Instance != null )
						DecorativeObjectManager.Instance.Visible = false;
				}

				//activate simple rendering mode for terrains
				foreach( HeightmapTerrain terrain in HeightmapTerrain.Instances )
				{
					saveHeightmapTerrainsSimpleRenderingState.Add( terrain, terrain.SimpleRendering );
					terrain.SimpleRendering = true;
				}
			}

			protected override void OnPostRenderTargetUpdate( RenderTargetEvent evt )
			{
				RenderSystem.Instance.ResetScissorTest();

				//restore simple rendering mode state for terrains
				foreach( HeightmapTerrain terrain in HeightmapTerrain.Instances )
				{
					bool simpleRendering;
					if( saveHeightmapTerrainsSimpleRenderingState.TryGetValue( terrain,
						out simpleRendering ) )
					{
						terrain.SimpleRendering = simpleRendering;
					}
				}
				saveHeightmapTerrainsSimpleRenderingState.Clear();

				//restore draw settings
				if( DecorativeObjectManager.Instance != null )
					DecorativeObjectManager.Instance.Visible = saveDrawDecorativeObjects;

				base.OnPostRenderTargetUpdate( evt );
			}
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			PropertiesToClient,
			CreateSplashToClient,
		}

		///////////////////////////////////////////

		[TypeField]
		WaterPlaneType _type = null; public new WaterPlaneType Type { get { return _type; } }

		public static List<WaterPlane> Instances
		{
			get { return instances; }
		}

		[DefaultValue( typeof( Vec2 ), "1000 1000" )]
		public Vec2 Size
		{
			get { return size; }
			set
			{
				if( size == value )
					return;

				size = value;
				needUpdatePlane = true;

				CreateVolumeBody();

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[DefaultValue( typeof( Vec3 ), "0 0 0" )]
		public Vec3 Position
		{
			get { return position; }
			set
			{
				if( position == value )
					return;

				position = value;
				needUpdatePlane = true;

				CreateVolumeBody();

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[DefaultValue( typeof( Vec2i ), "20 20" )]
		public Vec2i Segments
		{
			get { return segments; }
			set
			{
				if( value.X <= 0 || value.Y <= 0 )
				{
					Log.Warning( "Invalid value." );
					return;
				}

				segments = value;
				needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[DefaultValue( RenderQueueGroupID.Queue3 )]
		public RenderQueueGroupID RenderQueueGroup
		{
			get { return renderQueueGroup; }
			set
			{
				renderQueueGroup = value;
				needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[DefaultValue( 0.0f )]
		public float PhysicsHeight
		{
			get { return physicsHeight; }
			set
			{
				if( value < 0 )
				{
					Log.Warning( "Invalid value." );
					return;
				}

				physicsHeight = value;

				CreateVolumeBody();

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[DefaultValue( true )]
		public bool Visible
		{
			get { return visible; }
			set { visible = value; }
		}

		[Category( "WaterPlane (Fixed Pipeline)" )]
		[Editor( typeof( EditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
		[DefaultValue( "Types\\Special\\WaterPlane\\WaterFixedPipeline.jpg" )]
		public string FixedPipelineMap
		{
			get { return fixedPipelineMap; }
			set
			{
				if( fixedPipelineMap == value )
					return;

				fixedPipelineMap = value;

				needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[Category( "WaterPlane (Fixed Pipeline)" )]
		[DefaultValue( 10.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .5f, 100 )]
		public float FixedPipelineMapTiling
		{
			get { return fixedPipelineMapTiling; }
			set
			{
				if( fixedPipelineMapTiling == value )
					return;

				fixedPipelineMapTiling = value;

				needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[Category( "WaterPlane (Fixed Pipeline)" )]
		[DefaultValue( typeof( ColorValue ), "0 76 127" )]
		public ColorValue FixedPipelineColor
		{
			get { return fixedPipelineColor; }
			set
			{
				if( fixedPipelineColor == value )
					return;

				fixedPipelineColor = value;

				needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			instances.Add( this );
			base.OnPostCreate( loaded );

			CreateVolumeBody();

			AddTimer();
			needUpdatePlane = true;

			CreatePlane();

			RenderSystem.Instance.RenderSystemEvent += RenderSystem_RenderSystemEvent;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			RenderSystem.Instance.RenderSystemEvent -= RenderSystem_RenderSystemEvent;

			DestroyPlane();
			DestroyVolumeBody();

			base.OnDestroy();
			instances.Remove( this );
		}

		protected override void OnTick()
		{
			base.OnTick();

			TickPhysicsInfluence( true );
			UpdateBodiesSplashOffTime();

			if( server_shouldSendPropertiesToClients )
			{
				Server_SendPropertiesToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				server_shouldSendPropertiesToClients = false;
			}
		}

		void UpdateBodiesSplashOffTime()
		{
			for( int n = 0; n < bodiesSplashOffTime.Count; n++ )
			{
				SplashOffItem item = bodiesSplashOffTime[ n ];
				item.remainingTime -= TickDelta;
				if( item.remainingTime <= 0 )
				{
					bodiesSplashOffTime.RemoveAt( n );
					n--;
				}
				else
				{
					bodiesSplashOffTime[ n ] = item;
				}
			}
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			if( reflectionViewport != null )
			{
				if( reflectionViewport.DimensionsInPixels.Size != GetRequiredReflectionTextureSize() )
					needUpdatePlane = true;
			}

			if( needUpdatePlane )
				CreatePlane();
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( sceneNode != null )
				sceneNode.Visible = Visible && camera != reflectionCamera;

			//update material
			if( material != null )
				material.UpdateDynamicGpuParameters();

			//update visible flag of the reflection render texture
			Camera defaultCamera = RendererWorld.Instance.DefaultCamera;
			if( reflectionRenderTexture != null && camera == defaultCamera )
			{
				Bounds bounds = new Bounds( Position );
				bounds.Expand( new Vec3( Size.X, Size.Y, 1 ) * .5f );

				Frustum frustum = FrustumUtils.GetFrustumByCamera( defaultCamera,
					Map.Instance.NearFarClipDistance.Maximum );

				//frustum test mode
				if( EngineDebugSettings.FrustumTest && camera.AllowFrustumTestMode )
				{
					frustum.HalfWidth *= .5f;
					frustum.HalfHeight *= .5f;
				}

				bool inFrustum = frustum.IsIntersects( bounds ) && frustum.Origin.Z > Position.Z;

				reflectionRenderTexture.AutoUpdate = inFrustum && Visible;
			}

			if( EntitySystemWorld.Instance.IsEditor() )
			{
				//render volume
				if( physicsHeight != 0 && Type.PhysicsDensity != 0 )
				{
					Bounds bounds = new Bounds( Position - new Vec3( 0, 0, physicsHeight / 2 ) );
					bounds.Expand( new Vec3( Size.X, Size.Y, physicsHeight ) / 2 );

					camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
					camera.DebugGeometry.AddBounds( bounds );
				}
			}
		}

		void CreatePlane()
		{
			if( RenderSystem.Instance.IsDeviceLost() )
				return;

			DestroyPlane();

			Viewport defaultViewport = RendererWorld.Instance.DefaultViewport;

			if( RenderSystem.Instance.HasShaderModel2() &&
				RenderSystem.Instance.Capabilities.UserClipPlanes &&
				ReflectionLevel != ReflectionLevels.None )
			{
				CreateReflectionTexture();
			}

			string meshName = MeshManager.Instance.GetUniqueName( "WaterPlane" );

			Vec2 tile;
			if( fixedPipelineMapTiling != 0 )
				tile = size / fixedPipelineMapTiling;
			else
				tile = new Vec2( 0, 0 );

			meshPlane = MeshManager.Instance.CreatePlane( meshName, new Plane( new Vec3( 0, 0, 1 ), 0 ),
				size, segments, true, 1, tile, new Vec3( 0, 1, 0 ) );

			//create material
			string materialName = MaterialManager.Instance.GetUniqueName( "_GeneratedWaterPlane" );
			material = (WaterPlaneHighLevelMaterial)HighLevelMaterialManager.Instance.
				CreateMaterial( materialName, "WaterPlaneHighLevelMaterial" );
			material.Init( this );
			material.UpdateBaseMaterial();

			//change material of mesh
			foreach( SubMesh subMesh in meshPlane.SubMeshes )
				subMesh.MaterialName = material.Name;

			meshObject = SceneManager.Instance.CreateMeshObject( meshName );
			meshObject.RenderQueueGroup = renderQueueGroup;

			sceneNode = new SceneNode();
			sceneNode.Attach( meshObject );
			sceneNode.Position = position;
			sceneNode.Visible = Visible;

			needUpdatePlane = false;
		}

		void DestroyPlane()
		{
			DestroyReflectionTexture();

			if( sceneNode != null )
			{
				sceneNode.Dispose();
				sceneNode = null;
			}
			if( meshObject != null )
			{
				meshObject.Dispose();
				meshObject = null;
			}
			if( meshPlane != null )
			{
				meshPlane.Dispose();
				meshPlane = null;
			}

			if( material != null )
			{
				material.Dispose();
				material = null;
			}
		}

		SubmergedCheckItem GetSphereSubmergedCoef( Sphere sphere )
		{
			Vec3 pos = sphere.Origin;
			float r = sphere.Radius;

			if( pos.Z + r <= Position.Z )
			{
				return new SubmergedCheckItem( 1, pos );
			}
			else if( pos.Z - r >= Position.Z )
			{
				return new SubmergedCheckItem( 0, Vec3.Zero );
			}
			else
			{
				if( pos.Z > Position.Z )
				{
					float h = r - ( pos.Z - Position.Z );
					//float sphereVolume = ( 4.0f / 3.0f ) * MathFunctions.PI * r * r * r;
					//float v = ( 2.0f / 3.0f ) * MathFunctions.PI * r * r * h;
					//float coef = v / sphereVolume;
					float coef = ( 2.0f / 3.0f ) / ( 4.0f / 3.0f ) * h / r;
					Vec3 center = new Vec3( pos.X, pos.Y, Position.Z - h * .5f );
					return new SubmergedCheckItem( coef, center );
				}
				else
				{
					float h = r - ( Position.Z - pos.Z );
					//float sphereVolume = ( 4.0f / 3.0f ) * MathFunctions.PI * r * r * r;
					//float v = sphereVolume - ( 2.0f / 3.0f ) * MathFunctions.PI * r * r * h;
					//float coef = v / sphereVolume;
					float coef = 1.0f - ( 2.0f / 3.0f ) / ( 4.0f / 3.0f ) * h / r;
					Vec3 center = pos;
					return new SubmergedCheckItem( coef, center );
				}
			}
		}

		float GetShapeInfluenceDensity( Shape shape )
		{
			if( shape.SpecialLiquidDensity != 0 )
				return shape.SpecialLiquidDensity;
			return shape.Density;
		}

		public void TickPhysicsInfluence( bool allowSplashes )
		{
			if( volumeBody == null )
				return;

			EngineRandom random = World.Instance.Random;

			List<SubmergedCheckItem> submergedItems = listSubmergedCheckItemAllocator.Alloc();

			Vec2 halfSize = Size * .5f;
			Bounds volumeBounds = new Bounds(
				new Vec3( Position.X - halfSize.X, Position.Y - halfSize.Y, Position.Z - physicsHeight ),
				new Vec3( Position.X + halfSize.X, Position.Y + halfSize.Y, Position.Z ) );

			Body[] bodies = PhysicsWorld.Instance.VolumeCast( volumeBounds,
				(int)ContactGroup.CastOnlyDynamic );

			foreach( Body body in bodies )
			{
				if( body.Static )
					continue;

				Mat3 bodyRotation = body.Rotation.ToMat3();

				foreach( Shape shape in body.Shapes )
				{
					if( shape.ContactGroup == (int)ContactGroup.NoContact )
						continue;

					Vec3 shapePosition = body.Position + bodyRotation * shape.Position;
					float volume = shape.Volume;

					switch( shape.ShapeType )
					{
					case Shape.Type.Box:
						{
							BoxShape boxShape = (BoxShape)shape;

							Quat shapeRotation = body.Rotation;
							if( !shape.IsIdentityTransform )
								shapeRotation *= shape.Rotation;

							Vec3 halfD = boxShape.Dimensions * .5f;

							float r = Math.Min( Math.Min( halfD.X, halfD.Y ), halfD.Z );

							Vec3i stepsCount = new Vec3i( 1, 1, 1 );
							if( halfD.X > r * .3f )
								stepsCount.X = 2;
							if( halfD.Y > r * .3f )
								stepsCount.Y = 2;
							if( halfD.Z > r * .3f )
								stepsCount.Z = 2;

							for( int z = 0; z < stepsCount.Z; z++ )
							{
								for( int y = 0; y < stepsCount.Y; y++ )
								{
									for( int x = 0; x < stepsCount.X; x++ )
									{
										Vec3 localPos = Vec3.Zero;

										if( stepsCount.X == 2 )
											localPos.X = ( x == 0 ) ? ( -halfD.X + r ) : ( halfD.X - r );
										if( stepsCount.Y == 2 )
											localPos.Y = ( y == 0 ) ? ( -halfD.Y + r ) : ( halfD.Y - r );
										if( stepsCount.X == 2 )
											localPos.Z = ( z == 0 ) ? ( -halfD.Z + r ) : ( halfD.Z - r );

										Vec3 pos = shapePosition + shapeRotation * localPos;

										submergedItems.Add( GetSphereSubmergedCoef( new Sphere( pos, r ) ) );
									}
								}
							}
						}
						break;

					case Shape.Type.Capsule:
						{
							CapsuleShape capsuleShape = (CapsuleShape)shape;
							float r = capsuleShape.Radius;
							float l = capsuleShape.Length;

							Quat shapeRotation = body.Rotation;
							if( !shape.IsIdentityTransform )
								shapeRotation *= shape.Rotation;

							Vec3 pos;
							pos = shapePosition + shapeRotation * new Vec3( 0, 0, -l * .5f );
							submergedItems.Add( GetSphereSubmergedCoef( new Sphere( pos, r ) ) );
							pos = shapePosition + shapeRotation * new Vec3( 0, 0, l * .5f );
							submergedItems.Add( GetSphereSubmergedCoef( new Sphere( pos, r ) ) );
						}
						break;

					case Shape.Type.Sphere:
						{
							SphereShape sphereShape = (SphereShape)shape;
							float r = sphereShape.Radius;
							submergedItems.Add( GetSphereSubmergedCoef( new Sphere( shapePosition, r ) ) );
						}
						break;

					case Shape.Type.Mesh:
						{
							MeshShape meshShape = (MeshShape)shape;
							Bounds b;
							if( meshShape.GetDataBounds( out b ) )
							{
								float r = b.GetRadius();
								submergedItems.Add( GetSphereSubmergedCoef( new Sphere( shapePosition,
									r ) ) );
							}
						}
						break;
					}

					if( submergedItems.Count == 0 )
						continue;

					//calculate summary submerged coefficient and force center
					float submergedCoef;
					Vec3 submergedCenter;
					{
						if( submergedItems.Count != 1 )
						{
							submergedCoef = 0;
							submergedCenter = Vec3.Zero;

							float len = 0;
							foreach( SubmergedCheckItem item in submergedItems )
								len += item.coef;

							if( len != 0 )
							{
								float invLen = 1.0f / len;
								foreach( SubmergedCheckItem item in submergedItems )
								{
									submergedCoef += item.coef;
									submergedCenter += item.center * ( item.coef * invLen );
								}
								submergedCoef /= (float)submergedItems.Count;
							}
						}
						else
						{
							submergedCoef = submergedItems[ 0 ].coef;
							submergedCenter = submergedItems[ 0 ].center;
						}
					}

					//create splashes
					if( allowSplashes && submergedItems.Count != 0 )
					{
						const float minimalBodyVelocity = 3;
						const float minimalTimeBetweenSplashes = .25f;

						float length = body.LinearVelocity.LengthFast();
						if( length > minimalBodyVelocity )
						{
							int index = bodiesSplashOffTime.FindIndex( delegate( SplashOffItem item )
							{
								return item.body == body;
							} );

							if( index == -1 )
							{
								bool created = false;
								Vec3 splashPosition = Vec3.Zero;

								foreach( SubmergedCheckItem item in submergedItems )
								{
									if( item.coef > 0 && item.coef < 1 )
									{
										Vec3 pos = new Vec3( item.center.X, item.center.Y, Position.Z );

										//no create splashes too much nearly
										if( created && ( pos - splashPosition ).LengthFast() < .1f )
											continue;

										//create splash
										splashPosition = pos;
										CreateSplash( WaterPlaneType.SplashTypes.Body, splashPosition );
										created = true;
									}
								}

								if( created )
								{
									SplashOffItem item;
									item.body = body;
									item.remainingTime = minimalTimeBetweenSplashes;
									bodiesSplashOffTime.Add( item );
								}
							}
						}
					}

					submergedItems.Clear();

					if( submergedCoef == 0 )
						continue;

					//add forces

					float shapeDensity = GetShapeInfluenceDensity( shape );
					if( shapeDensity == 0 )
						continue;

					float densityCoef = Type.PhysicsDensity / shapeDensity;
					float mass = volume * shape.Density;

					//buoyancy force
					{
						const float roughnessLinearCoef = .5f;

						float coef = densityCoef * submergedCoef;
						coef += random.NextFloatCenter() * roughnessLinearCoef;

						Vec3 vector = -PhysicsWorld.Instance.Gravity * ( mass * coef );
						body.AddForce( ForceType.GlobalAtGlobalPos, TickDelta, vector, submergedCenter );
					}

					//linear damping
					{
						float constCoef = 2;

						float coef = submergedCoef * constCoef * densityCoef;

						Vec3 vector = -body.LinearVelocity * mass * coef;
						body.AddForce( ForceType.GlobalAtGlobalPos, TickDelta, vector, submergedCenter );
					}

					//angular damping
					{
						const float constCoef = .5f;
						const float roughnessAngularCoef = .25f;

						float coef = submergedCoef * constCoef;

						Vec3 vector = -body.AngularVelocity * mass * coef;

						float roughnessX = random.NextFloatCenter() * roughnessAngularCoef;
						float roughnessY = random.NextFloatCenter() * roughnessAngularCoef;
						float roughnessZ = random.NextFloatCenter() * roughnessAngularCoef;
						vector += new Vec3( roughnessX, roughnessY, roughnessZ );

						body.AddForce( ForceType.GlobalTorque, TickDelta, vector, Vec3.Zero );
					}
				}
			}

			listSubmergedCheckItemAllocator.Free( submergedItems );
		}

		WaterPlaneType.SplashItem[] GetSplashItemsByType( WaterPlaneType.SplashTypes splashType )
		{
			if( splashItemsCache == null )
				splashItemsCache = new Dictionary<WaterPlaneType.SplashTypes, WaterPlaneType.SplashItem[]>();

			WaterPlaneType.SplashItem[] items;
			if( !splashItemsCache.TryGetValue( splashType, out items ) )
			{
				items = Type.Splashes.FindAll( delegate( WaterPlaneType.SplashItem item )
				{
					return item.SplashType == splashType;
				} ).ToArray();
				splashItemsCache.Add( splashType, items );
			}
			return items;
		}

		public void CreateSplash( WaterPlaneType.SplashTypes splashType, Vec3 pos )
		{
			//get items array for this alias
			WaterPlaneType.SplashItem[] items = GetSplashItemsByType( splashType );
			if( items.Length == 0 )
				return;

			//random choose item
			WaterPlaneType.SplashItem item = items[ World.Instance.Random.Next( items.Length ) ];

			//create particle systems
			foreach( WaterPlaneType.SplashItem.ParticleItem particleItem in item.Particles )
			{
				Map.Instance.CreateFreeParticleSystem( particleItem.ParticleName, pos, Quat.Identity,
					new Vec3( particleItem.Scale, particleItem.Scale, particleItem.Scale ) );
			}

			//play sound
			if( !string.IsNullOrEmpty( item.SoundName ) )
			{
				Sound sound = SoundWorld.Instance.SoundCreate( item.SoundName, SoundMode.Mode3D );
				if( sound != null )
				{
					VirtualChannel channel = SoundWorld.Instance.SoundPlay( sound,
						EngineApp.Instance.DefaultSoundChannelGroup, .5f, true );
					if( channel != null )
					{
						channel.Position = pos;
						channel.Pause = false;
					}
				}
			}

			if( EntitySystemWorld.Instance.IsServer() )
				Server_SendCreateSplashToAllClients( splashType, pos );
		}

		public static WaterPlane GetWaterPlaneByBody( Body body )
		{
			return body.UserData as WaterPlane;
		}

		Vec2i GetRequiredReflectionTextureSize()
		{
			//create texture
			Vec2i textureSize = RendererWorld.Instance.DefaultViewport.DimensionsInPixels.Size;

			if( reflectionTextureSize == ReflectionTextureSizes.HalfOfFrameBuffer )
				textureSize /= 2;
			if( reflectionTextureSize == ReflectionTextureSizes.QuarterOfFrameBuffer )
				textureSize /= 4;

			if( textureSize.X == 0 )
				textureSize.X = 1;
			if( textureSize.Y == 0 )
				textureSize.Y = 1;

			return textureSize;
		}

		void CreateReflectionTexture()
		{
			DestroyReflectionTexture();

			Vec2i textureSize = GetRequiredReflectionTextureSize();

			//create render texture

			string textureName = TextureManager.Instance.GetUniqueName( "WaterPlaneReflection" );

			bool hdr = RendererWorld.Instance.DefaultViewport.GetCompositorInstance( "HDR" ) != null;

			reflectionTexture = TextureManager.Instance.Create( textureName, Texture.Type.Type2D,
				textureSize, 1, 0, hdr ? PixelFormat.Float16RGB : PixelFormat.R8G8B8,
				Texture.Usage.RenderTarget );

			reflectionRenderTexture = reflectionTexture.GetBuffer().GetRenderTarget();

			//create camera
			reflectionCamera = SceneManager.Instance.CreateCamera();
			reflectionCamera.AllowFrustumTestMode = true;

			//add viewport
			reflectionViewport = reflectionRenderTexture.AddViewport( reflectionCamera );
			reflectionViewport.ShadowsEnabled = false;
			reflectionViewport.MaterialScheme = MaterialSchemes.Low.ToString();

			//add listener
			renderTargetListener = new ReflectionRenderTargetListener( this );
			reflectionRenderTexture.AddListener( renderTargetListener );

			reflectionRenderTexture.AutoUpdate = Visible;
		}

		void DestroyReflectionTexture()
		{
			//remove reflectionTexture from material
			if( material != null )
				material.RemoveReflectionMapFromMaterial();

			if( reflectionViewport != null )
			{
				reflectionViewport.Dispose();
				reflectionViewport = null;
			}

			if( reflectionCamera != null )
			{
				reflectionCamera.Dispose();
				reflectionCamera = null;
			}

			if( renderTargetListener != null )
			{
				reflectionRenderTexture.RemoveListener( renderTargetListener );
				renderTargetListener.Dispose();
				renderTargetListener = null;
			}

			reflectionRenderTexture = null;

			if( reflectionTexture != null )
			{
				reflectionTexture.Dispose();
				reflectionTexture = null;
			}
		}

		[Browsable( false )]
		public Texture ReflectionTexture
		{
			get { return reflectionTexture; }
		}

		[DefaultValue( ReflectionLevels.OnlyModels )]
		public ReflectionLevels ReflectionLevel
		{
			get { return reflectionLevel; }
			set
			{
				if( reflectionLevel == value )
					return;
				reflectionLevel = value;
				needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		void CreateVolumeBody()
		{
			DestroyVolumeBody();

			if( !EntitySystemWorld.Instance.IsEditor() )
			{
				if( physicsHeight != 0 && Type.PhysicsDensity != 0 )
				{
					volumeBody = PhysicsWorld.Instance.CreateBody();
					volumeBody.UserData = this;
					volumeBody.Static = true;
					volumeBody.Position = Position - new Vec3( 0, 0, physicsHeight / 2 );

					BoxShape shape = volumeBody.CreateBoxShape();

					shape.Dimensions = new Vec3( Size.X, Size.Y, physicsHeight );
					shape.ContactGroup = (int)ContactGroup.NoContact;

					volumeBody.PushedToWorld = true;
				}
			}
		}

		void DestroyVolumeBody()
		{
			if( volumeBody != null )
			{
				volumeBody.Dispose();
				volumeBody = null;
			}
		}

		[DefaultValue( typeof( ColorValue ), "0 76 127" )]
		public ColorValue DeepColor
		{
			get { return deepColor; }
			set
			{
				if( deepColor == value )
					return;
				bool oldAlpha = deepColor.Alpha != 1;

				deepColor = value;

				bool alpha = deepColor.Alpha != 1;
				if( oldAlpha != alpha )
					needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[DefaultValue( typeof( ColorValue ), "0 255 255" )]
		public ColorValue ShallowColor
		{
			get { return shallowColor; }
			set
			{
				if( shallowColor == value )
					return;
				bool oldAlpha = shallowColor.Alpha != 1;

				shallowColor = value;

				bool alpha = shallowColor.Alpha != 1;
				if( oldAlpha != alpha )
					needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[DefaultValue( typeof( ColorValue ), "255 255 255" )]
		public ColorValue ReflectionColor
		{
			get { return reflectionColor; }
			set
			{
				if( reflectionColor == value )
					return;

				reflectionColor = value;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		[DefaultValue( ReflectionTextureSizes.HalfOfFrameBuffer )]
		public ReflectionTextureSizes ReflectionTextureSize
		{
			get { return reflectionTextureSize; }
			set
			{
				if( reflectionTextureSize == value )
					return;

				reflectionTextureSize = value;
				needUpdatePlane = true;

				if( EntitySystemWorld.Instance.IsServer() )
					server_shouldSendPropertiesToClients = true;
			}
		}

		void RenderSystem_RenderSystemEvent( RenderSystemEvents name )
		{
			if( name == RenderSystemEvents.DeviceRestored )
				needUpdatePlane = true;
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendPropertiesToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		void Server_SendPropertiesToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds,
				typeof( WaterPlane ), (ushort)NetworkMessages.PropertiesToClient );
			writer.Write( size );
			writer.Write( position );
			writer.Write( segments );
			writer.WriteVariableUInt32( (uint)renderQueueGroup );
			writer.WriteVariableUInt32( (uint)reflectionLevel );
			writer.Write( physicsHeight );
			writer.Write( deepColor );
			writer.Write( shallowColor );
			writer.Write( reflectionColor );
			writer.WriteVariableUInt32( (uint)reflectionTextureSize );
			writer.Write( fixedPipelineMap );
			writer.Write( fixedPipelineMapTiling );
			writer.Write( fixedPipelineColor );

			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.PropertiesToClient )]
		void Client_ReceivePropertiesToClient( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			Size = reader.ReadVec2();
			Position = reader.ReadVec3();
			Segments = reader.ReadVec2i();
			RenderQueueGroup = (RenderQueueGroupID)reader.ReadVariableUInt32();
			ReflectionLevel = (ReflectionLevels)reader.ReadVariableUInt32();
			PhysicsHeight = reader.ReadSingle();
			DeepColor = reader.ReadColorValue();
			ShallowColor = reader.ReadColorValue();
			ReflectionColor = reader.ReadColorValue();
			ReflectionTextureSize = (ReflectionTextureSizes)reader.ReadVariableUInt32();
			FixedPipelineMap = reader.ReadString();
			FixedPipelineMapTiling = reader.ReadSingle();
			FixedPipelineColor = reader.ReadColorValue();
		}

		void Server_SendCreateSplashToAllClients( WaterPlaneType.SplashTypes splashType, Vec3 pos )
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( WaterPlane ),
				(ushort)NetworkMessages.CreateSplashToClient );
			writer.WriteVariableUInt32( (uint)splashType );
			writer.Write( pos );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.CreateSplashToClient )]
		void Client_ReceiveCreateSplash( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			WaterPlaneType.SplashTypes splashType = (WaterPlaneType.SplashTypes)reader.
				ReadVariableUInt32();
			Vec3 pos = reader.ReadVec3();
			if( !reader.Complete() )
				return;
			CreateSplash( splashType, pos );
		}

	}
}
