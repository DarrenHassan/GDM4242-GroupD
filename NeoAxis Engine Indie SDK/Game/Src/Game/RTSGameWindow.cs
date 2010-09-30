// Copyright (C) 2006-2010 NeoAxis Group Ltd.
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
using GameEntities.RTS_Specific;

namespace Game
{
    public class RTSGameWindow : GameWindow
    {
        enum CameraType
        {
            Game,
            Free,

            Count
        }
        static CameraType cameraType;

        [Config("Map", "drawPathMotionMap")]
        public static bool mapDrawPathMotionMap;

        float cameraDistance = 23;
        SphereDir cameraDirection = new SphereDir(1.5f, .85f);
        Vec2 cameraPosition;

        //HUD
        EControl hudControl;

        //Select
        List<Unit> selectedUnits = new List<Unit>();

        //Select mode
        bool selectMode;
        Vec2 selectStartPos;
        bool selectDraggedMouse;

        //Task target choose
        int taskTargetChooseIndex = -1;

        //Task target build Building
        RTSBuildingType taskTargetBuildingType;
        MeshObject taskTargetBuildMeshObject;
        SceneNode taskTargetBuildSceneNode;

        //Player Faction
        FactionType playerFaction;

        //Minimap
        bool minimapChangeCameraPosition;
        EControl minimapControl;

        float timeForUpdateGameStatus;

        //
		double elapsedGameTime = 0;

        protected override void OnAttach()
        {
            base.OnAttach();

            EngineApp.Instance.KeysAndMouseButtonUpAll();

            //hudControl
            hudControl = ControlDeclarationManager.Instance.CreateControl("Gui\\RTSHUD.gui");
            Controls.Add(hudControl);

            ((EButton)hudControl.Controls["Menu"]).Click += delegate(EButton sender)
            {
                Controls.Add(new MenuWindow());
            };

            ((EButton)hudControl.Controls["Exit"]).Click += delegate(EButton sender)
            {
                string mapName = "Maps\\MainDemo\\Map.map";
                string spawnPointName = "SpawnPoint_FromRTSDemo";
                GameWorld.Instance.SetShouldChangeMap(mapName, spawnPointName, null);
            };

            ((EButton)hudControl.Controls["Help"]).Click += delegate(EButton sender)
            {
                hudControl.Controls["HelpWindow"].Visible = !hudControl.Controls["HelpWindow"].Visible;
            };

            ((EButton)hudControl.Controls["HelpWindow"].Controls["Close"]).Click += delegate(EButton sender)
            {
                hudControl.Controls["HelpWindow"].Visible = false;
            };

            ((EButton)hudControl.Controls["DebugPath"]).Click += delegate(EButton sender)
            {
                mapDrawPathMotionMap = !mapDrawPathMotionMap;
            };

            InitControlPanelButtons();
            UpdateControlPanel();

            //set playerFaction
            if (RTSFactionManager.Instance != null && RTSFactionManager.Instance.Factions.Count != 0)
                playerFaction = RTSFactionManager.Instance.Factions[0].FactionType;

            //minimap
            minimapControl = hudControl.Controls["Minimap"];
            string textureName = Map.Instance.GetSourceMapVirtualFileDirectory() + "\\Minimap\\Minimap";
            Texture minimapTexture = TextureManager.Instance.Load(textureName, Texture.Type.Type2D, 0);
            minimapControl.BackTexture = minimapTexture;
            minimapControl.RenderUI += new RenderUIDelegate(Minimap_RenderUI);

            //set camera position
            foreach (Entity entity in Map.Instance.Children)
            {
                SpawnPoint spawnPoint = entity as SpawnPoint;
                if (spawnPoint == null)
                    continue;
                cameraPosition = spawnPoint.Position.ToVec2();
                break;
            }

            //World serialized data
            if (World.Instance.GetCustomSerializationValue("cameraDistance") != null)
                cameraDistance = (float)World.Instance.GetCustomSerializationValue("cameraDistance");
            if (World.Instance.GetCustomSerializationValue("cameraDirection") != null)
                cameraDirection = (SphereDir)World.Instance.GetCustomSerializationValue("cameraDirection");
            if (World.Instance.GetCustomSerializationValue("cameraPosition") != null)
                cameraPosition = (Vec2)World.Instance.GetCustomSerializationValue("cameraPosition");
            for (int n = 0; ; n++)
            {
                Unit unit = World.Instance.GetCustomSerializationValue(
                    "selectedUnit" + n.ToString()) as Unit;
                if (unit == null)
                    break;
                SetEntitySelected(unit, true);
            }

            ResetTime();

            //render scene for loading resources
            EngineApp.Instance.RenderScene();

            EngineApp.Instance.MousePosition = new Vec2(.5f, .5f);
        }

        public override void OnBeforeWorldSave()
        {
            base.OnBeforeWorldSave();

            //World serialized data
            World.Instance.ClearAllCustomSerializationValues();
            World.Instance.SetCustomSerializationValue("cameraDistance", cameraDistance);
            World.Instance.SetCustomSerializationValue("cameraDirection", cameraDirection);
            World.Instance.SetCustomSerializationValue("cameraPosition", cameraPosition);
            for (int n = 0; n < selectedUnits.Count; n++)
            {
                Unit unit = selectedUnits[n];
                World.Instance.SetCustomSerializationValue("selectedUnit" + n.ToString(), unit);
            }
        }

        protected override void OnDetach()
        {
            //minimap
            if (minimapControl.BackTexture != null)
            {
                minimapControl.BackTexture.Dispose();
                minimapControl.BackTexture = null;
            }

            base.OnDetach();
        }

        protected override bool OnKeyDown(KeyEvent e)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnKeyDown(e);

            //change camera type
            if (e.Key == EKeys.F7)
            {
                cameraType = (CameraType)((int)GetRealCameraType() + 1);
                if (cameraType == CameraType.Count)
                    cameraType = (CameraType)0;

                FreeCameraEnabled = cameraType == CameraType.Free;

                return true;
            }

            return base.OnKeyDown(e);
        }

        protected override bool OnKeyUp(KeyEvent e)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnKeyUp(e);

            return base.OnKeyUp(e);
        }

        bool IsMouseInActiveArea()
        {
            if (!hudControl.Controls["ActiveArea"].GetScreenRectangle().IsContainsPoint(MousePosition))
                return false;
            return true;
        }

        protected override bool OnMouseDown(EMouseButtons button)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnMouseDown(button);

            if (button == EMouseButtons.Left && IsMouseInActiveArea() && TaskTargetChooseIndex == -1)
            {
                selectMode = true;
                selectDraggedMouse = false;
                selectStartPos = EngineApp.Instance.MousePosition;
                return true;
            }

            //minimap mouse change camera position
            if (button == EMouseButtons.Left && taskTargetChooseIndex == -1)
            {
                if (minimapControl.GetScreenRectangle().IsContainsPoint(MousePosition))
                {
                    minimapChangeCameraPosition = true;
                    cameraPosition = GetMapPositionByMouseOnMinimap();
                    return true;
                }
            }

            return base.OnMouseDown(button);
        }

        protected override bool OnMouseUp(EMouseButtons button)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnMouseUp(button);

            //do tasks
            if ((button == EMouseButtons.Right || button == EMouseButtons.Left) &&
                (!FreeCameraMouseRotating || !EngineApp.Instance.MouseRelativeMode))
            {
                bool pickingSuccess = false;
                Vec3 mouseMapPos = Vec3.Zero;
                Unit mouseOnObject = null;

                //pick on active area
                if (IsMouseInActiveArea())
                {
                    //get pick information
                    Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
                        EngineApp.Instance.MousePosition);
                    if (!float.IsNaN(ray.Direction.X))
                    {
                        RayCastResult result = PhysicsWorld.Instance.RayCast(ray,
                            (int)ContactGroup.CastOnlyContact);
                        if (result.Shape != null)
                        {
                            pickingSuccess = true;
                            mouseOnObject = MapSystemWorld.GetMapObjectByBody(result.Shape.Body) as Unit;
                            mouseMapPos = result.Position;
                        }
                    }
                }

                //pick on minimap
                if (minimapControl.GetScreenRectangle().IsContainsPoint(MousePosition))
                {
                    pickingSuccess = true;
                    Vec2 pos = GetMapPositionByMouseOnMinimap();
                    mouseMapPos = new Vec3(pos.X, pos.Y, GridPathFindSystem.Instance.GetMotionMapHeight(pos));
                }

                if (pickingSuccess)
                {
                    //do tasks
                    if (TaskTargetChooseIndex != -1)
                    {
                        if (button == EMouseButtons.Left)
                            DoTaskTargetChooseTasks(mouseMapPos, mouseOnObject);
                        if (button == EMouseButtons.Right)
                            TaskTargetChooseIndex = -1;
                    }
                    else
                    {
                        if (button == EMouseButtons.Right)
                            DoRightClickTasks(mouseMapPos, mouseOnObject);
                    }
                }
            }

            //select mode
            if (selectMode && button == EMouseButtons.Left)
                DoEndSelectMode();

            //minimap mouse change camera position
            if (minimapChangeCameraPosition)
                minimapChangeCameraPosition = false;

            return base.OnMouseUp(button);
        }

        bool IsEnableTaskTypeInTasks(List<AntUnitAI.UserControlPanelTask> tasks, AntUnitAI.Task.Types taskType)
        {
            if (tasks == null)
                return false;
            foreach (AntUnitAI.UserControlPanelTask task in tasks)
                if (task.Task.Type == taskType && task.Enable)
                    return true;
            return false;
        }

        // Called when selecting an object to perform a task
        void DoTaskTargetChooseTasks(Vec3 mouseMapPos, Unit mouseOnObject)
        {
            //Do task after task target choose

            bool toQueue = EngineApp.Instance.IsKeyPressed(EKeys.Shift);

            List<AntUnitAI.UserControlPanelTask> tasks = GetControlTasks();
            int index = TaskTargetChooseIndex;

            if (tasks != null && index < tasks.Count && tasks[index].Enable)
            {

                // Have more than one ant been selected to move
                AntUnitAI.Task.Types taskType = tasks[index].Task.Type;
                if (taskType == AntUnitAI.Task.Types.TrailMove &&
                    selectedUnits.Count > 1)
                {
                    // Find the destination
                    Vec2 destination;
                    if (mouseOnObject != null)
                        destination = mouseOnObject.Position.ToVec2();
                    else
                        destination = mouseMapPos.ToVec2();

                    // Order the selected units by distance to the destination
                    selectedUnits.Sort(delegate(Unit u1, Unit u2)
                    {
                        float u1Distance = (u1.Position.ToVec2() - destination).LengthFast();
                        float u2Distance = (u2.Position.ToVec2() - destination).LengthFast();
                        return u1Distance.CompareTo(u2Distance);
                    });
                }

                // The last ant to be moved in the group of selected ants
                Unit lastUnit = null;
                foreach (Unit unit in selectedUnits)
                {
                    AntUnitAI intellect = unit.Intellect as AntUnitAI;
                    if (intellect == null)
                        continue;

                    //AntUnitAI.Task.Types taskType = tasks[ index ].Task.Type;

                    List<AntUnitAI.UserControlPanelTask> aiTasks = intellect.GetControlPanelTasks();

                    if (!IsEnableTaskTypeInTasks(aiTasks, taskType))
                        continue;

                    switch (taskType)
                    {
                        // TrailMove
                        case AntUnitAI.Task.Types.TrailMove:
                            if (lastUnit == null)
                            {
                                // This is the first ant to be moved in the group of selected ants
                                if (mouseOnObject != null)
                                    intellect.DoTask(new AntUnitAI.Task(taskType, mouseOnObject), toQueue);
                                else
                                    intellect.DoTask(new AntUnitAI.Task(taskType, mouseMapPos), toQueue);
                            }
                            else
                                // This is one of the following ants
                                intellect.DoTask(new AntUnitAI.Task(taskType, lastUnit), toQueue);
                            // The next ant will follow this ant
                            lastUnit = unit;
                            break;
                        // Move, Attack, Repair
                        case AntUnitAI.Task.Types.Collect:
                            //Log.Warning("RTSGameWindow: Collect");
                            if (mouseOnObject != null)
                            {
                                // An entity has been selected
                                if (mouseOnObject.Type.Name == "RTSMine")
                                {
                                    intellect.DoTask(new AntUnitAI.Task(taskType, mouseOnObject), toQueue);
                                    //Log.Warning("Thats an RTSMine");
                                }
                                else
                                {
                                    //Log.Warning("Not an RTSMine");
                                    intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Move, mouseOnObject), toQueue);
                                }
                            }
                            else
                            {
                                intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Move, mouseMapPos), toQueue);
                            }
                            break;

                        case AntUnitAI.Task.Types.Move:
                        case AntUnitAI.Task.Types.Attack:
                        case AntUnitAI.Task.Types.Repair:
                            if (mouseOnObject != null)
                                // An entity has been selected
                                intellect.DoTask(new AntUnitAI.Task(taskType, mouseOnObject), toQueue);
                            else
                            {
                                // The position on the map has been clicked
                                if (taskType == AntUnitAI.Task.Types.Move)
                                    intellect.DoTask(new AntUnitAI.Task(taskType, mouseMapPos), toQueue);

                                if (taskType == AntUnitAI.Task.Types.Attack ||
                                    taskType == AntUnitAI.Task.Types.Repair)
                                {
                                    intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.BreakableMove,
                                        mouseMapPos), toQueue);
                                }
                            }
                            break;

                        // Could be relavent for the building of ant structures?
                        //BuildBuilding
                        case AntUnitAI.Task.Types.BuildBuilding:
                            {
                                if (!taskTargetBuildSceneNode.Visible)
                                    return;

                                Vec3 pos = taskTargetBuildSceneNode.Position;

							/*RTSMine specific
							bool mineFound = false;
							if( taskTargetBuildingType is RTSMineType )
							{

								Bounds bounds = new Bounds( pos - new Vec3( 2, 2, 2 ),
									pos + new Vec3( 2, 2, 2 ) );
								Map.Instance.GetObjects( bounds, delegate( MapObject obj )
								{

									if( obj.Type.Name == "RTSGeyser" )
									{

										mineFound = true;
										mouseMapPos = obj.Position;
									}
								} );



								if( !mineFound )
								{

									//no mine for build
									return;
								}
							}*/

                                if (IsFreeForBuildTaskTargetBuild(pos))
                                {
                                    intellect.DoTask(new AntUnitAI.Task(taskType, pos,
                                        tasks[index].Task.EntityType), toQueue);

                                    GameEngineApp.Instance.ControlManager.PlaySound(
                                        "Sounds\\Feedback\\RTSBuildBuilding.ogg");
                                }
                                else
                                {
                                    //no free for build
                                    return;
                                }
                            }
                            break;

                    }
                }
            }
            TaskTargetChooseIndex = -1;
        }
    
        // Enables right-click actions, such as move, attack
        void DoRightClickTasks(Vec3 mouseMapPos, Unit mouseOnObject)
        {
            bool toQueue = EngineApp.Instance.IsKeyPressed(EKeys.Shift);

            foreach (Unit unit in selectedUnits)
            {
                AntUnitAI intellect = unit.Intellect as AntUnitAI;
                if (intellect == null)
                    continue;

                if (intellect.Faction != playerFaction)
                    continue;

                List<AntUnitAI.UserControlPanelTask> tasks = intellect.GetControlPanelTasks();

                if (mouseOnObject != null)
                {
                    bool tasked = false;

                    if (IsEnableTaskTypeInTasks(tasks, AntUnitAI.Task.Types.Attack) &&
                        mouseOnObject.Intellect != null &&
                        intellect.Faction != null && mouseOnObject.Intellect.Faction != null &&
                        intellect.Faction != mouseOnObject.Intellect.Faction)
                    {
                        intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Attack,
                            mouseOnObject), toQueue);
                        tasked = true;
                    }

                    if (IsEnableTaskTypeInTasks(tasks, AntUnitAI.Task.Types.Repair) &&
                        mouseOnObject.Intellect != null &&
                        intellect.Faction != null && mouseOnObject.Intellect.Faction != null &&
                        intellect.Faction == mouseOnObject.Intellect.Faction)
                    {
                        intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Repair,
                            mouseOnObject), toQueue);
                        tasked = true;
                    }

                    if (!tasked && IsEnableTaskTypeInTasks(tasks, AntUnitAI.Task.Types.Move))
                        intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Move, mouseOnObject), toQueue);
                }
                else
                {
                    if (IsEnableTaskTypeInTasks(tasks, AntUnitAI.Task.Types.Move))
                        intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Move, mouseMapPos), toQueue);
                }
            }
        }

        void DoEndSelectMode()
        {
            selectMode = false;

            List<Unit> areaObjs = new List<Unit>();
            {
                if (selectDraggedMouse)
                {
                    Rect rect = new Rect(selectStartPos);
                    rect.Add(EngineApp.Instance.MousePosition);

                    Map.Instance.GetObjectsByScreenRectangle(rect, GameFilterGroups.UnitFilterGroup,
                        delegate(MapObject obj)
                        {
                            Unit unit = (Unit)obj;
                            areaObjs.Add(unit);
                        });
                }
                else
                {
                    Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
                        EngineApp.Instance.MousePosition);

                    RayCastResult result = PhysicsWorld.Instance.RayCast(ray,
                        (int)ContactGroup.CastOnlyContact);
                    if (result.Shape != null)
                    {
                        Unit unit = MapSystemWorld.GetMapObjectByBody(result.Shape.Body) as Unit;
                        if (unit != null)
                            areaObjs.Add(unit);
                    }
                }
            }

            //do select/unselect

            if (!EngineApp.Instance.IsKeyPressed(EKeys.Shift))
                ClearEntitySelection();

            if (areaObjs.Count == 0)
                return;

            if (!selectDraggedMouse && EngineApp.Instance.IsKeyPressed(EKeys.Shift))
            {
                //unselect
                foreach (Unit obj in areaObjs)
                {
                    if (selectedUnits.Contains(obj))
                    {
                        SetEntitySelected(obj, false);
                        return;
                    }
                }
            }

            bool needFactionSetted = false;
            FactionType needFaction = null;

            if (selectedUnits.Count == 0 && playerFaction != null)
            {
                foreach (Unit obj in areaObjs)
                {
                    FactionType objFaction = null;
                    if (obj.Intellect != null)
                        objFaction = obj.Intellect.Faction;

                    if (playerFaction == objFaction)
                    {
                        needFactionSetted = true;
                        needFaction = playerFaction;
                        break;
                    }
                }
            }

            if (selectedUnits.Count != 0)
            {
                needFactionSetted = true;
                needFaction = null;
                if (selectedUnits[0].Intellect != null)
                    needFaction = selectedUnits[0].Intellect.Faction;
            }

            if (!needFactionSetted && selectedUnits.Count == 0)
            {
                needFactionSetted = true;
                needFaction = null;
                if (areaObjs[0].Intellect != null)
                    needFaction = areaObjs[0].Intellect.Faction;
            }

            foreach (Unit obj in areaObjs)
            {
                FactionType objFaction = null;
                if (obj.Intellect != null)
                    objFaction = obj.Intellect.Faction;

                if (needFaction != objFaction)
                    continue;

                if (selectedUnits.Count != 0 && needFaction != playerFaction)
                    break;

                SetEntitySelected(obj, true);
            }
        }

        protected override bool OnMouseDoubleClick(EMouseButtons button)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnMouseDoubleClick(button);

            return base.OnMouseDoubleClick(button);
        }

        protected override bool OnMouseMove()
        {
            bool ret = base.OnMouseMove();

            //If atop openly any window to not process
            if (Controls.Count != 1)
                return ret;

            //select mode
            if (selectMode)
            {
                Vec2 diffPixels = (MousePosition - selectStartPos) *
                    new Vec2(EngineApp.Instance.VideoMode.X, EngineApp.Instance.VideoMode.Y);
                if (Math.Abs(diffPixels.X) >= 3 || Math.Abs(diffPixels.Y) >= 3)
                {
                    selectDraggedMouse = true;
                }
            }

            //minimap mouse change camera position
            if (minimapChangeCameraPosition)
                cameraPosition = GetMapPositionByMouseOnMinimap();

            return ret;
        }

        protected override void OnTick(float delta)
        {
            base.OnTick(delta);
            elapsedGameTime += delta;

            //If atop openly any window to not process
            if (Controls.Count != 1)
                return;

            //Remove deleted selected objects
            for (int n = 0; n < selectedUnits.Count; n++)
            {
                if (selectedUnits[n].IsSetDeleted)
                {
                    selectedUnits.RemoveAt(n);
                    n--;
                }
            }

            if (!FreeCameraMouseRotating)
                EngineApp.Instance.MouseRelativeMode = false;

            bool activeConsole = EngineConsole.Instance != null && EngineConsole.Instance.Active;

            if (GetRealCameraType() == CameraType.Game && !activeConsole)
            {
                Vec2 distanceRange = new Vec2(10, 300);
                Vec2 angleRange = new Vec2(.001f, MathFunctions.PI / 2 - .001f);

                if (EngineApp.Instance.IsKeyPressed(EKeys.PageUp))
                {
                    cameraDistance -= delta * (distanceRange[1] - distanceRange[0]) / 10.0f;
                    if (cameraDistance < distanceRange[0])
                        cameraDistance = distanceRange[0];
                }

                if (EngineApp.Instance.IsKeyPressed(EKeys.PageDown))
                {
                    cameraDistance += delta * (distanceRange[1] - distanceRange[0]) / 10.0f;
                    if (cameraDistance > distanceRange[1])
                        cameraDistance = distanceRange[1];
                }

                //rtsCameraDirection

                if (EngineApp.Instance.IsKeyPressed(EKeys.Home))
                {
                    cameraDirection.Vertical += delta * (angleRange[1] - angleRange[0]) / 2;
                    if (cameraDirection.Vertical >= angleRange[1])
                        cameraDirection.Vertical = angleRange[1];
                }

                if (EngineApp.Instance.IsKeyPressed(EKeys.End))
                {
                    cameraDirection.Vertical -= delta * (angleRange[1] - angleRange[0]) / 2;
                    if (cameraDirection.Vertical < angleRange[0])
                        cameraDirection.Vertical = angleRange[0];
                }

                if (EngineApp.Instance.IsKeyPressed(EKeys.Q))
                {
                    cameraDirection.Horizontal += delta * 2;
                    if (cameraDirection.Horizontal >= MathFunctions.PI * 2)
                        cameraDirection.Horizontal -= MathFunctions.PI * 2;
                }

                if (EngineApp.Instance.IsKeyPressed(EKeys.E))
                {
                    cameraDirection.Horizontal -= delta * 2;
                    if (cameraDirection.Horizontal < 0)
                        cameraDirection.Horizontal += MathFunctions.PI * 2;
                }


                //change cameraPosition
                if (!selectMode && Time > 2)
                {
                    Vec2 vector = Vec2.Zero;

                    if (EngineApp.Instance.IsKeyPressed(EKeys.Left) ||
                        EngineApp.Instance.IsKeyPressed(EKeys.A) || MousePosition.X < .005f)
                    {
                        vector.X--;
                    }
                    if (EngineApp.Instance.IsKeyPressed(EKeys.Right) ||
                        EngineApp.Instance.IsKeyPressed(EKeys.D) || MousePosition.X > 1.0f - .005f)
                    {
                        vector.X++;
                    }
                    if (EngineApp.Instance.IsKeyPressed(EKeys.Up) ||
                        EngineApp.Instance.IsKeyPressed(EKeys.W) || MousePosition.Y < .005f)
                    {
                        vector.Y++;
                    }
                    if (EngineApp.Instance.IsKeyPressed(EKeys.Down) ||
                        EngineApp.Instance.IsKeyPressed(EKeys.S) || MousePosition.Y > 1.0f - .005f)
                    {
                        vector.Y--;
                    }

                    if (vector != Vec2.Zero)
                    {
                        //rotate vector
                        float angle = MathFunctions.ATan(-vector.Y, vector.X) +
                            cameraDirection.Horizontal;
                        vector = new Vec2(MathFunctions.Sin(angle), MathFunctions.Cos(angle));

                        cameraPosition += vector * delta * 50;
                    }
                }

            }

            // Control the bad faction
            if (elapsedGameTime > 10f && elapsedGameTime < 11f)
            {
                foreach (Entity entity in Map.Instance.Children)
                {
                    RTSUnit unit = entity as RTSUnit;
                    if (unit == null)
                        continue;
                    if (unit.Intellect == null)
                        continue;
                    if (unit.Intellect.Faction != playerFaction)
                    {
                        AntUnitAI intellect = unit.Intellect as AntUnitAI;
                        if (intellect == null)
                            continue;
                        //intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Move, new Vec3(0, 0, 0)), false);
                        intellect.DoTask(new AntUnitAI.Task(AntUnitAI.Task.Types.Wander, 5, 50, 10), false);
                    }
                }
            }

            //gameStatus
            if (string.IsNullOrEmpty(hudControl.Controls["GameStatus"].Text))
            {
                timeForUpdateGameStatus -= delta;
                if (timeForUpdateGameStatus < 0)
                {
                    timeForUpdateGameStatus += 1;

                    bool existsAlly = false;
                    bool existsEnemy = false;

                    foreach (Entity entity in Map.Instance.Children)
                    {
                        RTSUnit unit = entity as RTSUnit;
                        if (unit == null)
                            continue;
                        if (unit.Intellect == null)
                            continue;
                        if (unit.Intellect.Faction == playerFaction)
                            existsAlly = true;
                        else
                            existsEnemy = true;
                    }

                    string gameStatus = "";
                    if (!existsAlly)
                        gameStatus = "!!! Defeat !!!";
                    if (!existsEnemy)
                        gameStatus = "!!! Victory !!!";

                    hudControl.Controls["GameStatus"].Text = gameStatus;
                }
            }
        }

        protected override void OnRender()
        {
            base.OnRender();

            if (GridPathFindSystem.Instance != null)
                GridPathFindSystem.Instance.DebugDraw = mapDrawPathMotionMap;

            UpdateHUD();
        }

        void UpdateHUD()
        {
            Camera camera = RendererWorld.Instance.DefaultCamera;

            hudControl.Visible = EngineDebugSettings.DrawGui;

            //Selected units bounds
            if (taskTargetBuildMeshObject == null)
            {
                Vec3 mouseMapPos = Vec3.Zero;
                Unit mouseOnObject = null;

                bool pickingSuccess = false;

                if (!EngineApp.Instance.MouseRelativeMode)
                {
                    Ray ray = camera.GetCameraToViewportRay(EngineApp.Instance.MousePosition);
                    if (!float.IsNaN(ray.Direction.X))
                    {
                        RayCastResult result = PhysicsWorld.Instance.RayCast(ray,
                            (int)ContactGroup.CastOnlyContact);
                        if (result.Shape != null)
                        {
                            pickingSuccess = true;
                            mouseOnObject = MapSystemWorld.GetMapObjectByBody(result.Shape.Body) as Unit;
                            mouseMapPos = result.Position;
                        }
                    }

                    if (selectMode && selectDraggedMouse)
                    {
                        Rect rect = new Rect(selectStartPos);
                        rect.Add(EngineApp.Instance.MousePosition);

                        Map.Instance.GetObjectsByScreenRectangle(rect, GameFilterGroups.UnitFilterGroup,
                            delegate(MapObject obj)
                            {
                                Unit unit = (Unit)obj;

                                camera.DebugGeometry.Color = new ColorValue(1, 1, 0);
                                Bounds bounds = obj.MapBounds;
                                bounds.Expand(.1f);
                                camera.DebugGeometry.AddBounds(bounds);
                            });
                    }
                    else
                    {
                        if (pickingSuccess && IsMouseInActiveArea())
                        {
                            if (mouseOnObject != null)
                            {
                                camera.DebugGeometry.Color = new ColorValue(1, 1, 0);

                                Bounds bounds = mouseOnObject.MapBounds;
                                bounds.Expand(.1f);
                                camera.DebugGeometry.AddBounds(bounds);
                            }
                            else
                            {
                                camera.DebugGeometry.Color = new ColorValue(1, 0, 0);
                                camera.DebugGeometry.AddSphere(new Sphere(mouseMapPos, .4f), 16);
                            }
                        }
                    }
                }

                //objects selected
                foreach (Unit unit in selectedUnits)
                {
                    ColorValue color;

                    if (playerFaction == null || unit.Intellect == null || unit.Intellect.Faction == null)
                        color = new ColorValue(1, 1, 0);
                    else if (playerFaction == unit.Intellect.Faction)
                        color = new ColorValue(0, 1, 0);
                    else
                        color = new ColorValue(1, 0, 0);

                    camera.DebugGeometry.Color = color;
                    camera.DebugGeometry.AddBounds(unit.MapBounds);
                }
            }

            //taskTargetBuild
            if (taskTargetBuildMeshObject != null)
            {
                taskTargetBuildSceneNode.Visible = false;

                Ray ray = new Ray(Vec3.Zero, Vec3.Zero);

                //pick on active area
                if (IsMouseInActiveArea())
                    ray = camera.GetCameraToViewportRay(EngineApp.Instance.MousePosition);

                //pick on minimap
                if (minimapControl.GetScreenRectangle().IsContainsPoint(MousePosition))
                {
                    Vec2 p = GetMapPositionByMouseOnMinimap();
                    ray = new Ray(new Vec3(p.X, p.Y, 1000), new Vec3(.001f, .001f, -2000));
                }

                if (ray.Direction != Vec3.Zero)
                {
                    RayCastResult result = PhysicsWorld.Instance.RayCast(ray,
                        (int)ContactGroup.CastOnlyCollision);
                    if (result.Shape != null)
                    {
                        Vec3 mouseMapPos = result.Position;

                        //snap
                        mouseMapPos = new Vec3((int)(mouseMapPos.X + .5f),
                            (int)(mouseMapPos.Y + .5f), mouseMapPos.Z);

                        //RTSMine specific
                        bool mineFound = false;
                        if (taskTargetBuildingType is RTSMineType)
                        {
                            Bounds bounds = new Bounds(mouseMapPos - new Vec3(2, 2, 2),
                                mouseMapPos + new Vec3(2, 2, 2));
                            Map.Instance.GetObjects(bounds, delegate(MapObject obj)
                            {
                                if (obj.Type.Name == "RTSGeyser")
                                {
                                    mineFound = true;
                                    mouseMapPos = obj.Position;
                                }
                            });
                        }

                        taskTargetBuildSceneNode.Position = mouseMapPos;
                        taskTargetBuildSceneNode.Visible = true;

                        //check free for build
                        bool free = IsFreeForBuildTaskTargetBuild(mouseMapPos);

                        //RTSMine specific
                        if (taskTargetBuildingType is RTSMineType)
                            if (!mineFound)
                                free = false;

                        foreach (MeshObject.SubObject subMesh in taskTargetBuildMeshObject.SubObjects)
                            subMesh.MaterialName = free ? "Green" : "Red";
                    }
                }
            }

            //Selected units HUD
            {
                string text = "";

                if (selectedUnits.Count > 1)
                {
                    foreach (Unit unit in selectedUnits)
                        text += unit.ToString() + "\n";
                }

                if (selectedUnits.Count == 1)
                {
                    Unit unit = selectedUnits[0];


                    text += unit.ToString() + "\n";
                    text += "\n";
                    text += string.Format("Life: {0}/{1}\n", unit.Life, unit.Type.LifeMax);
                    if (unit.Type is GenericAntCharacterType)
                    {
                        GenericAntCharacter ant = unit as GenericAntCharacter;
                        text += string.Format("Resources: {0}/{1}\n", ant.Resources, ant.Type.ResourcesMax);
                    }


                    text += "Intellect:\n";
                    if (unit.Intellect != null)
                    {
                        text += string.Format("- {0}\n", unit.Intellect.ToString());
                        FactionType faction = unit.Intellect.Faction;
                        text += string.Format("- Faction: {0}\n", faction != null ? faction.ToString() : "null");

                        AntUnitAI antUnitAI = unit.Intellect as AntUnitAI;
                        if (antUnitAI != null)
                        {
                            text += string.Format("- CurrentTask: {0}\n", antUnitAI.CurrentTask.ToString());
                        }
                    }
                    else
                        text += string.Format("- null\n");

                }

                hudControl.Controls["SelectedUnitsInfo"].Text = text;

                UpdateControlPanel();
            }

            //RTSFactionManager
            {
                string text = "";

                if (RTSFactionManager.Instance != null)
                {
                    foreach (RTSFactionManager.FactionItem item in RTSFactionManager.Instance.Factions)
                    {
                        string s = "  " + item.ToString();
                        s += ", Money " + ((int)item.Money).ToString();
                        if (item.FactionType == playerFaction)
                            s += " (Player)";
                        text += s + "\n";
                    }
                }
                else
                    text += "RTSFactionManager not exists\n";

                hudControl.Controls["DebugText"].Text = text;
            }

            UpdateHUDControlIcon();
        }

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

        List<AntUnitAI.UserControlPanelTask> GetControlTasks()
        {
            List<AntUnitAI.UserControlPanelTask> tasks = null;

            if (selectedUnits.Count != 0)
            {
                FactionType faction = null;
                if (selectedUnits[0].Intellect != null)
                    faction = selectedUnits[0].Intellect.Faction;

                if (faction == playerFaction)
                {
                    foreach (Unit unit in selectedUnits)
                    {
                        AntUnitAI intellect = unit.Intellect as AntUnitAI;
                        if (intellect != null)
                        {
                            List<AntUnitAI.UserControlPanelTask> t = intellect.GetControlPanelTasks();
                            if (tasks == null)
                            {
                                tasks = t;
                            }
                            else
                            {
                                for (int n = 0; n < tasks.Count; n++)
                                {
                                    if (n >= t.Count)
                                        continue;

                                    if (tasks[n].Task.Type != t[n].Task.Type)
                                        continue;
                                    if (t[n].Active)
                                    {
                                        tasks[n] = new AntUnitAI.UserControlPanelTask(
                                            tasks[n].Task, true, tasks[n].Enable);
                                    }

                                    if (tasks[n].Task.Type == AntUnitAI.Task.Types.ProductUnit ||
                                        tasks[n].Task.Type == AntUnitAI.Task.Types.BuildBuilding)
                                    {
                                        if (tasks[n].Task.EntityType != t[n].Task.EntityType)
                                            tasks[n] = new AntUnitAI.UserControlPanelTask(
                                                new AntUnitAI.Task(AntUnitAI.Task.Types.None));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return tasks;
        }

        void InitControlPanelButtons()
        {
            for (int n = 0; ; n++)
            {
                EButton button = (EButton)hudControl.Controls["ControlPanelButton" + n.ToString()];
                if (button == null)
                    break;
                button.Click += new EButton.ClickDelegate(ControlPanelButton_Click);
            }
        }

        void ControlPanelButton_Click(EButton sender)
        {
            int index = int.Parse(sender.Name.Substring("ControlPanelButton".Length));

            TaskTargetChooseIndex = -1;

            List<AntUnitAI.UserControlPanelTask> tasks = GetControlTasks();

            if (tasks == null || index >= tasks.Count)
                return;
            if (!tasks[index].Enable)
                return;

            AntUnitAI.Task.Types taskType = tasks[index].Task.Type;

            switch (taskType)
            {
                //Stop, SelfDestroy
                case AntUnitAI.Task.Types.Stop:
                case AntUnitAI.Task.Types.SelfDestroy:
                    foreach (Unit unit in selectedUnits)
                    {
                        AntUnitAI intellect = unit.Intellect as AntUnitAI;
                        if (intellect == null)
                            continue;

                        if (IsEnableTaskTypeInTasks(intellect.GetControlPanelTasks(), taskType))
                            intellect.DoTask(new AntUnitAI.Task(taskType), false);
                    }
                    break;

                
                //ProductUnit
                case AntUnitAI.Task.Types.ProductUnit:
                    foreach( Unit unit in selectedUnits )
                    {
                        RTSBuildingAI intellect = unit.Intellect as RTSBuildingAI;
                        if( intellect == null )
                            continue;

                        if( IsEnableTaskTypeInTasks( intellect.GetControlPanelTasks(), taskType ) )
                            intellect.DoTask( new AntUnitAI.Task( taskType, tasks[ index ].Task.EntityType ), false );
                    }
                    break;

                //Move, TrailMove, Attack, Repair
                case AntUnitAI.Task.Types.Move:
                case AntUnitAI.Task.Types.TrailMove:
                case AntUnitAI.Task.Types.Collect:
                case AntUnitAI.Task.Types.Attack:
                case AntUnitAI.Task.Types.Repair:
                    //do taskTargetChoose
                    TaskTargetChooseIndex = index;
                    break;

                //BuildBuilding
                case AntUnitAI.Task.Types.BuildBuilding:
                    if (selectedUnits.Count == 1)
                    {
                        Unit unit = selectedUnits[0];
                        AntUnitAI intellect = unit.Intellect as AntUnitAI;
                        if (intellect != null)
                        {
                            //do taskTargetChoose
                            TaskTargetChooseIndex = index;

                            taskTargetBuildingType = (RTSBuildingType)tasks[index].Task.EntityType;

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

                            taskTargetBuildMeshObject = SceneManager.Instance.CreateMeshObject(meshName);
                            taskTargetBuildSceneNode = new SceneNode();
                            taskTargetBuildSceneNode.Attach(taskTargetBuildMeshObject);
                            taskTargetBuildSceneNode.Visible = false;
                        }
                    }
                    break;

            }
        }

        void UpdateControlPanel()
        {
            List<AntUnitAI.UserControlPanelTask> tasks = GetControlTasks();

            //check for need reset taskTargetChooseIndex
            if (TaskTargetChooseIndex != -1)
            {
                if (tasks == null || TaskTargetChooseIndex >= tasks.Count || !tasks[TaskTargetChooseIndex].Enable)
                    TaskTargetChooseIndex = -1;
            }

            for (int n = 0; ; n++)
            {
                EControl control = hudControl.Controls["ControlPanelButton" + n.ToString()];

                if (control == null)
                    break;

                control.Visible = tasks != null && n < tasks.Count;

                if (control.Visible)
                    if (tasks[n].Task.Type == AntUnitAI.Task.Types.None)
                        control.Visible = false;

                if (control.Visible)
                {
                    string text = null;

                    if (tasks[n].Task.EntityType != null)
                        text += tasks[n].Task.EntityType.FullName;

                    if (text == null)
                        text = tasks[n].Task.ToString();

                    control.Text = text;
                    control.Enable = tasks[n].Enable;

                    if (n == TaskTargetChooseIndex)
                        control.ColorMultiplier = new ColorValue(1, 0, 0);
                    else if (tasks[n].Active)
                        control.ColorMultiplier = new ColorValue(1, 1, 0);
                    else
                        control.ColorMultiplier = new ColorValue(1, 1, 1);
                }
            }
        }

        void DrawHUD(GuiRenderer renderer)
        {
            if (selectMode && selectDraggedMouse)
            {
                Rect rect = new Rect(selectStartPos);
                rect.Add(EngineApp.Instance.MousePosition);

                Vec2i windowSize = EngineApp.Instance.VideoMode;
                Vec2 thickness = new Vec2(1.0f / (float)windowSize.X, 1.0f / (float)windowSize.Y);

                renderer.AddQuad(new Rect(rect.Left, rect.Top + thickness.Y,
                    rect.Right, rect.Top + thickness.Y * 2), new ColorValue(0, 0, 0, .5f));
                renderer.AddQuad(new Rect(rect.Left, rect.Bottom,
                    rect.Right, rect.Bottom + thickness.Y), new ColorValue(0, 0, 0, .5f));
                renderer.AddQuad(new Rect(rect.Left + thickness.X, rect.Top,
                    rect.Left + thickness.X * 2, rect.Bottom), new ColorValue(0, 0, 0, .5f));
                renderer.AddQuad(new Rect(rect.Right, rect.Top,
                    rect.Right + thickness.X, rect.Bottom), new ColorValue(0, 0, 0, .5f));

                renderer.AddQuad(new Rect(rect.Left, rect.Top,
                    rect.Right, rect.Top + thickness.Y), new ColorValue(0, 1, 0, 1));
                renderer.AddQuad(new Rect(rect.Left, rect.Bottom - thickness.Y,
                    rect.Right, rect.Bottom), new ColorValue(0, 1, 0, 1));
                renderer.AddQuad(new Rect(rect.Left, rect.Top,
                    rect.Left + thickness.X, rect.Bottom), new ColorValue(0, 1, 0, 1));
                renderer.AddQuad(new Rect(rect.Right - thickness.X, rect.Top,
                    rect.Right, rect.Bottom), new ColorValue(0, 1, 0, 1));
            }
        }

        //Draw minimap
        void Minimap_RenderUI(EControl sender, GuiRenderer renderer)
        {
            Rect screenMapRect = sender.GetScreenRectangle();

            Bounds initialBounds = Map.Instance.InitialCollisionBounds;
            Rect mapRect = new Rect(initialBounds.Minimum.ToVec2(), initialBounds.Maximum.ToVec2());

            Vec2 mapSizeInv = new Vec2(1, 1) / mapRect.Size;

            //draw units
            Vec2 screenPixel = new Vec2(1, 1) / new Vec2(EngineApp.Instance.VideoMode.ToVec2());

            foreach (Entity entity in Map.Instance.Children)
            {
                RTSUnit unit = entity as RTSUnit;
                if (unit == null)
                    continue;

                Rect rect = new Rect(unit.MapBounds.Minimum.ToVec2(), unit.MapBounds.Maximum.ToVec2());

                rect -= mapRect.Minimum;
                rect.Minimum *= mapSizeInv;
                rect.Maximum *= mapSizeInv;
                rect.Minimum = new Vec2(rect.Minimum.X, 1.0f - rect.Minimum.Y);
                rect.Maximum = new Vec2(rect.Maximum.X, 1.0f - rect.Maximum.Y);
                rect.Minimum *= screenMapRect.Size;
                rect.Maximum *= screenMapRect.Size;
                rect += screenMapRect.Minimum;

                //increase 1 pixel
                rect.Maximum += new Vec2(screenPixel.X, -screenPixel.Y);

                ColorValue color;

                if (playerFaction == null || unit.Intellect == null || unit.Intellect.Faction == null)
                    color = new ColorValue(1, 1, 0);
                else if (playerFaction == unit.Intellect.Faction)
                    color = new ColorValue(0, 1, 0);
                else
                    color = new ColorValue(1, 0, 0);

                renderer.AddQuad(rect, color);
            }

            //Draw camera borders
            {
                Camera camera = RendererWorld.Instance.DefaultCamera;

                if (camera.Position.Z > 0)
                {

                    Plane groundPlane = new Plane(0, 0, 1, 0);

                    Vec2[] points = new Vec2[4];

                    for (int n = 0; n < 4; n++)
                    {
                        Vec2 p = Vec2.Zero;

                        switch (n)
                        {
                            case 0: p = new Vec2(0, 0); break;
                            case 1: p = new Vec2(1, 0); break;
                            case 2: p = new Vec2(1, 1); break;
                            case 3: p = new Vec2(0, 1); break;
                        }

                        Ray ray = camera.GetCameraToViewportRay(p);

                        float scale;
                        groundPlane.RayIntersection(ray, out scale);

                        Vec3 pos = ray.GetPointOnRay(scale);
                        if (ray.Direction.Z > 0)
                            pos = ray.Origin + ray.Direction.GetNormalize() * 10000;

                        Vec2 point = pos.ToVec2();

                        point -= mapRect.Minimum;
                        point *= mapSizeInv;
                        point = new Vec2(point.X, 1.0f - point.Y);
                        point *= screenMapRect.Size;
                        point += screenMapRect.Minimum;

                        points[n] = point;
                    }

                    for (int n = 0; n < 4; n++)
                        renderer.AddLine(points[n], points[(n + 1) % 4], new ColorValue(1, 1, 1),
                            screenMapRect);
                }
            }
        }

        protected override void OnRenderUI(GuiRenderer renderer)
        {
            base.OnRenderUI(renderer);
            DrawHUD(renderer);
        }

        CameraType GetRealCameraType()
        {
            return cameraType;
        }

        public void ClearEntitySelection()
        {
            while (selectedUnits.Count != 0)
                SetEntitySelected(selectedUnits[selectedUnits.Count - 1], false);
        }

        public void SetEntitySelected(Unit entity, bool selected)
        {
            bool modified = false;

            if (selected)
            {
                if (!selectedUnits.Contains(entity))
                {
                    selectedUnits.Add(entity);
                    modified = true;
                }
            }
            else
                modified = selectedUnits.Remove(entity);

            //if( modified )
            //{
            //}
        }

        int TaskTargetChooseIndex
        {
            get { return taskTargetChooseIndex; }
            set
            {
                taskTargetChooseIndex = value;

                if (taskTargetChooseIndex == -1 && taskTargetBuildMeshObject != null)
                {
                    taskTargetBuildSceneNode.Detach(taskTargetBuildMeshObject);
                    taskTargetBuildMeshObject.Dispose();
                    taskTargetBuildMeshObject = null;
                    taskTargetBuildSceneNode.Dispose();
                    taskTargetBuildSceneNode = null;
                }
            }
        }

        Vec2 GetMapPositionByMouseOnMinimap()
        {
            Rect screenMapRect = minimapControl.GetScreenRectangle();

            Bounds initialBounds = Map.Instance.InitialCollisionBounds;
            Rect mapRect = new Rect(initialBounds.Minimum.ToVec2(), initialBounds.Maximum.ToVec2());

            Vec2 point = MousePosition;

            point -= screenMapRect.Minimum;
            point /= screenMapRect.Size;
            point = new Vec2(point.X, 1.0f - point.Y);
            point *= mapRect.Size;
            point += mapRect.Minimum;

            return point;
        }

        void UpdateHUDControlIcon()
        {
            string iconName = null;
            if (selectedUnits.Count != 0)
                iconName = selectedUnits[0].Type.Name;

            EControl control = hudControl.Controls["ControlUnitIcon"];

            if (!string.IsNullOrEmpty(iconName))
            {
                string fileName = string.Format("Gui\\HUD\\Icons\\{0}.png", iconName);

                bool needUpdate = false;

                if (control.BackTexture != null)
                {
                    string current = control.BackTexture.Name;
                    current = current.Replace('/', '\\');

                    if (string.Compare(fileName, current, true) != 0)
                        needUpdate = true;
                }
                else
                    needUpdate = true;

                if (needUpdate)
                {
                    if (VirtualFile.Exists(fileName))
                        control.BackTexture = TextureManager.Instance.Load(fileName, Texture.Type.Type2D, 0);
                    else
                        control.BackTexture = null;
                }
            }
            else
                control.BackTexture = null;
        }

        protected override void OnGetCameraTransform(out Vec3 position, out Vec3 forward,
            out Vec3 up, ref Degree cameraFov)
        {
            Vec3 offset;
            {
                Quat rot = new Angles(0, 0, MathFunctions.RadToDeg(
                    cameraDirection.Horizontal)).ToQuat();
                rot *= new Angles(0, MathFunctions.RadToDeg(cameraDirection.Vertical), 0).ToQuat();
                offset = rot * new Vec3(1, 0, 0);
                offset *= cameraDistance;
            }
            Vec3 lookAt = new Vec3(cameraPosition.X, cameraPosition.Y, 0);

            position = lookAt + offset;
            forward = -offset;
            up = new Vec3(0, 0, 1);
        }
    }
}
