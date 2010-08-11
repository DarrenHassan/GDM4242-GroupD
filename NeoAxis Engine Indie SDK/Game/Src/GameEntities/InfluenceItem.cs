// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="InfluenceItem"/> entity type.
	/// </summary>
	public class InfluenceItemType : ItemType
	{
		[FieldSerialize]
		InfluenceType influenceType;
		[FieldSerialize]
		float influenceTime;

		public InfluenceType InfluenceType
		{
			get { return influenceType; }
			set { influenceType = value; }
		}

		[DefaultValue( 0.0f )]
		public float InfluenceTime
		{
			get { return influenceTime; }
			set { influenceTime = value; }
		}
	}

	public class InfluenceItem : Item
	{
		InfluenceItemType _type = null; public new InfluenceItemType Type { get { return _type; } }

		protected override bool OnTake( Unit unit )
		{
			base.OnTake( unit );

			if( Type.InfluenceType == null )
			{
				Log.Warning( "InfluenceItem.OnTake: Type.InfluenceType == null" );
				return false;
			}

			unit.AddInfluence( Type.InfluenceType, Type.InfluenceTime, true );
			return true;
		}
	}
}
