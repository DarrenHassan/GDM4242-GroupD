// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="ItemCreator"/> entity type.
	/// </summary>
	public class ItemCreatorType : MapObjectType
	{
	}

	public class ItemCreator : MapObject
	{
		[FieldSerialize]
		ItemType itemType;
		[FieldSerialize]
		float createRemainingTime;

		[FieldSerialize]
		float remainingTime;
		[FieldSerialize]
		Item item;

		//

		ItemCreatorType _type = null; public new ItemCreatorType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( item == null && remainingTime == 0 )
				remainingTime = createRemainingTime;

			if( remainingTime != 0 )
			{
				remainingTime -= TickDelta;
				if( remainingTime <= 0 )
				{
					remainingTime = 0;

					Item i = (Item)Entities.Instance.Create( itemType, Parent );
					i.Position = Position;
					i.PostCreate();
					Item = i;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );
			if( item == entity )
				item = null;
		}

		public ItemType ItemType
		{
			get { return itemType; }
			set { itemType = value; }
		}

		public float CreateRemainingTime
		{
			get { return createRemainingTime; }
			set { createRemainingTime = value; }
		}

		[Browsable( false )]
		public Item Item
		{
			get { return item; }
			set
			{
				if( item != null )
					RemoveRelationship( item );
				item = value;
				if( item != null )
					AddRelationship( item );
			}
		}

	}
}
