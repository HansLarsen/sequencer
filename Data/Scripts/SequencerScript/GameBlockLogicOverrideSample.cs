using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;



namespace PepiHax
{
    public class ActionStorage : IEquatable<ActionStorage>, IComparable<ActionStorage> //I couldnt make any sense of the action system, so i'm just storing both a reference to the block and the action it self.
    {
        public IMyCubeBlock block;
        public long ownerInside;
        public String terminalActions;
        public int timeUntilNew = 1;
        public int index = -1;
        public bool isTrigger = false;
        public bool currentlyAt = false;
        public IMyBlockGroup blocks;
        public bool isGroup = false;
        public bool isPist = false;
        public bool isStat = false;
        public int degressExtendiness = 1;

        public ActionStorage(IMySlimBlock owner, IMySlimBlock saveBlock, ITerminalAction actionForBlock, int time, int id)
        {
            block = saveBlock.FatBlock;
            terminalActions = actionForBlock.Id;
            timeUntilNew = time;
            index = id;
            ownerInside = owner.FatBlock.EntityId;
        }
        public ActionStorage(IMySlimBlock owner, IMySlimBlock saveBlock, int id)
        {
            block = saveBlock.FatBlock;
            index = id;
            isTrigger = true;
            ownerInside = owner.FatBlock.EntityId;
        }
        public ActionStorage(IMySlimBlock owner, IMyBlockGroup saveBlock, String actionForBlock, int time, int id)
        {
            blocks = saveBlock;
            isGroup = true;
            timeUntilNew = time;
            index = id;
            terminalActions = actionForBlock;
            ownerInside = owner.FatBlock.EntityId;
        }
        public ActionStorage(IMySlimBlock owner, IMySlimBlock saveBlock, bool isPiston, int extendiness, int time, int id)
        {
            block = saveBlock.FatBlock;
            if (isPiston) isPist = true;
            else isStat = true;
            timeUntilNew = time;
            index = id;
            ownerInside = owner.FatBlock.EntityId;
        }
        public IMySlimBlock GetBlock()
        {
            if (isGroup) return null;
            return block.SlimBlock;
        }
        public int GetTime()
        {
            return timeUntilNew;
        }
        public void SetTime(int time)
        {
            timeUntilNew = time;
        }
        public void TriggerNow()
        {

            if (isTrigger) return;
            else if (isPist && terminalActions == "Length")
            {
                IMyPistonBase pistonBase = block as IMyPistonBase;
                pistonBase.MaxLimit = degressExtendiness;
                pistonBase.MinLimit = degressExtendiness;
                return;
            }
            else if (isStat && terminalActions == "UpperDegress")
            {
                IMyMotorStator motorStator = block as IMyMotorStator;
                motorStator.UpperLimitDeg = ((float)degressExtendiness * (float)Math.PI) / 180;
                MyLog.Default.WriteLineAndConsole(motorStator.UpperLimitDeg.ToString() + ":" + (float)degressExtendiness);
                return;
            }
            else if (isStat && terminalActions == "LowerDegress")
            {
                IMyMotorStator motorStator = block as IMyMotorStator;
                motorStator.LowerLimitRad = ((float)degressExtendiness * (float)Math.PI) / 180;
                MyLog.Default.WriteLineAndConsole(motorStator.LowerLimitDeg.ToString() + ":" + (float)degressExtendiness);
                return;
            }
            else if (isGroup)
            {
                List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
                blocks.GetBlocks(groupBlocks);
                MyLog.Default.WriteLineAndConsole("TerminalAction: " + terminalActions);
                MyLog.Default.WriteLineAndConsole(groupBlocks.ToString());
                foreach (IMyTerminalBlock blockssss in groupBlocks)
                {
                    MyLog.Default.WriteLineAndConsole(blockssss.ToString());
                    ITerminalAction tempTerminalActions = MyAPIGateway.TerminalActionsHelper.GetActionWithName(terminalActions, blockssss.GetType());
                    tempTerminalActions.Apply(blockssss);
                }
                return;
            }
            MyLog.Default.WriteLineAndConsole(ownerInside.ToString() + terminalActions);
            ITerminalAction tempTerminalAction2 = MyAPIGateway.TerminalActionsHelper.GetActionWithName(terminalActions, block.GetType());
            tempTerminalAction2.Apply(block);
            return;
        }
        public string Name()
        {
            if (isGroup)
            {
                List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
                blocks.GetBlocks(groupBlocks);
                foreach (IMyTerminalBlock blockssss in groupBlocks)
                {
                    ITerminalAction tempTerminalActions = MyAPIGateway.TerminalActionsHelper.GetActionWithName(terminalActions, blockssss.GetType());
                    return index + ": " + blocks.Name.ToString() + ": " + tempTerminalActions.Name;
                }
            }
            return this.ToString();
        }
        public override string ToString()
        {
            if (isTrigger) return index + " : Trigger";
            else if (isGroup) return index + ": " + blocks.Name.ToString() + ": " + terminalActions;
            return index + " : " + block.DisplayNameText + ": " + terminalActions;
        }
        public bool IsTrigger()
        {
            return isTrigger;
        }
        public bool Equals(ActionStorage obj)
        {
            if (this.isTrigger == obj.isTrigger && this.blocks == obj.blocks && this.index == obj.index) return true;
            else if (this.isTrigger == obj.isTrigger && this.block == obj.block && this.index == obj.index) return true;
            else if (this.terminalActions == obj.terminalActions && this.block == obj.block && this.index == obj.index) return true;
            return false;
        }
        public int CompareTo(ActionStorage obj)
        {
            if (obj == null) return 1;
            return this.index.CompareTo(obj.index);
        }
        public IMyCubeBlock GetOwner()
        {
            return MyAPIGateway.Entities.GetEntityById(ownerInside) as IMyCubeBlock;
        }
    }

    public class LocalStorage<Type> //A Storage system that stores via block id.
    {
        long[] blockIDs = new long[255];
        Type[] intersave = new Type[255];
        int id = 0;

        public LocalStorage()
        {
        }

        public LocalStorage(long[] blockID, Type[] interSave)
        {
            intersave = interSave;
            blockIDs = blockID;
        }

        public long[] GetAllIds()
        {
            return blockIDs;
        }

        public Type[] GetAllSaved()
        {
            return intersave;
        }

        public void Add(IMyCubeBlock block, Type save)
        {
            if (id == 255)
            {
                throw new System.ArgumentException("More then 255 sequencers present");
            }
            for (int i = 0; i < 255; i++)
            {
                if (blockIDs[i] == block.EntityId)
                {
                    intersave[i] = save;
                    return;
                }
            }

            MyLog.Default.WriteLineAndConsole("----------------ID: " + id);
            blockIDs[id] = block.EntityId;
            intersave[id] = save;
            MyLog.Default.WriteLineAndConsole("--------Type:" + save.GetType());
            id++;
            return;
        }
        public Type Get(IMyCubeBlock block)
        {
            for (int i = 0; i < 254; i++)
            {
                if (blockIDs[i] == block.EntityId)
                {
                    return intersave[i];
                }
            }
            return default(Type);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_VirtualMass), false, "LargeTimerSequencer")]
    public class SequencerComponent : MyGameLogicComponent
    {
        MyObjectBuilder_EntityBase m_objectBuilder = null;
        public static IMyTerminalBlock myTerminalBlock;
        public static IMyCubeGrid myCubeGrid;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            (Entity as IMyTerminalBlock).RefreshCustomInfo();
            myTerminalBlock = Entity as IMyTerminalBlock;
            myCubeGrid = myTerminalBlock.CubeGrid;

            base.Init(objectBuilder);
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return m_objectBuilder;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SequencerSession : MySessionComponentBase
    {
        bool first = true; //The init class runs before the terminal elements is made, so we have to make our owne init in the updater.
        Random rand = new Random();
        List<string> blockSubtypes = new List<string>(); //I'm using Lucas blocksubtypeid checker so that the display elements dosent appear on the normal virtual mass block.
        List<string>[] typesSupported = new List<string>[5]; // Unclean names, can either be subtypeid or just the type id. ( This was because the airtight hangar door dosent have a subtypeid ).
        String[] messages = new String[5]; // The array with the cleaned names.
        IMyTerminalControlListbox box2; // These are saved so that we can update them.
        IMyTerminalControlListbox box3;
        IMyTerminalControlListbox box4;
        IMyTerminalControlSlider slider;
        IMyTerminalControlSlider slider2;
        LocalStorage<int> currentTime = new LocalStorage<int>(); // The current slider time, used to update the slider.
        LocalStorage<int> currentDeg = new LocalStorage<int>(); // The current slider time, used to update the slider.
        LocalStorage<ConcurrentBag<IMySlimBlock>> localBlockStorage = new LocalStorage<ConcurrentBag<IMySlimBlock>>(); // This is the grid and it is all stored here.
        LocalStorage<ConcurrentBag<MyTerminalControlListBoxItem>> SelectedBlockTypes = new LocalStorage<ConcurrentBag<MyTerminalControlListBoxItem>>(); // When door is selected, they are all sorted and stored here.
        LocalStorage<ConcurrentBag<ActionStorage>> totalStoreSaves = new LocalStorage<ConcurrentBag<ActionStorage>>(); // These are the stored actions that apprear in the last menu, and is looped thourgh.
        LocalStorage<List<MyTerminalControlListBoxItem>> selectedStoredAction = new LocalStorage<List<MyTerminalControlListBoxItem>>(); // The actions thats is currently selected.
        LocalStorage<bool> trigger = new LocalStorage<bool>(); // This stores weather the block has been triggered or not.
        Stopwatch stopwatch = new Stopwatch();  //This is used to keep the time since start, need to find another way of doing this.
        LocalStorage<long> nextTriggerTime = new LocalStorage<long>();  //The time at which the next trigger is supose to happen. Is also stored per block.
        LocalStorage<List<IMyBlockGroup>> blockGroups = new LocalStorage<List<IMyBlockGroup>>(); //The groups found on the current grid.
        LocalStorage<List<IMyBlockGroup>> selectedBlockGroups = new LocalStorage<List<IMyBlockGroup>>(); //The selected block groups from the current grid.
        string[] names = { "block", "ownerInside", "terminalActions", "timeUntilNew", "index", "isTrigger", "currentlyAt", "blocks", "isGroup" }; //save names for the arrays, because i couldnt get anything else to work.
        Guid GetGuid = new Guid("31A64912-4276-40C4-BAED-E3F2343B56B3");

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            for (int i = 0; i < typesSupported.Length; i++)
            {
                typesSupported[i] = new List<string>();
            }

            typesSupported[0].Add("SmallLight");
            typesSupported[0].Add("SmallBlockSmallLight");
            typesSupported[0].Add("LargeBlockLight_1corner");
            typesSupported[0].Add("LargeBlockLight_2corner");
            typesSupported[0].Add("SmallBlockLight_1corner");
            typesSupported[0].Add("SmallBlockLight_2corner");
            typesSupported[1].Add("LargeStator");
            typesSupported[1].Add("LargeRotor");
            typesSupported[1].Add("SmallStator");
            typesSupported[1].Add("SmallRotor");
            typesSupported[1].Add("LargeAdvancedRotor");
            typesSupported[1].Add("SmallAdvancedRotor");
            typesSupported[2].Add("LargePistonBase");
            typesSupported[2].Add("SmallPistonBase");
            typesSupported[2].Add("MyObjectBuilder_MotorStator");
            typesSupported[3].Add("LargeBlockSlideDoor");
            typesSupported[3].Add("MyObjectBuilder_AirtightHangarDoor");
            typesSupported[3].Add("");
            typesSupported[3].Add("");

            messages[0] = "Light";
            messages[1] = "Stator";
            messages[2] = "Piston";
            messages[3] = "Door";
            messages[4] = "Lights Out!";

            stopwatch.Start();

            base.Init(sessionComponent);
        }

        public override void UpdateBeforeSimulation()
        {
            if (first == true)
            {
                first = false;

                blockSubtypes.Add("LargeTimerSequencer");
                blockSubtypes.Add("SmallTimerSequencer");
                #region Controls
                //Adds a ControlList which containes the types
                IMyTerminalControlListbox box1 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyArtificialMassBlock>("List: 1");
                box1.Enabled = Block => SubtypeIdCheck(Block);
                box1.Visible = Block => SubtypeIdCheck(Block);
                box1.SupportsMultipleBlocks = false;
                box1.Multiselect = true;
                box1.ListContent = SelectionBox;
                box1.VisibleRowsCount = 4;
                box1.ItemSelected = SelectedType;
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(box1);

                //Groups.
                box4 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyArtificialMassBlock>("List: 4");
                box4.Enabled = Block => SubtypeIdCheck(Block);
                box4.Visible = Block => SubtypeIdCheck(Block);
                box4.Multiselect = true;
                box4.ListContent = box4List;
                box4.VisibleRowsCount = 4;
                box4.ItemSelected = box4Selected;
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(box4);

                //Adds a ControlList whichs containes the item names with their actions.
                box2 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyArtificialMassBlock>("List: 2");
                box2.Enabled = Block => SubtypeIdCheck(Block);
                box2.Visible = Block => SubtypeIdCheck(Block);
                box2.Multiselect = true;
                box2.ListContent = SelectionBoxActions;
                box2.VisibleRowsCount = 10;
                box2.ItemSelected = SelectedAction;
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(box2);

                //Separator
                var sepB = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyArtificialMassBlock>("SeparatorB");
                sepB.Enabled = Block => true;
                sepB.Visible = Block => SubtypeIdCheck(Block);
                //blockButton.Title = MyStringId.GetOrCompute("Replace Colors");
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(sepB);

                //Time Slider
                slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyArtificialMassBlock>("Silder");
                slider.Enabled = Block => SubtypeIdCheck(Block);
                slider.Visible = Block => SubtypeIdCheck(Block);
                slider.Title = MyStringId.GetOrCompute("Time");
                slider.Tooltip = MyStringId.GetOrCompute("Interval between actions");
                slider.SetLimits(0, 25);
                slider.Writer = SliderWriter;
                slider.Getter = SliderGetter;
                slider.Setter = SliderSetter;
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(slider);

                slider2 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyArtificialMassBlock>("Silder: 2");
                slider2.Enabled = Block => SubtypeIdCheck(Block);
                slider2.Visible = Block => SubtypeIdCheck(Block);
                slider2.Title = MyStringId.GetOrCompute("Length / Degress");
                slider2.Tooltip = MyStringId.GetOrCompute("Interval between actions");
                slider2.SetLimits(0, 360);
                slider2.Writer = Slider2Writer;
                slider2.Getter = Slider2Getter;
                slider2.Setter = Slider2Setter;
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(slider2);

                //Adds a ControlList whichs containes the item names with their actions.
                box3 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyArtificialMassBlock>("List: 3");
                box3.Enabled = Block => SubtypeIdCheck(Block);
                box3.Visible = Block => SubtypeIdCheck(Block);
                box3.Multiselect = true;
                box3.ListContent = box3List;
                box3.VisibleRowsCount = 10;
                box3.ItemSelected = box3Selected;
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(box3);

                //Removes an Action
                IMyTerminalControlButton blockButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyArtificialMassBlock>("Remove Button");
                blockButton.Enabled = Block => true;
                blockButton.Visible = Block => SubtypeIdCheck(Block);
                blockButton.Title = MyStringId.GetOrCompute("Remove");
                blockButton.Action = Remove;
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(blockButton);

                //Store Color 1
                IMyTerminalControlButton blockButton2 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyArtificialMassBlock>("Remove Button");
                blockButton2.Enabled = Block => true;
                blockButton2.Visible = Block => SubtypeIdCheck(Block);
                blockButton2.Title = MyStringId.GetOrCompute("Test");
                blockButton2.Action = Test;
                MyAPIGateway.TerminalControls.AddControl<IMyArtificialMassBlock>(blockButton2);

                //Adds action to the block, there can be used by other blocks to trigger this one.
                IMyTerminalAction replaceAction = MyAPIGateway.TerminalControls.CreateAction<IMyArtificialMassBlock>("Run Once");
                replaceAction.Enabled = Block => SubtypeIdCheck(Block);
                replaceAction.Action = RunOnce;
                replaceAction.Name = new StringBuilder("Run Once");
                MyAPIGateway.TerminalControls.AddAction<IMyArtificialMassBlock>(replaceAction);

                replaceAction = MyAPIGateway.TerminalControls.CreateAction<IMyArtificialMassBlock>("Run Forever");
                replaceAction.Enabled = Block => SubtypeIdCheck(Block);
                replaceAction.Action = RunForever;
                replaceAction.Name = new StringBuilder("Run Forever");
                MyAPIGateway.TerminalControls.AddAction<IMyArtificialMassBlock>(replaceAction);
                #endregion

                LoadData();
            }
            Updater();
        }

        public void LoadData()
        {

            MyLog.Default.WriteLineAndConsole("--------------Sequencer started Loading----------------------------");

            List<long> entityIds;
            if (MyAPIGateway.Utilities.GetVariable<List<long>>("Sequencer", out entityIds) == true)
            {
                for (int i = 0; i < entityIds.Count; i++)
                {
                    if (entityIds[i] == 0) continue;
                    int[] lengths = new int[names.Length];

                    for (int a = 0; a < lengths.Length; a++)
                    {
                        lengths[a] = new int();
                    }

                    MyLog.Default.WriteLineAndConsole("Cake");

                    long length;
                    MyAPIGateway.Utilities.GetVariable("length:" + entityIds[i], out length);

                    ConcurrentBag<ActionStorage> actionStorages = new ConcurrentBag<ActionStorage>();

                    List<long> indtruder;
                    MyAPIGateway.Utilities.GetVariable("indexes:" + entityIds[i], out indtruder);

                    List<long> block;
                    MyAPIGateway.Utilities.GetVariable("block:" + entityIds[i], out block);

                    List<string> terminalActions;
                    MyAPIGateway.Utilities.GetVariable("terminalActions:" + entityIds[i], out terminalActions);

                    List<long> timeUntilNew;
                    MyAPIGateway.Utilities.GetVariable("time:" + entityIds[i], out timeUntilNew);

                    List<string> isTrigger;
                    MyAPIGateway.Utilities.GetVariable("isTrigger:" + entityIds[i], out isTrigger);

                    List<string> currentlyAt;
                    MyAPIGateway.Utilities.GetVariable("currentlyAt:" + entityIds[i], out currentlyAt);

                    List<string> blocks;
                    MyAPIGateway.Utilities.GetVariable("blocks:" + entityIds[i], out blocks);

                    List<string> isGroup;
                    MyAPIGateway.Utilities.GetVariable("isGroup:" + entityIds[i], out isGroup);

                    List<string> isPist;
                    MyAPIGateway.Utilities.GetVariable("isPist:" + entityIds[i], out isPist);

                    List<string> isStat;
                    MyAPIGateway.Utilities.GetVariable("isStat:" + entityIds[i], out isStat);

                    List<long> degressExtendiness;
                    MyAPIGateway.Utilities.GetVariable("degRess:" + entityIds[i], out degressExtendiness);

                    if (block == null)
                    {
                        MyLog.Default.WriteLineToConsole("NullBlock");
                    }
                    if (indtruder == null)
                    {
                        MyLog.Default.WriteLineAndConsole("Nulldetected");
                    }
                    if (timeUntilNew == null)
                    {
                        MyLog.Default.WriteLineAndConsole("NullTime");
                    }
                    MyLog.Default.WriteLineAndConsole("timeUntilNew: " + timeUntilNew.Count());
                    MyLog.Default.WriteLineAndConsole("block: " + block.Count() + " index: " + indtruder.Count() + " Length: " + length);

                    IMyCubeBlock owner = MyAPIGateway.Entities.GetEntityById(entityIds[i]) as IMyCubeBlock;

                    MyLog.Default.WriteLineAndConsole(owner.ToString());

                    for (int u = 0; u < length; u++)
                    {
                        try
                        {
                            ActionStorage actionStorage;
                            if (string.Equals(isStat[u],"True"))
                            {
                                MyLog.Default.WriteLineAndConsole(indtruder[u].ToString() + " :Rotor " + degressExtendiness[u] + " :Degress" );
                                IMyCubeBlock cubeBlock = MyAPIGateway.Entities.GetEntityById(block[u]) as IMyCubeBlock;
                                actionStorage = new ActionStorage(owner.SlimBlock, cubeBlock.SlimBlock, false, (int)degressExtendiness[u], (int)timeUntilNew[u], (int)indtruder[u]);
                                actionStorage.terminalActions = terminalActions[u];
                                actionStorage.degressExtendiness = (int)degressExtendiness[u];
                            }
                            else if (isPist[u] == "True")
                            {
                                MyLog.Default.WriteLineAndConsole(indtruder[u].ToString() + " :Piston");
                                IMyCubeBlock cubeBlock = MyAPIGateway.Entities.GetEntityById(block[u]) as IMyCubeBlock;
                                actionStorage = new ActionStorage(owner.SlimBlock, cubeBlock.SlimBlock, true, (int)degressExtendiness[u], (int)timeUntilNew[u], (int)indtruder[u]);
                            }
                            else if (isTrigger[u] == "True")
                            {
                                MyLog.Default.WriteLineAndConsole(indtruder[u].ToString() + ": " + owner.BlockDefinition.ToString() + " :Trigger");
                                actionStorage = new ActionStorage(owner.SlimBlock, owner.SlimBlock, (int)indtruder[u]);
                            }
                            else if (isGroup[u] == "True")
                            {
                                MyLog.Default.WriteLineAndConsole("Sequencer: isGroup" + u);
                                IMyCubeGrid cubeGrid = owner.CubeGrid;
                                IMyGridTerminalSystem gridTerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
                                IMyBlockGroup blockGroup = gridTerminalSystem.GetBlockGroupWithName(blocks[u]);
                                actionStorage = new ActionStorage(owner.SlimBlock, blockGroup, terminalActions[u], (int)timeUntilNew[u], (int)indtruder[u]);
                            }
                            else
                            {
                                MyLog.Default.WriteLineAndConsole("Sequencer: else: " + u + " isStator:" + isStat[u] + " isPiston:" + isPist[u] + " isTrigger:" + isTrigger[u]+ " isGroup:" + isGroup[u] + " :" + string.Equals(isStat[u], "True"));
                                IMyCubeBlock cubeBlock = MyAPIGateway.Entities.GetEntityById(block[u]) as IMyCubeBlock;
                                MyLog.Default.WriteLineAndConsole("Type: " + cubeBlock.GetType());
                                ITerminalAction terminalAction = MyAPIGateway.TerminalActionsHelper.GetActionWithName(terminalActions[u], cubeBlock.GetType());
                                actionStorage = new ActionStorage(owner.SlimBlock, cubeBlock.SlimBlock, terminalAction, (int)timeUntilNew[u], (int)indtruder[u]);
                            }

                            if (timeUntilNew[u] != null) actionStorage.timeUntilNew = (int)timeUntilNew[u];

                            if (currentlyAt[u] != null)
                            {
                                if (currentlyAt[u] == "true")
                                {
                                    actionStorage.currentlyAt = true;
                                }
                            }
                            actionStorages.Add(actionStorage);
                        }
                        catch
                        {
                            MyLog.Default.WriteLineAndConsole("We Failed");
                        }
                    }
                    totalStoreSaves.Add(owner, actionStorages);
                }
            }

            MyLog.Default.WriteLineAndConsole("--------------Sequencer stopped Loading----------------------------");
            return;
        }

        public override void SaveData()
        {
            Save();
        }

        public void Save()
        {
            MyLog.Default.WriteLineAndConsole("Sequencer: ----------------Started Saving----------------");

            ConcurrentBag<ActionStorage>[] tempItems = totalStoreSaves.GetAllSaved();
            if (tempItems == null ) return;
            long[] ids = totalStoreSaves.GetAllIds();
            MyLog.Default.WriteLineAndConsole(ids.Count() + ":" + tempItems.Count());
            if (ids == null ) return;

            MyLog.Default.WriteLineAndConsole(ids.Count() + ":" + tempItems.Count());
            List<long> entityIds = new List<long>();
            foreach (long id in ids)
            {
                if (id != 0)
                    entityIds.Add(id);
            }

            foreach (ConcurrentBag<ActionStorage> toBeSaved in tempItems)
            {
                if (toBeSaved == null) continue;
                List<ActionStorage> toBeSavedAsList = new List<ActionStorage>(toBeSaved);
                int length = toBeSavedAsList.Count();

                List<long> block = new List<long>(length);
                long ownerInside = new long();
                List<string> terminalActions = new List<string>(length);
                List<long> timeUntilNew = new List<long>(length);
                List<long> index = new List<long>(length);
                List<string> isTrigger = new List<string>(length);
                List<string> currentlyAt = new List<string>(length);
                List<string> blocks = new List<string>(length);
                List<string> isGroup = new List<string>(length);
                List<string> isPist = new List<string>();
                List<string> isStat = new List<string>();
                List<long> degressExtendiness = new List<long>();


                MyLog.Default.WriteLineAndConsole("Length: " + length);
                for (int i = 0; i < length; i++)
                {
                    if (toBeSavedAsList[i] == null) continue;

                    if (toBeSavedAsList[i].block != null) block.Add(toBeSavedAsList[i].block.EntityId);
                    else { block.Add(1L); }

                    if (toBeSavedAsList[i].ownerInside != null) ownerInside = toBeSavedAsList[i].ownerInside;
                    else { ownerInside = 0; }

                    if (toBeSavedAsList[i].terminalActions != null) terminalActions.Add(toBeSavedAsList[i].terminalActions);
                    else { terminalActions.Add("Nope"); }

                    if (toBeSavedAsList[i].timeUntilNew != null) timeUntilNew.Add(toBeSavedAsList[i].timeUntilNew);
                    else { timeUntilNew.Add(0); }

                    if (toBeSavedAsList[i].index != null) index.Add(toBeSavedAsList[i].index);
                    else { index.Add(0); }

                    MyLog.Default.WriteLineAndConsole(toBeSavedAsList[i].index.ToString());

                    if (toBeSavedAsList[i].isTrigger != null) isTrigger.Add(toBeSavedAsList[i].isTrigger.ToString());
                    else { isTrigger.Add("False"); }

                    if (toBeSavedAsList[i].currentlyAt != null) currentlyAt.Add(toBeSavedAsList[i].currentlyAt.ToString());
                    else { currentlyAt.Add("False"); }

                    if (toBeSavedAsList[i].blocks != null) blocks.Add(toBeSavedAsList[i].blocks.Name);
                    else { blocks.Add("Nope"); }

                    if (toBeSavedAsList[i].isGroup != null) isGroup.Add(toBeSavedAsList[i].isGroup.ToString());
                    else { isGroup.Add("False"); }

                    isPist.Add(toBeSavedAsList[i].isPist.ToString());

                    isStat.Add(toBeSavedAsList[i].isStat.ToString());

                    degressExtendiness.Add((long)toBeSavedAsList[i].degressExtendiness);

                    MyLog.Default.WriteLineAndConsole(toBeSavedAsList[i].ToString() + ":" + "End");
                }

                //try
                //{
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "indExes" + ":" + ownerInside, index);
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "length" + ":" + ownerInside, length);
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "block" + ":" + ownerInside, block);
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "terminalActions" + ":" + ownerInside, terminalActions);
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "timeUntilNew" + ":" + ownerInside, timeUntilNew);
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "isTrigger" + ":" + ownerInside, isTrigger);
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "currentlyAt" + ":" + ownerInside, currentlyAt);
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "blocks" + ":" + ownerInside, blocks);
                //    MyAPIGateway.Utilities.SetVariable("Sequencer:" + "isGroup" + ":" + ownerInside, isGroup);

                    
                //}
                //catch
                //{ }

                foreach(int temp in timeUntilNew)
                {
                    MyLog.Default.WriteLineAndConsole(temp.ToString());
                }

                MyAPIGateway.Utilities.SetVariable("indexes:" + ownerInside, index);
                MyAPIGateway.Utilities.SetVariable("length:" + ownerInside, (long)length);
                MyAPIGateway.Utilities.SetVariable("block:" + ownerInside, block);
                MyAPIGateway.Utilities.SetVariable("terminalActions:" + ownerInside, terminalActions);
                MyAPIGateway.Utilities.SetVariable("time:" + ownerInside, timeUntilNew);
                MyAPIGateway.Utilities.SetVariable("isTrigger:" + ownerInside, isTrigger);
                MyAPIGateway.Utilities.SetVariable("currentlyAt:" + ownerInside, currentlyAt);
                MyAPIGateway.Utilities.SetVariable("blocks:" + ownerInside, blocks);
                MyAPIGateway.Utilities.SetVariable("isGroup:" + ownerInside, isGroup);
                MyAPIGateway.Utilities.SetVariable("isPist:" + ownerInside, isPist);
                MyAPIGateway.Utilities.SetVariable("isStat:" + ownerInside, isStat);
                MyAPIGateway.Utilities.SetVariable("degRess:" + ownerInside, degressExtendiness);

                MyLog.Default.WriteLineAndConsole("Sequencer Wrote:" +
                        " block:" + block.Count() +
                        " terminalAction:" + terminalActions.Count() +
                        " timeUntilNew:" + timeUntilNew.Count() +
                        " indexes:" + index.Count() +
                        " isTrigger:" + isTrigger.Count() +
                        " currentlyAt:" + currentlyAt.Count() +
                        " blocks:" + blocks.Count() +
                        " isGroup:" + isGroup.Count() +
                        " isPist:" + isPist.Count() +
                        " isStat:" + isStat.Count() +
                        " degRess:" + degressExtendiness.Count()
                        );

                MyLog.Default.WriteLineAndConsole("Sequencer: --------------Done Writing to world file-----------");

            }
            try
            {
                MyLog.Default.WriteLineAndConsole("SecondWrite");
                if (entityIds != null)
                    MyAPIGateway.Utilities.SetVariable("Sequencer", entityIds);
            }
            catch
            { }
            MyLog.Default.WriteLineAndConsole("Sequencer: ----------------Done Saving----------------");
        }

        #region Time slider
        void SliderWriter(IMyTerminalBlock block, StringBuilder tempBuilder)
        {
            tempBuilder.Clear();
            tempBuilder.Append(SliderGetter(block).ToString());
            tempBuilder.Append(" Sec");
            return;
        }
        float SliderGetter(IMyTerminalBlock block)
        {
            float storedValue = 0f;

            if (currentTime.Get(block) == null) return -1f;
            int tempTime = currentTime.Get(block);

            storedValue = (float)tempTime;

            return storedValue;
        }
        void SliderSetter(IMyTerminalBlock block, float sliderValue)
        {
            float roundedValue = (float)Math.Round(sliderValue, 0, MidpointRounding.AwayFromZero);

            if (roundedValue > 25)
            {
                roundedValue = 25;
            }

            if (roundedValue < 0)
            {
                roundedValue = 0f;
            }

            List<MyTerminalControlListBoxItem> selectedTempAction; ;
            selectedTempAction = selectedStoredAction.Get(block);
            if (selectedTempAction == null) return;
            ConcurrentBag<ActionStorage> tempActionStorage = totalStoreSaves.Get(block);
            if (tempActionStorage == null) return;
            List<ActionStorage> tempList = new List<ActionStorage>(tempActionStorage);
            int count = selectedTempAction.Count();
            foreach (MyTerminalControlListBoxItem temps2 in selectedTempAction)
            {
                foreach (ActionStorage temps in tempList)
                {
                    if (temps2.Text.ToString() == temps.Name())
                    {
                        temps.SetTime((int)roundedValue);
                        break;
                    }
                }
            }
            currentTime.Add(block, (int)roundedValue);
            selectedStoredAction.Add(block, selectedTempAction);
            return;
        }
        #endregion
        #region Length / Degress
        void Slider2Writer(IMyTerminalBlock block, StringBuilder tempBuilder)
        {
            tempBuilder.Clear();
            tempBuilder.Append(Slider2Getter(block).ToString());
            return;
        }
        float Slider2Getter(IMyTerminalBlock block)
        {
            float storedValue = 0f;

            if (currentDeg.Get(block) == null) return -1f;
            int tempTime = currentDeg.Get(block);

            storedValue = (float)tempTime;

            return storedValue;
        }
        void Slider2Setter(IMyTerminalBlock block, float sliderValue)
        {
            float roundedValue = (float)Math.Round(sliderValue, 0, MidpointRounding.AwayFromZero);

            if (roundedValue > 360)
            {
                roundedValue = 360;
            }

            if (roundedValue < 0)
            {
                roundedValue = 0f;
            }

            List<MyTerminalControlListBoxItem> selectedTempAction;
            selectedTempAction = selectedStoredAction.Get(block);
            if (selectedTempAction == null) return;
            ConcurrentBag<ActionStorage> tempActionStorage = totalStoreSaves.Get(block);
            if (tempActionStorage == null) return;
            List<ActionStorage> tempList = new List<ActionStorage>(tempActionStorage);
            int count = selectedTempAction.Count();
            foreach (MyTerminalControlListBoxItem temps2 in selectedTempAction)
            {
                foreach (ActionStorage temps in tempList)
                {
                    if (temps2.Text.ToString() == temps.Name() && temps.isStat)
                    {
                        temps.degressExtendiness = (int)roundedValue;
                        currentDeg.Add(block, (int)roundedValue);
                        break;
                    }
                    else if (temps2.Text.ToString() == temps.Name() && temps.isPist && roundedValue < 11)
                    {
                        temps.degressExtendiness = (int)roundedValue;
                        currentDeg.Add(block, (int)roundedValue);
                        break;
                    }
                    else if (temps2.Text.ToString() == temps.Name() && temps.isPist && roundedValue > 10)
                    {
                        temps.degressExtendiness = 10;
                        currentDeg.Add(block, 10);
                        break;
                    }
                }
            }
            selectedStoredAction.Add(block, selectedTempAction);
            return;
        }
        #endregion
        #region First Box with types
        void SelectionBox(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> firstList, List<MyTerminalControlListBoxItem> secondList)
        {
            MyLog.Default.WriteLineAndConsole("--------SelectionBox---------");
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            block.CubeGrid.GetBlocks(blocks);
            ConcurrentBag<IMySlimBlock> blocksMultiThread = new ConcurrentBag<IMySlimBlock>(blocks);
            ConcurrentBag<List<String>> types = new ConcurrentBag<List<String>>(typesSupported);
            ConcurrentBag<String> found = new ConcurrentBag<String>();
            ConcurrentBag<String> cleanFound = new ConcurrentBag<String>();
            ConcurrentBag<IMySlimBlock> savedBlocks = new ConcurrentBag<IMySlimBlock>();

            MyAPIGateway.Parallel.ForEach(blocksMultiThread, tempBlock =>  // Goes through all the blocks around us asynced
            {
                if (tempBlock == null) return;
                MyAPIGateway.Parallel.ForEach(types, tempListString =>   // Goes through the types array and retrives the types name lists.
                {
                    if (tempListString == null) return;
                    foreach (String tempString in tempListString)    // Goes throught the list of names in each array location
                    {
                        String blockSubId = tempBlock.BlockDefinition.Id.SubtypeId.ToString();
                        String blockSubType = tempBlock.BlockDefinition.Id.TypeId.ToString();
                        if (tempString == blockSubId)
                        {
                            foreach (String tempNameString in messages)  // Goes Through the Messages array,
                            {
                                if (tempString.Contains(tempNameString))
                                {
                                    found.Add(tempNameString);
                                    savedBlocks.Add(tempBlock);
                                    return;
                                }
                            }
                            break;
                        }
                        else if (tempString == blockSubType)
                        {
                            foreach (String tempNameString in messages)   // Goes through the types array and retrives the types name lists.
                            {
                                if (tempString.Contains(tempNameString))
                                {
                                    found.Add(tempNameString);
                                    savedBlocks.Add(tempBlock);
                                    return;
                                }
                            }
                            break;
                        }
                    }
                });
            });

            List<String> NewArray = new List<string>();
            foreach (String tempString in found)
            {
                if (NewArray.Contains(tempString))
                {
                    continue;
                }
                NewArray.Add(tempString);
                continue;
            }

            MyTerminalControlListBoxItem first = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-Select objects below-"), MyStringId.GetOrCompute("-Select objects below-"), "abd");
            firstList.Add(first);

            foreach (String objects in NewArray)
            {
                firstList.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(objects), MyStringId.GetOrCompute(objects), "abd"));
            }
            lock (savedBlocks)
            {
                localBlockStorage.Add(block, savedBlocks);
            }
            return;
        }
        void SelectedType(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> listItems)
        {
            MyLog.Default.WriteLineAndConsole("--------SelectedType---------");
            if (listItems.Count == 0)
            {
                return;
            }

            //MyAPIGateway.Utilities.SetVariable<List<MyTerminalControlListBoxItem>>("SelectedBlockTypes" + block.EntityId.ToString(), listItems);
            SelectedBlockTypes.Add(block, new ConcurrentBag<MyTerminalControlListBoxItem>( listItems ));
            MyAPIGateway.Utilities.ShowMessage("Sequencer", "Saved First Selected");
            box2.UpdateVisual();
        }
        #endregion
        #region Box with the actions that is found
        void SelectionBoxActions(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> firstList, List<MyTerminalControlListBoxItem> secondList)
        {
            MyLog.Default.WriteLineAndConsole("--------SelectionBoxActions---------");
            MyTerminalControlListBoxItem first = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-Select action below-"), MyStringId.GetOrCompute("-Select action below-"), "abd");
            firstList.Add(first);

            MyTerminalControlListBoxItem second = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("Trigger"), MyStringId.GetOrCompute("Trigger"), "abd");
            firstList.Add(second);

            MyLog.Default.WriteLineAndConsole("--------SelectionBoxActions---------: blocksMultiThread!");
            ConcurrentBag<IMySlimBlock> blocksMultiThread = localBlockStorage.Get(block);
            if (blocksMultiThread == null) blocksMultiThread = new ConcurrentBag<IMySlimBlock>();
            MyLog.Default.WriteLineAndConsole("--------SelectionBoxActions---------: tempList!");
            ConcurrentBag<MyTerminalControlListBoxItem> selections = SelectedBlockTypes.Get(block);
            if (selections == null) selections = new ConcurrentBag<MyTerminalControlListBoxItem>();
            MyLog.Default.WriteLineAndConsole("--------SelectionBoxActions---------: AllDone!");

            ConcurrentBag<MyTerminalControlListBoxItem> foundActions = new ConcurrentBag<MyTerminalControlListBoxItem>();

            //MyAPIGateway.Parallel.ForEach(blocksMultiThread, tempBlock =>  // Goes through all the blocks around us asynced.
            foreach (IMySlimBlock tempBlock in blocksMultiThread)
            {
                if (tempBlock == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Sequencer", "Blocks Was null");
                    return;
                }
                foreach (MyTerminalControlListBoxItem tempSelect in selections) // Goes through all the selections asynced.
                {
                    string subid = tempBlock.BlockDefinition.Id.SubtypeId.ToString();
                    string typeid = tempBlock.BlockDefinition.Id.TypeId.ToString();
                    if (subid.Contains(tempSelect.Text.ToString()) || typeid.Contains(tempSelect.Text.ToString()))
                    {
                        MyLog.Default.WriteLineAndConsole("--------SelectionBoxActions---------: " + tempBlock.BlockDefinition.Id.SubtypeId.ToString());

                        MyLog.Default.WriteLineAndConsole("FatBlock: " + tempBlock.FatBlock.ToString());
                        IMyCubeBlock temp = tempBlock.FatBlock;
                        MyLog.Default.WriteLineAndConsole("TerminalBlock: " + temp.ToString());
                        MyLog.Default.WriteLineAndConsole("Type: " + temp.GetType().ToString());

                        List<ITerminalAction> tempActions = new List<ITerminalAction>();
                        MyAPIGateway.TerminalActionsHelper.GetActions(temp.GetType(), tempActions);

                        if (typeid.Contains("Stator"))
                        {
                            foundActions.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(temp.DisplayNameText +": UpperDegrees"), MyStringId.GetOrCompute(temp.DisplayNameText +": UpperDegrees"), temp));
                            foundActions.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(temp.DisplayNameText +": LowerDegrees"), MyStringId.GetOrCompute(temp.DisplayNameText +": LowerDegrees"), temp));
                        }
                        else if (typeid.Contains("Piston"))
                        {
                            foundActions.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(temp.DisplayNameText+": Length"), MyStringId.GetOrCompute(temp.DisplayNameText+": Length"), temp));
                        }

                        MyLog.Default.WriteLineAndConsole("ActionsFound: " + tempActions.Count());
                        foreach (ITerminalAction tempAction in tempActions)
                        {
                            MyLog.Default.WriteLineAndConsole("Sequencer" + tempSelect.Text + ": " + tempAction.Name);
                            MyTerminalControlListBoxItem myTerminalControlListBoxItem = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(temp.DisplayNameText + ": " + tempAction.Name), MyStringId.GetOrCompute(temp.DisplayNameText + ": " + tempAction.Name), "abd");
                            foundActions.Add(myTerminalControlListBoxItem);
                        }
                    }
                }
            }
            //});
            List<IMyBlockGroup> tempGroupList = selectedBlockGroups.Get(block);
            ConcurrentBag<MyTerminalControlListBoxItem> foundActions2 = new ConcurrentBag<MyTerminalControlListBoxItem>();
            if (tempGroupList != null)
            {
                ConcurrentBag<IMyBlockGroup> tempGroupSelection = new ConcurrentBag<IMyBlockGroup>(tempGroupList);
                MyLog.Default.WriteLineAndConsole("tempGroupSelection: " + tempGroupSelection.Count());
                MyAPIGateway.Parallel.ForEach(tempGroupSelection, tempGroupSelected =>
                {
                    MyLog.Default.WriteLineAndConsole("Groups: " + tempGroupSelected.Name.ToString());
                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    tempGroupSelected.GetBlocks(blocks);
                    if (blocks == null) return;
                    List<MyTerminalControlListBoxItem> actionStorages = new List<MyTerminalControlListBoxItem>();
                    foreach(IMyTerminalBlock terminalBlocks in blocks)
                    {
                        List<ITerminalAction> terminalActions = new List<ITerminalAction>();
                        MyAPIGateway.TerminalActionsHelper.GetActions(terminalBlocks.GetType(), terminalActions);
                        foreach(IMyTerminalAction action in terminalActions)
                        {
                            foundActions2.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("G: " + tempGroupSelected.Name + ": " + action.Id),MyStringId.GetOrCompute("G: " + tempGroupSelected.Name + ": " + action.Id), "adc"));
                        }
                    }
                });
                lock (foundActions2)
                {
                    MyAPIGateway.Parallel.ForEach(foundActions2, tempFoundAction =>
                    {
                        bool first2 = true;
                        foreach(MyTerminalControlListBoxItem tempFoundAction2 in foundActions)
                        {
                            if (tempFoundAction.Text == tempFoundAction2.Text)
                            {
                                if (first2)
                                {
                                    first2 = false;
                                }
                            }
                        }
                        if (first2)
                        {
                            foundActions.Add(tempFoundAction);
                        }
                    });
                }
            }

            lock (foundActions)
            {
                foreach (MyTerminalControlListBoxItem objects in foundActions)
                {
                    MyLog.Default.WriteLineAndConsole("Objects writen to the screen: " + objects.Text.ToString());
                    firstList.Add(objects);
                    //MyAPIGateway.Utilities.ShowMessage("Sequencer", objects.Text.ToString());
                }
            }
            return;
        }
        void SelectedAction(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> listItems)
        {
            MyLog.Default.WriteLineAndConsole("--------SelectedAction---------");
            ConcurrentBag<IMySlimBlock> blocks = localBlockStorage.Get(block);
            if (blocks == null) return;

            ConcurrentBag<MyTerminalControlListBoxItem> listItems2 = new ConcurrentBag<MyTerminalControlListBoxItem>(listItems);
            ConcurrentBag<ActionStorage> storeSaves = new ConcurrentBag<ActionStorage>();
            storeSaves = totalStoreSaves.Get(block);
            if (storeSaves == null) storeSaves = new ConcurrentBag<ActionStorage>();
            bool newFirst = false;

            if (storeSaves.Count() == 0)
            {
                newFirst = true;
            }

            MyLog.Default.WriteLineAndConsole("listItems2: " + listItems2.Count() + " storeSaves: " + storeSaves.Count());

            MyAPIGateway.Parallel.ForEach(listItems2, Item =>
            {
                MyLog.Default.WriteLineAndConsole((String)Item.UserData.ToString());
                if (Item.Text.ToString().Contains("UpperDegrees"))
                {
                    IMyCubeBlock blockTemp = (IMyCubeBlock)Item.UserData;
                    ActionStorage actionStorage = new ActionStorage(block.SlimBlock, blockTemp.SlimBlock, false, 0, 5, storeSaves.Count());
                    if (newFirst)
                    {
                        actionStorage.currentlyAt = true;
                    }
                    actionStorage.terminalActions = "UpperDegress";
                    storeSaves.Add(actionStorage);
                }
                else if (Item.Text.ToString().Contains("LowerDegrees"))
                {
                    IMyCubeBlock blockTemp = (IMyCubeBlock)Item.UserData;
                    ActionStorage actionStorage = new ActionStorage(block.SlimBlock, blockTemp.SlimBlock, false, 0, 5, storeSaves.Count());
                    if (newFirst)
                    {
                        actionStorage.currentlyAt = true;
                    }
                    actionStorage.terminalActions = "LowerDegress";
                    storeSaves.Add(actionStorage);
                }
                else if (Item.Text.ToString().Contains("Length"))
                {
                    IMyCubeBlock blockTemp = (IMyCubeBlock)Item.UserData;
                    ActionStorage actionStorage = new ActionStorage(block.SlimBlock, blockTemp.SlimBlock, true, 0, 5, storeSaves.Count());
                    if (newFirst)
                    {
                        actionStorage.currentlyAt = true;
                    }
                    actionStorage.terminalActions = "Length";
                    storeSaves.Add(actionStorage);
                }
                else if (Item.Text.ToString() == "Trigger")
                {
                    ActionStorage actionStorage = new ActionStorage(block.SlimBlock, block.SlimBlock, storeSaves.Count());
                    if (newFirst)
                    {
                        actionStorage.currentlyAt = true;
                    }
                    storeSaves.Add(actionStorage);
                }
                else if (Item.Text.ToString()[0] == 'G')
                {
                    String tempString = Item.Text.String.Remove(0, 3);
                    String[] tempOrders = tempString.Split(':');
                    String tempTempOrders = tempOrders[1].Remove(0, 1);

                    MyLog.Default.WriteLineAndConsole("'" + tempTempOrders + "': " + tempString);

                    List<IMyBlockGroup> blockGroups =  selectedBlockGroups.Get(block);
                    if (blockGroups == null) return;

                    foreach (IMyBlockGroup tempBlock in blockGroups)
                    {
                        if (tempOrders[0] == tempBlock.Name)
                        {
                            ActionStorage actionStorage = new ActionStorage(block.SlimBlock, tempBlock, tempTempOrders, 5, storeSaves.Count());
                            if (newFirst)
                            {
                                actionStorage.currentlyAt = true;
                            }
                            storeSaves.Add(actionStorage);
                        }
                    }
                }
                else
                {
                    String[] tempOrders = Item.Text.String.Split(':');
                    foreach (IMySlimBlock tempBlock in blocks)
                    {
                        MyLog.Default.WriteLineAndConsole("Wiriting to totalStoreSaves");
                        MyLog.Default.WriteLineAndConsole(tempBlock.FatBlock.DisplayNameText + " + " + tempOrders[0] + " : " + (tempBlock.FatBlock.DisplayNameText == tempOrders[0]));
                        if (tempBlock.FatBlock.DisplayNameText == tempOrders[0])
                        {
                            String tempTempOrders = tempOrders[1].Remove(0, 1);
                            MyLog.Default.WriteLineAndConsole("Orders: " + tempTempOrders);
                            List<ITerminalAction> terminalActions = new List<ITerminalAction>();
                            MyAPIGateway.TerminalActionsHelper.GetActions(tempBlock.FatBlock.GetType(), terminalActions);
                            MyLog.Default.WriteLineAndConsole("Actions: " + terminalActions.Count());

                            foreach (ITerminalAction tempAction in terminalActions)
                            {
                                MyLog.Default.WriteLineAndConsole(tempAction.Name.ToString() + " - " + tempTempOrders);
                                if (tempTempOrders == tempAction.Name.ToString())
                                {
                                    ActionStorage actionStorage = new ActionStorage(block.SlimBlock, tempBlock, tempAction, 5, storeSaves.Count());
                                    if (newFirst)
                                    {
                                        actionStorage.currentlyAt = true;
                                    }
                                    storeSaves.Add(actionStorage);
                                }
                            }
                        }
                    }
                }
            });
            totalStoreSaves.Add(block, storeSaves);
            box3.UpdateVisual();
        }
        #endregion
        #region Our current list of Actions
        void box3List(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> firstList, List<MyTerminalControlListBoxItem> secondList)
        {
            MyLog.Default.WriteLineAndConsole("--------Box3List---------");
            ConcurrentBag<ActionStorage> temp = totalStoreSaves.Get(block) as ConcurrentBag<ActionStorage>;
            if (temp == null) return;
            List<ActionStorage> tempTempActionStorage = new List<ActionStorage>(temp);
            tempTempActionStorage.Sort();
            if (temp == null) return;
            firstList.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-Current Actions-"), MyStringId.GetOrCompute("-Current Actions-"), "adc"));
            foreach (ActionStorage tempAction in tempTempActionStorage)
            {
                firstList.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(tempAction.Name()), MyStringId.GetOrCompute(tempAction.Name()), "adc"));
            }
        }
        void box3Selected(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> listItems)
        {
            MyLog.Default.WriteLineAndConsole("---------------Items:" + listItems.Count.ToString());
            selectedStoredAction.Add(block, listItems);

            List<MyTerminalControlListBoxItem> selectedTempAction = listItems;
            if (selectedTempAction == null) return;
            ConcurrentBag<ActionStorage> tempActionStorage = totalStoreSaves.Get(block) as ConcurrentBag<ActionStorage>;
            if (tempActionStorage == null) return;
            List<ActionStorage> tempList = new List<ActionStorage>(tempActionStorage);
            int time = 0;
            int count = listItems.Count();
            foreach (MyTerminalControlListBoxItem temps2 in selectedTempAction)
            {
                foreach (ActionStorage temps in tempList)
                {
                    if (temps2.Text.ToString() == temps.Name())
                    {
                        if (count == 1)
                        {
                            time = temps.GetTime();
                        }
                        else if (count > 1)
                        {
                            time = temps.GetTime() + time;
                        }
                        if (temps.isStat || temps.isPist)
                        {
                            currentDeg.Add(block, temps.degressExtendiness);
                        }

                        break;
                    }
                }
            }
            time = time / count;
            currentTime.Add(block, time);
            slider.UpdateVisual();
            slider2.UpdateVisual();
        }
        #endregion
        #region Groups menu
        void box4List(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> firstList, List<MyTerminalControlListBoxItem> secondList)
        {
            IMyCubeGrid cubeGrid = block.CubeGrid;
            IMyGridTerminalSystem gridTerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);

            firstList.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-Groups-"), MyStringId.GetOrCompute("-Groups-"), "adc"));

            List<IMyBlockGroup> blockGroup = new List<IMyBlockGroup>();
            gridTerminalSystem.GetBlockGroups(blockGroup);
            blockGroups.Add(block, blockGroup);
            MyLog.Default.WriteLineAndConsole(blockGroup.ToString());

            foreach (IMyBlockGroup tempBlockGroup in blockGroup)
            {
                firstList.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(tempBlockGroup.Name), MyStringId.GetOrCompute(tempBlockGroup.Name), "adc"));
            }
        }
        void box4Selected(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> listItems)
        {
            if (listItems[0].Text.ToString() == "-Groups-")
            {
                selectedBlockGroups.Add(block, new List<IMyBlockGroup>());
                box2.UpdateVisual();
                return;
            }

            List<IMyBlockGroup> blockGroup;
            blockGroup = blockGroups.Get(block);
            if (blockGroup == null) return;
            List<IMyBlockGroup> selectedBlockGroup = new List<IMyBlockGroup>();
            foreach (MyTerminalControlListBoxItem Items in listItems)
            {
                foreach (IMyBlockGroup tempBlockGroup in blockGroup)
                {
                    if (Items.Text.ToString() == tempBlockGroup.Name.ToString())
                    {
                        selectedBlockGroup.Add(tempBlockGroup);
                        break;
                    }
                }
            }
            selectedBlockGroups.Add(block, selectedBlockGroup);
            MyLog.Default.WriteLineAndConsole("Updating box2 from groups: " + selectedBlockGroup.Count());
            box2.UpdateVisual();
        }
        #endregion
        #region Buttons
        void Remove(IMyTerminalBlock block)
        {
            MyLog.Default.WriteLineAndConsole("--------Removers---------");
            List<MyTerminalControlListBoxItem> selectedTempAction = selectedStoredAction.Get(block) as List<MyTerminalControlListBoxItem>;
            if (selectedTempAction == null) return;
            ConcurrentBag<ActionStorage> tempActionStorage = totalStoreSaves.Get(block) as ConcurrentBag<ActionStorage>;
            if (tempActionStorage == null) return;
            List<ActionStorage> tempList = new List<ActionStorage>(tempActionStorage);
            bool currentAt = false;
            foreach (MyTerminalControlListBoxItem temps2 in selectedTempAction)
            {
                foreach (ActionStorage temps in tempList)
                {
                    if (temps2.Text.ToString() == temps.Name())
                    {
                        if (temps.currentlyAt)
                        {
                            currentAt = true;
                        }
                        tempList.Remove(temps);
                        break;
                    }
                }
            }
            tempList.Sort();
            for (int i = 0; i < tempList.Count() - 1; i++)
            {
                MyLog.Default.WriteLineAndConsole("Seq" + tempList[i].ToString());
                tempList[i].index = i;
            }
            if (currentAt && tempList.Count() >= 1)
            {
                tempList[0].currentlyAt = true;
            }
            ConcurrentBag<ActionStorage> tempCon = new ConcurrentBag<ActionStorage>(tempList);
            totalStoreSaves.Add(block, tempCon);
            box3.UpdateVisual();
        }
        void Test(IMyTerminalBlock block)
        {
            ConcurrentBag<ActionStorage> tempActionBag = totalStoreSaves.Get(block);
            if (tempActionBag == null) return;
            ConcurrentBag<ActionStorage>[] tempItems = totalStoreSaves.GetAllSaved();
            if (tempItems == null) return;
            foreach (ActionStorage tempAction in tempActionBag)
            {
                tempAction.TriggerNow();
            }
            MyAPIGateway.Parallel.ForEach(tempItems, Items =>
            {
                if (Items == null) return;
                foreach (ActionStorage tempAction in Items)
                {
                    if (tempAction == null) return;
                    MyLog.Default.WriteLineAndConsole("--------" + tempAction.currentlyAt);
                }
                MyLog.Default.WriteLineAndConsole("InBag:" + Items.Count());
            });
        }
        #endregion
        #region Actions
        void RunOnce(IMyTerminalBlock block)
        {
            trigger.Add(block, true);
            try
            {
                MyAPIGateway.Utilities.SetVariable<bool>("runForever" + block.EntityId.ToString(), false);
            }
            catch (Exception exc)
            {

            }
            MyLog.Default.WriteLineAndConsole("We ran Once: " + block.EntityId.ToString());
        }
        void RunForever(IMyTerminalBlock block)
        {
            trigger.Add(block, true);
            try
            {
                MyAPIGateway.Utilities.SetVariable<bool>("runForever" + block.EntityId.ToString(), true);
            }
            catch (Exception exc)
            {

            }
        }
        #endregion
        public bool SubtypeIdCheck(IMyTerminalBlock block)
        {

            var blockDef = block.SlimBlock.BlockDefinition as MyDefinitionBase;

            foreach (var name in blockSubtypes)
            {

                if (blockDef.Id.SubtypeId.ToString().Contains(name) == true)
                {

                    return true;

                }

            }

            return false;

        }
        void Updater()
        {
            ConcurrentBag<ActionStorage>[] tempItems = totalStoreSaves.GetAllSaved();
            List<ConcurrentBag<ActionStorage>> actionStorages = new List<ConcurrentBag<ActionStorage>> ();

            foreach ( ConcurrentBag<ActionStorage> Items in tempItems)
            {
                actionStorages.Add(Items);
            }

            //MyAPIGateway.Parallel.ForEach(tempItems, Items =>
            foreach (ConcurrentBag<ActionStorage> Items in actionStorages)
            {
                if (Items == null) continue;
                int newAt = -1;
                //MyAPIGateway.Parallel.ForEach(Items, tempAction =>
                foreach ( ActionStorage tempAction in Items)
                {
                    if (Items == null) break;
                    bool ignoreTrigger = false;
                    IMyCubeBlock fatBlock = tempAction.GetOwner();
                    if (fatBlock == null) break;
                    long next = nextTriggerTime.Get(fatBlock);
                    bool isTriggered = trigger.Get(fatBlock);
                    if (isTriggered == true)
                    {
                        //MyLog.Default.WriteLineAndConsole("IsTriggered");
                        if (next == -1)
                        {
                            MyLog.Default.WriteLineAndConsole("GotTrigger");
                        }
                        else
                        {
                            if ( tempAction.currentlyAt && tempAction.IsTrigger())
                            {
                                next = -1;
                            }
                        }
                    }

                    if (next >= stopwatch.ElapsedMilliseconds)
                    {
                        //MyLog.Default.WriteLineAndConsole("Next: " + next + " StopWatch:" + stopwatch.ElapsedMilliseconds);
                        return;
                    }
                    //MyLog.Default.WriteLineAndConsole(fatBlock.Name + "IsTriggered: " + isTriggered + " next:" + next + "/" + stopwatch.ElapsedMilliseconds);

                    MyAPIGateway.Utilities.GetVariable<bool>("runForever" + fatBlock.EntityId.ToString(), out ignoreTrigger);

                    if (tempAction.currentlyAt)
                    {
                        if (ignoreTrigger == true)
                        {
                            //MyLog.Default.WriteLineAndConsole("triggerIgnored");
                            tempAction.currentlyAt = false;
                            nextTriggerTime.Add(fatBlock, stopwatch.ElapsedMilliseconds + (tempAction.GetTime() * 1000));
                            tempAction.TriggerNow();
                            trigger.Add(fatBlock, false);
                            if (tempAction.index + 1 == Items.Count())
                            {
                                newAt = 0;
                                //MyLog.Default.WriteLineToConsole("newAt: " + newAt);
                                break;
                            }
                            else
                            {
                                newAt = tempAction.index + 1;
                                //MyLog.Default.WriteLineToConsole("newAt: " + newAt);
                                break;
                            }
                        }
                        else if (tempAction.IsTrigger() && ignoreTrigger == false)
                        {
                            //MyLog.Default.WriteLineAndConsole("IsTrigger: " + isTriggered);
                            if (isTriggered == true)
                            {
                                //MyLog.Default.WriteLineAndConsole("Is A trigger thats triggered");
                                tempAction.currentlyAt = false;
                                nextTriggerTime.Add(fatBlock, stopwatch.ElapsedMilliseconds + (tempAction.GetTime() * 1000));
                                trigger.Add(fatBlock, false);
                                if (tempAction.index + 1 == Items.Count())
                                {
                                    newAt = 0;
                                    //MyLog.Default.WriteLineToConsole("newAt: " + newAt);
                                    break;
                                }
                                else
                                {
                                    newAt = tempAction.index + 1;
                                    //MyLog.Default.WriteLineToConsole("newAt: " + newAt);
                                    break;
                                }
                            }
                            nextTriggerTime.Add(fatBlock,-1);
                        }
                        else
                        {
                            //MyLog.Default.WriteLineAndConsole("ElseCatagory");
                            tempAction.currentlyAt = false;
                            nextTriggerTime.Add(fatBlock, stopwatch.ElapsedMilliseconds + (tempAction.GetTime() * 1000));
                            tempAction.TriggerNow();
                            trigger.Add(fatBlock, false);
                            if (tempAction.index + 1 == Items.Count())
                            {
                                newAt = 0;
                                break;
                            }
                            else
                            {
                                newAt = tempAction.index + 1;
                                break;
                            }
                        }
                    }
                }//);
                if (newAt != -1)
                {
                    foreach(ActionStorage tempAction in Items)
                    {
                        if (tempAction.index == newAt)
                        {
                            tempAction.currentlyAt = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
