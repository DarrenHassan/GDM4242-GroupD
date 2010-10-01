// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="HealthItem"/> entity type.
	/// </summary>
	public class HealthItemType : ItemType
	{
		[FieldSerialize]
		float health;

		[DefaultValue( 0.0f )]
		public float Health
		{
			get { return health; }
			set { health = value; }
		}
	}

	/// <summary>
	/// Represents a item of the healths. When the player take this item his 
	/// <see cref="Dynamic.Life"/> increase.
	/// </summary>
	public class HealthItem : Item
	{
		HealthItemType _type = null; public new HealthItemType Type { get { return _type; } }

        public HealthItem()
        {
            FilterGroups |= GameFilterGroups.HealthFilterGroup;
        }
		protected override bool OnTake( Unit unit )
		{
			bool take = base.OnTake( unit );

			float lifeMax = unit.Type.LifeMax;

			if( unit.Life < lifeMax )
			{
				float life = unit.Life + Type.Health;
				if( life > lifeMax )
					life = lifeMax;

				unit.Life = life;

				take = true;
			}

			return take;
		}
	}
}
