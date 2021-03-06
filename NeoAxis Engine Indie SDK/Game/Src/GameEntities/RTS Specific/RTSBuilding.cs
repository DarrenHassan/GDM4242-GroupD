// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.PhysicsSystem;

using GameEntities.RTS_Specific;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="RTSBuilding"/> entity type.
	/// </summary>
	public class RTSBuildingType : RTSUnitType
	{

	}

	public class RTSBuilding : RTSUnit
	{
		[FieldSerialize]
		RTSUnitType productUnitType;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float productUnitProgress;

		MapObjectAttachedMesh productUnitAttachedMesh;

		[DefaultValue( 1.0f )]
		[FieldSerialize]
		float buildedProgress = 1;

        // This building's gather point
        [FieldSerialize]
        [DefaultValue(typeof(Vec3), "0 0 0")]
        Vec3 gatherPoint;

		//

		RTSBuildingType _type = null; public new RTSBuildingType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();

			//for world load/save
			if( productUnitType != null )
				CreateProductUnitAttachedMesh();

			UpdateAttachedObjectsVisibility();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

            // Darkness
            if (InitialFaction != null)
            {
                bool badFaction = InitialFaction.Name == "BadFaction";
                if (badFaction == false)
                {
                    Vec3 pos = new Vec3();
                    pos = this.Position;
                    float XPos = pos.X;
                    float YPos = pos.Y;
                    if (Darkness.Instance != null)
                        Darkness.Instance.ClearMapPosition(XPos, YPos, 1);
                }
            }

			TickProductUnit();
		}

        [DefaultValue(typeof(Vec3), "0 0 0")]
        public Vec3 GatherPoint
        {
            get { return gatherPoint; }
            set { gatherPoint = value; }
        }

		void TickProductUnit()
		{
			if( productUnitType == null )
				return;

			productUnitProgress += TickDelta / productUnitType.BuildTime;

			Degree angleDelta = TickDelta * 20;
            float positionOffsetZ = 12;

			if( productUnitAttachedMesh != null ) 
            {
				productUnitAttachedMesh.RotationOffset *= new Angles( 0, 0, angleDelta ).ToQuat();
                productUnitAttachedMesh.PositionOffset = new Vec3(0, 0, positionOffsetZ);
            }

			if( BuildUnitProgress >= 1 )
			{
				CreateProductedUnit();
				StopProductUnit();
			}

			MapObjectAttachedObject buildPlatformMesh = GetAttachedObjectByAlias( "buildPlatform" );
			if( buildPlatformMesh != null )
				buildPlatformMesh.RotationOffset *= new Angles( 0, 0, angleDelta ).ToQuat();
		}

		public void StartProductUnit( RTSUnitType unitType )
		{
			StopProductUnit();

			//check cost
			RTSFactionManager.FactionItem factionItem = RTSFactionManager.Instance.
				GetFactionItemByType( Intellect.Faction );
            // Reduce the factions money by the unit's cost
			if( factionItem != null )
			{
				float cost = unitType.BuildCost;

				if( factionItem.Money - cost < 0 )
					return;

				factionItem.Money -= cost;
			}

			productUnitType = unitType;
			productUnitProgress = 0;

			CreateProductUnitAttachedMesh();

			UpdateAttachedObjectsVisibility();
		}

		public void StopProductUnit()
		{
			DestroyProductUnitAttachedMesh();

			productUnitType = null;
			productUnitProgress = 0;

			UpdateAttachedObjectsVisibility();
		}

		void CreateProductUnitAttachedMesh()
		{
			productUnitAttachedMesh = new MapObjectAttachedMesh();
			Attach( productUnitAttachedMesh );

			string meshName = null;
			//Vec3 meshOffset = new Vec3(0,0,10);
            Vec3 meshOffset = Vec3.Zero;
			Vec3 meshScale = new Vec3( 1, 1, 1);
			{
				foreach( MapObjectTypeAttachedObject typeAttachedObject in 
					productUnitType.AttachedObjects )
				{
					MapObjectTypeAttachedMesh typeAttachedMesh =
						typeAttachedObject as MapObjectTypeAttachedMesh;
					if( typeAttachedMesh == null )
						continue;

					meshName = typeAttachedMesh.MeshName;
					meshOffset = typeAttachedMesh.Position;
					meshScale = typeAttachedMesh.Scale;
					break;
				}
			}

			productUnitAttachedMesh.MeshName = meshName;

			Vec3 pos = meshOffset;
			{
				MapObjectAttachedObject buildPointAttachedHelper =
					GetAttachedObjectByAlias( "productUnitPoint" );
				if( buildPointAttachedHelper != null )
					pos += buildPointAttachedHelper.PositionOffset;
			}
			productUnitAttachedMesh.PositionOffset = pos;

			productUnitAttachedMesh.ScaleOffset = meshScale;

			if( Type.Name == "RTSHeadquaters" || Type.Name == "AntColmena" )
			{
				foreach( MeshObject.SubObject subMesh in productUnitAttachedMesh.MeshObject.SubObjects )
					subMesh.MaterialName = "RTSBuildMaterial";
			}
		}

		void DestroyProductUnitAttachedMesh()
		{
			if( productUnitAttachedMesh != null )
			{
				Detach( productUnitAttachedMesh );
				productUnitAttachedMesh = null;
			}
		}

		[Browsable( false )]
		public RTSUnitType BuildUnitType
		{
			get { return productUnitType; }
		}

		[Browsable( false )]
		public float BuildUnitProgress
		{
			get { return productUnitProgress; }
		}

		void CreateProductedUnit()
		{
			RTSUnit unit = (RTSUnit)Entities.Instance.Create( productUnitType, Map.Instance );

            if (unit.Type.Name == "ForagerAnt")
            {
                ForagerAnt character;
                character = unit as ForagerAnt;

                if (character == null)
                {
                    Log.Fatal("RTSBuilding: CreateProductedUnit: character == null");
                }

                Vec2 p = GridPathFindSystem.Instance.GetNearestFreePosition(Position.ToVec2(),
                    character.Type.Radius * 2);
                unit.Position = new Vec3(p.X, p.Y, GridPathFindSystem.Instance.GetMotionMapHeight(p) +
                    character.Type.Height * .5f);

                if (Intellect != null)
                    unit.InitialFaction = Intellect.Faction;

                unit.PostCreate();
                // Move the unit to the gather point
                AntUnitAI intellect = unit.Intellect as AntUnitAI;
                if (intellect != null &&
                    this.GatherPoint.X != 0 && this.GatherPoint.Y != 0 && this.GatherPoint.Z != 0)
                {
                    // Paths cannot be found to locations on the map already occupied by another ant,
                    // randomly generate a new gather point within 15 units of the original gather point                  
                    Random rand = new Random();
                    Vec3 newGatherPoint = new Vec3(this.GatherPoint.X + ((float)rand.NextDouble() * 30f - 15f),
                        this.GatherPoint.Y + ((float)rand.NextDouble() * 30f - 15f), this.GatherPoint.Z);
                    intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Move, newGatherPoint),
                        false);
                }

            }
            else 
            {
                GenericAntCharacter character;
                character = unit as GenericAntCharacter;

                if (character == null)
                {
                    Log.Fatal("RTSBuilding: CreateProductedUnit: character == null");
                }

                Vec2 p = GridPathFindSystem.Instance.GetNearestFreePosition(Position.ToVec2(),
                    character.Type.Radius * 2);
                unit.Position = new Vec3(p.X, p.Y, GridPathFindSystem.Instance.GetMotionMapHeight(p) +
                    character.Type.Height * .5f);

                if (Intellect != null)
                    unit.InitialFaction = Intellect.Faction;

                unit.PostCreate();
                // Move the unit to the gather point
                AntUnitAI intellect = unit.Intellect as AntUnitAI;
                if (intellect != null && 
                    this.GatherPoint.X != 0 && this.GatherPoint.Y != 0 && this.GatherPoint.Z != 0)
                {      
                    // Paths cannot be found to locations on the map already occupied by another ant,
                    // randomly generate a new gather point within 15 units of the original gather point                  
                    Random rand = new Random();
                    Vec3 newGatherPoint = new Vec3(this.GatherPoint.X + ((float)rand.NextDouble() * 30f - 15f),
                        this.GatherPoint.Y + ((float)rand.NextDouble() * 30f - 15f), this.GatherPoint.Z);
                    intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Move, newGatherPoint), 
                        false);
                }
            }
		}

		[DefaultValue( 1.0f )]
		public float BuildedProgress
		{
			get { return buildedProgress; }
			set
			{
				buildedProgress = value;

				UpdateAttachedObjectsVisibility();
			}
		}

		protected override void OnDamage( MapObject prejudicial, Vec3 pos, Shape shape, float damage,
			bool allowMoveDamageToParent )
		{
			float oldLife = Life;

			base.OnDamage( prejudicial, pos, shape, damage, allowMoveDamageToParent );

			if( damage < 0 && BuildedProgress != 1 )
			{
				BuildedProgress += ( -damage ) / Type.LifeMax;
				if( BuildedProgress > 1 )
					BuildedProgress = 1;

				if( BuildedProgress != 1 && Life == Type.LifeMax )
					Life = Type.LifeMax - .01f;
			}

			float halfLife = Type.LifeMax * .5f;
			if( Life > halfLife && oldLife <= halfLife )
				UpdateAttachedObjectsVisibility();
			else if( Life < halfLife && oldLife >= halfLife )
				UpdateAttachedObjectsVisibility();

			float quarterLife = Type.LifeMax * .25f;
			if( Life > quarterLife && oldLife <= quarterLife )
				UpdateAttachedObjectsVisibility();
			else if( Life < quarterLife && oldLife >= quarterLife )
				UpdateAttachedObjectsVisibility();
		}

		void UpdateAttachedObjectsVisibility()
		{
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				//lessHalfLife
				if( attachedObject.Alias == "lessHalfLife" )
				{
					attachedObject.Visible = ( Life < Type.LifeMax * .5f && buildedProgress == 1 );
					continue;
				}

				//lessQuarterLife
				if( attachedObject.Alias == "lessQuarterLife" )
				{
					attachedObject.Visible = ( Life < Type.LifeMax * .25f && buildedProgress == 1 );
					continue;
				}

				//productUnit
				if( attachedObject.Alias == "productUnit" )
				{
					attachedObject.Visible = productUnitType != null;
					continue;
				}

				//building
				{
					string showAlias = null;

					if( buildedProgress < .25f )
						showAlias = "building0";
					else if( buildedProgress < .5f )
						showAlias = "building1";
					else if( buildedProgress < 1 )
						showAlias = "building2";

					if( showAlias != null )
						attachedObject.Visible = ( attachedObject.Alias == showAlias );
					else
						attachedObject.Visible = !attachedObject.Alias.Contains( "building" );
				}

			}
		}

	}
}
