// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace GameEntities.RTS_Specific
{
	/// <summary>
	/// Defines the <see cref="RTSBuildingAI"/> entity type.
	/// </summary>
    public class RTSBuildingAIType : AntUnitAIType
	{
	}

	public class RTSBuildingAI : AntUnitAI
	{

		//

		RTSBuildingAIType _type = null; public new RTSBuildingAIType Type { get { return _type; } }

		public override List<AntUnitAI.UserControlPanelTask> GetControlPanelTasks()
		{
			List<UserControlPanelTask> list = new List<UserControlPanelTask>();

			if( ControlledObject.BuildedProgress == 1 )
			{
				if( ControlledObject.BuildUnitType == null )
				{
					//RTSHeadquaters specific
                    if ( ControlledObject.Type.Name == "RTSHeadquaters" || ControlledObject.Type.Name == "AntColmena" )
					{
						RTSUnitType unitType = (RTSUnitType)EntityTypes.Instance.GetByName( "BuilderAnt" );
						list.Add( new UserControlPanelTask( new Task( Task.Types.ProductUnit, unitType ),
							CurrentTask.Type == Task.Types.ProductUnit ) );

                        unitType = (RTSUnitType)EntityTypes.Instance.GetByName( "ForagerAnt" );
                        list.Add(new UserControlPanelTask(new Task(Task.Types.ProductUnit, unitType),
                            CurrentTask.Type == Task.Types.ProductUnit));
					}

					//RTSFactory specific
                    if ( ControlledObject.Type.Name == "RTSFactory" || ControlledObject.Type.Name == "AntBarrack" )
					{
                        RTSUnitType unitType = (RTSUnitType)EntityTypes.Instance.GetByName( "WarriorAnt" );
						list.Add( new UserControlPanelTask( new Task( Task.Types.ProductUnit, unitType ),
							CurrentTask.Type == Task.Types.ProductUnit ) );
					}
				}
				else
				{
					list.Add( new UserControlPanelTask( new Task( Task.Types.Stop ),
						CurrentTask.Type == Task.Types.Stop ) );
				}
			}
			else
			{
				//building
				list.Add( new UserControlPanelTask( new Task( Task.Types.SelfDestroy ) ) );
			}

			return list;
		}

		[Browsable( false )]
		public new RTSBuilding ControlledObject
		{
			//!!!!slowly
			get { return (RTSBuilding)base.ControlledObject; }
		}

		protected override void TickTasks()
		{
			base.TickTasks();

			RTSBuilding controlledObj = ControlledObject;
			if( controlledObj == null )
				return;

			switch( CurrentTask.Type )
			{

			case Task.Types.ProductUnit:
				//!!!!!!temp
				if( ControlledObject.BuildUnitType == null )
					DoTask( new Task( Task.Types.Stop ), false );
				break;
			}

		}

		protected override void DoTaskInternal( AntUnitAI.Task task )
		{
			if( task.Type != Task.Types.ProductUnit )
				ControlledObject.StopProductUnit();

			base.DoTaskInternal( task );

			if( task.Type == Task.Types.ProductUnit )
			{
				ControlledObject.StartProductUnit( (RTSUnitType)task.EntityType );
			}
		}
	}
}
