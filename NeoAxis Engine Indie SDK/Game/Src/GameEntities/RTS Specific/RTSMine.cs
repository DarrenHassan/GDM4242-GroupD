// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MathEx;

namespace GameEntities
{
    /// <summary>
    /// Defines the <see cref="RTSMine"/> entity type.
    /// </summary>
    public class RTSMineType : RTSBuildingType
    {
        [FieldSerialize]
        float moneyPerSecond = 0.0f;

        [DefaultValue(0.0f)]
        public float MoneyPerSecond
        {
            get { return moneyPerSecond; }
            set { moneyPerSecond = value; }
        }
    }

    public class RTSMine : RTSBuilding
    {
        RTSMineType _type = null; public new RTSMineType Type { get { return _type; } }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);
            AddTimer();
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
        protected override void OnTick()
        {
            base.OnTick();

            //Add money to faction
            if (BuildedProgress == 1)
            {
                if (RTSFactionManager.Instance != null)
                {
                    if (Intellect != null && Intellect.Faction != null)
                    {
                        RTSFactionManager.FactionItem factionItem = RTSFactionManager.Instance.
                            GetFactionItemByType(Intellect.Faction);

                        if (factionItem != null)
                            factionItem.Money += Type.MoneyPerSecond * TickDelta;
                    }
                }
            }

            //Rotation propeller
            //if( BuildedProgress == 1 )
            //{
            //float angle = -Entities.Instance.TickTime * 500;
            //AttachedObjects[ 3 ].RotationOffset = new Angles( 0, 0, angle ).ToQuat();
            //}
        }
    }
}