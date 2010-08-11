// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Terminal"/> entity type.
	/// </summary>
	public class TerminalType : GameGuiObjectType
	{
	}

	public class Terminal : GameGuiObject
	{
		GameGuiObjectType _type = null; public new GameGuiObjectType Type { get { return _type; } }

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				MapObjectAttachedObject attachedObject = GetAttachedObjectByAlias( "clientNotSupported" );
				if( attachedObject != null )
					attachedObject.Visible = true;
			}
		}
	}
}
