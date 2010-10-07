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
using Game;

namespace GameEntities.RTS_Specific
{
    class BadFactionPlayer
    {
        // Computer's faction
        FactionType badFaction;
        // The current action the bad faction is performing
        protected Action currentAction;

        IEnumerator<Action> openingStrategy;
        int strategyIterator;

        // The builder ant
        AntUnitAI builderAI;
        GenericAntCharacter builder;
        // The hive
        RTSBuildingAI hiveAI;
        RTSBuilding hive;
        // The barracks
        RTSBuildingAI barrackAI;
        RTSBuilding barrack;

        // The building being built
        RTSBuildingType taskTargetBuildingType;

        protected enum Action
        {
            Null,
            WarriorsExplore,
            CreateBuilder,
            BuildBarracks,
        }
        
        public BadFactionPlayer()
        {
            // Initialise the bad faction's characters
            builderAI = null;
            builder = null;
            hiveAI = null;
            hive = null;
            barrackAI = null;
            barrack = null;

            // Set the computer's faction
            if (RTSFactionManager.Instance != null && RTSFactionManager.Instance.Factions.Count != 1)
                badFaction = RTSFactionManager.Instance.Factions[1].FactionType;

            // Initialise the opening strategy
            openingStrategy = OpeningStrategy().GetEnumerator();
            strategyIterator = 0;
            // Initalise the first action
            currentAction = Action.Null;
        }

        // The open strategy
        protected IEnumerable<Action> OpeningStrategy()
        {
            while (hiveAI != null)
            {
                if (strategyIterator == 0)
                {
                    strategyIterator = 1;
                    yield return Action.CreateBuilder;
                }
                else if (strategyIterator == 1)
                {
                    strategyIterator = 2;
                    yield return Action.WarriorsExplore;
                }
                else if (strategyIterator == 2)
                {
                    if (builderAI != null)
                    {
                        strategyIterator = 3;
                        yield return Action.BuildBarracks;
                    }
                    else
                        yield return Action.Null;
                }
                else if (strategyIterator == 3)
                    yield return Action.Null;
            }
        }

        public void PerformAction(double elapsedGameTime, LinkedList<Entity> mapChildren)
        {
            // Identify the bad faction's entities
            foreach (Entity entity in mapChildren)
            {
                GenericAntCharacter unit = entity as GenericAntCharacter;
                if (unit != null)
                {
                    if (unit.Intellect != null)
                    {
                        if (unit.Intellect.Faction == badFaction)
                        {
                            // Identify the builder ant if it has not already been found
                            if (builderAI == null && unit.Type.Name == "BuilderAnt")
                            {
                                AntUnitAI intellect = unit.Intellect as AntUnitAI;
                                if (intellect != null)
                                {
                                    // Builder ant found
                                    builderAI = intellect;
                                    builder = unit;
                                }
                            }
                        }
                    }
                }
                else
                {
                    RTSBuilding building = entity as RTSBuilding;
                    if (building != null)
                    {
                        if (building.Intellect != null)
                        {
                            if (building.Intellect.Faction == badFaction)
                            {
                                // Identify the hive if it has not already been found
                                if (hiveAI == null && building.Type.Name == "AntColmena")
                                {
                                    RTSBuildingAI intellect = building.Intellect as RTSBuildingAI;
                                    if (intellect != null)
                                    {
                                        // Hive found
                                        hiveAI = intellect;
                                        hive = building;
                                    }
                                }
                                // Identify the barracks if it has not already been found
                                else if (barrackAI == null && building.Type.Name == "AntBarrack")
                                {
                                    RTSBuildingAI intellect = building.Intellect as RTSBuildingAI;
                                    if (intellect != null)
                                    {
                                        // barrack found
                                        barrackAI = intellect;
                                        barrack = building;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Move the strategy on to the next action
            openingStrategy.MoveNext();
            currentAction = openingStrategy.Current;            

            switch (currentAction)
            {
                case Action.WarriorsExplore:
                    WarriorsExplore(mapChildren);
                    break;
                case Action.CreateBuilder:
                    CreateBuilder();;
                    break;
                case Action.BuildBarracks:
                    BuildBarracks();
                    break;
                case Action.Null:
                    break;
            }
        }

        // Create a builder ant
        void CreateBuilder()
        {
            if (hiveAI != null)
            {
                hiveAI.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.ProductUnit,
                    (RTSUnitType)EntityTypes.Instance.GetByName("BuilderAnt")), false);
            }
        }

        // Build a barracks
        void BuildBarracks()
        {
            if (builderAI != null)
            {
                taskTargetBuildingType = (RTSBuildingType)EntityTypes.Instance.GetByName("AntBarrack");

                string meshName = null;
                {
                    foreach (MapObjectTypeAttachedObject typeAttachedObject in
                        taskTargetBuildingType.AttachedObjects)
                    {
                        MapObjectTypeAttachedMesh typeMeshAttachedObject = typeAttachedObject as
                            MapObjectTypeAttachedMesh;
                        if (typeMeshAttachedObject != null)
                        {
                            meshName = typeMeshAttachedObject.MeshName;
                            break;
                        }
                    }
                }

                MeshObject taskTargetBuildMeshObject = SceneManager.Instance.CreateMeshObject(meshName);
                SceneNode taskTargetBuildSceneNode = new SceneNode();
                taskTargetBuildSceneNode.Attach(taskTargetBuildMeshObject);
                taskTargetBuildSceneNode.Visible = false;

                // Randomly generate positions within 25 units of the hive
                Random rand = new Random();
                Vec3 pos;
                do
                {
                    pos = new Vec3(hive.Position.X + ((float)rand.NextDouble() * 50f - 25f),
                        hive.Position.Y + ((float)rand.NextDouble() * 50f - 25f), hive.Position.Z);
                } while (!IsFreeForBuildTaskTargetBuild(pos));

                // Build the barracks
                builderAI.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.BuildBuilding, pos,
                    (RTSBuildingType)EntityTypes.Instance.GetByName("AntBarrack")), false);

                GameEngineApp.Instance.ControlManager.PlaySound(
                    "Sounds\\Feedback\\RTSBuildBuilding.ogg");
            }
        }

        // Task all the warrior ants with exploring
        void WarriorsExplore(LinkedList<Entity> mapChildren)
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

        // Is the location free for building
        bool IsFreeForBuildTaskTargetBuild(Vec3 pos)
        {
            Bounds bounds;
            {
                PhysicsModel physicsModel = PhysicsWorld.Instance.LoadPhysicsModel(taskTargetBuildingType.PhysicsModel);
                if (physicsModel == null)
                {
                    Log.Fatal(string.Format("No physics model for \"{0}\"", taskTargetBuildingType.ToString()));
                    return false;
                }
                bounds = physicsModel.GetGlobalBounds();
                physicsModel.Dispose();

                bounds += pos;
            }

            Rect rect = new Rect(bounds.Minimum.ToVec2(), bounds.Maximum.ToVec2());
            return GridPathFindSystem.Instance.IsFreeInMapMotion(rect);
        }
    }
}
