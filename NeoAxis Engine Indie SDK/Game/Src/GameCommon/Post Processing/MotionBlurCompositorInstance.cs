// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.Renderer;

namespace GameCommon
{
	/// <summary>
	/// Represents work with the MotionBlur post effect.
	/// </summary>
	[CompositorName( "MotionBlur" )]
	public class MotionBlurCompositorInstance : CompositorInstance
	{
		static float blur = .8f;

		public static float Blur
		{
			get { return blur; }
			set { blur = value; }
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 666 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].
					Passes[ 0 ].FragmentProgramParameters;
				if( parameters != null )
					parameters.SetNamedConstant( "blur", Blur );
			}
		}
	}
}
