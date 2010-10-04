using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.FileSystem;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace GameEntities.RTS_Specific
{
    class BadFactionPlayer
    {
        //Player Faction
        FactionType badFaction;
        // Lists of ant types
        LinkedList<AntUnitAI> builderAnts;
        LinkedList<AntUnitAI> foragerAnts;
        LinkedList<AntUnitAI> warriorAnts;
        
        public BadFactionPlayer()
        {
            //set playerFaction
            if (RTSFactionManager.Instance != null && RTSFactionManager.Instance.Factions.Count != 1)
                badFaction = RTSFactionManager.Instance.Factions[1].FactionType;
        }

        public void PerformAction(double elapsedGameTime, LinkedList<Entity> mapChildren)
        {
            // Cause all the ants to wander 
            if (elapsedGameTime > 10f && elapsedGameTime < 11f)
            {
                //foreach (Entity entity in Map.Instance.Children)
                foreach(Entity entity in mapChildren)
                {
                    RTSUnit unit = entity as RTSUnit;
                    if (unit == null)
                        continue;
                    if (unit.Intellect == null)
                        continue;
                    if (unit.Intellect.Faction == badFaction)
                    {
                        AntUnitAI intellect = unit.Intellect as AntUnitAI;
                        if (intellect == null)
                            continue;
                        // Radius, distance, jitter
                        intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Wander, 1, 100, 1), false);
                    }
                }
            }
        }
    }
}
