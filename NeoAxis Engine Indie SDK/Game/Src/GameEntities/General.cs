// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.MapSystem;
using Engine.SoundSystem;

namespace GameEntities
{
	/// <summary>
	/// Defines possible substances of <see cref="Dynamic"/> objects.
	/// Substances are necessary for work of influences (<see cref="Influence"/>). 
	/// The certain influences operate only on the set substances.
	/// </summary>
	[Flags]
	public enum Substance
	{
		None = 0,
		Flesh = 2,
		Metal = 4,
		Wood = 8,
	}

	/// <summary>
	/// User defined Map filter groups.
	/// </summary>
	public class GameFilterGroups
	{
		//see "Unit" class constructor in Unit.cs. There you can found initialization of this group.
		public const Map.FilterGroups UnitFilterGroup = Map.FilterGroups.Group1;
        public const Map.FilterGroups MineFilterGroup = Map.FilterGroups.Group2;
	}
}
