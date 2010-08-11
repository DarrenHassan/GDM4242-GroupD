// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="AI"/> entity type.
	/// </summary>
	public abstract class AIType : IntellectType
	{
	}

	public abstract class AI : Intellect
	{
		AIType _type = null; public new AIType Type { get { return _type; } }
	}
}
