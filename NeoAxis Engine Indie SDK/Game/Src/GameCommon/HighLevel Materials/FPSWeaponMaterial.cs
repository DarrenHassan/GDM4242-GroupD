// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.Renderer;

namespace GameCommon
{
	/// <summary>
	/// Material for correct rendering of weapons in a FPS mode.
	/// </summary>
	[Description( "Material for correct rendering of weapons in the FPS mode." )]
	public class FPSWeaponMaterial : ShaderBaseMaterial
	{
		protected override void OnClone( HighLevelMaterial sourceMaterial )
		{
			base.OnClone( sourceMaterial );
		}

		protected override string OnGetExtensionFileName()
		{
			return "FPSWeapon.shaderBaseExtension";
		}
	}
}
