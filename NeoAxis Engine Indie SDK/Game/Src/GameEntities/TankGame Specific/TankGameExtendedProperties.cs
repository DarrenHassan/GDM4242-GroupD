// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace GameEntities
{
	public class TankGameExtendedProperties : EntityExtendedProperties
	{
		[FieldSerialize]
		MapCurve way;
		[FieldSerialize]
		Region activateRegion;

		//

		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );

			if( way == entity )
				way = null;
			if( activateRegion == entity )
				activateRegion = null;
		}

		public MapCurve Way
		{
			get { return way; }
			set
			{
				if( way != null )
					Owner.RemoveRelationship( way );
				way = value;
				if( way != null )
					Owner.AddRelationship( way );
			}
		}

		public Region ActivationRegion
		{
			get { return activateRegion; }
			set
			{
				if( activateRegion != null )
					Owner.RemoveRelationship( activateRegion );
				activateRegion = value;
				if( activateRegion != null )
					Owner.AddRelationship( activateRegion );
			}
		}

	}
}
