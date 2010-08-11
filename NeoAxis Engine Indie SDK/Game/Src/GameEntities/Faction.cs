// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.EntitySystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Faction"/> entity type.
	/// </summary>
	public class FactionType : EntityType
	{
	}

	/// <summary>
	/// Concept of the command. Opponents with an artificial intelligences attack 
	/// units of another's fraction.
	/// </summary>
	public class Faction : Entity
	{
		FactionType _type = null; public new FactionType Type { get { return _type; } }
	}
}
