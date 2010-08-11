// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
	public enum MaterialSchemes
	{
		//no normal mapping, no receiving shadows, no specular.
		//HeightmapTerrain will use SimpleRendering mode for this scheme.
		//used for generation WaterPlane reflection.
		Low,

		//maximum quality.
		Default
	}
}
