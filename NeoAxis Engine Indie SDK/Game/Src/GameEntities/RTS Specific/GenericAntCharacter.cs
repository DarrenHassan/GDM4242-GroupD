using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace GameEntities.RTS_Specific
{
    public class GenericAntCharacterType : RTSCharacterType
    {
    }
    public class GenericAntCharacter : RTSCharacter
    {
        GenericAntCharacterType _type = null; public new GenericAntCharacterType Type { get { return _type; } }

        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);
        }
    }
}
