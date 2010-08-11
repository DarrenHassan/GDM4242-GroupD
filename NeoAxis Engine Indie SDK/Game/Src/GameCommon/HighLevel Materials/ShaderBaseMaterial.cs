// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.FileSystem;
using Engine.Utils;

namespace GameCommon
{
	/// <summary>
	/// Base template for shader materials.
	/// </summary>
	[Description( "Base template for shader materials (Diffuse, Specular, Emission, Cubemap reflections, Normal mapping, Parallax mapping)." )]
	public class ShaderBaseMaterial : HighLevelMaterial
	{
		//General
		MaterialBlendingTypes blending;
		bool lighting = true;
		bool culling = true;
		bool useNormals = true;
		bool receiveShadows = true;
		CompareFunction alphaRejectFunction = CompareFunction.AlwaysPass;
		byte alphaRejectValue = 127;
		bool alphaToCoverage;
		Range fadingByDistanceRange;
		bool allowFog = true;
		bool depthWrite = true;
		bool depthTest = true;

		//Diffuse
		ColorValue diffuseColor = new ColorValue( 1, 1, 1 );
		float diffusePower = 1;
		bool diffuseScaleDynamic;
		bool diffuseVertexColor;
		MapItem diffuse1Map;
		DiffuseMapItem diffuse2Map;
		DiffuseMapItem diffuse3Map;
		DiffuseMapItem diffuse4Map;

		//Reflection
		ColorValue reflectionColor = new ColorValue( 0, 0, 0 );
		float reflectionPower = 1;
		bool reflectionScaleDynamic;
		MapItem reflectionMap;
		string reflectionSpecificCubemap = "";

		//Emission
		ColorValue emissionColor = new ColorValue( 0, 0, 0 );
		float emissionPower = 1;
		bool emissionScaleDynamic;
		MapItem emissionMap;

		//Specular
		ColorValue specularColor = new ColorValue( 0, 0, 0 );
		float specularPower = 1;
		bool specularScaleDynamic;
		MapItem specularMap;
		float specularShininess = 20;

		//Height
		MapItem normalMap;

		MapItem heightMap;
		bool heightFromNormalMapAlpha;
		float heightScale = .04f;

		//for cubemap reflections
		List<Pair<Pass, TextureUnitState>> cubemapEventUnitStates;

		//for maps animations
		List<MapItem> mapsWithAnimations;

		List<Pass> subscribedPassesForRenderObjectPass;

		string defaultTechniqueErrorString;
		bool fixedPipelineInitialized;

		///////////////////////////////////////////

		//gpu parameters constants
		public enum GpuParameters
		{
			dynamicDiffuseScale = 1,
			dynamicEmissionScale,
			dynamicReflectionScale,
			dynamicSpecularScaleAndShininess,

			fadingByDistanceRange,

			diffuse1MapTransformMul,
			diffuse1MapTransformAdd,
			diffuse2MapTransformMul,
			diffuse2MapTransformAdd,
			diffuse3MapTransformMul,
			diffuse3MapTransformAdd,
			diffuse4MapTransformMul,
			diffuse4MapTransformAdd,
			reflectionMapTransformMul,
			reflectionMapTransformAdd,
			emissionMapTransformMul,
			emissionMapTransformAdd,
			specularMapTransformMul,
			specularMapTransformAdd,
			normalMapTransformMul,
			normalMapTransformAdd,
			heightMapTransformMul,
			heightMapTransformAdd,
		}

		///////////////////////////////////////////

		public enum MaterialBlendingTypes
		{
			Opaque,
			AlphaAdd,
			AlphaBlend,
		}

		///////////////////////////////////////////

		public enum TexCoordIndexes
		{
			TexCoord0,
			TexCoord1,
			TexCoord2,
			TexCoord3,
		}

		///////////////////////////////////////////

		//for expand properties and allow change texture name from the group textbox in the propertyGrid
		public class MapItemTypeConverter : ExpandableObjectConverter
		{
			public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
			{
				return sourceType == typeof( string );
			}

			public override object ConvertFrom( ITypeDescriptorContext context,
				System.Globalization.CultureInfo culture, object value )
			{
				if( value.GetType() == typeof( string ) )
				{
					PropertyInfo property = typeof( ShaderBaseMaterial ).GetProperty(
						context.PropertyDescriptor.Name );
					MapItem map = (MapItem)property.GetValue( context.Instance, null );
					map.Texture = (string)value;
					return map;
				}
				return base.ConvertFrom( context, culture, value );
			}
		}

		///////////////////////////////////////////

		//special EditorTextureUITypeEditor for MapItem classes
		public class MapItemEditorTextureUITypeEditor : UITypeEditor
		{
			public override object EditValue( ITypeDescriptorContext context,
				IServiceProvider provider, object value )
			{
				MapItem map = (MapItem)value;

				string path = map.Texture;
				if( ResourceUtils.DoUITypeEditorEditValueDelegate( "Texture", ref path, null ) )
				{
					if( path == null )
						path = "";

					//create new MapItem and copy properties.
					//it is need for true property grid updating.
					Type type = map.GetType();
					ConstructorInfo constructor = type.GetConstructor(
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
						null, new Type[] { typeof( ShaderBaseMaterial ) }, null );
					MapItem newMap = (MapItem)constructor.Invoke( new object[] { map.owner } );
					newMap.OnClone( map );

					newMap.Texture = path;

					return newMap;
				}

				return value;
			}

			public override UITypeEditorEditStyle GetEditStyle( ITypeDescriptorContext context )
			{
				return UITypeEditorEditStyle.Modal;
			}
		}

		///////////////////////////////////////////

		[TypeConverter( typeof( MapItemTypeConverter ) )]
		[Editor( typeof( MapItemEditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
		public class MapItem
		{
			internal ShaderBaseMaterial owner;
			string texture = "";
			TexCoordIndexes texCoord = TexCoordIndexes.TexCoord0;
			bool clamp;
			TransformItem transform;

			internal List<TextureUnitState> textureUnitStatesForFixedPipeline;

			//

			internal MapItem( ShaderBaseMaterial owner )
			{
				this.owner = owner;
				transform = new TransformItem( this );
			}

			[Editor( typeof( EditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public string Texture
			{
				get { return texture; }
				set { texture = value; }
			}

			[DefaultValue( TexCoordIndexes.TexCoord0 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public TexCoordIndexes TexCoord
			{
				get { return texCoord; }
				set { texCoord = value; }
			}

			[DefaultValue( false )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public bool Clamp
			{
				get { return clamp; }
				set { clamp = value; }
			}

			public TransformItem Transform
			{
				get { return transform; }
				set { transform = value; }
			}

			public override string ToString()
			{
				if( string.IsNullOrEmpty( texture ) )
					return "";
				return texture;
			}

			public virtual void Load( TextBlock block )
			{
				if( block.IsAttributeExist( "texture" ) )
					texture = block.GetAttribute( "texture" );

				if( block.IsAttributeExist( "texCoord" ) )
					texCoord = (TexCoordIndexes)Enum.Parse( typeof( TexCoordIndexes ),
						block.GetAttribute( "texCoord" ) );

				if( block.IsAttributeExist( "clamp" ) )
					clamp = bool.Parse( block.GetAttribute( "clamp" ) );

				TextBlock transformBlock = block.FindChild( "transform" );
				if( transformBlock != null )
					transform.Load( transformBlock );
			}

			public virtual void Save( TextBlock block )
			{
				if( !string.IsNullOrEmpty( texture ) )
					block.SetAttribute( "texture", texture );

				if( texCoord != TexCoordIndexes.TexCoord0 )
					block.SetAttribute( "texCoord", texCoord.ToString() );

				if( clamp )
					block.SetAttribute( "clamp", clamp.ToString() );

				if( transform.IsDataExists() )
				{
					TextBlock transformBlock = block.AddChild( "transform" );
					transform.Save( transformBlock );
				}
			}

			public virtual bool IsDataExists()
			{
				return !string.IsNullOrEmpty( texture ) || texCoord != TexCoordIndexes.TexCoord0 ||
					clamp || transform.IsDataExists();
			}

			internal virtual void OnClone( MapItem source )
			{
				texture = source.texture;
				texCoord = source.texCoord;
				clamp = source.clamp;
				transform.OnClone( source.transform );
			}
		}

		///////////////////////////////////////////

		public class DiffuseMapItem : MapItem
		{
			public enum MapBlendingTypes
			{
				Add,
				Modulate,
				AlphaBlend,
			}

			MapBlendingTypes blending = MapBlendingTypes.Modulate;

			internal DiffuseMapItem( ShaderBaseMaterial owner )
				: base( owner )
			{
			}

			[DefaultValue( MapBlendingTypes.Modulate )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public MapBlendingTypes Blending
			{
				get { return blending; }
				set { blending = value; }
			}

			public override void Load( TextBlock block )
			{
				base.Load( block );

				if( block.IsAttributeExist( "blending" ) )
					blending = (MapBlendingTypes)Enum.Parse( typeof( MapBlendingTypes ),
						block.GetAttribute( "blending" ) );
			}

			public override void Save( TextBlock block )
			{
				base.Save( block );

				if( blending != MapBlendingTypes.Modulate )
					block.SetAttribute( "blending", blending.ToString() );
			}

			public override bool IsDataExists()
			{
				if( blending != MapBlendingTypes.Modulate )
					return true;
				return base.IsDataExists();
			}

			internal override void OnClone( MapItem source )
			{
				base.OnClone( source );
				blending = ( (DiffuseMapItem)source ).blending;
			}
		}

		///////////////////////////////////////////

		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class TransformItem
		{
			internal MapItem owner;
			Vec2 scroll;
			Vec2 scale = new Vec2( 1, 1 );
			float rotate;
			bool dynamicParameters;
			AnimationItem animation;

			//

			internal TransformItem( MapItem owner )
			{
				this.owner = owner;
				animation = new AnimationItem( this );
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( -1, 1 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public Vec2 Scroll
			{
				get { return scroll; }
				set
				{
					if( scroll == value )
						return;

					scroll = value;

					MapItem map = owner;
					map.owner.UpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			[DefaultValue( typeof( Vec2 ), "1 1" )]
			[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( .1f, 30 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public Vec2 Scale
			{
				get { return scale; }
				set
				{
					if( scale == value )
						return;

					scale = value;

					MapItem map = owner;
					map.owner.UpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			[DefaultValue( 0.0f )]
			[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( -1, 1 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public float Rotate
			{
				get { return rotate; }
				set
				{
					if( rotate == value )
						return;

					rotate = value;

					MapItem map = owner;
					map.owner.UpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			[DefaultValue( false )]
			public bool DynamicParameters
			{
				get { return dynamicParameters; }
				set { dynamicParameters = value; }
			}

			public AnimationItem Animation
			{
				get { return animation; }
				set { animation = value; }
			}

			public override string ToString()
			{
				string text = "";
				if( scroll != Vec2.Zero )
					text += string.Format( "Scroll: {0}", scroll );
				if( scale != new Vec2( 1, 1 ) )
				{
					if( text != "" )
						text += ", ";
					text += string.Format( "Scale: {0}", scale );
				}
				if( rotate != 0 )
				{
					if( text != "" )
						text += ", ";
					text += string.Format( "Rotate: {0}", rotate );
				}
				if( dynamicParameters )
				{
					if( text != "" )
						text += ", ";
					text += string.Format( "Dynamic Parameters: {0}", dynamicParameters.ToString() );
				}
				if( animation.IsDataExists() )
				{
					if( text != "" )
						text += ", ";
					text += string.Format( "Animation: {0}", animation.ToString() );
				}
				return text;
			}

			public void Load( TextBlock block )
			{
				if( block.IsAttributeExist( "scroll" ) )
					scroll = Vec2.Parse( block.GetAttribute( "scroll" ) );
				if( block.IsAttributeExist( "scale" ) )
					scale = Vec2.Parse( block.GetAttribute( "scale" ) );
				if( block.IsAttributeExist( "rotate" ) )
					rotate = float.Parse( block.GetAttribute( "rotate" ) );
				if( block.IsAttributeExist( "dynamicParameters" ) )
					dynamicParameters = bool.Parse( block.GetAttribute( "dynamicParameters" ) );

				TextBlock animationBlock = block.FindChild( "animation" );
				if( animationBlock != null )
					animation.Load( animationBlock );
			}

			public void Save( TextBlock block )
			{
				if( scroll != Vec2.Zero )
					block.SetAttribute( "scroll", scroll.ToString() );
				if( scale != new Vec2( 1, 1 ) )
					block.SetAttribute( "scale", scale.ToString() );
				if( rotate != 0 )
					block.SetAttribute( "rotate", rotate.ToString() );
				if( dynamicParameters )
					block.SetAttribute( "dynamicParameters", dynamicParameters.ToString() );

				if( animation.IsDataExists() )
				{
					TextBlock animationBlock = block.AddChild( "animation" );
					animation.Save( animationBlock );
				}
			}

			public bool IsDataExists()
			{
				return scroll != Vec2.Zero || scale != new Vec2( 1, 1 ) ||
					rotate != 0 || dynamicParameters || animation.IsDataExists();
			}

			internal void OnClone( TransformItem source )
			{
				scroll = source.scroll;
				scale = source.scale;
				rotate = source.rotate;
				dynamicParameters = source.dynamicParameters;
				animation.OnClone( source.animation );
			}
		}

		///////////////////////////////////////////

		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class AnimationItem
		{
			internal TransformItem owner;
			Vec2 scrollSpeed;
			Vec2 scrollRound;
			float rotateSpeed;

			internal AnimationItem( TransformItem owner )
			{
				this.owner = owner;
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( -3, 3 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public Vec2 ScrollSpeed
			{
				get { return scrollSpeed; }
				set
				{
					if( scrollSpeed == value )
						return;

					scrollSpeed = value;

					MapItem map = owner.owner;
					map.owner.InitializeAndUpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( 0, 1 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public Vec2 ScrollRound
			{
				get { return scrollRound; }
				set { scrollRound = value; }
			}

			[DefaultValue( 0.0f )]
			[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( -3, 3 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public float RotateSpeed
			{
				get { return rotateSpeed; }
				set
				{
					if( rotateSpeed == value )
						return;

					rotateSpeed = value;

					MapItem map = owner.owner;
					map.owner.InitializeAndUpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			public override string ToString()
			{
				string text = "";
				if( scrollSpeed != Vec2.Zero )
					text += string.Format( "Scroll: {0}", scrollSpeed );
				if( rotateSpeed != 0 )
				{
					if( text != "" )
						text += ", ";
					text += string.Format( "Rotate: {0}", rotateSpeed );
				}
				return text;
			}

			public void Load( TextBlock block )
			{
				if( block.IsAttributeExist( "scrollSpeed" ) )
					scrollSpeed = Vec2.Parse( block.GetAttribute( "scrollSpeed" ) );
				if( block.IsAttributeExist( "scrollRound" ) )
					scrollRound = Vec2.Parse( block.GetAttribute( "scrollRound" ) );
				if( block.IsAttributeExist( "rotateSpeed" ) )
					rotateSpeed = float.Parse( block.GetAttribute( "rotateSpeed" ) );
			}

			public void Save( TextBlock block )
			{
				if( scrollSpeed != Vec2.Zero )
					block.SetAttribute( "scrollSpeed", scrollSpeed.ToString() );
				if( scrollRound != Vec2.Zero )
					block.SetAttribute( "scrollRound", scrollRound.ToString() );
				if( rotateSpeed != 0 )
					block.SetAttribute( "rotateSpeed", rotateSpeed.ToString() );
			}

			public bool IsDataExists()
			{
				return scrollSpeed != Vec2.Zero || scrollRound != Vec2.Zero || rotateSpeed != 0;
			}

			internal void OnClone( AnimationItem source )
			{
				scrollSpeed = source.scrollSpeed;
				scrollRound = source.scrollRound;
				rotateSpeed = source.rotateSpeed;
			}
		}

		///////////////////////////////////////////

		[Category( "_ShaderBase" )]
		[DefaultValue( MaterialBlendingTypes.Opaque )]
		public MaterialBlendingTypes Blending
		{
			get { return blending; }
			set { blending = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( true )]
		public bool Lighting
		{
			get { return lighting; }
			set { lighting = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( true )]
		public bool Culling
		{
			get { return culling; }
			set { culling = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( true )]
		public bool UseNormals
		{
			get { return useNormals; }
			set { useNormals = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( true )]
		public bool ReceiveShadows
		{
			get { return receiveShadows; }
			set { receiveShadows = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( CompareFunction.AlwaysPass )]
		public CompareFunction AlphaRejectFunction
		{
			get { return alphaRejectFunction; }
			set { alphaRejectFunction = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( (byte)127 )]
		public byte AlphaRejectValue
		{
			get { return alphaRejectValue; }
			set { alphaRejectValue = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( false )]
		public bool AlphaToCoverage
		{
			get { return alphaToCoverage; }
			set { alphaToCoverage = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( typeof( Range ), "0 0" )]
		public Range FadingByDistanceRange
		{
			get { return fadingByDistanceRange; }
			set
			{
				if( fadingByDistanceRange == value )
					return;
				fadingByDistanceRange = value;
				UpdateFadingByDistanceRangeGpuParameter();
			}
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( true )]
		public bool AllowFog
		{
			get { return allowFog; }
			set { allowFog = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( true )]
		[Description( "Depth write flag will be automatically disabled if \"Blending\" not equal to \"Opaque\"." )]
		public bool DepthWrite
		{
			get { return depthWrite; }
			set { depthWrite = value; }
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( true )]
		public bool DepthTest
		{
			get { return depthTest; }
			set { depthTest = value; }
		}

		[Category( "Diffuse" )]
		[DefaultValue( typeof( ColorValue ), "255 255 255" )]
		public ColorValue DiffuseColor
		{
			get { return diffuseColor; }
			set
			{
				if( diffuseColor == value )
					return;
				diffuseColor = value;
				UpdateDynamicDiffuseScaleGpuParameter();
			}
		}

		[Category( "Diffuse" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 10 )]
		public float DiffusePower
		{
			get { return diffusePower; }
			set
			{
				if( diffusePower == value )
					return;
				diffusePower = value;
				UpdateDynamicDiffuseScaleGpuParameter();
			}
		}

		[Category( "Diffuse" )]
		[DefaultValue( false )]
		public bool DiffuseScaleDynamic
		{
			get { return diffuseScaleDynamic; }
			set { diffuseScaleDynamic = value; }
		}

		[Category( "Diffuse" )]
		[DefaultValue( false )]
		public bool DiffuseVertexColor
		{
			get { return diffuseVertexColor; }
			set { diffuseVertexColor = value; }
		}

		[Category( "Diffuse" )]
		public MapItem Diffuse1Map
		{
			get { return diffuse1Map; }
			set { diffuse1Map = value; }
		}

		[Category( "Diffuse" )]
		public DiffuseMapItem Diffuse2Map
		{
			get { return diffuse2Map; }
			set { diffuse2Map = value; }
		}

		[Category( "Diffuse" )]
		public DiffuseMapItem Diffuse3Map
		{
			get { return diffuse3Map; }
			set { diffuse3Map = value; }
		}

		[Category( "Diffuse" )]
		public DiffuseMapItem Diffuse4Map
		{
			get { return diffuse4Map; }
			set { diffuse4Map = value; }
		}

		[Category( "Reflection Cubemap" )]
		[DefaultValue( typeof( ColorValue ), "0 0 0" )]
		public ColorValue ReflectionColor
		{
			get { return reflectionColor; }
			set
			{
				if( reflectionColor == value )
					return;
				reflectionColor = value;
				UpdateDynamicReflectionScaleGpuParameter();
			}
		}

		[Category( "Reflection Cubemap" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 10 )]
		public float ReflectionPower
		{
			get { return reflectionPower; }
			set
			{
				if( reflectionPower == value )
					return;
				reflectionPower = value;
				UpdateDynamicReflectionScaleGpuParameter();
			}
		}

		[Category( "Reflection Cubemap" )]
		[DefaultValue( false )]
		public bool ReflectionScaleDynamic
		{
			get { return reflectionScaleDynamic; }
			set { reflectionScaleDynamic = value; }
		}

		[Category( "Reflection Cubemap" )]
		public MapItem ReflectionMap
		{
			get { return reflectionMap; }
			set { reflectionMap = value; }
		}

		[Category( "Reflection Cubemap" )]
		[Editor( typeof( EditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
		[EditorTextureType( Texture.Type.CubeMap )]
		public string ReflectionSpecificCubemap
		{
			get { return reflectionSpecificCubemap; }
			set { reflectionSpecificCubemap = value; }
		}

		[Category( "Emission" )]
		[DefaultValue( typeof( ColorValue ), "0 0 0" )]
		public ColorValue EmissionColor
		{
			get { return emissionColor; }
			set
			{
				if( emissionColor == value )
					return;
				emissionColor = value;
				UpdateDynamicEmissionScaleGpuParameter();
			}
		}

		[Category( "Emission" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 10 )]
		public float EmissionPower
		{
			get { return emissionPower; }
			set
			{
				if( emissionPower == value )
					return;
				emissionPower = value;
				UpdateDynamicEmissionScaleGpuParameter();
			}
		}

		[Category( "Emission" )]
		[DefaultValue( false )]
		public bool EmissionScaleDynamic
		{
			get { return emissionScaleDynamic; }
			set { emissionScaleDynamic = value; }
		}

		[Category( "Emission" )]
		public MapItem EmissionMap
		{
			get { return emissionMap; }
			set { emissionMap = value; }
		}

		[Category( "Specular" )]
		[DefaultValue( typeof( ColorValue ), "0 0 0" )]
		public ColorValue SpecularColor
		{
			get { return specularColor; }
			set
			{
				if( specularColor == value )
					return;
				specularColor = value;
				UpdateDynamicSpecularScaleAndShininessGpuParameter();
			}
		}

		[Category( "Specular" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 10 )]
		public float SpecularPower
		{
			get { return specularPower; }
			set
			{
				if( specularPower == value )
					return;
				specularPower = value;
				UpdateDynamicSpecularScaleAndShininessGpuParameter();
			}
		}

		[Category( "Specular" )]
		[DefaultValue( false )]
		public bool SpecularScaleDynamic
		{
			get { return specularScaleDynamic; }
			set { specularScaleDynamic = value; }
		}

		[Category( "Specular" )]
		public MapItem SpecularMap
		{
			get { return specularMap; }
			set { specularMap = value; }
		}

		[Category( "Specular" )]
		[DefaultValue( 20.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 100 )]
		public float SpecularShininess
		{
			get { return specularShininess; }
			set
			{
				if( specularShininess == value )
					return;
				specularShininess = value;
				UpdateDynamicSpecularScaleAndShininessGpuParameter();
			}
		}

		[Category( "Height" )]
		public MapItem NormalMap
		{
			get { return normalMap; }
			set { normalMap = value; }
		}

		[Category( "Height" )]
		public MapItem HeightMap
		{
			get { return heightMap; }
			set { heightMap = value; }
		}

		[Category( "Height" )]
		[DefaultValue( false )]
		public bool HeightFromNormalMapAlpha
		{
			get { return heightFromNormalMapAlpha; }
			set { heightFromNormalMapAlpha = value; }
		}

		[Category( "Height" )]
		[DefaultValue( .04f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .01f, .1f )]
		public float HeightScale
		{
			get { return heightScale; }
			set { heightScale = value; }
		}

		public ShaderBaseMaterial()
		{
			diffuse1Map = new MapItem( this );
			diffuse2Map = new DiffuseMapItem( this );
			diffuse3Map = new DiffuseMapItem( this );
			diffuse4Map = new DiffuseMapItem( this );
			reflectionMap = new MapItem( this );
			emissionMap = new MapItem( this );
			specularMap = new MapItem( this );
			normalMap = new MapItem( this );
			heightMap = new MapItem( this );

			SceneManager.Instance.FogChanged += SceneManager_FogChanged;
			SceneManager.Instance.ShadowSettingsChanged += SceneManager_ShadowSettingsChanged;
		}

		public override void Dispose()
		{
			SceneManager.Instance.ShadowSettingsChanged -= SceneManager_ShadowSettingsChanged;
			SceneManager.Instance.FogChanged -= SceneManager_FogChanged;
			base.Dispose();
		}

		protected override void OnClone( HighLevelMaterial sourceMaterial )
		{
			base.OnClone( sourceMaterial );

			ShaderBaseMaterial source = (ShaderBaseMaterial)sourceMaterial;

			//General
			blending = source.blending;
			lighting = source.lighting;
			culling = source.culling;
			useNormals = source.useNormals;
			receiveShadows = source.receiveShadows;
			alphaRejectFunction = source.alphaRejectFunction;
			alphaRejectValue = source.alphaRejectValue;
			alphaToCoverage = source.alphaToCoverage;
			fadingByDistanceRange = source.fadingByDistanceRange;
			allowFog = source.allowFog;
			depthWrite = source.depthWrite;
			depthTest = source.depthTest;

			//Diffuse
			diffuseColor = source.diffuseColor;
			diffusePower = source.diffusePower;
			diffuseScaleDynamic = source.diffuseScaleDynamic;
			diffuseVertexColor = source.diffuseVertexColor;
			diffuse1Map.OnClone( source.diffuse1Map );
			diffuse2Map.OnClone( source.diffuse2Map );
			diffuse3Map.OnClone( source.diffuse3Map );
			diffuse4Map.OnClone( source.diffuse4Map );

			//Reflection
			reflectionColor = source.reflectionColor;
			reflectionPower = source.reflectionPower;
			reflectionScaleDynamic = source.reflectionScaleDynamic;
			reflectionMap.OnClone( source.reflectionMap );
			reflectionSpecificCubemap = source.reflectionSpecificCubemap;

			//Emission
			emissionColor = source.emissionColor;
			emissionPower = source.emissionPower;
			emissionScaleDynamic = source.emissionScaleDynamic;
			emissionMap.OnClone( source.emissionMap );

			//Specular
			specularColor = source.specularColor;
			specularPower = source.specularPower;
			specularScaleDynamic = source.specularScaleDynamic;
			specularMap.OnClone( source.specularMap );
			specularShininess = source.specularShininess;

			//Height
			normalMap.OnClone( source.normalMap );
			heightMap.OnClone( source.heightMap );
			heightFromNormalMapAlpha = source.heightFromNormalMapAlpha;
			heightScale = source.heightScale;
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			//General
			{
				if( block.IsAttributeExist( "blending" ) )
					blending = (MaterialBlendingTypes)Enum.Parse(
						typeof( MaterialBlendingTypes ), block.GetAttribute( "blending" ) );

				if( block.IsAttributeExist( "lighting" ) )
					lighting = bool.Parse( block.GetAttribute( "lighting" ) );

				if( block.IsAttributeExist( "culling" ) )
					culling = bool.Parse( block.GetAttribute( "culling" ) );

				if( block.IsAttributeExist( "useNormals" ) )
					useNormals = bool.Parse( block.GetAttribute( "useNormals" ) );

				if( block.IsAttributeExist( "receiveShadows" ) )
					receiveShadows = bool.Parse( block.GetAttribute( "receiveShadows" ) );

				if( block.IsAttributeExist( "alphaRejectFunction" ) )
					alphaRejectFunction = (CompareFunction)Enum.Parse( typeof( CompareFunction ),
						block.GetAttribute( "alphaRejectFunction" ) );

				if( block.IsAttributeExist( "alphaRejectValue" ) )
					alphaRejectValue = byte.Parse( block.GetAttribute( "alphaRejectValue" ) );

				if( block.IsAttributeExist( "alphaToCoverage" ) )
					alphaToCoverage = bool.Parse( block.GetAttribute( "alphaToCoverage" ) );

				if( block.IsAttributeExist( "fadingByDistanceRange" ) )
					fadingByDistanceRange = Range.Parse( block.GetAttribute( "fadingByDistanceRange" ) );

				if( block.IsAttributeExist( "allowFog" ) )
					allowFog = bool.Parse( block.GetAttribute( "allowFog" ) );

				if( block.IsAttributeExist( "depthWrite" ) )
					depthWrite = bool.Parse( block.GetAttribute( "depthWrite" ) );

				if( block.IsAttributeExist( "depthTest" ) )
					depthTest = bool.Parse( block.GetAttribute( "depthTest" ) );
			}

			//Diffuse
			{
				//old version compatibility
				if( block.IsAttributeExist( "diffuseScale" ) )
				{
					diffuseColor = ColorValue.Parse( block.GetAttribute( "diffuseScale" ) );
					float power = Math.Max( Math.Max( diffuseColor.Red, diffuseColor.Green ),
						diffuseColor.Blue );
					if( power > 1 )
					{
						diffuseColor.Red /= power;
						diffuseColor.Green /= power;
						diffuseColor.Blue /= power;
						diffusePower = power;
					}
				}

				if( block.IsAttributeExist( "diffuseColor" ) )
					diffuseColor = ColorValue.Parse( block.GetAttribute( "diffuseColor" ) );
				if( block.IsAttributeExist( "diffusePower" ) )
					diffusePower = float.Parse( block.GetAttribute( "diffusePower" ) );

				if( block.IsAttributeExist( "diffuseScaleDynamic" ) )
					diffuseScaleDynamic = bool.Parse( block.GetAttribute( "diffuseScaleDynamic" ) );

				if( block.IsAttributeExist( "diffuseVertexColor" ) )
					diffuseVertexColor = bool.Parse( block.GetAttribute( "diffuseVertexColor" ) );

				TextBlock diffuse1MapBlock = block.FindChild( "diffuse1Map" );
				if( diffuse1MapBlock != null )
					diffuse1Map.Load( diffuse1MapBlock );

				TextBlock diffuse2MapBlock = block.FindChild( "diffuse2Map" );
				if( diffuse2MapBlock != null )
					diffuse2Map.Load( diffuse2MapBlock );

				TextBlock diffuse3MapBlock = block.FindChild( "diffuse3Map" );
				if( diffuse3MapBlock != null )
					diffuse3Map.Load( diffuse3MapBlock );

				TextBlock diffuse4MapBlock = block.FindChild( "diffuse4Map" );
				if( diffuse4MapBlock != null )
					diffuse4Map.Load( diffuse4MapBlock );

				//old version compatibility
				if( block.IsAttributeExist( "diffuseMap" ) )
					diffuse1Map.Texture = block.GetAttribute( "diffuseMap" );
			}

			//Reflection
			{
				if( block.IsAttributeExist( "reflectionScale" ) )
				{
					reflectionColor = ColorValue.Parse( block.GetAttribute( "reflectionScale" ) );
					float power = Math.Max( Math.Max( reflectionColor.Red, reflectionColor.Green ),
						Math.Max( reflectionColor.Blue, reflectionColor.Alpha ) );
					if( power > 1 )
					{
						reflectionColor /= power;
						reflectionPower = power;
					}
				}

				if( block.IsAttributeExist( "reflectionColor" ) )
					reflectionColor = ColorValue.Parse( block.GetAttribute( "reflectionColor" ) );
				if( block.IsAttributeExist( "reflectionPower" ) )
					reflectionPower = float.Parse( block.GetAttribute( "reflectionPower" ) );

				if( block.IsAttributeExist( "reflectionScaleDynamic" ) )
					reflectionScaleDynamic = bool.Parse( block.GetAttribute( "reflectionScaleDynamic" ) );

				TextBlock reflectionMapBlock = block.FindChild( "reflectionMap" );
				if( reflectionMapBlock != null )
					reflectionMap.Load( reflectionMapBlock );

				if( block.IsAttributeExist( "reflectionSpecificCubemap" ) )
					reflectionSpecificCubemap = block.GetAttribute( "reflectionSpecificCubemap" );

				//old version compatibility
				if( block.IsAttributeExist( "reflectionMap" ) )
					reflectionMap.Texture = block.GetAttribute( "reflectionMap" );
			}

			//Emission
			{
				if( block.IsAttributeExist( "emissionScale" ) )
				{
					emissionColor = ColorValue.Parse( block.GetAttribute( "emissionScale" ) );
					float power = Math.Max( Math.Max( emissionColor.Red, emissionColor.Green ),
						Math.Max( emissionColor.Blue, emissionColor.Alpha ) );
					if( power > 1 )
					{
						emissionColor /= power;
						emissionPower = power;
					}
				}

				if( block.IsAttributeExist( "emissionColor" ) )
					emissionColor = ColorValue.Parse( block.GetAttribute( "emissionColor" ) );
				if( block.IsAttributeExist( "emissionPower" ) )
					emissionPower = float.Parse( block.GetAttribute( "emissionPower" ) );

				if( block.IsAttributeExist( "emissionScaleDynamic" ) )
					emissionScaleDynamic = bool.Parse( block.GetAttribute( "emissionScaleDynamic" ) );

				TextBlock emissionMapBlock = block.FindChild( "emissionMap" );
				if( emissionMapBlock != null )
					emissionMap.Load( emissionMapBlock );

				//old version compatibility
				if( block.IsAttributeExist( "emissionMap" ) )
					emissionMap.Texture = block.GetAttribute( "emissionMap" );
			}

			//Specular
			{
				if( block.IsAttributeExist( "specularScale" ) )
				{
					specularColor = ColorValue.Parse( block.GetAttribute( "specularScale" ) );
					float power = Math.Max( Math.Max( specularColor.Red, specularColor.Green ),
						Math.Max( specularColor.Blue, specularColor.Alpha ) );
					if( power > 1 )
					{
						specularColor /= power;
						specularPower = power;
					}
				}

				if( block.IsAttributeExist( "specularColor" ) )
					specularColor = ColorValue.Parse( block.GetAttribute( "specularColor" ) );
				if( block.IsAttributeExist( "specularPower" ) )
					specularPower = float.Parse( block.GetAttribute( "specularPower" ) );

				if( block.IsAttributeExist( "specularScaleDynamic" ) )
					specularScaleDynamic = bool.Parse( block.GetAttribute( "specularScaleDynamic" ) );

				TextBlock specularMapBlock = block.FindChild( "specularMap" );
				if( specularMapBlock != null )
					specularMap.Load( specularMapBlock );

				if( block.IsAttributeExist( "specularShininess" ) )
					specularShininess = float.Parse( block.GetAttribute( "specularShininess" ) );

				//old version compatibility
				if( block.IsAttributeExist( "specularMap" ) )
					specularMap.Texture = block.GetAttribute( "specularMap" );
			}

			//Height
			{
				TextBlock normalMapBlock = block.FindChild( "normalMap" );
				if( normalMapBlock != null )
					normalMap.Load( normalMapBlock );

				TextBlock heightMapBlock = block.FindChild( "heightMap" );
				if( heightMapBlock != null )
					heightMap.Load( heightMapBlock );

				if( block.IsAttributeExist( "heightFromNormalMapAlpha" ) )
					heightFromNormalMapAlpha = bool.Parse( block.GetAttribute( "heightFromNormalMapAlpha" ) );

				if( block.IsAttributeExist( "heightScale" ) )
					heightScale = float.Parse( block.GetAttribute( "heightScale" ) );

				//old version compatibility
				if( block.IsAttributeExist( "normalMap" ) )
					normalMap.Texture = block.GetAttribute( "normalMap" );
				if( block.IsAttributeExist( "heightMap" ) )
					heightMap.Texture = block.GetAttribute( "heightMap" );
			}

			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			//General
			{
				if( blending != MaterialBlendingTypes.Opaque )
					block.SetAttribute( "blending", blending.ToString() );

				if( !lighting )
					block.SetAttribute( "lighting", lighting.ToString() );

				if( !culling )
					block.SetAttribute( "culling", culling.ToString() );

				if( !useNormals )
					block.SetAttribute( "useNormals", useNormals.ToString() );

				if( !receiveShadows )
					block.SetAttribute( "receiveShadows", receiveShadows.ToString() );

				if( alphaRejectFunction != CompareFunction.AlwaysPass )
					block.SetAttribute( "alphaRejectFunction", alphaRejectFunction.ToString() );

				if( alphaRejectValue != 127 )
					block.SetAttribute( "alphaRejectValue", alphaRejectValue.ToString() );

				if( alphaToCoverage )
					block.SetAttribute( "alphaToCoverage", alphaToCoverage.ToString() );

				if( fadingByDistanceRange != new Range( 0, 0 ) )
					block.SetAttribute( "fadingByDistanceRange", fadingByDistanceRange.ToString() );

				if( !allowFog )
					block.SetAttribute( "allowFog", allowFog.ToString() );

				if( !depthWrite )
					block.SetAttribute( "depthWrite", depthWrite.ToString() );

				if( !depthTest )
					block.SetAttribute( "depthTest", depthTest.ToString() );
			}

			//Diffuse
			{
				if( diffuseColor != new ColorValue( 1, 1, 1 ) )
					block.SetAttribute( "diffuseColor", diffuseColor.ToString() );
				if( diffusePower != 1 )
					block.SetAttribute( "diffusePower", diffusePower.ToString() );

				if( diffuseScaleDynamic )
					block.SetAttribute( "diffuseScaleDynamic", diffuseScaleDynamic.ToString() );

				if( diffuseVertexColor )
					block.SetAttribute( "diffuseVertexColor", diffuseVertexColor.ToString() );

				if( diffuse1Map.IsDataExists() )
				{
					TextBlock diffuse1MapBlock = block.AddChild( "diffuse1Map" );
					diffuse1Map.Save( diffuse1MapBlock );
				}

				if( diffuse2Map.IsDataExists() )
				{
					TextBlock diffuse2MapBlock = block.AddChild( "diffuse2Map" );
					diffuse2Map.Save( diffuse2MapBlock );
				}

				if( diffuse3Map.IsDataExists() )
				{
					TextBlock diffuse3MapBlock = block.AddChild( "diffuse3Map" );
					diffuse3Map.Save( diffuse3MapBlock );
				}

				if( diffuse4Map.IsDataExists() )
				{
					TextBlock diffuse4MapBlock = block.AddChild( "diffuse4Map" );
					diffuse4Map.Save( diffuse4MapBlock );
				}
			}

			//Reflection
			{
				if( reflectionColor != new ColorValue( 0, 0, 0 ) )
					block.SetAttribute( "reflectionColor", reflectionColor.ToString() );
				if( reflectionPower != 1 )
					block.SetAttribute( "reflectionPower", reflectionPower.ToString() );

				if( reflectionScaleDynamic )
					block.SetAttribute( "reflectionScaleDynamic", reflectionScaleDynamic.ToString() );

				if( reflectionMap.IsDataExists() )
				{
					TextBlock reflectionMapBlock = block.AddChild( "reflectionMap" );
					reflectionMap.Save( reflectionMapBlock );
				}

				if( !string.IsNullOrEmpty( reflectionSpecificCubemap ) )
					block.SetAttribute( "reflectionSpecificCubemap", reflectionSpecificCubemap );
			}

			//Emission
			{
				if( emissionColor != new ColorValue( 0, 0, 0 ) )
					block.SetAttribute( "emissionColor", emissionColor.ToString() );
				if( emissionPower != 1 )
					block.SetAttribute( "emissionPower", emissionPower.ToString() );

				if( emissionScaleDynamic )
					block.SetAttribute( "emissionScaleDynamic", emissionScaleDynamic.ToString() );

				if( emissionMap.IsDataExists() )
				{
					TextBlock emissionMapBlock = block.AddChild( "emissionMap" );
					emissionMap.Save( emissionMapBlock );
				}
			}

			//Specular
			{
				if( specularColor != new ColorValue( 0, 0, 0 ) )
					block.SetAttribute( "specularColor", specularColor.ToString() );
				if( specularPower != 1 )
					block.SetAttribute( "specularPower", specularPower.ToString() );

				if( specularScaleDynamic )
					block.SetAttribute( "specularScaleDynamic", specularScaleDynamic.ToString() );

				if( specularMap.IsDataExists() )
				{
					TextBlock specularMapBlock = block.AddChild( "specularMap" );
					specularMap.Save( specularMapBlock );
				}

				if( specularShininess != 20 )
					block.SetAttribute( "specularShininess", specularShininess.ToString() );
			}

			//Height
			{
				if( normalMap.IsDataExists() )
				{
					TextBlock normalMapBlock = block.AddChild( "normalMap" );
					normalMap.Save( normalMapBlock );
				}

				if( heightFromNormalMapAlpha )
					block.SetAttribute( "heightFromNormalMapAlpha", heightFromNormalMapAlpha.ToString() );

				if( heightMap.IsDataExists() )
				{
					TextBlock heightMapBlock = block.AddChild( "heightMap" );
					heightMap.Save( heightMapBlock );
				}

				if( heightScale != .04f )
					block.SetAttribute( "heightScale", heightScale.ToString() );
			}
		}

		protected virtual void OnSetProgramAutoConstants(
			GpuProgramParameters parameters, int lightCount )
		{
			parameters.SetNamedAutoConstant( "worldMatrix",
				GpuProgramParameters.AutoConstantType.WorldMatrix );
			parameters.SetNamedAutoConstant( "worldViewMatrix",
				GpuProgramParameters.AutoConstantType.WorldViewMatrix );
			parameters.SetNamedAutoConstant( "worldViewProjMatrix",
				GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
			parameters.SetNamedAutoConstant( "viewProjMatrix",
				GpuProgramParameters.AutoConstantType.ViewProjMatrix );
			parameters.SetNamedAutoConstant( "cameraPositionObjectSpace",
				GpuProgramParameters.AutoConstantType.CameraPositionObjectSpace );
			parameters.SetNamedAutoConstant( "cameraPosition",
				GpuProgramParameters.AutoConstantType.CameraPosition );
			parameters.SetNamedAutoConstant( "farClipDistance",
				GpuProgramParameters.AutoConstantType.FarClipDistance );

			parameters.SetNamedAutoConstant( "texelOffsets",
				GpuProgramParameters.AutoConstantType.TexelOffsets );
			parameters.SetNamedAutoConstant( "alphaRejectValue",
				GpuProgramParameters.AutoConstantType.AlphaRejectValue );

			parameters.SetNamedAutoConstant( "disableFetch4ForBrokenDrivers",
				GpuProgramParameters.AutoConstantType.DisableFetch4ForBrokenDrivers );

			//Light
			parameters.SetNamedAutoConstant( "ambientLightColor",
				GpuProgramParameters.AutoConstantType.AmbientLightColor );

			if( lightCount != 0 )
			{
				parameters.SetNamedAutoConstant( "textureViewProjMatrix0",
					GpuProgramParameters.AutoConstantType.TextureViewProjMatrix, 0 );
				parameters.SetNamedAutoConstant( "textureViewProjMatrix1",
					GpuProgramParameters.AutoConstantType.TextureViewProjMatrix, 1 );
				parameters.SetNamedAutoConstant( "textureViewProjMatrix2",
					GpuProgramParameters.AutoConstantType.TextureViewProjMatrix, 2 );
				parameters.SetNamedAutoConstant( "lightShadowFarClipDistance",
					GpuProgramParameters.AutoConstantType.LightShadowFarClipDistance, 0 );
				parameters.SetNamedAutoConstant( "shadowFarDistance",
					GpuProgramParameters.AutoConstantType.ShadowFarDistance );
				parameters.SetNamedAutoConstant( "shadowColorIntensity",
					GpuProgramParameters.AutoConstantType.ShadowColorIntensity );
				parameters.SetNamedAutoConstant( "shadowTextureSizes",
					GpuProgramParameters.AutoConstantType.ShadowTextureSizes );
				parameters.SetNamedAutoConstant( "shadowDirectionalLightSplitDistances",
					GpuProgramParameters.AutoConstantType.ShadowDirectionalLightSplitDistances );
				parameters.SetNamedAutoConstant( "lightPositionArray",
					GpuProgramParameters.AutoConstantType.LightPositionArray, lightCount );
				parameters.SetNamedAutoConstant( "lightPositionObjectSpaceArray",
					GpuProgramParameters.AutoConstantType.LightPositionObjectSpaceArray, lightCount );
				parameters.SetNamedAutoConstant( "lightDirectionArray",
					GpuProgramParameters.AutoConstantType.LightDirectionArray, lightCount );
				parameters.SetNamedAutoConstant( "lightDirectionObjectSpaceArray",
					GpuProgramParameters.AutoConstantType.LightDirectionObjectSpaceArray, lightCount );
				parameters.SetNamedAutoConstant( "lightAttenuationArray",
					GpuProgramParameters.AutoConstantType.LightAttenuationArray, lightCount );
				parameters.SetNamedAutoConstant( "lightDiffuseColorPowerScaledArray",
					GpuProgramParameters.AutoConstantType.LightDiffuseColorPowerScaledArray, lightCount );
				parameters.SetNamedAutoConstant( "lightSpecularColorPowerScaledArray",
					GpuProgramParameters.AutoConstantType.LightSpecularColorPowerScaledArray, lightCount );
				parameters.SetNamedAutoConstant( "spotLightParamsArray",
					GpuProgramParameters.AutoConstantType.SpotLightParamsArray, lightCount );
				parameters.SetNamedAutoConstant( "lightCastShadowsArray",
					GpuProgramParameters.AutoConstantType.LightCastShadowsArray, lightCount );
			}

			//Fog
			parameters.SetNamedAutoConstant( "fogParams",
				GpuProgramParameters.AutoConstantType.FogParams );
			parameters.SetNamedAutoConstant( "fogColor",
				GpuProgramParameters.AutoConstantType.FogColor );

			//Time
			//1 hour interval. for better precision
			parameters.SetNamedAutoConstantFloat( "time",
				GpuProgramParameters.AutoConstantType.Time0X, 3600.0f );

			//lightmap
			parameters.SetNamedAutoConstant( "lightmapUVTransform",
				GpuProgramParameters.AutoConstantType.LightmapUVTransform );

			//clip planes
			for( int n = 0; n < 6; n++ )
			{
				parameters.SetNamedAutoConstant( "clipPlane" + n.ToString(),
					GpuProgramParameters.AutoConstantType.ClipPlane, n );
			}

			parameters.SetNamedAutoConstant( "instancing",
				GpuProgramParameters.AutoConstantType.Instancing );
		}

		protected virtual string OnGetExtensionFileName()
		{
			return null;
		}

		void GenerateTexCoordString( StringBuilder builder, int texCoord, TransformItem transformItem,
			string transformGpuParameterNamePrefix )
		{
			if( transformItem.IsDataExists() )
			{
				builder.AppendFormat(
					"mul(float2x2({0}Mul.x,{0}Mul.y,{0}Mul.z,{0}Mul.w),texCoord{1})+{0}Add",
					transformGpuParameterNamePrefix, texCoord );
			}
			else
			{
				builder.AppendFormat( "texCoord{0}", texCoord );
			}
		}

		protected virtual bool OnIsNeedSpecialShadowCasterMaterial()
		{
			if( AlphaRejectFunction != CompareFunction.AlwaysPass )
				return true;
			return false;
		}

		protected virtual void OnAddCompileArguments( StringBuilder arguments ) { }

		bool CreateDefaultTechnique( out bool shadersIsNotSupported )
		{
			shadersIsNotSupported = false;

			string sourceFile = "Materials\\Common\\ShaderBase.cg_hlsl";
			string vertexSyntax;
			string fragmentSyntax;
			int maxSamplerCount = 16;
			int maxTexCoordCount = 8;

			if( RenderSystem.Instance.IsDirect3D() )
			{
				vertexSyntax = "vs_2_0";
				fragmentSyntax = "ps_2_0";
			}
			else
			{
				vertexSyntax = "arbvp1";
				fragmentSyntax = "arbfp1";
			}

			//technique is supported?
			{
				if( !GpuProgramManager.Instance.IsSyntaxSupported( fragmentSyntax ) )
				{
					defaultTechniqueErrorString = string.Format(
						"The fragment shaders ({0}) are not supported.", fragmentSyntax );
					shadersIsNotSupported = true;
					return false;
				}

				if( !GpuProgramManager.Instance.IsSyntaxSupported( vertexSyntax ) )
				{
					defaultTechniqueErrorString = string.Format(
						"The vertex shaders ({0}) are not supported.", vertexSyntax );
					shadersIsNotSupported = true;
					return false;
				}
			}

			//switch to shader model 3. it need for high shadowmaps.
			if( RenderSystem.Instance.HasShaderModel3() )
			{
				if( RenderSystem.Instance.IsDirect3D() )
				{
					vertexSyntax = "vs_3_0";
					fragmentSyntax = "ps_3_0";
				}
				else
				{
					vertexSyntax = "arbvp1";
					fragmentSyntax = "arbfp1";
				}
				maxTexCoordCount = 10;
			}

			bool atiHardwareShadows = false;
			bool nvidiaHardwareShadows = false;
			{
				if( RenderSystem.Instance.HasShaderModel3() &&
					TextureManager.Instance.IsFormatSupported( Texture.Type.Type2D,
					PixelFormat.Depth24, Texture.Usage.RenderTarget ) )
				{
					if( RenderSystem.Instance.Capabilities.Vendor == GPUVendors.ATI )
						atiHardwareShadows = true;
					if( RenderSystem.Instance.Capabilities.Vendor == GPUVendors.NVidia )
						nvidiaHardwareShadows = true;
				}
			}


			BaseMaterial.ReceiveShadows = receiveShadows;

			//create techniques
			foreach( MaterialSchemes materialScheme in Enum.GetValues( typeof( MaterialSchemes ) ) )
			{
				Technique technique = BaseMaterial.CreateTechnique();
				technique.SchemeName = materialScheme.ToString();

				//for Shader Model 2 or for stencil shadows
				//pass 0: ambient pass
				//pass 1: directional light
				//pass 2: point light
				//pass 3: spot light

				//for Shader Model 3
				//pass 0: ambient pass
				//pass 1: ambient pass + first directional light
				//pass 2: directional light (ignore first directional light)
				//pass 3: point light
				//pass 4: spot light

				bool mergeAmbientAndDirectionalLightPasses = true;
				if( SceneManager.Instance.IsShadowTechniqueStencilBased() )
					mergeAmbientAndDirectionalLightPasses = false;
				if( !RenderSystem.Instance.HasShaderModel3() )
					mergeAmbientAndDirectionalLightPasses = false;

				int passCount;
				if( lighting )
					passCount = mergeAmbientAndDirectionalLightPasses ? 5 : 4;
				else
					passCount = 1;

				for( int nPass = 0; nPass < passCount; nPass++ )
				{
					//create pass
					Pass pass = technique.CreatePass();
					Pass shadowCasterPass = null;

					pass.DepthWrite = depthWrite;
					pass.DepthCheck = depthTest;

					bool ambientPass;
					bool lightPass;

					RenderLightType lightType = RenderLightType.Directional;

					if( lighting )
					{
						if( mergeAmbientAndDirectionalLightPasses )
						{
							ambientPass = nPass <= 1;
							lightPass = nPass >= 1;

							switch( nPass )
							{
							case 0:
								//ambient only. this pass skipped when exists directional light.
								pass.SpecialRendering = true;
								pass.SpecialRenderingAllowOnlyNotExistsSpecificLights = true;
								pass.SpecialRenderingLightType = RenderLightType.Directional;
								break;
							case 1:
								//ambient + first directional light
								lightType = RenderLightType.Directional;
								pass.SpecialRendering = true;
								pass.SpecialRenderingAllowOnlyExistsSpecificLights = true;
								pass.SpecialRenderingMaxLightCount = 1;
								pass.SpecialRenderingLightType = lightType;
								break;
							case 2:
								//directional light (ignore first directional light)
								lightType = RenderLightType.Directional;
								pass.SpecialRendering = true;
								pass.SpecialRenderingIteratePerLight = true;
								pass.SpecialRenderingSkipLightCount = 1;
								pass.SpecialRenderingLightType = lightType;
								break;
							case 3:
								//point light
								lightType = RenderLightType.Point;
								pass.SpecialRendering = true;
								pass.SpecialRenderingIteratePerLight = true;
								pass.SpecialRenderingLightType = lightType;
								break;
							case 4:
								//spot light
								lightType = RenderLightType.Spot;
								pass.SpecialRendering = true;
								pass.SpecialRenderingIteratePerLight = true;
								pass.SpecialRenderingLightType = lightType;
								break;
							}
						}
						else
						{
							ambientPass = nPass == 0;
							lightPass = nPass != 0;

							switch( nPass )
							{
							case 1: lightType = RenderLightType.Directional; break;
							case 2: lightType = RenderLightType.Point; break;
							case 3: lightType = RenderLightType.Spot; break;
							}

							if( lightPass )
							{
								pass.SpecialRendering = true;
								pass.SpecialRenderingIteratePerLight = true;
								pass.SpecialRenderingLightType = lightType;
							}

							if( SceneManager.Instance.IsShadowTechniqueStencilBased() )
							{
								if( ambientPass )
									pass.StencilShadowsIlluminationStage = IlluminationStage.Ambient;
								if( lightPass )
									pass.StencilShadowsIlluminationStage = IlluminationStage.PerLight;
							}
						}
					}
					else
					{
						ambientPass = true;
						lightPass = false;
					}

					int lightCount = lightPass ? 1 : 0;

					bool needLightmap = lightPass && lightType == RenderLightType.Directional &&
						LightmapTexCoordIndex != -1;

					//create shadow caster material
					if( BaseMaterial.ShadowTextureCasterMaterial == null &&
						SceneManager.Instance.IsShadowTechniqueShadowmapBased() &&
						OnIsNeedSpecialShadowCasterMaterial() )
					{
						Material shadowCasterMaterial = MaterialManager.Instance.Create(
							MaterialManager.Instance.GetUniqueName( BaseMaterial.Name + "_ShadowCaster" ) );

						BaseMaterial.ShadowTextureCasterMaterial = shadowCasterMaterial;

						Technique shadowCasterTechnique = shadowCasterMaterial.CreateTechnique();
						shadowCasterPass = shadowCasterTechnique.CreatePass();
						shadowCasterPass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );
					}

					/////////////////////////////////////
					//configure general pass settings
					{
						//disable Direct3D standard fog features
						pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

						//Light pass
						if( !ambientPass )
						{
							pass.DepthWrite = false;
							pass.SourceBlendFactor = SceneBlendFactor.One;
							pass.DestBlendFactor = SceneBlendFactor.One;
						}

						//Transparent
						if( blending != MaterialBlendingTypes.Opaque )
						{
							pass.DepthWrite = false;

							switch( blending )
							{
							case MaterialBlendingTypes.AlphaAdd:
								pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
								pass.DestBlendFactor = SceneBlendFactor.One;
								break;
							case MaterialBlendingTypes.AlphaBlend:
								pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
								pass.DestBlendFactor = SceneBlendFactor.OneMinusSourceAlpha;
								break;
							}
						}

						//AlphaReject
						pass.AlphaRejectFunction = alphaRejectFunction;
						pass.AlphaRejectValue = alphaRejectValue;
						pass.AlphaToCoverage = alphaToCoverage;
						if( shadowCasterPass != null )
						{
							shadowCasterPass.AlphaRejectFunction = alphaRejectFunction;
							shadowCasterPass.AlphaRejectValue = alphaRejectValue;
						}

						//Culling
						if( !culling )
						{
							pass.CullingMode = CullingMode.None;
							//shadow caster material
							if( shadowCasterPass != null )
								shadowCasterPass.CullingMode = CullingMode.None;
						}
					}

					/////////////////////////////////////
					//generate general compile arguments and create texture unit states
					StringBuilder generalArguments = new StringBuilder( 256 );
					int generalSamplerCount = 0;
					int generalTexCoordCount = 4;
					{
						if( RenderSystem.Instance.IsDirect3D() )
							generalArguments.Append( " -DDIRECT3D" );
						if( RenderSystem.Instance.IsOpenGL() )
							generalArguments.Append( " -DOPENGL" );

						if( RenderSystem.Instance.HasShaderModel3() )
							generalArguments.Append( " -DSHADER_MODEL_3" );
						else
							generalArguments.Append( " -DSHADER_MODEL_2" );

						if( atiHardwareShadows )
							generalArguments.Append( " -DATI_HARDWARE_SHADOWS" );
						if( nvidiaHardwareShadows )
							generalArguments.Append( " -DNVIDIA_HARDWARE_SHADOWS" );

						if( ambientPass )
							generalArguments.Append( " -DAMBIENT_PASS" );
						generalArguments.AppendFormat( " -DLIGHT_COUNT={0}", lightCount );
						if( lighting )
							generalArguments.Append( " -DLIGHTING" );
						if( culling )
							generalArguments.Append( " -DCULLING" );
						if( useNormals )
							generalArguments.Append( " -DUSE_NORMALS" );

						generalArguments.AppendFormat( " -DBLENDING_{0}", blending.ToString().ToUpper() );

						//hardware instancing
						if( RenderSystem.Instance.HasShaderModel3() &&
							RenderSystem.Instance.Capabilities.HardwareInstancing )
						{
							bool reflectionDynamicCubemap = false;
							if( ( ReflectionColor != new ColorValue( 0, 0, 0 ) && ReflectionPower != 0 ) ||
								reflectionScaleDynamic )
							{
								if( string.IsNullOrEmpty( ReflectionSpecificCubemap ) )
									reflectionDynamicCubemap = true;
							}

							if( blending == MaterialBlendingTypes.Opaque && !reflectionDynamicCubemap )
							{
								pass.SupportHardwareInstancing = true;

								generalArguments.Append( " -DINSTANCING" );

								if( shadowCasterPass != null )
									shadowCasterPass.SupportHardwareInstancing = true;
							}
						}

						//Fog
						FogMode fogMode = SceneManager.Instance.GetFogMode();
						if( allowFog && fogMode != FogMode.None )
						{
							generalArguments.Append( " -DFOG_ENABLED" );
							generalArguments.Append( " -DFOG_" + fogMode.ToString().ToUpper() );
						}

						//FadingByDistanceRange
						if( fadingByDistanceRange != new Range( 0, 0 ) )
							generalArguments.Append( " -DFADING_BY_DISTANCE" );

						//TexCoord23
						if(
							( !string.IsNullOrEmpty( diffuse1Map.Texture ) && (int)diffuse1Map.TexCoord > 1 ) ||
							( !string.IsNullOrEmpty( diffuse2Map.Texture ) && (int)diffuse2Map.TexCoord > 1 ) ||
							( !string.IsNullOrEmpty( diffuse3Map.Texture ) && (int)diffuse3Map.TexCoord > 1 ) ||
							( !string.IsNullOrEmpty( diffuse4Map.Texture ) && (int)diffuse4Map.TexCoord > 1 ) ||
							( !string.IsNullOrEmpty( reflectionMap.Texture ) && (int)reflectionMap.TexCoord > 1 ) ||
							( !string.IsNullOrEmpty( emissionMap.Texture ) && (int)emissionMap.TexCoord > 1 ) ||
							( !string.IsNullOrEmpty( specularMap.Texture ) && (int)specularMap.TexCoord > 1 ) ||
							( !string.IsNullOrEmpty( normalMap.Texture ) && (int)normalMap.TexCoord > 1 ) ||
							( !string.IsNullOrEmpty( heightMap.Texture ) && (int)heightMap.TexCoord > 1 ) ||
							( needLightmap && LightmapTexCoordIndex > 1 ) )
						{
							generalArguments.Append( " -DTEXCOORD23" );
							generalArguments.AppendFormat( " -DTEXCOORD23_TEXCOORD=TEXCOORD{0}",
								generalTexCoordCount );
							generalTexCoordCount++;
						}

						//Diffuse
						{
							if( IsDynamicDiffuseScale() )
							{
								generalArguments.Append( " -DDYNAMIC_DIFFUSE_SCALE" );
							}
							else
							{
								ColorValue scale = DiffuseColor *
									new ColorValue( DiffusePower, DiffusePower, DiffusePower, 1 );
								generalArguments.AppendFormat( " -DDIFFUSE_SCALE=half4({0},{1},{2},{3})",
									scale.Red, scale.Green, scale.Blue, scale.Alpha );
							}

							if( diffuseVertexColor )
							{
								generalArguments.Append( " -DDIFFUSE_VERTEX_COLOR" );
								generalArguments.AppendFormat( " -DVERTEX_COLOR_TEXCOORD=TEXCOORD{0}",
									generalTexCoordCount );
								generalTexCoordCount++;
							}

							for( int mapIndex = 1; mapIndex <= 4; mapIndex++ )
							{
								MapItem map = null;
								switch( mapIndex )
								{
								case 1: map = diffuse1Map; break;
								case 2: map = diffuse2Map; break;
								case 3: map = diffuse3Map; break;
								case 4: map = diffuse4Map; break;
								}

								if( !string.IsNullOrEmpty( map.Texture ) )
								{
									generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP", mapIndex );
									generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP_REGISTER=s{1}",
										mapIndex, generalSamplerCount );
									generalSamplerCount++;

									generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP_TEXCOORD=", mapIndex );
									GenerateTexCoordString( generalArguments, (int)map.TexCoord, map.Transform,
										string.Format( "diffuse{0}MapTransform", mapIndex ) );

									TextureUnitState state = pass.CreateTextureUnitState( map.Texture );
									if( map.Clamp )
										state.SetTextureAddressingMode( TextureAddressingMode.Clamp );

									//shadow caster material
									if( shadowCasterPass != null )
									{
										TextureUnitState casterState =
											shadowCasterPass.CreateTextureUnitState( map.Texture );
										if( map.Clamp )
											casterState.SetTextureAddressingMode( TextureAddressingMode.Clamp );
									}

									if( mapIndex > 1 )
									{
										//Opaque			= srcColor * 1 + destColor * 0
										//Add				= srcColor * 1 + destColor * 1
										//Modulate		= srcColor * destColor + destColor * 0
										//AlphaBlend	= srcColor * srcColor.a + destColor * (1 - srcColor.a)
										generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP_BLEND=blend{1}",
											mapIndex, ( (DiffuseMapItem)map ).Blending.ToString() );
									}
								}
							}
						}

						//Reflection
						if( materialScheme > MaterialSchemes.Low )
						{
							if( ( ReflectionColor != new ColorValue( 0, 0, 0 ) && ReflectionPower != 0 ) ||
								reflectionScaleDynamic )
							{
								generalArguments.Append( " -DREFLECTION" );

								generalArguments.AppendFormat( " -DREFLECTION_TEXCOORD=TEXCOORD{0}",
									generalTexCoordCount );
								generalTexCoordCount++;

								if( IsDynamicReflectionScale() )
								{
									generalArguments.Append( " -DDYNAMIC_REFLECTION_SCALE" );
								}
								else
								{
									ColorValue scale = ReflectionColor * ReflectionPower;
									generalArguments.AppendFormat( " -DREFLECTION_SCALE=half3({0},{1},{2})",
										scale.Red, scale.Green, scale.Blue );
								}

								if( !string.IsNullOrEmpty( reflectionMap.Texture ) )
								{
									generalArguments.Append( " -DREFLECTION_MAP" );
									generalArguments.AppendFormat( " -DREFLECTION_MAP_REGISTER=s{0}",
										generalSamplerCount );
									generalSamplerCount++;

									generalArguments.Append( " -DREFLECTION_MAP_TEXCOORD=" );
									GenerateTexCoordString( generalArguments, (int)reflectionMap.TexCoord,
										reflectionMap.Transform, "reflectionMapTransform" );

									TextureUnitState state = pass.CreateTextureUnitState(
										reflectionMap.Texture );
									if( reflectionMap.Clamp )
										state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
								}

								generalArguments.AppendFormat( " -DREFLECTION_CUBEMAP_REGISTER=s{0}",
									generalSamplerCount );
								generalSamplerCount++;

								TextureUnitState textureState = pass.CreateTextureUnitState();
								textureState.SetTextureAddressingMode( TextureAddressingMode.Clamp );
								if( !string.IsNullOrEmpty( ReflectionSpecificCubemap ) )
								{
									textureState.SetCubicTextureName( ReflectionSpecificCubemap, true );
								}
								else
								{
									SubscribePassToRenderObjectPassEvent( pass );

									if( cubemapEventUnitStates == null )
										cubemapEventUnitStates = new List<Pair<Pass, TextureUnitState>>();
									cubemapEventUnitStates.Add(
										new Pair<Pass, TextureUnitState>( pass, textureState ) );
								}
							}
						}

						//Emission
						if( ambientPass )
						{
							if( ( EmissionColor != new ColorValue( 0, 0, 0 ) && EmissionPower != 0 ) ||
								IsDynamicEmissionScale() )
							{
								generalArguments.Append( " -DEMISSION" );

								if( IsDynamicEmissionScale() )
								{
									generalArguments.Append( " -DDYNAMIC_EMISSION_SCALE" );
								}
								else
								{
									ColorValue scale = EmissionColor * EmissionPower;
									generalArguments.AppendFormat( " -DEMISSION_SCALE=half3({0},{1},{2})",
										scale.Red, scale.Green, scale.Blue );
								}

								if( !string.IsNullOrEmpty( emissionMap.Texture ) )
								{
									generalArguments.Append( " -DEMISSION_MAP" );
									generalArguments.AppendFormat( " -DEMISSION_MAP_REGISTER=s{0}",
										generalSamplerCount );
									generalSamplerCount++;

									generalArguments.Append( " -DEMISSION_MAP_TEXCOORD=" );
									GenerateTexCoordString( generalArguments, (int)emissionMap.TexCoord,
										emissionMap.Transform, "emissionMapTransform" );

									TextureUnitState state = pass.CreateTextureUnitState(
										emissionMap.Texture );
									if( emissionMap.Clamp )
										state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
								}
							}
						}

						//Specular
						if( materialScheme > MaterialSchemes.Low )
						{
							if( lightPass )
							{
								if( ( SpecularColor != new ColorValue( 0, 0, 0 ) && SpecularPower != 0 ) ||
									IsDynamicSpecularScaleAndShininess() )
								{
									generalArguments.Append( " -DSPECULAR" );

									if( IsDynamicSpecularScaleAndShininess() )
									{
										generalArguments.Append( " -DDYNAMIC_SPECULAR_SCALE" );
									}
									else
									{
										ColorValue scale = SpecularColor * SpecularPower;
										generalArguments.AppendFormat( " -DSPECULAR_SCALE=half3({0},{1},{2})",
											scale.Red, scale.Green, scale.Blue );
									}

									if( !string.IsNullOrEmpty( specularMap.Texture ) )
									{
										generalArguments.Append( " -DSPECULAR_MAP" );
										generalArguments.AppendFormat( " -DSPECULAR_MAP_REGISTER=s{0}",
											generalSamplerCount );
										generalSamplerCount++;

										generalArguments.Append( " -DSPECULAR_MAP_TEXCOORD=" );
										GenerateTexCoordString( generalArguments, (int)specularMap.TexCoord,
											specularMap.Transform, "specularMapTransform" );

										TextureUnitState state = pass.CreateTextureUnitState(
											specularMap.Texture );
										if( specularMap.Clamp )
											state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
									}
								}
							}
						}

						//NormalMap
						if( materialScheme > MaterialSchemes.Low )
						{
							if( !string.IsNullOrEmpty( normalMap.Texture ) )
							{
								generalArguments.Append( " -DNORMAL_MAP" );

								if( ambientPass )
								{
									generalArguments.AppendFormat(
										" -DAMBIENT_LIGHT_DIRECTION_TEXCOORD=TEXCOORD{0}",
										generalTexCoordCount );
									generalTexCoordCount++;
								}

								generalArguments.AppendFormat( " -DNORMAL_MAP_REGISTER=s{0}",
									generalSamplerCount );
								generalSamplerCount++;

								generalArguments.Append( " -DNORMAL_MAP_TEXCOORD=" );
								GenerateTexCoordString( generalArguments, (int)normalMap.TexCoord,
									normalMap.Transform, "normalMapTransform" );

								TextureUnitState state = pass.CreateTextureUnitState( normalMap.Texture );
								if( normalMap.Clamp )
									state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
							}
						}

						//Height
						if( materialScheme > MaterialSchemes.Low )
						{
							if( !string.IsNullOrEmpty( normalMap.Texture ) )
							{
								if( !string.IsNullOrEmpty( heightMap.Texture ) || HeightFromNormalMapAlpha )
								{
									if( heightFromNormalMapAlpha )
									{
										generalArguments.Append( " -DHEIGHT_FROM_NORMAL_MAP_ALPHA" );
									}
									else
									{
										generalArguments.Append( " -DHEIGHT_MAP" );
										generalArguments.AppendFormat( " -DHEIGHT_MAP_REGISTER=s{0}",
											generalSamplerCount );
										generalSamplerCount++;

										generalArguments.Append( " -DHEIGHT_MAP_TEXCOORD=" );
										GenerateTexCoordString( generalArguments, (int)heightMap.TexCoord,
											heightMap.Transform, "heightMapTransform" );

										TextureUnitState state = pass.CreateTextureUnitState(
											heightMap.Texture );
										if( heightMap.Clamp )
											state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
									}
									generalArguments.AppendFormat( " -DHEIGHT_SCALE={0}", heightScale );
								}
							}
						}

						//Shadow
						if( materialScheme > MaterialSchemes.Low )
						{
							if( lightPass )
							{
								if( SceneManager.Instance.IsShadowTechniqueShadowmapBased() && 
									ReceiveShadows )
								{
									//!!!!!!
									bool shadowPSSM = lightType == RenderLightType.Directional;
									shadowPSSM = false;

									generalArguments.Append( " -DSHADOW_MAP" );

									if( RenderSystem.Instance.HasShaderModel3() &&
										SceneManager.Instance.ShadowTechnique == ShadowTechniques.ShadowmapHigh )
									{
										generalArguments.Append( " -DSHADOW_MAP_HIGH" );
									}
									else if( RenderSystem.Instance.HasShaderModel3() &&
										SceneManager.Instance.ShadowTechnique == ShadowTechniques.ShadowmapMedium )
									{
										generalArguments.Append( " -DSHADOW_MAP_MEDIUM" );
									}
									else
									{
										generalArguments.Append( " -DSHADOW_MAP_LOW" );
									}

									if( shadowPSSM )
										generalArguments.Append( " -DSHADOW_PSSM" );

									int shadowMapCount = shadowPSSM ? 3 : 1;
									for( int n = 0; n < shadowMapCount; n++ )
									{
										generalArguments.AppendFormat( " -DSHADOW_MAP{0}_REGISTER=s{1}",
											n, generalSamplerCount );
										generalSamplerCount++;

										generalArguments.AppendFormat( " -DSHADOW_UV{0}_TEXCOORD=TEXCOORD{1}",
											n, generalTexCoordCount );
										generalTexCoordCount++;

										TextureUnitState state = pass.CreateTextureUnitState( "" );
										state.ContentType = TextureUnitState.ContentTypes.Shadow;
										state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
										state.SetTextureFiltering( FilterOptions.Point,
											FilterOptions.Point, FilterOptions.None );

										if( atiHardwareShadows )
											state.Fetch4 = true;

										if( nvidiaHardwareShadows )
										{
											state.SetTextureFiltering( FilterOptions.Linear,
												FilterOptions.Linear, FilterOptions.None );
										}
									}
								}
							}
						}

						//Lightmap
						if( needLightmap )
						{
							generalArguments.Append( " -DLIGHTMAP" );

							generalArguments.AppendFormat( " -DLIGHTMAP_REGISTER=s{0}", generalSamplerCount );
							generalSamplerCount++;

							if( LightmapTexCoordIndex > 3 )
							{
								defaultTechniqueErrorString = "LightmapTexCoordIndex > 3 is not supported.";
								return false;
							}

							generalArguments.AppendFormat( " -DLIGHTMAP_TEXCOORD=texCoord{0}",
								LightmapTexCoordIndex );

							TextureUnitState state = pass.CreateTextureUnitState( "" );
							state.ContentType = TextureUnitState.ContentTypes.Lightmap;
						}
					}

					//check maximum sampler count
					if( generalSamplerCount > maxSamplerCount )
					{
						defaultTechniqueErrorString = string.Format(
							"The limit of amount of textures is exceeded. Need: {0}, Maximum: {1}. ({2})",
							generalSamplerCount, maxSamplerCount, FileName );
						return false;
					}

					//check maximum texture coordinates count
					if( generalTexCoordCount > maxTexCoordCount )
					{
						defaultTechniqueErrorString = string.Format(
							"The limit of amount of texture coordinates is exceeded. Need: {0}, " +
							"Maximum: {1}. ({2})", generalTexCoordCount, maxTexCoordCount, FileName );
						return false;
					}

					/////////////////////////////////////
					//generate replace strings for program compiling
					List<KeyValuePair<string, string>> replaceStrings =
						new List<KeyValuePair<string, string>>();
					{
						//extension file includes
						string extensionFileName = OnGetExtensionFileName();
						if( extensionFileName != null )
						{
							string directory = "Materials/Common/";
							string replaceText = string.Format( "#include \"{0}{1}\"",
								directory, extensionFileName );
							replaceStrings.Add( new KeyValuePair<string, string>(
								"_INCLUDE_EXTENSION_FILE", replaceText ) );
						}
						else
						{
							replaceStrings.Add( new KeyValuePair<string, string>(
								"_INCLUDE_EXTENSION_FILE", "" ) );
						}
					}

					OnAddCompileArguments( generalArguments );

					/////////////////////////////////////
					//generate programs

					//generate program for only ambient pass
					if( ambientPass && !lightPass )
					{
						//vertex program
						GpuProgram vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
							"ShaderBase_Vertex_", GpuProgramType.VertexProgram, sourceFile,
							"main_vp", vertexSyntax, generalArguments.ToString(), replaceStrings,
							out defaultTechniqueErrorString );
						if( vertexProgram == null )
							return false;

						OnSetProgramAutoConstants( vertexProgram.DefaultParameters, 0 );
						pass.VertexProgramName = vertexProgram.Name;

						//fragment program
						GpuProgram fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
							"ShaderBase_Fragment_", GpuProgramType.FragmentProgram, sourceFile,
							"main_fp", fragmentSyntax, generalArguments.ToString(), replaceStrings,
							out defaultTechniqueErrorString );
						if( fragmentProgram == null )
							return false;

						OnSetProgramAutoConstants( fragmentProgram.DefaultParameters, 0 );
						pass.FragmentProgramName = fragmentProgram.Name;
					}

					//generate program for light passes
					if( lightPass )
					{
						StringBuilder arguments = new StringBuilder( generalArguments.Length + 100 );
						arguments.Append( generalArguments.ToString() );
						int texCoordCount = generalTexCoordCount;

						arguments.AppendFormat( " -DLIGHTTYPE_{0}", lightType.ToString().ToUpper() );

						if( !culling )
						{
							if( !RenderSystem.Instance.HasShaderModel3() ||
								RenderSystem.Instance.IsOpenGL() )
							{
								arguments.AppendFormat(
									" -DEYE_DIRECTION_OBJECT_SPACE_TEXCOORD=TEXCOORD{0}", texCoordCount );
								texCoordCount++;
							}
						}

						for( int nLight = 0; nLight < lightCount; nLight++ )
						{
							arguments.AppendFormat(
								" -DOBJECT_LIGHT_DIRECTION_AND_ATTENUATION_{0}_TEXCOORD=TEXCOORD{1}",
								nLight, texCoordCount );
							texCoordCount++;

							if( lightType == RenderLightType.Spot )
							{
								arguments.AppendFormat(
									" -DOBJECT_WORLD_LIGHT_DIRECTION_{0}_TEXCOORD=TEXCOORD{1}",
									nLight, texCoordCount );
								texCoordCount++;
							}
						}

						//check maximum texture coordinates count
						if( texCoordCount > maxTexCoordCount )
						{
							defaultTechniqueErrorString = string.Format(
								"The limit of amount of texture coordinates is exceeded. Need: {0}, " +
								"Maximum: {1}. ({2})", texCoordCount, maxTexCoordCount, FileName );
							return false;
						}

						//vertex program
						GpuProgram vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
							"ShaderBase_Vertex_", GpuProgramType.VertexProgram, sourceFile,
							"main_vp", vertexSyntax, arguments.ToString(), replaceStrings,
							out defaultTechniqueErrorString );
						if( vertexProgram == null )
							return false;

						OnSetProgramAutoConstants( vertexProgram.DefaultParameters, lightCount );
						pass.VertexProgramName = vertexProgram.Name;

						//fragment program
						GpuProgram fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
							"ShaderBase_Fragment_", GpuProgramType.FragmentProgram, sourceFile,
							"main_fp", fragmentSyntax, arguments.ToString(), replaceStrings,
							out defaultTechniqueErrorString );
						if( fragmentProgram == null )
							return false;

						OnSetProgramAutoConstants( fragmentProgram.DefaultParameters, lightCount );
						pass.FragmentProgramName = fragmentProgram.Name;
					}

					//shadow caster material
					if( shadowCasterPass != null )
					{
						StringBuilder arguments = new StringBuilder( generalArguments.Length + 40 );
						arguments.Append( generalArguments.ToString() );

						if( alphaRejectFunction != CompareFunction.AlwaysPass )
						{
							arguments.AppendFormat( " -DALPHA_REJECT_FUNCTION_{0}",
								alphaRejectFunction.ToString().ToUpper() );
						}

						//vertex program
						GpuProgram vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
							"ShaderBase_ShadowCaster_Vertex_", GpuProgramType.VertexProgram, sourceFile,
							"shadowCaster_vp", vertexSyntax, arguments.ToString(),
							replaceStrings, out defaultTechniqueErrorString );
						if( vertexProgram == null )
							return false;

						OnSetProgramAutoConstants( vertexProgram.DefaultParameters, 0 );
						shadowCasterPass.VertexProgramName = vertexProgram.Name;

						//fragment program
						GpuProgram fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
							"ShaderBase_ShadowCaster_Fragment_", GpuProgramType.FragmentProgram, sourceFile,
							"shadowCaster_fp", fragmentSyntax, arguments.ToString(),
							replaceStrings, out defaultTechniqueErrorString );
						if( fragmentProgram == null )
							return false;

						OnSetProgramAutoConstants( fragmentProgram.DefaultParameters, 0 );
						shadowCasterPass.FragmentProgramName = fragmentProgram.Name;
					}

				}
			}

			InitializeAndUpdateDynamicGpuParameters();

			return true;
		}

		void SubscribePassToRenderObjectPassEvent( Pass pass )
		{
			if( subscribedPassesForRenderObjectPass == null )
				subscribedPassesForRenderObjectPass = new List<Pass>();
			if( !subscribedPassesForRenderObjectPass.Contains( pass ) )
			{
				pass.RenderObjectPass += Pass_RenderObjectPass;
				subscribedPassesForRenderObjectPass.Add( pass );
			}
		}

		void UpdateMapTransformForFixedPipeline( MapItem map )
		{
			List<TextureUnitState> states = map.textureUnitStatesForFixedPipeline;
			if( states == null )
				return;

			foreach( TextureUnitState state in states )
			{
				TransformItem transform = map.Transform;
				AnimationItem animation = transform.Animation;

				state.TextureScroll = transform.Scroll;
				state.TextureRotate = transform.Rotate * ( MathFunctions.PI * 2 );

				if( transform.Scale != new Vec2( 1, 1 ) )
				{
					Vec2 s = Vec2.Zero;
					if( transform.Scale.X != 0 )
						s.X = 1.0f / transform.Scale.X;
					if( transform.Scale.Y != 0 )
						s.Y = 1.0f / transform.Scale.Y;
					state.TextureScale = s;
					state.TextureScroll -= ( new Vec2( 1, 1 ) - transform.Scale ) / 2;
				}

				//property RotateRound is not supported

				state.SetScrollAnimation( -animation.ScrollSpeed );
				state.SetRotateAnimation( -animation.RotateSpeed );
			}
		}

		void FixedPipelineAddDiffuseMapsToPass( Pass pass )
		{
			for( int mapIndex = 1; mapIndex <= 4; mapIndex++ )
			{
				MapItem map = null;
				switch( mapIndex )
				{
				case 1: map = diffuse1Map; break;
				case 2: map = diffuse2Map; break;
				case 3: map = diffuse3Map; break;
				case 4: map = diffuse4Map; break;
				}

				if( !string.IsNullOrEmpty( map.Texture ) )
				{
					TextureUnitState state = pass.CreateTextureUnitState(
						map.Texture, (int)map.TexCoord );

					if( map.Clamp )
						state.SetTextureAddressingMode( TextureAddressingMode.Clamp );

					if( map.textureUnitStatesForFixedPipeline == null )
						map.textureUnitStatesForFixedPipeline = new List<TextureUnitState>();
					map.textureUnitStatesForFixedPipeline.Add( state );
					UpdateMapTransformForFixedPipeline( map );

					if( mapIndex > 1 && mapIndex < 5 )
					{
						DiffuseMapItem.MapBlendingTypes mapBlending = ( (DiffuseMapItem)map ).Blending;
						switch( mapBlending )
						{
						case DiffuseMapItem.MapBlendingTypes.Add:
							state.SetColorOperation( LayerBlendOperation.Add );
							break;
						case DiffuseMapItem.MapBlendingTypes.Modulate:
							state.SetColorOperation( LayerBlendOperation.Modulate );
							break;
						case DiffuseMapItem.MapBlendingTypes.AlphaBlend:
							state.SetColorOperation( LayerBlendOperation.AlphaBlend );
							break;
						}
					}
				}
			}
		}

		void CreateFixedPipelineTechnique()
		{
			ColorValue diffuseScale = DiffuseColor *
				new ColorValue( DiffusePower, DiffusePower, DiffusePower, 1 );

			//ReceiveShadows
			{
				BaseMaterial.ReceiveShadows = receiveShadows;

				//disable receiving shadows when alpha function is enabled
				if( AlphaRejectFunction != CompareFunction.AlwaysPass )
				{
					if( SceneManager.Instance.IsShadowTechniqueShadowmapBased() )
						BaseMaterial.ReceiveShadows = false;
				}
			}

			Technique tecnhique = BaseMaterial.CreateTechnique();


			if( SceneManager.Instance.IsShadowTechniqueStencilBased() )
			{
				//stencil shadows are enabled

				//ambient pass
				if( blending == MaterialBlendingTypes.Opaque )
				{
					Pass pass = tecnhique.CreatePass();
					pass.NormalizeNormals = true;

					pass.Ambient = diffuseScale;
					pass.Diffuse = new ColorValue( 0, 0, 0 );
					pass.Specular = new ColorValue( 0, 0, 0 );

					pass.AlphaRejectFunction = alphaRejectFunction;
					pass.AlphaRejectValue = alphaRejectValue;
					pass.Lighting = lighting;
					if( !culling )
						pass.CullingMode = CullingMode.None;

					pass.DepthWrite = depthWrite;
					pass.DepthCheck = depthTest;

					if( !allowFog || blending == MaterialBlendingTypes.AlphaAdd )
						pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

					FixedPipelineAddDiffuseMapsToPass( pass );

					pass.StencilShadowsIlluminationStage = IlluminationStage.Ambient;
				}

				{
					Pass pass = tecnhique.CreatePass();
					pass.NormalizeNormals = true;

					pass.Ambient = new ColorValue( 0, 0, 0 );
					pass.Diffuse = diffuseScale;
					if( string.IsNullOrEmpty( SpecularMap.Texture ) )
						pass.Specular = SpecularColor * SpecularPower;
					pass.Shininess = SpecularShininess;

					pass.AlphaRejectFunction = alphaRejectFunction;
					pass.AlphaRejectValue = alphaRejectValue;
					pass.Lighting = lighting;
					if( !culling )
						pass.CullingMode = CullingMode.None;

					pass.DepthWrite = false;
					pass.DepthCheck = depthTest;

					if( !allowFog || blending == MaterialBlendingTypes.AlphaAdd )
						pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

					pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
					pass.DestBlendFactor = SceneBlendFactor.One;

					if( blending != MaterialBlendingTypes.Opaque )
					{
						switch( blending )
						{
						case MaterialBlendingTypes.AlphaAdd:
							pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
							pass.DestBlendFactor = SceneBlendFactor.One;
							break;
						case MaterialBlendingTypes.AlphaBlend:
							pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
							pass.DestBlendFactor = SceneBlendFactor.OneMinusSourceAlpha;
							break;
						}
					}

					FixedPipelineAddDiffuseMapsToPass( pass );

					pass.StencilShadowsIlluminationStage = IlluminationStage.PerLight;
				}

			}
			else
			{
				//stencil shadows are disabled

				Pass pass = tecnhique.CreatePass();
				pass.NormalizeNormals = true;

				pass.Ambient = diffuseScale;
				pass.Diffuse = diffuseScale;

				if( string.IsNullOrEmpty( SpecularMap.Texture ) )
					pass.Specular = SpecularColor * SpecularPower;
				pass.Shininess = SpecularShininess;
				pass.AlphaRejectFunction = alphaRejectFunction;
				pass.AlphaRejectValue = alphaRejectValue;
				pass.Lighting = lighting;
				if( !culling )
					pass.CullingMode = CullingMode.None;

				pass.DepthWrite = depthWrite;
				pass.DepthCheck = depthTest;

				if( !allowFog || blending == MaterialBlendingTypes.AlphaAdd )
					pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

				if( blending != MaterialBlendingTypes.Opaque )
				{
					pass.DepthWrite = false;

					switch( blending )
					{
					case MaterialBlendingTypes.AlphaAdd:
						pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
						pass.DestBlendFactor = SceneBlendFactor.One;
						break;
					case MaterialBlendingTypes.AlphaBlend:
						pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
						pass.DestBlendFactor = SceneBlendFactor.OneMinusSourceAlpha;
						break;
					}
				}

				FixedPipelineAddDiffuseMapsToPass( pass );
			}

			//pass for emission
			if( ( emissionColor != new ColorValue( 0, 0, 0 ) && emissionPower != 0 ) ||
				emissionScaleDynamic )
			{
				Pass pass = tecnhique.CreatePass();
				pass.NormalizeNormals = true;

				pass.Ambient = new ColorValue( 0, 0, 0 );
				pass.SelfIllumination = emissionColor * emissionPower;

				pass.DepthWrite = false;
				pass.DepthCheck = depthTest;

				pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
				pass.DestBlendFactor = SceneBlendFactor.One;

				if( !allowFog || blending == MaterialBlendingTypes.AlphaAdd )
					pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

				pass.AlphaRejectFunction = alphaRejectFunction;
				pass.AlphaRejectValue = alphaRejectValue;

				pass.Lighting = lighting;
				if( !culling )
					pass.CullingMode = CullingMode.None;

				if( !string.IsNullOrEmpty( EmissionMap.Texture ) )
				{
					TextureUnitState state = pass.CreateTextureUnitState( EmissionMap.Texture );

					if( EmissionMap.Clamp )
						state.SetTextureAddressingMode( TextureAddressingMode.Clamp );

					if( EmissionMap.textureUnitStatesForFixedPipeline == null )
						EmissionMap.textureUnitStatesForFixedPipeline = new List<TextureUnitState>();
					EmissionMap.textureUnitStatesForFixedPipeline.Add( state );
					UpdateMapTransformForFixedPipeline( EmissionMap );
				}

				if( SceneManager.Instance.IsShadowTechniqueStencilBased() )
					pass.StencilShadowsIlluminationStage = IlluminationStage.Decal;
			}


			fixedPipelineInitialized = true;
		}

		void ClearBaseMaterial()
		{
			if( fixedPipelineInitialized )
			{
				diffuse1Map.textureUnitStatesForFixedPipeline = null;
				diffuse2Map.textureUnitStatesForFixedPipeline = null;
				diffuse3Map.textureUnitStatesForFixedPipeline = null;
				diffuse4Map.textureUnitStatesForFixedPipeline = null;
				emissionMap.textureUnitStatesForFixedPipeline = null;
				reflectionMap.textureUnitStatesForFixedPipeline = null;
				specularMap.textureUnitStatesForFixedPipeline = null;
			}

			//destroy shadow caster material
			Material shadowCasterMaterial = BaseMaterial.ShadowTextureCasterMaterial;
			if( shadowCasterMaterial != null )
			{
				BaseMaterial.ShadowTextureCasterMaterial = null;
				shadowCasterMaterial.Dispose();
			}

			if( subscribedPassesForRenderObjectPass != null )
			{
				foreach( Pass pass in subscribedPassesForRenderObjectPass )
					pass.RenderObjectPass -= Pass_RenderObjectPass;
				subscribedPassesForRenderObjectPass.Clear();
			}

			if( mapsWithAnimations != null )
				mapsWithAnimations.Clear();

			cubemapEventUnitStates = null;

			//clear material
			BaseMaterial.RemoveAllTechniques();

			fixedPipelineInitialized = false;
		}

		protected override bool OnInitBaseMaterial()
		{
			if( !base.OnInitBaseMaterial() )
				return false;

			defaultTechniqueErrorString = null;

			bool shadersIsNotSupported;
			bool success = CreateDefaultTechnique( out shadersIsNotSupported );

			if( !success )
			{
				//no fatal error if is the Resource Editor
				if( !shadersIsNotSupported && !EngineApp.Instance.IsResourceEditor )
				{
					if( !string.IsNullOrEmpty( defaultTechniqueErrorString ) )
					{
						Log.Fatal( "Cannot create material \"{0}\". {1}", Name,
							defaultTechniqueErrorString );
					}
					return false;
				}

				ClearBaseMaterial();
				CreateFixedPipelineTechnique();
			}

			return true;
		}

		protected override void OnClearBaseMaterial()
		{
			ClearBaseMaterial();
			base.OnClearBaseMaterial();
		}

		void InitializeAndUpdateDynamicGpuParameters()
		{
			//initialize and update gpu parameters
			UpdateDynamicDiffuseScaleGpuParameter();
			UpdateDynamicEmissionScaleGpuParameter();
			UpdateDynamicReflectionScaleGpuParameter();
			UpdateDynamicSpecularScaleAndShininessGpuParameter();
			UpdateFadingByDistanceRangeGpuParameter();
			InitializeAndUpdateMapTransformGpuParameters( diffuse1Map );
			InitializeAndUpdateMapTransformGpuParameters( diffuse2Map );
			InitializeAndUpdateMapTransformGpuParameters( diffuse3Map );
			InitializeAndUpdateMapTransformGpuParameters( diffuse4Map );
			InitializeAndUpdateMapTransformGpuParameters( reflectionMap );
			InitializeAndUpdateMapTransformGpuParameters( emissionMap );
			InitializeAndUpdateMapTransformGpuParameters( specularMap );
			InitializeAndUpdateMapTransformGpuParameters( normalMap );
			InitializeAndUpdateMapTransformGpuParameters( heightMap );
		}

		void SetCustomGpuParameter( GpuParameters parameter, Vec4 value )
		{
			for( int nMaterial = 0; nMaterial < 2; nMaterial++ )
			{
				Material material;
				if( nMaterial == 0 )
				{
					material = BaseMaterial;
				}
				else
				{
					material = BaseMaterial.ShadowTextureCasterMaterial;
					if( material == null )
						continue;
				}

				foreach( Technique technique in material.Techniques )
				{
					foreach( Pass pass in technique.Passes )
					{
						if( pass.VertexProgramParameters != null ||
							pass.FragmentProgramParameters != null )
						{
							if( pass.VertexProgramParameters != null )
							{
								if( !pass.IsCustomGpuParameterInitialized( (int)parameter ) )
								{
									pass.VertexProgramParameters.SetNamedAutoConstant( parameter.ToString(),
										GpuProgramParameters.AutoConstantType.Custom, (int)parameter );
								}
							}

							if( pass.FragmentProgramParameters != null )
							{
								if( !pass.IsCustomGpuParameterInitialized( (int)parameter ) )
								{
									pass.FragmentProgramParameters.SetNamedAutoConstant( parameter.ToString(),
										GpuProgramParameters.AutoConstantType.Custom, (int)parameter );
								}
							}

							pass.SetCustomGpuParameter( (int)parameter, value );
						}
					}
				}
			}
		}

		bool IsDynamicDiffuseScale()
		{
			return diffuseScaleDynamic || ( diffuseColor != new ColorValue( 0, 0, 0 ) &&
				diffuseColor != new ColorValue( 1, 1, 1 ) ) || diffusePower != 1;
		}

		void UpdateDynamicDiffuseScaleGpuParameter()
		{
			if( IsDynamicDiffuseScale() )
			{
				ColorValue scale = DiffuseColor *
					new ColorValue( DiffusePower, DiffusePower, DiffusePower, 1 );
				SetCustomGpuParameter( GpuParameters.dynamicDiffuseScale, scale.ToVec4() );
			}
		}

		bool IsDynamicEmissionScale()
		{
			return emissionScaleDynamic || ( emissionColor != new ColorValue( 0, 0, 0 ) &&
				emissionColor != new ColorValue( 1, 1, 1 ) ) ||
				( emissionPower != 0 && emissionPower != 1 );
		}

		void UpdateDynamicEmissionScaleGpuParameter()
		{
			if( IsDynamicEmissionScale() )
			{
				SetCustomGpuParameter( GpuParameters.dynamicEmissionScale,
					emissionColor.ToVec4() * emissionPower );
			}
		}

		bool IsDynamicReflectionScale()
		{
			return reflectionScaleDynamic || ( reflectionColor != new ColorValue( 0, 0, 0 ) &&
				reflectionColor != new ColorValue( 1, 1, 1 ) ) ||
				( reflectionPower != 0 && reflectionPower != 1 );
		}

		void UpdateDynamicReflectionScaleGpuParameter()
		{
			if( IsDynamicReflectionScale() )
			{
				SetCustomGpuParameter( GpuParameters.dynamicReflectionScale,
					reflectionColor.ToVec4() * reflectionPower );
			}
		}

		bool IsDynamicSpecularScaleAndShininess()
		{
			return specularScaleDynamic || specularColor != new ColorValue( 0, 0, 0 );
		}

		void UpdateDynamicSpecularScaleAndShininessGpuParameter()
		{
			if( IsDynamicSpecularScaleAndShininess() )
			{
				ColorValue scale = specularColor * specularPower;
				SetCustomGpuParameter( GpuParameters.dynamicSpecularScaleAndShininess,
					new Vec4( scale.Red, scale.Green, scale.Blue, specularShininess ) );
			}
		}

		void UpdateFadingByDistanceRangeGpuParameter()
		{
			if( fadingByDistanceRange == Range.Zero )
				return;

			Range range = fadingByDistanceRange;
			if( range.Maximum < range.Minimum + .01f )
				range.Maximum = range.Minimum + .01f;
			SetCustomGpuParameter( GpuParameters.fadingByDistanceRange,
				new Vec4( range.Minimum, 1.0f / ( range.Maximum - range.Minimum ), 0, 0 ) );
		}

		void InitializeAndUpdateMapTransformGpuParameters( MapItem map )
		{
			//subscribe parameters for animation updating via RenderObjectPass event
			if( map.Transform.Animation.IsDataExists() )
			{
				//add map to mapsWithAnimations
				if( mapsWithAnimations == null )
					mapsWithAnimations = new List<MapItem>();
				if( !mapsWithAnimations.Contains( map ) )
					mapsWithAnimations.Add( map );

				foreach( Technique technique in BaseMaterial.Techniques )
					foreach( Pass pass in technique.Passes )
						SubscribePassToRenderObjectPassEvent( pass );
			}

			//update parameters
			UpdateMapTransformGpuParameters( map );
		}

		void UpdateMapTransformGpuParameters( MapItem map )
		{
			TransformItem transform = map.Transform;

			if( !transform.IsDataExists() )
				return;

			//calculate scroll and rotate
			Vec2 scroll = transform.Scroll;
			Vec2 scale = transform.Scale;
			float rotate = transform.Rotate;

			AnimationItem animation = transform.Animation;
			if( animation.IsDataExists() )
			{
				float time = RendererWorld.Instance.FrameRenderTime;

				Vec2 animationScroll = animation.ScrollSpeed * time;

				Vec2 round = animation.ScrollRound;
				if( round.X != 0 )
				{
					animationScroll.X =
						MathFunctions.Round( animationScroll.X * ( 1.0f / round.X ) ) * round.X;
				}
				if( round.Y != 0 )
				{
					animationScroll.Y =
						MathFunctions.Round( animationScroll.Y * ( 1.0f / round.Y ) ) * round.Y;
				}

				scroll += animationScroll;
				rotate += animation.RotateSpeed * time;
			}

			scroll.X = scroll.X % 1.0f;
			scroll.Y = scroll.Y % 1.0f;
			rotate = rotate % 1.0f;

			//calculate matrix
			Mat3 matrix;
			{
				if( scale != new Vec2( 1, 1 ) )
					matrix = Mat3.FromScale( new Vec3( scale.X, scale.Y, 1 ) );
				else
					matrix = Mat3.Identity;

				if( rotate != 0 )
				{
					Mat3 m;
					m = new Mat3( 1, 0, -.5f, 0, 1, -.5f, 0, 0, 1 );
					m *= Mat3.FromRotateByZ( rotate * ( MathFunctions.PI * 2 ) );
					m *= new Mat3( 1, 0, .5f, 0, 1, .5f, 0, 0, 1 );
					matrix *= m;
				}

				if( scroll != Vec2.Zero )
					matrix *= new Mat3( 1, 0, scroll.X, 0, 1, scroll.Y, 0, 0, 1 );
			}

			//find gpu parameters
			GpuParameters mulGpuParameter;
			GpuParameters addGpuParameter;
			{
				if( map == diffuse1Map )
				{
					mulGpuParameter = GpuParameters.diffuse1MapTransformMul;
					addGpuParameter = GpuParameters.diffuse1MapTransformAdd;
				}
				else if( map == diffuse2Map )
				{
					mulGpuParameter = GpuParameters.diffuse2MapTransformMul;
					addGpuParameter = GpuParameters.diffuse2MapTransformAdd;
				}
				else if( map == diffuse3Map )
				{
					mulGpuParameter = GpuParameters.diffuse3MapTransformMul;
					addGpuParameter = GpuParameters.diffuse3MapTransformAdd;
				}
				else if( map == diffuse4Map )
				{
					mulGpuParameter = GpuParameters.diffuse4MapTransformMul;
					addGpuParameter = GpuParameters.diffuse4MapTransformAdd;
				}
				else if( map == reflectionMap )
				{
					mulGpuParameter = GpuParameters.reflectionMapTransformMul;
					addGpuParameter = GpuParameters.reflectionMapTransformAdd;
				}
				else if( map == emissionMap )
				{
					mulGpuParameter = GpuParameters.emissionMapTransformMul;
					addGpuParameter = GpuParameters.emissionMapTransformAdd;
				}
				else if( map == specularMap )
				{
					mulGpuParameter = GpuParameters.specularMapTransformMul;
					addGpuParameter = GpuParameters.specularMapTransformAdd;
				}
				else if( map == normalMap )
				{
					mulGpuParameter = GpuParameters.normalMapTransformMul;
					addGpuParameter = GpuParameters.normalMapTransformAdd;
				}
				else if( map == heightMap )
				{
					mulGpuParameter = GpuParameters.heightMapTransformMul;
					addGpuParameter = GpuParameters.heightMapTransformAdd;
				}
				else
				{
					Log.Fatal( "ShaderBaseMaterial: Internal error (UpdateMapTransformGpuParameters)." );
					return;
				}
			}

			//set parameters
			SetCustomGpuParameter( mulGpuParameter,
				new Vec4( matrix.Item0.X, matrix.Item0.Y, matrix.Item1.X, matrix.Item1.Y ) );
			SetCustomGpuParameter( addGpuParameter,
				new Vec4( matrix.Item0.Z, matrix.Item1.Z, 0, 0 ) );
		}

		void Pass_RenderObjectPass( Pass pass, Vec3 objectWorldPosition )
		{
			//update cubemap reflection textures
			if( cubemapEventUnitStates != null )
			{
				foreach( Pair<Pass, TextureUnitState> item in cubemapEventUnitStates )
					if( item.First == pass )
						UpdateReflectionCubemap( item.Second, objectWorldPosition );
			}

			//update maps transform with animations
			if( mapsWithAnimations != null )
			{
				foreach( MapItem map in mapsWithAnimations )
					UpdateMapTransformGpuParameters( map );
			}
		}

		void UpdateReflectionCubemap( TextureUnitState textureUnitState, Vec3 objectWorldPosition )
		{
			string textureName = "";

			//get cubemap from CubemapZone's
			if( Map.Instance != null )
			{
				Map.Instance.GetObjects( new Bounds( objectWorldPosition ), delegate( MapObject obj )
				{
					CubemapZone zone = obj as CubemapZone;
					if( zone != null && !zone.GlobalZone )
						textureName = zone.GetTextureName();
				} );

				if( string.IsNullOrEmpty( textureName ) )
				{
					CubemapZone zone = CubemapZone.GetGlobalZone();
					if( zone != null )
						textureName = zone.GetTextureName();
				}
			}

			//get cubemap from SkyBox
			if( string.IsNullOrEmpty( textureName ) )
				textureName = SceneManager.Instance.GetSkyBoxTextureName();

			//update state
			textureUnitState.SetCubicTextureName( textureName, true );
		}

		void SceneManager_FogChanged( bool fogModeChanged )
		{
			if( allowFog && fogModeChanged && IsBaseMaterialInitialized() )
				UpdateBaseMaterial();
		}

		void SceneManager_ShadowSettingsChanged( bool shadowTechniqueChanged )
		{
			if( shadowTechniqueChanged && IsBaseMaterialInitialized() )
				UpdateBaseMaterial();
		}

		public bool IsDefaultTechniqueCreated()
		{
			return string.IsNullOrEmpty( defaultTechniqueErrorString );
		}

		protected override void OnGetEditorShowInformation( out string[] lines, out ColorValue color )
		{
			base.OnGetEditorShowInformation( out lines, out color );

			if( !IsDefaultTechniqueCreated() )
			{
				List<string> list = new List<string>();

				list.Add( string.Format( "The fixed pipeline is used." ) );
				list.Add( "" );
				list.Add( "Reason:" );

				bool tooManyInstructions = false;

				string[] errorStrings = defaultTechniqueErrorString.Split( new char[] { '\n' },
					StringSplitOptions.RemoveEmptyEntries );
				foreach( string errorString in errorStrings )
				{
					//ignore warnings
					if( errorString.Contains( ": warning X" ) )
						continue;

					if( errorString.Contains( "Compiled shader code uses too many arithmetic " +
						"instruction slots" ) )
					{
						tooManyInstructions = true;
					}

					string[] strings = StringUtils.TextWordWrap( errorString, 70 );
					foreach( string s in strings )
						list.Add( s );
				}

				if( EngineApp.Instance.IsResourceEditor && tooManyInstructions )
				{
					bool limitToShaderModel2 = false;
					{
						TextBlock block = TextBlockUtils.LoadFromVirtualFile(
							"Definitions/ResourceEditor.config" );
						if( block != null )
						{
							if( block.IsAttributeExist( "limitToShaderModel2" ) )
								limitToShaderModel2 = bool.Parse( block.GetAttribute( "limitToShaderModel2" ) );
						}
					}

					if( limitToShaderModel2 )
					{
						list.Add( "" );
						list.Add( "Note: For debugging purposes shader version is limited to 2." );
						list.Add( "You can disable this limitation in the " +
							"\"Definitions\\ResourceEditor.config\"." );
					}
				}

				lines = list.ToArray();
				color = new ColorValue( 1, 0, 0 );
			}
		}

		void PreloadTexture( string textureName )
		{
			Texture texture = TextureManager.Instance.Load( textureName );
			if( texture != null )
				texture.Touch();
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			if( !string.IsNullOrEmpty( diffuse1Map.Texture ) )
				PreloadTexture( diffuse1Map.Texture );
			if( !string.IsNullOrEmpty( diffuse2Map.Texture ) )
				PreloadTexture( diffuse2Map.Texture );
			if( !string.IsNullOrEmpty( diffuse3Map.Texture ) )
				PreloadTexture( diffuse3Map.Texture );
			if( !string.IsNullOrEmpty( diffuse4Map.Texture ) )
				PreloadTexture( diffuse4Map.Texture );
			if( !string.IsNullOrEmpty( reflectionMap.Texture ) )
				PreloadTexture( reflectionMap.Texture );
			if( !string.IsNullOrEmpty( reflectionSpecificCubemap ) )
				PreloadTexture( reflectionSpecificCubemap );
			if( !string.IsNullOrEmpty( emissionMap.Texture ) )
				PreloadTexture( emissionMap.Texture );
			if( !string.IsNullOrEmpty( specularMap.Texture ) )
				PreloadTexture( specularMap.Texture );
			if( !string.IsNullOrEmpty( normalMap.Texture ) )
				PreloadTexture( normalMap.Texture );
			if( !string.IsNullOrEmpty( heightMap.Texture ) )
				PreloadTexture( heightMap.Texture );
		}
	}
}
