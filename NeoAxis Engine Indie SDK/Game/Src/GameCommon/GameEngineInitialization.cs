// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.Utils;
using GameCommon;

namespace GameCommon
{
	/// <summary>
	/// Class for execute actions after initialization of the engine.
	/// </summary>
	/// <remarks>
	/// It is class works in simulation application and editors (Resource Editor, Map Editor).
	/// </remarks>
	public class GameEngineInitialization : EngineInitialization
	{
		protected override bool OnInit()
		{
			CorrectRenderTechnique();

			//Initialize HDR compositor for HDR render technique
			if( EngineApp.RenderTechnique == "HDR" )
				InitializeHDRCompositor();

			//Initialize LDRBloom for standard technique
			if( EngineApp.RenderTechnique == "Standard" )
			{
				if( IsActivateLDRBloomByDefault() )
					InitializeLDRBloomCompositor();
			}

			return true;
		}

		bool IsHDRSupported()
		{
			Compositor compositor = CompositorManager.Instance.GetByName( "HDR" );
			if( compositor == null || !compositor.IsSupported() )
				return false;

			bool floatTexturesSupported = TextureManager.Instance.IsEquivalentFormatSupported(
				Texture.Type.Type2D, PixelFormat.Float16RGB, Texture.Usage.RenderTarget );
			if( !floatTexturesSupported )
				return false;

			//!!!!!need also check for support blending for float textures

			if( RenderSystem.Instance.GPUIsGeForce() &&
				RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
				RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV30 )
				return false;
			if( RenderSystem.Instance.GPUIsRadeon() &&
				RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
				RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
				return false;

			return true;
		}

		bool IsActivateHDRByDefault()
		{
			if( IsHDRSupported() )
			{
				if( RenderSystem.Instance.GPUIsGeForce() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
						RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV40 )
					{
						return false;
					}
					else
						return true;
				}
				if( RenderSystem.Instance.GPUIsRadeon() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
						RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
					{
						return false;
					}
					else
						return true;
				}
			}

			return false;
		}

		bool IsActivateLDRBloomByDefault()
		{
			//if( RenderSystem.Instance.HasShaderModel2() )
			//   return true;

			return false;
		}

		void CorrectRenderTechnique()
		{
			//HDR choose by default
			if( string.IsNullOrEmpty( EngineApp.RenderTechnique ) && IsHDRSupported() )
				EngineApp.RenderTechnique = IsActivateHDRByDefault() ? "HDR" : "Standard";

			//HDR render technique support check
			if( EngineApp.RenderTechnique == "HDR" && !IsHDRSupported() )
			{
				bool nullRenderSystem = RenderSystem.Instance.Name.ToLower().Contains( "null" );

				if( !nullRenderSystem )//no warning for null render system
				{
					Log.Warning( "HDR render technique is not supported. " +
						"Using \"Standard\" render technique." );
				}
				EngineApp.RenderTechnique = "Standard";
			}

			if( string.IsNullOrEmpty( EngineApp.RenderTechnique ) )
				EngineApp.RenderTechnique = "Standard";
		}

		void InitializeHDRCompositor()
		{
			bool editor = EngineApp.Instance.IsResourceEditor || EngineApp.Instance.IsMapEditor;

			//Add HDR compositor
			HDRCompositorInstance instance = (HDRCompositorInstance)
				RendererWorld.Instance.DefaultViewport.AddCompositor( "HDR", 0 );

			if( instance == null )
				return;

			//Enable HDR compositor
			instance.Enabled = true;

			//Load default settings
			TextBlock fileBlock = TextBlockUtils.LoadFromVirtualFile( "Definitions/Renderer.config" );
			if( fileBlock != null )
			{
				TextBlock hdrBlock = fileBlock.FindChild( "hdr" );
				if( hdrBlock != null )
				{
					if( !editor )//No adaptation in the editors
					{
						if( hdrBlock.IsAttributeExist( "adaptation" ) )
							HDRCompositorInstance.Adaptation = bool.Parse( hdrBlock.GetAttribute( "adaptation" ) );

						if( hdrBlock.IsAttributeExist( "adaptationVelocity" ) )
							HDRCompositorInstance.AdaptationVelocity =
								float.Parse( hdrBlock.GetAttribute( "adaptationVelocity" ) );

						if( hdrBlock.IsAttributeExist( "adaptationMiddleBrightness" ) )
							HDRCompositorInstance.AdaptationMiddleBrightness =
								float.Parse( hdrBlock.GetAttribute( "adaptationMiddleBrightness" ) );

						if( hdrBlock.IsAttributeExist( "adaptationMinimum" ) )
							HDRCompositorInstance.AdaptationMinimum =
								float.Parse( hdrBlock.GetAttribute( "adaptationMinimum" ) );

						if( hdrBlock.IsAttributeExist( "adaptationMaximum" ) )
							HDRCompositorInstance.AdaptationMaximum =
								float.Parse( hdrBlock.GetAttribute( "adaptationMaximum" ) );
					}

					if( hdrBlock.IsAttributeExist( "bloomBrightThreshold" ) )
						HDRCompositorInstance.BloomBrightThreshold =
							float.Parse( hdrBlock.GetAttribute( "bloomBrightThreshold" ) );

					if( hdrBlock.IsAttributeExist( "bloomScale" ) )
					{
						HDRCompositorInstance.BloomScale =
							float.Parse( hdrBlock.GetAttribute( "bloomScale" ) );
					}
				}
			}
		}

		void InitializeLDRBloomCompositor()
		{
			LDRBloomCompositorInstance instance = (LDRBloomCompositorInstance)
				RendererWorld.Instance.DefaultViewport.AddCompositor(
				"LDRBloom", 0 );

			if( instance == null )
				return;

			instance.Enabled = true;
		}

	}
}
