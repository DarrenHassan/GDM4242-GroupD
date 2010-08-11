// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.Utils;
using Engine.Renderer;

namespace GameCommon
{
	/// <summary>
	/// Material for grass and trees. Adds wave in the vertex shader.
	/// </summary>
	[Description( "Material for grass and trees. Adds wave in the vertex shader." )]
	public class VegetationMaterial : ShaderBaseMaterial
	{
		bool waveOnlyInVerticalPosition;
		bool receiveObjectsPositionsFromVertices;

		//

		[Description( "The inclined objects will not be waving. For example tumbled down trees." )]
		[Category( "Vegetation" )]
		[DefaultValue( false )]
		public bool WaveOnlyInVerticalPosition
		{
			get { return waveOnlyInVerticalPosition; }
			set { waveOnlyInVerticalPosition = value; }
		}
		
		/// <summary>
		/// Gets or sets a value which indicates it is necessary to reveice objects 
		/// positions from the vertices.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Usually it is necessary for batched geometry (for waving).
		/// </para>
		/// <para>
		/// Positions will be taken from Type: TextureCoordinates, Index: 4.
		/// </para>
		/// </remarks>
		[Description( "Reveice objects positions from the vertices. Usually it is necessary for batched geometry (for waving). Positions will be taken from Type: TextureCoordinates, Index: 4." )]
		[Category( "Vegetation" )]
		[DefaultValue( false )]
		public bool ReceiveObjectsPositionsFromVertices
		{
			get { return receiveObjectsPositionsFromVertices; }
			set { receiveObjectsPositionsFromVertices = value; }
		}

		protected override void OnClone( HighLevelMaterial sourceMaterial )
		{
			base.OnClone( sourceMaterial );
			VegetationMaterial source = (VegetationMaterial)sourceMaterial;
			waveOnlyInVerticalPosition = source.waveOnlyInVerticalPosition;
			receiveObjectsPositionsFromVertices = source.receiveObjectsPositionsFromVertices;
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			if( block.IsAttributeExist( "waveOnlyInVerticalPosition" ) )
			{
				waveOnlyInVerticalPosition = 
					bool.Parse( block.GetAttribute( "waveOnlyInVerticalPosition" ) );
			}
			if( block.IsAttributeExist( "receiveObjectsPositionsFromVertices" ) )
			{
				receiveObjectsPositionsFromVertices = 
					bool.Parse( block.GetAttribute( "receiveObjectsPositionsFromVertices" ) );
			}

			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			if( waveOnlyInVerticalPosition )
				block.SetAttribute( "waveOnlyInVerticalPosition", waveOnlyInVerticalPosition.ToString() );
			if( receiveObjectsPositionsFromVertices )
			{
				block.SetAttribute( "receiveObjectsPositionsFromVertices", 
					receiveObjectsPositionsFromVertices.ToString() );
			}
		}

		protected override string OnGetExtensionFileName()
		{
			return "Vegetation.shaderBaseExtension";
		}

		protected override void OnAddCompileArguments( StringBuilder arguments )
		{
			base.OnAddCompileArguments( arguments );

			if( waveOnlyInVerticalPosition )
				arguments.Append( " -DWAVE_ONLY_IN_VERTICAL_POSITION" );
			if( receiveObjectsPositionsFromVertices )
				arguments.Append( " -DRECEIVE_OBJECTS_POSITIONS_FROM_VERTICES" );
		}

		protected override bool OnIsNeedSpecialShadowCasterMaterial()
		{
			return true;
			//return base.OnIsNeedSpecialShadowCasterMaterial();
		}


	}
}
