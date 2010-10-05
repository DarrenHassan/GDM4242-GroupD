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
        Action currentAction;

        enum Action
        {
            Idle,
            CreateBuilder,
        }
        
        public BadFactionPlayer()
        {
            //set playerFaction
            if (RTSFactionManager.Instance != null && RTSFactionManager.Instance.Factions.Count != 1)
                badFaction = RTSFactionManager.Instance.Factions[1].FactionType;
            currentAction = Action.CreateBuilder;
        }

        public void PerformAction(double elapsedGameTime, LinkedList<Entity> mapChildren)
        {
            switch (currentAction)
            {
                case Action.Idle:
                    IdleAction(mapChildren);
                    break;
                case Action.CreateBuilder:
                    CreateBuilder(mapChildren);
                    currentAction = Action.Idle;
                    break;
            }
        }

        // Create a builder ant
        void CreateBuilder(LinkedList<Entity> mapChildren)
        {
            foreach (Entity entity in mapChildren)
            {
                RTSBuilding building = entity as RTSBuilding;
                if (building == null)
                    continue;
                if (building.Intellect == null)
                    continue;
                if (building.Intellect.Faction == badFaction)
                {
                    RTSBuildingAI intellect = building.Intellect as RTSBuildingAI;
                    intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.ProductUnit, (RTSUnitType)EntityTypes.Instance.GetByName( "BuilderAnt" )), false);
                }
            }
        }

        // Cause the warrior ants to wander
        void IdleAction(LinkedList<Entity> mapChildren)
        {
            // Cycle through all the entities 
            foreach(Entity entity in mapChildren)
            {
                GenericAntCharacter unit = entity as GenericAntCharacter;
                if (unit == null)
                    continue;
                if (unit.Intellect == null)
                    continue;
                if (unit.Intellect.Faction == badFaction)
                {
                    // Cause only the warrior ants to wander
                    if (unit.Type.Name == "WarriorAnt")
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
