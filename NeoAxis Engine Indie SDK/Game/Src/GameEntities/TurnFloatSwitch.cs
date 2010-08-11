// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="TurnFloatSwitch"/> entity type.
	/// </summary>
	public class TurnFloatSwitchType : FloatSwitchType
	{
		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float turnCoefficient = 1;

		[DefaultValue( 1.0f )]
		public float TurnCoefficient
		{
			get { return turnCoefficient; }
			set { turnCoefficient = value; }
		}

	}

	/// <summary>
	/// Defines the parametrical switch which turns the <see cref="Switch.UseAttachedMesh"/>.
	/// </summary>
	public class TurnFloatSwitch : FloatSwitch
	{
		TurnFloatSwitchType _type = null; public new TurnFloatSwitchType Type { get { return _type; } }

		protected override void OnValueChange()
		{
			base.OnValueChange();

			if( UseAttachedMesh != null )
			{
				float angle = Value * Type.TurnCoefficient;
				angle = MathFunctions.RadiansNormalize360( angle );

				UseAttachedMesh.RotationOffset = new Angles(
					0, 0, MathFunctions.RadToDeg( angle ) ).ToQuat();
			}
		}
	}
}
