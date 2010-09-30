using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;

namespace GameEntities.RTS_Specific
{
    public class AntUnitAIType : AIType
    {
    }
    public class AntUnitAI : AI
    {

        //protected abstract IEnumerable<Vec2> BehaveSeqGen();  // Implemented via "yield" in subclasses
        IEnumerator<Vec3> behaveSeq;

        protected Vec3 lastVec = Vec3.Zero;           // Stores the force vector from the last tick.
        protected bool failed = false;      // Set by behaviours that fail to achieve their purpose 

        //optimization
        //List<Weapon> initialWeapons;

        float inactiveFindTaskTimer;

        public GenericAntCharacter Controlled { get; set; }



        [FieldSerialize]
        Task currentTask = new Task(Task.Types.Stop);

        [FieldSerialize]
        List<Task> tasks = new List<Task>();

        ///////////////////////////////////////////

        public struct Task
        {
            [FieldSerialize]
            [DefaultValue(Types.None)]
            Types type;

            [FieldSerialize]
            [DefaultValue(typeof(Vec3), "0 0 0")]
            Vec3 position;

            [FieldSerialize]
            DynamicType entityType;

            [FieldSerialize]
            Dynamic entity;

            [FieldSerialize]
            float wanderRadius;

            [FieldSerialize]
            float wanderDistance;

            [FieldSerialize]
            float wanderJitter;

            public enum Types
            {
                None,
                Stop,
                BreakableAttack,//for automatic attacks
                Hold,
                Collect,
                TrailMove,
                Move,
                BreakableMove,//for automatic attacks
                Wander,
                Attack,
                Repair,
                BreakableRepair,//for automatic repair
                BuildBuilding,
                ProductUnit,
                SelfDestroy,//for cancel build building               
            }

            public Task(Types type)
            {
                this.type = type;
                this.position = new Vec3(float.NaN, float.NaN, float.NaN);
                this.entityType = null;
                this.entity = null;
                this.wanderRadius = float.NaN;
                this.wanderDistance = float.NaN;
                this.wanderJitter = float.NaN;
            }

            public Task(Types type, Vec3 position)
            {
                this.type = type;
                this.position = position;
                this.entityType = null;
                this.entity = null;
                this.wanderRadius = float.NaN;
                this.wanderDistance = float.NaN;
                this.wanderJitter = float.NaN;
            }

            public Task(Types type, DynamicType entityType)
            {
                this.type = type;
                this.position = new Vec3(float.NaN, float.NaN, float.NaN);
                this.entityType = entityType;
                this.entity = null;
                this.wanderRadius = float.NaN;
                this.wanderDistance = float.NaN;
                this.wanderJitter = float.NaN;
            }

            public Task(Types type, Vec3 position, DynamicType entityType)
            {
                this.type = type;
                this.position = position;
                this.entityType = entityType;
                this.entity = null;
                this.wanderRadius = float.NaN;
                this.wanderDistance = float.NaN;
                this.wanderJitter = float.NaN;
            }

            public Task(Types type, Dynamic entity)
            {
                this.type = type;
                this.position = new Vec3(float.NaN, float.NaN, float.NaN);
                this.entityType = null;
                this.entity = entity;
                this.wanderRadius = float.NaN;
                this.wanderDistance = float.NaN;
                this.wanderJitter = float.NaN;
            }

            public Task(Types type, float wanderRadius, float wanderDistance, float wanderJitter)
            {
                this.type = type;
                this.position = new Vec3(float.NaN, float.NaN, float.NaN);
                this.entityType = null;
                this.entity = null;
                this.wanderRadius = wanderRadius;
                this.wanderDistance = wanderDistance;
                this.wanderJitter = wanderJitter;
            }


            public Types Type
            {
                get { return type; }
            }

            public Vec3 Position
            {
                get { return position; }
            }

            public DynamicType EntityType
            {
                get { return entityType; }
            }

            public Dynamic Entity
            {
                get { return entity; }
            }

            public float WanderRadius
            {
                get { return wanderRadius; }
            }

            public float WanderDistance
            {
                get { return wanderDistance; }
            }

            public float WanderJitter
            {
                get { return wanderJitter; }
            }

            public override string ToString()
            {
                string s = type.ToString();
                if (!float.IsNaN(position.X))
                    s += ", Position: " + position.ToString();
                if (entityType != null)
                    s += ", EntityType: " + entityType.Name;
                if (entity != null)
                    s += ", Entity: " + entity.ToString();
                return s;
            }
        }

        ///////////////////////////////////////////

        public struct UserControlPanelTask
        {
            Task task;
            bool active;
            bool enable;

            public UserControlPanelTask(Task task)
            {
                this.task = task;
                this.active = true;
                this.enable = true;
            }

            public UserControlPanelTask(Task task, bool active)
            {
                this.task = task;
                this.active = active;
                this.enable = true;
            }

            public UserControlPanelTask(Task task, bool active, bool enable)
            {
                this.task = task;
                this.active = active;
                this.enable = enable;
            }

            public Task Task
            {
                get { return task; }
            }

            public bool Active
            {
                get { return active; }
            }

            public bool Enable
            {
                get { return enable; }
            }

        }

        ///////////////////////////////////////////

        AntUnitAIType _type = null; public new AntUnitAIType Type { get { return _type; } }

        public AntUnitAI()
        {
            inactiveFindTaskTimer = World.Instance.Random.NextFloat() * 2;
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);
            AddTimer();
            if (base.ControlledObject.Type.Name == "GenericAntCharacter")
                Controlled = (GenericAntCharacter)base.ControlledObject;
            behaveSeq = BehaveSeqGen().GetEnumerator();
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
        protected override void OnDestroy()
        {
            ClearTaskList();
            base.OnDestroy();
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
        protected override void OnRelatedEntityDelete(Entity entity)
        {
            base.OnRelatedEntityDelete(entity);

            for (int n = 0; n < tasks.Count; n++)
            {
                if (tasks[n].Entity == entity)
                {
                    tasks.RemoveAt(n);
                    n--;
                }
            }

            if (currentTask.Entity == entity)
                DoNextTask();
        }

        //protected float GetMoveObjectPriority( Unit obj )
        //{
        //	return 0;
        //}

        protected float GetAttackObjectPriority(Unit obj)
        {
            if (ControlledObject == obj)
                return 0;

            if (obj.Intellect == null)
                return 0;

            //RTSConstructor specific
            if (ControlledObject.Type.Name == "RTSConstructor" || ControlledObject.Type.Name == "BuilderAnt")
            {
                if (Faction == obj.Intellect.Faction)
                {
                    if (obj.Life < obj.Type.LifeMax)
                    {
                        Vec3 distance = obj.Position - ControlledObject.Position;
                        float len = distance.LengthFast();
                        return 1.0f / len + 1.0f;
                    }
                }
            }
            else
            {
                if (Faction != null && obj.Intellect.Faction != null && Faction != obj.Intellect.Faction)
                {
                    Vec3 distance = obj.Position - ControlledObject.Position;
                    float len = distance.LengthFast();
                    return 1.0f / len + 1.0f;
                }
            }

            return 0;
        }

		bool InactiveFindTask()
		{			
            //if( initialWeapons.Count == 0 )
			//	return false;
			RTSUnit controlledObj = ControlledObject;
			if( controlledObj == null )
				return false;

            Vec3 controlledObjPos = controlledObj.Position;
            float radius = controlledObj./*Type.*/ViewRadius;

            // Look for a health item with the ant's view radius
            Item healthItem = null;
            Map.Instance.GetObjects(new Sphere(controlledObjPos, radius),
            GameFilterGroups.HealthFilterGroup, delegate(MapObject mapObject)
            {
                HealthItem obj = (HealthItem)mapObject;
                // TODO: update the health items for ants
                if (obj.Type.Name == "SmallHealthItem")
                {
                    healthItem = obj;
                }
            });

            // Has a health item been seen?
            if (healthItem != null) 
            {
                //  Is the ant hurt
                Unit unit = controlledObj as Unit;
                if (controlledObj.Life < unit.Type.LifeMax)
                {              
                    // Yes 
                    float distanceToHealth = (controlledObj.Position - healthItem.Position).LengthFast();
                    if (distanceToHealth > 2.0f)
                    {
                        // Go to the health iteam
                        DoTask(new Task(Task.Types.BreakableMove, healthItem), false);
                    }
                    else
                    {
                        // When at the health item take it
                        healthItem.Take(unit);
                    }
                    return true;
                }
            }

            // Attack nearby enemies
			Dynamic newTaskAttack = null;
			float attackObjectPriority = 0;
			Map.Instance.GetObjects( new Sphere( controlledObjPos, radius ),
				GameFilterGroups.UnitFilterGroup, delegate( MapObject mapObject )
			{
				Unit obj = (Unit)mapObject;
				Vec3 objPos = obj.Position;

				//check distance
				Vec3 diff = objPos - controlledObjPos;
				float objDistance = diff.LengthFast();
				if( objDistance > radius )
					return;

				float priority = GetAttackObjectPriority( obj );
				if( priority != 0 && priority > attackObjectPriority )
				{
					attackObjectPriority = priority;
					newTaskAttack = obj;
				}
			} );

            if (newTaskAttack != null)
            {
                //RTSConstructor specific
                if (ControlledObject.Type.Name == "RTSConstructor" || ControlledObject.Type.Name == "BuilderAnt")
                    DoTask(new Task(Task.Types.BreakableRepair, newTaskAttack), false);
                else
                    DoTask(new Task(Task.Types.BreakableAttack, newTaskAttack), false);
                return true;
            }
			return false;
		}


        [Browsable(false)]
        public new RTSUnit ControlledObject
        {
            //!!!!slowly
            get { return (RTSUnit)base.ControlledObject; }
        }

        void UpdateInitialWeapons()
        {
            RTSUnit controlledObj = ControlledObject;

            //initialWeapons = new List<Weapon>();

            foreach (MapObjectAttachedObject attachedObject in controlledObj.AttachedObjects)
            {
                MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
                if (attachedMapObject != null)
                {
                    Weapon weapon = attachedMapObject.MapObject as Weapon;
                    if (weapon != null)
                    {
                        //initialWeapons.Add( weapon );
                    }
                }
            }
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
        protected override void OnTick()
        {
            base.OnTick();

            //if( initialWeapons == null )
            //	UpdateInitialWeapons();

            TickTasks();

            if ((currentTask.Type == Task.Types.Stop ||
                currentTask.Type == Task.Types.BreakableMove ||
                currentTask.Type == Task.Types.BreakableAttack ||
                currentTask.Type == Task.Types.BreakableRepair ||
                currentTask.Type == Task.Types.Wander 
                ) && tasks.Count == 0)
            {
                inactiveFindTaskTimer -= TickDelta;
                if (inactiveFindTaskTimer <= 0)
                {
                    inactiveFindTaskTimer += 1.0f;
                    if (!InactiveFindTask())
                        inactiveFindTaskTimer += .5f;
                }
            }

        }

        protected IEnumerable<Vec3> BehaveSeqGen() // Adds 
        {
            //while (Target == null) yield return Vec2.Zero;


            while (true)
            {
                {
                    if (Controlled.Resources < Controlled.Type.ResourcesMax)
                    {
                        Controlled.Resources += 8;
                        //Log.Warning("Collected 10 units of resources");
                        yield return Vec3.Zero;
                    }
                    else if (Controlled.Resources >= Controlled.Type.ResourcesMax)
                    {
                        //Log.Warning("Success: Reached capacity");
                        // Vec3 depo_pos = new Vec3(Controlled.Depot.Position.X,Controlled.Depot.Position.Y,Controlled.Depot.Position.Z);
                        yield return new Vec3(337.80f, 337.70f, 0.01f);
                        // ToVec2();
                        //new Vec2(Controlled.Depot.X,Controlled.Depot.Y);
                    }
                    else
                    {
                        Log.Warning("Error");
                    }
                }
            }


        }

        protected virtual void TickTasks()
        {
            RTSUnit controlledObj = ControlledObject;
            if (controlledObj == null)
                return;

            switch (currentTask.Type)
            {

                //Stop
                case Task.Types.Stop:
                    controlledObj.Stop();
                    break;


                //Collect
                case Task.Types.Collect:
                    if (currentTask.Entity != null)
                    {
                        if (Controlled.Resources == Controlled.Type.ResourcesMax)
                        {
                            if (Controlled.Depot != null)
                            {
                                controlledObj.Move(Controlled.Depot.Position);
                            }
                            else
                            {
                                controlledObj.Move(new Vec3(337.80f, 337.70f, 0.01f));
                            }
                        }
                        else
                        {
                            Vec3 pos = currentTask.Entity.Position;

                            if ((controlledObj.Position.ToVec2() - currentTask.Entity.Position.ToVec2()).LengthFast() < 11f)
                            {
                                behaveSeq.MoveNext();
                            }
                            else
                            {
                                controlledObj.Move(pos);
                            }
                        }


                    }
                    else
                    {
                        Log.Warning("uhoh");
                        //controlledObj.Move(currentTask.Position);
                    }
                    break;


                //Move
                case Task.Types.TrailMove:
                    if (currentTask.Entity != null)
                    {
                        // Move to within a certain distance of the entity we're moving to
                        if ((controlledObj.Position.ToVec2() - currentTask.Entity.Position.ToVec2()).LengthFast() > 10f)
                            controlledObj.Move(currentTask.Entity.Position);
                        else
                            // but no closer
                            controlledObj.Stop();
                    }
                    else
                        controlledObj.Move(currentTask.Position);
                    break;
                case Task.Types.Move:
                case Task.Types.BreakableMove:
                    if (currentTask.Entity != null)
                    {
                        controlledObj.Move(currentTask.Entity.Position);
                    }
                    else
                    {
                        Vec3 pos = currentTask.Position;

                        if ((controlledObj.Position.ToVec2() - pos.ToVec2()).LengthFast() < 1.5f &&
                            Math.Abs(controlledObj.Position.Z - pos.Z) < 3.0f)
                        {
                            //get to
                            DoNextTask();
                        }
                        else
                            controlledObj.Move(pos);
                    }
                    break;

                // Wander
                case Task.Types.Wander:
                    Random rand = new Random();

                    Vec3 wanderTargetPos = controlledObj.Position;

                    //first, add a small random vector to the target's position
                    wanderTargetPos +=
                        Vec3.Construct((float)(rand.NextDouble() * 2f - 1f) * currentTask.WanderJitter, ((float)rand.NextDouble() * 2f - 1f) * currentTask.WanderJitter, 0f);

                    //reproject this new vector back on to the circle
                    wanderTargetPos = Vec3.Normalize(wanderTargetPos) * currentTask.WanderRadius;

                    //move the target into a position WanderDist in front of the agent
                    Vec3 moveTargetPos = wanderTargetPos + new Vec3(currentTask.WanderDistance, 0f, 0f);

                    //convert the target coordinates into world space instead of relative to the owner.
                    Vec3 targetWorldPos = (controlledObj.GetTransform() * new Vec4(moveTargetPos, 0f)).ToVec3();

                    //and steer towards it
                    controlledObj.Move(targetWorldPos);
                    break;
                //Attack, Repair
                case Task.Types.Attack:
                case Task.Types.BreakableAttack:
                //case Task.Types.Repair:
                //case Task.Types.BreakableRepair:
                    {
                        //healed
                        if ((currentTask.Type == Task.Types.Repair ||
                            currentTask.Type == Task.Types.BreakableRepair)
                            && currentTask.Entity != null)
                        {
                            if (currentTask.Entity.Life == currentTask.Entity.Type.LifeMax)
                            {
                                DoNextTask();
                                break;
                            }
                        }

                        //float needDistance = controlledObj.Type.OptimalAttackDistanceRange.Maximum;
                        float maxAttackDistance = controlledObj.Type.OptimalAttackDistanceRange.Maximum;

                        // Find the position of the target entity
                        Vec3 targetPos;
                        if (currentTask.Entity != null)
                            targetPos = currentTask.Entity.Position;
                        else
                            targetPos = currentTask.Position;

                        float distance = (controlledObj.Position - targetPos).LengthFast();

                        // Is this ant within attack range
                        if (distance <= maxAttackDistance)
                        {
                            //Yes, stop
                            controlledObj.Stop();

                            // Turn to face victim
                            GenericAntCharacter character = controlledObj as GenericAntCharacter;
                            if (character != null)
                                character.SetLookDirection(targetPos);


                            if (currentTask.Type == Task.Types.Attack ||
                                currentTask.Type == Task.Types.BreakableAttack)
                            {

                                Ray ray = new Ray(controlledObj.Position,
                                    controlledObj.Position - currentTask.Entity.Position);

                                RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
                                        ray, (int)ContactGroup.CastOnlyContact);

                                foreach (RayCastResult result in piercingResult)
                                {
                                    MapObject obj = MapSystemWorld.GetMapObjectByBody(result.Shape.Body);

                                    if (obj != null)
                                    {
                                        float impulse = 10.0f;
                                        if (impulse != 0 && obj.PhysicsModel != null)
                                        {
                                            result.Shape.Body.AddForce(ForceType.GlobalAtGlobalPos, 0,
                                                currentTask.Entity.Rotation.GetForward() * impulse,
                                                currentTask.Entity.Position);
                                        }

                                        Dynamic dynamic = currentTask.Entity as Dynamic;
                                        
                                        if (dynamic != null)
                                        {
                                            // TODO: Calculate damage based on Ant type
                                            float damage = 1.0f;
                                            if (damage != 0)
                                            {
                                                dynamic.DoDamage(controlledObj, currentTask.Entity.Position,
                                                    result.Shape, damage, true);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // No - move to target
                            controlledObj.Move(targetPos);
                        }


                        /*
                        if( distance != 0 )
                        {
                            bool lineVisibility = false;
                            {
                                if( distance < needDistance )
                                {
                                    lineVisibility = true;

                                    //direct line visibility check 

                                    //!!!!!!slowly. it is necessary to check less often
                                    Vec3 start = initialWeapons[ 0 ].Position;
                                    Ray ray = new Ray( start, targetPos - start );

                                    RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
                                        ray, (int)ContactGroup.CastOnlyContact );

                                    foreach( RayCastResult result in piercingResult )
                                    {
                                        MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

                                        if( obj != null && obj == currentTask.Entity )
                                            break;

                                        if( obj != controlledObj )
                                        {
                                            lineVisibility = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            //movement control 
                            if( lineVisibility )
                            {
                                //stop
                                controlledObj.Stop();

                                //RTSCharacter character = controlledObj as RTSCharacter;
                                GenericAntCharacter character = controlledObj as GenericAntCharacter;
                                if( character != null )
                                    character.SetLookDirection( targetPos );
                            }
                            else
                            {
                                //move to target
                                controlledObj.Move( targetPos );
                            }

                            //weapons control
                            if( lineVisibility )
                            {
                                foreach( Weapon weapon in initialWeapons )
                                {
                                    Vec3 pos = targetPos;
                                    Gun gun = weapon as Gun;
                                    if( gun != null && currentTask.Entity != null )
                                        gun.GetAdvanceAttackTargetPosition( false, currentTask.Entity, false, out pos );
                                    weapon.SetForceFireRotationLookTo( pos );

                                    if( weapon.Ready )
                                    {
                                        Range range;

                                        range = weapon.Type.WeaponNormalMode.UseDistanceRange;
                                        if( distance >= range.Minimum && distance <= range.Maximum )
                                            weapon.TryFire( false );

                                        range = weapon.Type.WeaponAlternativeMode.UseDistanceRange;
                                        if( distance >= range.Minimum && distance <= range.Maximum )
                                            weapon.TryFire( true );
                                    }
                                }
                            }
                        }*/

                    }
                    break;

                case Task.Types.Repair:
                case Task.Types.BreakableRepair:
                    {
                        if ( ( currentTask.Entity != null ) && ( currentTask.Entity.Life == currentTask.Entity.Type.LifeMax ) )
                        {
                            DoNextTask();
                            break;
                        }

                        float maxAttackDistance = controlledObj.Type.OptimalAttackDistanceRange.Maximum;

                        // Find the position of the target entity
                        Vec3 targetPos;
                        if (currentTask.Entity != null)
                            targetPos = currentTask.Entity.Position;
                        else
                            targetPos = currentTask.Position;

                        float distance = (controlledObj.Position - targetPos).LengthFast();

                        // Is this ant within attack range
                        if (distance <= maxAttackDistance)
                        {
                            //Yes, stop
                            controlledObj.Stop();

                            // Turn to face victim
                            GenericAntCharacter character = controlledObj as GenericAntCharacter;
                            if (character != null)
                                character.SetLookDirection(targetPos);

                            currentTask.Entity.Life += 1;
                        }
                        else
                        {
                            // No - move to target
                            controlledObj.Move(targetPos);
                        }
 
                    }
                    break;

                //BuildBuilding
                case Task.Types.BuildBuilding:
                    {
                        float needDistance = controlledObj.Type.OptimalAttackDistanceRange.Maximum;

                        Vec3 targetPos = currentTask.Position;

                        float distance = (controlledObj.Position - targetPos).LengthFast();

                        if (distance < needDistance)
                        {
                            controlledObj.Stop();

                            //get to

                            //check free area for build
                            //bool free;
                            //{
                            //    Bounds bounds;
                            //    {
                            //        PhysicsModel physicsModel = PhysicsWorld.Instance.LoadPhysicsModel(
                            //            currentTask.EntityType.PhysicsModel);
                            //        if (physicsModel == null)
                            //            Log.Fatal(string.Format("No physics model for \"{0}\"",
                            //                currentTask.EntityType.ToString()));
                            //        bounds = physicsModel.GetGlobalBounds();

                            //        bounds += targetPos;
                            //    }

                            //    Rect rect = new Rect(bounds.Minimum.ToVec2(), bounds.Maximum.ToVec2());
                            //    free = GridPathFindSystem.Instance.IsFreeInMapMotion(rect);
                            //}

                            //if (!free)
                            //{
                                //not free
                            //    DoNextTask();
                            //    break;
                            //}

                            //check cost
                            RTSFactionManager.FactionItem factionItem = RTSFactionManager.Instance.GetFactionItemByType(Faction);
                            if (factionItem != null)
                            {
                                float cost = ((RTSBuildingType)currentTask.EntityType).BuildCost;

                                if ((factionItem.Money - cost) < 0)
                                {
                                    //No money
                                    DoNextTask();
                                    break;
                                }

                                factionItem.Money -= cost;
                            }


                            RTSBuilding building = (RTSBuilding)Entities.Instance.Create(currentTask.EntityType, Map.Instance);
                            building.Position = currentTask.Position;

                            building.InitialFaction = Faction;

                            building.PostCreate();
                            building.BuildedProgress = 1;
                            building.Life = 100;

                            //Repair
                            DoTaskInternal(new Task(Task.Types.Repair, building));
                        }
                        else
                            controlledObj.Move(targetPos);
                    }
                    break;

            }
        }

        void ClearTaskList()
        {
            foreach (Task task in tasks)
                if (task.Entity != null)
                    RemoveRelationship(task.Entity);
            tasks.Clear();
        }

        protected virtual void DoTaskInternal(Task task)
        {
            if (currentTask.Entity != null)
                RemoveRelationship(currentTask.Entity);

            currentTask = task;

            if (currentTask.Entity != null)
                AddRelationship(currentTask.Entity);

            //Stop
            if (task.Type == Task.Types.Stop)
            {
                if (ControlledObject != null)
                    ControlledObject.Stop();
            }

            //SelfDestroy
            if (task.Type == Task.Types.SelfDestroy)
            {
                ControlledObject.Die();
            }
        }

        public void DoTask(Task task, bool toQueue)
        {
            if (toQueue && currentTask.Type == Task.Types.Stop && tasks.Count == 0)
                toQueue = false;

            if (!toQueue)
            {
                ClearTaskList();
                DoTaskInternal(task);
            }
            else
            {
                if (task.Entity != null)
                    AddRelationship(task.Entity);
                tasks.Add(task);
            }
        }

        protected void DoNextTask()
        {
            if (currentTask.Entity != null)
                RemoveRelationship(currentTask.Entity);

            if (tasks.Count != 0)
            {
                Task task = tasks[0];
                tasks.RemoveAt(0);
                if (task.Entity != null)
                    RemoveRelationship(task.Entity);

                DoTaskInternal(task);
            }
            else
            {
                DoTask(new Task(Task.Types.Stop), false);
            }
        }

        public Task CurrentTask
        {
            get { return currentTask; }
        }

        public virtual List<UserControlPanelTask> GetControlPanelTasks()
        {
            List<UserControlPanelTask> list = new List<UserControlPanelTask>();

            // Adds these task to the control panel by default
            list.Add(new UserControlPanelTask(new Task(Task.Types.Stop), currentTask.Type == Task.Types.Stop));
            list.Add(new UserControlPanelTask(new Task(Task.Types.Move),
                currentTask.Type == Task.Types.Move || currentTask.Type == Task.Types.BreakableMove));
            list.Add(new UserControlPanelTask(new Task(Task.Types.Collect), currentTask.Type == Task.Types.Collect));
            list.Add(new UserControlPanelTask(new Task(Task.Types.TrailMove), currentTask.Type == Task.Types.TrailMove));


            //RTSConstructor specific
            if (ControlledObject.Type.Name == "RTSConstructor" || ControlledObject.Type.Name == "BuilderAnt")
            {
                list.Add(new UserControlPanelTask(new Task(Task.Types.Repair),
                    currentTask.Type == Task.Types.Repair || currentTask.Type == Task.Types.BreakableRepair));

                RTSBuildingType buildingType;

                // Adds these task to the control panel if the entity selected is an RTSConstructor
                //buildingType = (RTSBuildingType)EntityTypes.Instance.GetByName("RTSHeadquaters");
                //list.Add(new UserControlPanelTask(new Task(Task.Types.BuildBuilding, buildingType),
                    //CurrentTask.Type == Task.Types.BuildBuilding && CurrentTask.EntityType == buildingType));

                //buildingType = (RTSBuildingType)EntityTypes.Instance.GetByName("RTSMine");
                //list.Add(new UserControlPanelTask(new Task(Task.Types.BuildBuilding, buildingType),
                    //CurrentTask.Type == Task.Types.BuildBuilding && CurrentTask.EntityType == buildingType));

                //buildingType = (RTSBuildingType)EntityTypes.Instance.GetByName("RTSFactory");
                //list.Add(new UserControlPanelTask(new Task(Task.Types.BuildBuilding, buildingType),
                    //CurrentTask.Type == Task.Types.BuildBuilding && CurrentTask.EntityType == buildingType));

                //buildingType = (RTSBuildingType)EntityTypes.Instance.GetByName("AntStorage");
                //list.Add(new UserControlPanelTask(new Task(Task.Types.BuildBuilding, buildingType),
                    //CurrentTask.Type == Task.Types.BuildBuilding && CurrentTask.EntityType == buildingType));

                buildingType = (RTSBuildingType)EntityTypes.Instance.GetByName("AntBarrack");
                list.Add(new UserControlPanelTask(new Task(Task.Types.BuildBuilding, buildingType),
                    CurrentTask.Type == Task.Types.BuildBuilding && CurrentTask.EntityType == buildingType));
            }
            else if (ControlledObject.Type.Name == "WarriorAnt")
            {
                list.Add(new UserControlPanelTask(new Task(Task.Types.Attack),
                    currentTask.Type == Task.Types.Attack || currentTask.Type == Task.Types.BreakableAttack));
            }
            else if (ControlledObject.Type.Name == "AntColmena")
            {
                list.Add(new UserControlPanelTask(new Task(Task.Types.Attack),
                currentTask.Type == Task.Types.Attack || currentTask.Type == Task.Types.BreakableAttack));
            }


            return list;
        }
    }
}
