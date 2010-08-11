// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using Engine;
using Engine.Utils;
using Engine.Renderer;
using Engine.MathEx;

namespace GameCommon
{
	/// <summary>
	/// Simple example of high level material.
	/// </summary>
	[Description( "Example of a simple material template." )]
	public class SimpleExampleMaterial : HighLevelMaterial
	{
		string diffuseMap = "";

		//

		[Category( "SimpleExample" )]
		[Editor( typeof( EditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
		public string DiffuseMap
		{
			get { return diffuseMap; }
			set { diffuseMap = value; }
		}

		protected override void OnClone( HighLevelMaterial sourceMaterial )
		{
			base.OnClone( sourceMaterial );
			SimpleExampleMaterial source = (SimpleExampleMaterial)sourceMaterial;
			diffuseMap = source.diffuseMap;
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			if( block.IsAttributeExist( "diffuseMap" ) )
				diffuseMap = block.GetAttribute( "diffuseMap" );

			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			if( !string.IsNullOrEmpty( diffuseMap ) )
				block.SetAttribute( "diffuseMap", diffuseMap );
		}

		void SetProgramAutoConstants( GpuProgramParameters parameters, int lightCount )
		{
			parameters.SetNamedAutoConstant( "worldMatrix",
				GpuProgramParameters.AutoConstantType.WorldMatrix );
			parameters.SetNamedAutoConstant( "worldViewProjMatrix",
				GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );

			parameters.SetNamedAutoConstant( "ambientLightColor",
				GpuProgramParameters.AutoConstantType.AmbientLightColor );

			if( lightCount != 0 )
			{
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
				parameters.SetNamedAutoConstant( "spotLightParamsArray",
					GpuProgramParameters.AutoConstantType.SpotLightParamsArray, lightCount );
			}
		}

		protected override bool OnInitBaseMaterial()
		{
			if( !base.OnInitBaseMaterial() )
				return false;

			bool success = CreateDefaultTechnique();
			if( !success )
				CreateFixedPipelineTechnique();

			return true;
		}

		bool CreateDefaultTechnique()
		{
			string sourceFile = "Materials\\Common\\SimpleExample.cg_hlsl";

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

			//technique is supported?
			if( !GpuProgramManager.Instance.IsSyntaxSupported( fragmentSyntax ) )
				return false;
			if( !GpuProgramManager.Instance.IsSyntaxSupported( vertexSyntax ) )
				return false;

			BaseMaterial.ReceiveShadows = false;

			//create techniques
			foreach( MaterialSchemes materialScheme in Enum.GetValues( typeof( MaterialSchemes ) ) )
			{
				Technique technique = BaseMaterial.CreateTechnique();
				technique.SchemeName = materialScheme.ToString();

				//pass 0: ambient pass
				//pass 1: directional light
				//pass 2: point light
				//pass 3: spot light

				for( int nPass = 0; nPass < 4; nPass++ )
				{
					//create pass
					Pass pass = technique.CreatePass();

					bool ambientPass = nPass <= 1;
					bool lightPass = nPass >= 1;

					RenderLightType lightType = RenderLightType.Directional;

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

					int lightCount = lightPass ? 1 : 0;

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
					}

					/////////////////////////////////////
					//generate general compile arguments and create texture unit states
					StringBuilder generalArguments = new StringBuilder( 256 );
					{
						if( ambientPass )
							generalArguments.Append( " -DAMBIENT_PASS" );
						generalArguments.AppendFormat( " -DLIGHT_COUNT={0}", lightCount );
						generalArguments.Append( " -DLIGHTING" );

						//DiffuseMap
						if( !string.IsNullOrEmpty( DiffuseMap ) )
						{
							generalArguments.Append( " -DDIFFUSE_MAP" );
							pass.CreateTextureUnitState( DiffuseMap );
						}
					}

					/////////////////////////////////////
					//generate programs

					//generate program for only ambient pass
					if( ambientPass && !lightPass )
					{
						string error;

						//vertex program
						GpuProgram vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
							"SimpleExample_Vertex_", GpuProgramType.VertexProgram, sourceFile,
							"main_vp", vertexSyntax, generalArguments.ToString(),
							out error );
						if( vertexProgram == null )
						{
							Log.Fatal( error );
							return false;
						}

						SetProgramAutoConstants( vertexProgram.DefaultParameters, 0 );
						pass.VertexProgramName = vertexProgram.Name;

						//fragment program
						GpuProgram fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
							"SimpleExample_Fragment_", GpuProgramType.FragmentProgram, sourceFile,
							"main_fp", fragmentSyntax, generalArguments.ToString(),
							out error );
						if( fragmentProgram == null )
						{
							Log.Fatal( error );
							return false;
						}

						SetProgramAutoConstants( fragmentProgram.DefaultParameters, 0 );
						pass.FragmentProgramName = fragmentProgram.Name;
					}

					//generate program for light passes
					if( lightPass )
					{
						string error;

						StringBuilder arguments = new StringBuilder( generalArguments.Length + 100 );
						arguments.Append( generalArguments.ToString() );

						arguments.AppendFormat( " -DLIGHTTYPE_{0}", lightType.ToString().ToUpper() );

						//vertex program
						GpuProgram vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
							"SimpleExample_Vertex_", GpuProgramType.VertexProgram, sourceFile,
							"main_vp", vertexSyntax, arguments.ToString(),
							out error );
						if( vertexProgram == null )
						{
							Log.Fatal( error );
							return false;
						}

						SetProgramAutoConstants( vertexProgram.DefaultParameters, lightCount );
						pass.VertexProgramName = vertexProgram.Name;

						//fragment program
						GpuProgram fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
							"SimpleExample_Fragment_", GpuProgramType.FragmentProgram, sourceFile,
							"main_fp", fragmentSyntax, arguments.ToString(),
							out error );
						if( fragmentProgram == null )
						{
							Log.Fatal( error );
							return false;
						}

						SetProgramAutoConstants( fragmentProgram.DefaultParameters, lightCount );
						pass.FragmentProgramName = fragmentProgram.Name;
					}
				}
			}

			return true;
		}

		void CreateFixedPipelineTechnique()
		{
			Technique tecnhique = BaseMaterial.CreateTechnique();
			Pass pass = tecnhique.CreatePass();
			pass.NormalizeNormals = true;

			pass.CreateTextureUnitState( DiffuseMap );
		}

		protected override void OnClearBaseMaterial()
		{
			//clear material
			BaseMaterial.RemoveAllTechniques();

			base.OnClearBaseMaterial();
		}
	}
}
