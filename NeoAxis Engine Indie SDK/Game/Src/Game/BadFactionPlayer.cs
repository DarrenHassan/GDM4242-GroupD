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
        int actionInterval = 5;
        double lastActionTime = 0;

        IEnumerator<Action> openingStrategy;
        int strategyIterator;
        IEnumerator<Action> foragerStrategy;
        int foragerIterator;

        // The forager ant
        AntUnitAI foragerAI;
        ForagerAnt forager;
        // The builder ant
        AntUnitAI builderAI;
        GenericAntCharacter builder;
        // The hive
        RTSBuildingAI hiveAI;
        RTSBuilding hive;
        // The barracks
        RTSBuildingAI barrackAI;
        RTSBuilding barrack;
        RTSBuildingAI depotAI;
        RTSMine depot;

        // Wander paramters
        static int maxWanderDistance = 200;
        static int minWanderDistance = 50;
        static int wanderIncrement = 10;
        int wanderRadius = 1;
        int wanderDistance = minWanderDistance;
        int wanderJitter = 1;        

        // Good faction's location
        bool goodFactionPositionFound = false;
        Vec3 goodFactionPosition;

        // The building being built
        RTSBuildingType taskTargetBuildingType;

        protected enum Action
        {
            Null,
            WarriorsExplore,
            CreateBuilder,
            CreateForager,
            BuildDepot,
            StartCollecting,
            BuildBarracks,
            CreateWarrior,
            WarriorsAttackPosition,
            WarriorStop,
        }
        
        public BadFactionPlayer()
        {
            // Initialise the bad faction's characters
            foragerAI = null;
            forager = null;
            builderAI = null;
            builder = null;
            hiveAI = null;
            hive = null;
            barrackAI = null;
            barrack = null;
            depotAI = null;
            depot = null;

            // Set the computer's faction
            if (RTSFactionManager.Instance != null && RTSFactionManager.Instance.Factions.Count != 1)
                badFaction = RTSFactionManager.Instance.Factions[1].FactionType;

            // Initialise the opening strategy
            openingStrategy = OpeningStrategy().GetEnumerator();
            strategyIterator = 0;
            foragerStrategy = ForagerStrategy().GetEnumerator();
            foragerIterator = 0;
            // Initalise the first action
            currentAction = Action.Null;
        }


        // The open strategy
        protected IEnumerable<Action> OpeningStrategy()
        {
            while (true)
            {
                if (strategyIterator == 0)
                {
                    if (hiveAI != null)
                    {
                        if (hiveAI.CurrentTask.Type == AntUnitAI.Task.Types.Stop)
                        {
                            strategyIterator = 1;
                            yield return Action.CreateBuilder;
                        }
                        else
                        {
                            strategyIterator = 1;
                            //yield return Action.Null;
                        }
                    }
                }
                else if (strategyIterator == 1)
                {
                    if (builderAI != null)
                    {
                        if (builderAI.CurrentTask.Type == AntUnitAI.Task.Types.Stop)
                        {
                            strategyIterator = 2;
                            yield return Action.BuildBarracks;
                        }
                        else
                        {
                            // The builder is busy
                            strategyIterator = 2;
                            //yield return Action.Null;
                        }
                    }
                    else
                    {
                        strategyIterator = 0;
                        yield return Action.Null;
                    }

                }
                else if (strategyIterator == 2)
                {
                    if (barrackAI != null)
                    {
                        strategyIterator = 3;
                        yield return Action.CreateWarrior;
                    }
                    else
                    {
                        strategyIterator = 1;
                        yield return Action.Null;
                    }
                }
                else if (strategyIterator == 3)
                {
                    // Has the good faction's position been found
                    if (!goodFactionPositionFound)
                    {
                        // No - explore a wider area
                        if (wanderDistance >= maxWanderDistance)
                            wanderDistance = minWanderDistance;
                        else
                            wanderDistance += wanderIncrement;
                        yield return Action.WarriorsExplore;
                        strategyIterator = 2;
                    }
                    else
                    {
                        // Yes - move to the good faction's position
                        yield return Action.WarriorsAttackPosition;
                    }
                }
                else if (strategyIterator == 4)
                {
                    strategyIterator = 3;
                    yield return Action.WarriorStop;
                }
            }
        }

        protected IEnumerable<Action> ForagerStrategy()
        {
            while (true)
            {
                if (foragerIterator == 0)
                {
                    if (hiveAI.CurrentTask.Type == RTSBuildingAI.Task.Types.Stop)
                    {
                        foragerIterator = 1;
                        yield return Action.CreateForager;
                    }
                    else
                    {
                        yield return Action.Null;
                    }
                }
                else if (foragerIterator == 1)
                {
                    if (builderAI != null)
                    {
                        if (builderAI.CurrentTask.Type == AntUnitAI.Task.Types.Stop)
                        {
                            foragerIterator = 2;
                            yield return Action.BuildDepot;
                        }
                        else
                            yield return Action.Null;
                    } 
                    else
                        yield return Action.Null;
                }
                else if (foragerIterator == 2)
                {

                    if ((foragerAI != null) && (foragerAI.CurrentTask.Type == AntUnitAI.Task.Types.Stop))
                    {
                        yield return Action.StartCollecting;
                    } else
                        yield return Action.Null;
                }
                /*else if (strategyIterator == 1)
                {
                    if (builderAI != null)
                    {
                        strategyIterator = 2;
                        yield return Action.BuildBarracks;
                    }
                    else
                        yield return Action.Null;
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
                }*/ 
            }
        }
        public void PerformAction(double elapsedGameTime, LinkedList<Entity> mapChildren)
        {
            // Only perform actions at discrete intervals
            if ((int)elapsedGameTime % actionInterval == 0 && 
                elapsedGameTime >= lastActionTime + actionInterval)
            {
                lastActionTime = elapsedGameTime;
                // Idensitfy the bad faction's characters
                IdentifyCharacters(mapChildren);

                // Move the strategy on to the next action

                
                
                openingStrategy.MoveNext();
                currentAction = openingStrategy.Current;
                switch (currentAction)
                {
                    case Action.WarriorsExplore:
                        WarriorsExplore(mapChildren);
                        break;
                    case Action.CreateBuilder:
                        CreateBuilder();
                        break;
                    case Action.BuildBarracks:
                        BuildBarracks();
                        break;
                    case Action.CreateWarrior:
                        CreateWarrior();
                        break;
                    case Action.WarriorsAttackPosition:
                        WarriorsAttackPosition(mapChildren);
                        break;
                    case Action.WarriorStop:
                        WarriorStop(mapChildren);
                        break;
                    case Action.Null:
                        break;
                }
                foragerStrategy.MoveNext();
                currentAction = foragerStrategy.Current;

                switch (currentAction)
                {
                    case Action.CreateForager:
                        CreateForager();
                        break;
                    case Action.BuildDepot:
                        BuildDepot();
                        break;
                    case Action.StartCollecting:
                        StartCollecting();
                        break;
                }
            }
        }

        // Identify the bad faction's entities
        private void IdentifyCharacters(LinkedList<Entity> mapChildren)
        {

            builderAI = null;
            builder = null;
            hiveAI = null;
            hive = null;
            barrackAI = null;
            barrack = null;
            depotAI = null;
            depot = null;

            // For each map entity            
            foreach (Entity entity in mapChildren)
            {
                // Is this a GenericAntCharacter entity
                //GenericAntCharacter unit = entity as GenericAntCharacter;
                //RTSUnit rtsunit = entity as RTSUnit;
                if (entity.Type.Name == "ForagerAnt")
                {
                    ForagerAnt unit = entity as ForagerAnt;

                    if (unit != null)
                    {
                        if (unit.Intellect != null)
                        {
                            if (unit.Intellect.Faction == badFaction)
                            {
                               if (foragerAI == null)
                                {
                                    AntUnitAI intellect = unit.Intellect as AntUnitAI;
                                    if (intellect != null)
                                    {
                                        foragerAI = intellect;
                                        forager = unit;
                                    }
                                }
                            }
                        }
                    }

                }
                else if (entity.Type.Name == "BuilderAnt")
                {
                    GenericAntCharacter unit = entity as GenericAntCharacter;

                        if (unit != null)
                    {
                        if (unit.Intellect != null)
                        {
                            if (unit.Intellect.Faction == badFaction)
                            {
                                // Identify the builder ant if it has not already been found
                                if (builderAI == null)
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
                }
                /*else if (entity.Type.Name == "RTSDepot")
                {
                    RTSMine mine = entity as RTSMine;
                    if (mine != null)
                    {
                        if (mine.Intellect != null)
                        {
                            if (mine.Intellect.Faction == badFaction)
                            {
                                if (depotAI == null)
                                {
                                    RTSBuildingAI intellect = mine.Intellect as RTSBuildingAI;
                                    if (intellect != null)
                                    {
                                        // barrack found
                                        depotAI = intellect;
                                        depot = mine;
                                        Log.Warning("depot = (RTSMine)building");
                                    }
                                }
                            }
                        }
                    }
                   

                }*/ 
                else
                {
                    // Is this a RTSBuilding entity
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
        }
        // Create a forager ant
        private void CreateForager()
        {
            if (hiveAI != null)
            {
                hiveAI.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.ProductUnit,
                    (RTSUnitType)EntityTypes.Instance.GetByName("ForagerAnt")), false);
            }
        }
        private void StartCollecting()
        {
            ForagerAnt Controlled = forager;
            Vec3 controlledObjPos = Controlled.Position;
            float radius = Controlled./*Type*/ViewRadius*3;
            //int count = 0; 
            float minDistance = 0f;
            Map.Instance.GetObjects(new Sphere(controlledObjPos, radius),
            GameFilterGroups.MineFilterGroup, delegate(MapObject mapObject)
            {
                if (mapObject.Type.Name == "RTSMine")
                {
                    //Log.Warning("Found a mine");
                    RTSMine obj = (RTSMine)mapObject;
                    if (minDistance == 0)
                    {
                        Controlled.CurrentMine = obj;
                        minDistance = (controlledObjPos.ToVec2() - obj.Position.ToVec2()).LengthFast();
                    }
                    else if ((controlledObjPos.ToVec2() - obj.Position.ToVec2()).LengthFast() < minDistance)
                    {
                        Controlled.CurrentMine = obj;
                        minDistance = (controlledObjPos.ToVec2() - obj.Position.ToVec2()).LengthFast();
                    }

                    //controlledObj.Stop();
                    //break;
                }
                //Log.Warning("...");
            });
            //Log.Warning("StartCollecting");
            if (Controlled.CurrentMine != null)
            {
                //Log.Warning("depot != null");
                //Log.Warning("DoTask()");
                //foragerAI.CurrentTask.Entity = Controlled.CurrentMine;
                foragerAI.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Collect, Controlled.CurrentMine), false);

            }
             
        }

        // Create a builder ant
        private void CreateBuilder()
        {
            if (hiveAI != null)
            {
                hiveAI.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.ProductUnit,
                    (RTSUnitType)EntityTypes.Instance.GetByName("BuilderAnt")), false);
            }
        }

        //  Create a warrior ant
        private void CreateWarrior()
        {
            if (barrackAI != null)
            {
                barrackAI.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.ProductUnit,
                    (RTSUnitType)EntityTypes.Instance.GetByName("WarriorAnt")), false);
            }
        }
        // Build a depot
        private void BuildDepot()
        {
            if (builderAI != null)
            {
                taskTargetBuildingType = (RTSBuildingType)EntityTypes.Instance.GetByName("RTSDepot");

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
                    (RTSBuildingType)EntityTypes.Instance.GetByName("RTSDepot")), false);

                GameEngineApp.Instance.ControlManager.PlaySound(
                    "Sounds\\Feedback\\RTSBuildBuilding.ogg");
            }
        }

        // Build a barracks
        private void BuildBarracks()
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

        // Task all the warrior ants with exploring, looking for the good faction
        private void WarriorsExplore(LinkedList<Entity> mapChildren)
        {
            // Cycle through all the entities 
            foreach(Entity entity in mapChildren)
            {
                GenericAntCharacter unit = entity as GenericAntCharacter;
                if (unit == null)
                    continue;
                if (unit.Intellect == null)
                    continue;
                if (unit.Life == 0)
                    continue;
                if (unit.Intellect.Faction == badFaction)
                {
                    // Cause only the warrior ants to wander
                    if (unit.Type.Name == "WarriorAnt")
                    {
                        AntUnitAI intellect = unit.Intellect as AntUnitAI;
                        if (intellect == null)
                            continue;

                        // Has this warrior ant found an alive member of the good faction
                        if (intellect.CurrentTask.Type == AntUnitAI.Task.Types.BreakableAttack && 
                            intellect.CurrentTask.Entity != null && !goodFactionPositionFound)
                        {
                            // Yes - record where the good faction was seen
                            goodFactionPositionFound = true;
                            goodFactionPosition = intellect.CurrentTask.Entity.Position;
                        }
                                             
                        // No - wander with Radius, distance, jitter
                        intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Wander,
                            wanderRadius, wanderDistance, wanderJitter), false);
                    }
                }
            }
        }

        // Move all the warrior ants to where the good faction was seen
        private void WarriorsAttackPosition(LinkedList<Entity> mapChildren)
        {
            if (goodFactionPositionFound)
            {
                // Cycle through all the entities 
                foreach (Entity entity in mapChildren)
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

                            // Move to the location the good faction was seen
                            intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.BreakableAttack,
                                goodFactionPosition), false);
                        }
                    }
                }
                // Forget the good faction's position
                goodFactionPositionFound = false;
            }
        }

        // Cause all the warrior ants to stop
        private void WarriorStop(LinkedList<Entity> mapChildren)
        {
            // Cycle through all the entities 
            foreach (Entity entity in mapChildren)
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

                        // Stop
                        intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Stop), false);
                    }
                }
            }
        }

        // Is the location free for building
        private bool IsFreeForBuildTaskTargetBuild(Vec3 pos)
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
