using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace GameEntities.RTS_Specific
{
    public class AntType : RTSCharacterType
    {
        /*[FieldSerialize]
        [DefaultValue("Walk")]
        string walkAnimationName = "Walk";
        
        [DefaultValue("Walk")]
        public new string WalkAnimationName
        {
            get { return walkAnimationName; }
            set { walkAnimationName = value; }
        }*/
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
