using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.Renderer;

namespace GameEntities.RTS_Specific
{
    public class GenericAntCharacterType : RTSUnitType
    {
        const float heightDefault = 3.0f;
        [FieldSerialize]
        float height = heightDefault;

        const float antRadiusDefault = 3.0f;
        [FieldSerialize]
        float antRadius = antRadiusDefault;

        const float radiusDefault = 0.9f;
        [FieldSerialize]
        float radius = radiusDefault;

        [DefaultValue(heightDefault)]
        public float Height
        {
            get { return height; }
            set { height = value; }
        }

        [DefaultValue(antRadiusDefault)]
        public float AntRadius
        {
            get { return antRadius; }
            set { antRadius = value; }
        }

        [DefaultValue(radiusDefault)]
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        } 

        // Max velocity per second
        const float maxVelocityDefault = 5;
        [FieldSerialize]
        float maxVelocity = maxVelocityDefault;

        [FieldSerialize]
        [DefaultValue("idle")]
        string idleAnimationName = "idle";

        [FieldSerialize]
        [DefaultValue("walk")]
        string walkAnimationName = "walk";

        [FieldSerialize]
        [DefaultValue(1.0f)]
        float walkAnimationVelocityMultiplier = 1;

        [DefaultValue(maxVelocityDefault)]
        public float MaxVelocity
        {
            get { return maxVelocity; }
            set { maxVelocity = value; }
        }

        [DefaultValue("idle")]
        public string IdleAnimationName
        {
            get { return idleAnimationName; }
            set { idleAnimationName = value; }
        }

        [DefaultValue("walk")]
        public string WalkAnimationName
        {
            get { return walkAnimationName; }
            set { walkAnimationName = value; }
        }

        [DefaultValue(1.0f)]
        public float WalkAnimationVelocityMultiplier
        {
            get { return walkAnimationVelocityMultiplier; }
            set { walkAnimationVelocityMultiplier = value; }
        }
    }


    public class GenericAntCharacter : RTSUnit
    {        
        Body mainBody;

        [FieldSerialize(FieldSerializeSerializationTypes.World)]
        Vec2 pathFoundedToPosition = new Vec2(float.NaN, float.NaN);

        [FieldSerialize(FieldSerializeSerializationTypes.World)]
        // A list of 2d coordinates describing a path 
        List<Vec2> path = new List<Vec2>();

        float pathFindWaitTime;

        Vec3 oldMainBodyPosition;
        Vec3 mainBodyVelocity;

        GenericAntCharacterType _type = null; public new GenericAntCharacterType Type { get { return _type; } }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
        /// Called immediately after this object is created
        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);

            pathFindWaitTime = World.Instance.Random.NextFloat();

            CreatePhysicsModel();
            Body body = PhysicsModel.CreateBody();
            mainBody = body;
            body.Name = "main";
            body.Static = true;
            body.Position = Position;
            body.Rotation = Rotation;
            /*float AntLength = Type.Height - Type.AntRadius * 2;
            if (AntLength < 0)
            {
                Log.Error("Error: Length < 0");
                return;
            }*/
            CapsuleShape shape = body.CreateCapsuleShape();
            //CylinderShape shape = body.CreateCylinderShape();
            //BoxShape shape = body.CreateBoxShape();
            shape.Length = Type.Height;
            shape.Radius = Type.AntRadius;
            shape.ContactGroup = (int)ContactGroup.Dynamic;

            //!!!!!need?
            //for update in GridPathFindSystem
            Vec3 p = Position;
            Position = p;

            PhysicsModel.PushToWorld();

            if (mainBody != null)
                oldMainBodyPosition = mainBody.Position;

            // Notificiation of OnTick() events
            AddTimer();
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
        protected override void OnTick()
        {
            base.OnTick();

            if (MoveEnabled)
                TickMove();
            else
                path.Clear();

            mainBodyVelocity = (mainBody.Position - oldMainBodyPosition) *
                EntitySystemWorld.Instance.GameFPS;
            oldMainBodyPosition = mainBody.Position;
        }

        public void SetLookDirection(Vec3 pos)
        {
            Vec2 diff = pos.ToVec2() - Position.ToVec2();

            if (diff == Vec2.Zero)
                return;

            Radian dir = MathFunctions.ATan16(diff.Y, diff.X);

            float halfAngle = dir * 0.5f;
            Quat rot = new Quat(new Vec3(0, 0, MathFunctions.Sin16(halfAngle)), MathFunctions.Cos16(halfAngle));
            Rotation = rot;
        }

        bool DoPathFind()
        {
            Dynamic targetObj = null;
            {
                //not true because use Intellect
                AntUnitAI ai = Intellect as AntUnitAI;
                if (ai != null)
                    targetObj = ai.CurrentTask.Entity;
            }

            GridPathFindSystem.Instance.RemoveObjectFromMotionMap(this);

            Bounds bounds = PhysicsModel.GetGlobalBounds();

            float radius = Type.Radius;
            Rect targetRect = new Rect(MovePosition.ToVec2() - new Vec2(radius, radius),
                MovePosition.ToVec2() + new Vec2(radius, radius));

            if (targetObj != null && targetObj != this)
                GridPathFindSystem.Instance.RemoveObjectFromMotionMap(targetObj);

            GridPathFindSystem.Instance.DoTempClearMotionMap(targetRect);

            const int maxFieldsDistance = 1000;
            const int maxFieldsToCheck = 100000;
            // Find a path to the specified position
            bool found = GridPathFindSystem.Instance.DoFind(Type.Radius * 2 * 1.1f, Position.ToVec2(),
                MovePosition.ToVec2(), maxFieldsDistance, maxFieldsToCheck, true, false, path);

            GridPathFindSystem.Instance.RestoreAllTempClearedMotionMap();

            if (targetObj != null && targetObj != this)
                GridPathFindSystem.Instance.AddObjectToMotionMap(targetObj);

            GridPathFindSystem.Instance.AddObjectToMotionMap(this);

            return found;
        }

        /// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnRender(Camera)"/>.</summary>
        protected override void OnRender(Camera camera)
        {
            base.OnRender(camera);

            if (path.Count != 0 && GridPathFindSystem.Instance.DebugDraw)
            {
                GridPathFindSystem.Instance.DebugDrawPath(camera,
                    Position.ToVec2(), path, new ColorValue(0, 0, 1));
            }
        }

        void TickMove()
        {

            //path find control
            {
                if (pathFindWaitTime != 0)
                {
                    pathFindWaitTime -= TickDelta;
                    if (pathFindWaitTime < 0)
                        pathFindWaitTime = 0;
                }

                // If a path has not been found to the target and the time to wait is zero
                if (pathFoundedToPosition != MovePosition.ToVec2() && pathFindWaitTime == 0)
                    path.Clear();

                // If we don't have a path
                if (path.Count == 0)
                {
                    if (pathFindWaitTime == 0)
                    {
                        // Find a path to move position
                        if (DoPathFind())
                        {
                            pathFoundedToPosition = MovePosition.ToVec2();
                            pathFindWaitTime = .5f;
                        }
                        else
                        {
                            // If a path has not been found, re-try in 1 second
                            pathFindWaitTime = 1.0f;
                        }
                    }
                }
            }

            if (path.Count == 0)
                return;

            //line movement to path[ 0 ]
            {
                Vec2 destPoint = path[0];
                // The difference between the first point in the path and this object's current position
                Vec2 diff = destPoint - Position.ToVec2();

                if (diff == Vec2.Zero)
                {
                    path.RemoveAt(0);
                    return;
                }

                Radian dir = MathFunctions.ATan16(diff.Y, diff.X);

                float halfAngle = dir * 0.5f;
                Quat rot = new Quat(new Vec3(0, 0, MathFunctions.Sin16(halfAngle)),
                    MathFunctions.Cos16(halfAngle));
                // Rotate the object
                Rotation = rot;
                // Normalise the difference vector
                Vec2 dirVector = diff.GetNormalizeFast();
                // The difference vector multiplied by the velocity per 1/30 second
                Vec2 dirStep = dirVector * (Type.MaxVelocity * TickDelta);
                // Calculate the object's new position
                Vec2 newPos = Position.ToVec2() + dirStep;
                // If the first point in the path is less than the direction step
                if (Math.Abs(diff.X) <= Math.Abs(dirStep.X) && Math.Abs(diff.Y) <= Math.Abs(dirStep.Y))
                {
                    // then move to the first position in the path
                    //unit at point
                    newPos = path[0];
                    path.RemoveAt(0);
                }

                GridPathFindSystem.Instance.RemoveObjectFromMotionMap(this);

                bool free;
                {
                    float radius = Type.Radius;
                    Rect targetRect = new Rect(newPos - new Vec2(radius, radius), newPos + new Vec2(radius, radius));

                    free = GridPathFindSystem.Instance.IsFreeInMapMotion(targetRect);
                }

                GridPathFindSystem.Instance.AddObjectToMotionMap(this);

                if (free)
                {
                    float newZ = GridPathFindSystem.Instance.GetMotionMapHeight(newPos) + Type.Height * .5f;
                    // Set the object's position
                    Position = new Vec3(newPos.X, newPos.Y, newZ);
                }
                else
                    path.Clear();
            }
        }

        protected override void OnUpdateBaseAnimation()
        {
            base.OnUpdateBaseAnimation();

            //walk animation
            if (mainBodyVelocity.ToVec2().LengthFast() > .1f)
            {
                float velocity = (Rotation.GetInverse() * mainBodyVelocity).X *
                    Type.WalkAnimationVelocityMultiplier;
                UpdateBaseAnimation(Type.WalkAnimationName, true, true, velocity);
                return;
            }

            //idle animation
            {
                UpdateBaseAnimation(Type.IdleAnimationName, true, true, 1);
                return;
            }
        }
    }
}
