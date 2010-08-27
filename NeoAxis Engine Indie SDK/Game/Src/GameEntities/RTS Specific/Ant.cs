using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace GameEntities.RTS_Specific
{
    public class AntType : RTSCharacterType
    {
    }
    public class Ant : RTSCharacter
    {
        AntType _type = null; public new AntType Type { get { return _type; } }

        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);
        }
    }
}
