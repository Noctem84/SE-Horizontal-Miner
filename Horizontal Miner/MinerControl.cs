using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public class MinerControl : IMovable
        {
            private readonly Program program;

            public List<IMyPistonBase> pistonsFront = new List<IMyPistonBase>();
            public List<IMyPistonBase> pistonsBack = new List<IMyPistonBase>();
            public List<IMyPistonBase> pistonsArm = new List<IMyPistonBase>();
            public List<IMyPistonBase> pistonsWelder = new List<IMyPistonBase>();
            public List<IMyPistonBase> pistonsDrills = new List<IMyPistonBase>();
            public List<IMyPistonBase> pistonsMount = new List<IMyPistonBase>();
            public Lock frontLock = new Lock();
            public Lock backLock = new Lock();
            public List<IMyShipConnector> lockStart = new List<IMyShipConnector>();
            public List<IMyProjector> projectors = new List<IMyProjector>();
            public List<IMyShipWelder> minerWelder = new List<IMyShipWelder>();
            public List<IMyShipDrill> minerDrills = new List<IMyShipDrill>();

            public float pistonSpeedPerEntity = 0;
            public float pistonSpeedPerEntityDrill = 0;
            public const float pistonSpeedVerticalTotal = 0.05f;
            public const float pistonSpeedLocks = 1.0f;
            public const float pistonSpeedWelder = 0.5f;
            public const float pistonSpeedHorizontalDrillTotal = 0.2f;

            public readonly string initPhrase;
            public const string frontIdentifier = "front";
            public const string backIdentifier = "back";
            public const string armIdentifier = "arm";
            public const string welderIdentifier = "welder";
            public const string drillIdentifier = "drill";

            public Dictionary<RunState, StateTracker> states = new Dictionary<RunState, StateTracker>();

            public RunState RunState;
            public bool Forward { get; set; }
            public bool CompactMode { get; set; }
            public int StepLimitation { get; set; }
            public int BuildSteps { get; set; }
            public bool ExtendRail { get; set; }
            public bool ContinueMove { get; set; }
            public readonly DisplayService displayService;
            private MinerStates minerStates;



            public MinerControl(Program program, DisplayService displayService, string initPhrase, string storedValues)
            {
                states.Add(RunState.Checking, new StateTracker(0));
                states.Add(RunState.Moving, new StateTracker(0));
                states.Add(RunState.Stopped, new StateTracker(0));
                states.Add(RunState.Resetting, new StateTracker(0));
                this.initPhrase = initPhrase;
                this.program = program;
                this.displayService = displayService;
                Forward = true;
                ExtendRail = false;
                ContinueMove = true;
                StepLimitation = -1;
                BuildSteps = 0;
                minerStates = new MinerStates(this, displayService);
                initEquipment();
                if (storedValues != null && storedValues.Length > 0)
                {
                    load(storedValues);
                }
            }

            public void initEquipment()
            {
                StringBuilder stringBuilder = new StringBuilder();
                initPistons(stringBuilder);
                initLocks(stringBuilder);
                initDrillHead(stringBuilder);
                this.displayService.writeToDisplays(stringBuilder, false, 0);
            }

            private void initPistons(StringBuilder stringBuilder)
            {
                List<IMyPistonBase> allPistons = new List<IMyPistonBase>();
                program.GridTerminalSystem.GetBlocksOfType(allPistons);
                pistonsArm.Clear();
                pistonsFront.Clear();
                pistonsBack.Clear();
                pistonsWelder.Clear();
                pistonsDrills.Clear();
                foreach (var piston in allPistons)
                {
                    string customData = piston.CustomData;
                    if (customData.Contains(initPhrase))
                    {
                        if (customData.Contains(frontIdentifier))
                        {
                            pistonsFront.Add(piston);
                            pistonsMount.Add(piston);
                        }
                        if (customData.Contains(backIdentifier))
                        {
                            pistonsBack.Add(piston);
                            pistonsMount.Add(piston);
                        }
                        if (customData.Contains(armIdentifier))
                        {
                            pistonsArm.Add(piston);
                        }
                        if (customData.Contains(welderIdentifier))
                        {
                            pistonsWelder.Add(piston);
                        }
                        if (customData.Contains(drillIdentifier))
                        {
                            pistonsDrills.Add(piston);
                        }
                    }
                }
                stringBuilder.Append("Component Status:\n");
                stringBuilder.Append("found " + pistonsFront.Count + " pistons | front\n");
                stringBuilder.Append("found " + pistonsBack.Count + " pistons | back\n");
                stringBuilder.Append("found " + pistonsArm.Count + " pistons | arm\n");
                stringBuilder.Append("found " + pistonsWelder.Count + " pistons | welder\n");
                stringBuilder.Append("found " + pistonsDrills.Count + " pistons | drill\n");
                pistonSpeedPerEntity = pistonSpeedVerticalTotal / pistonsArm.Count;
                pistonSpeedPerEntityDrill = pistonSpeedHorizontalDrillTotal / pistonsDrills.Count;
            }

            private void initLocks(StringBuilder stringBuilder)
            {
                List<IMyShipMergeBlock> allMergeBlocks = new List<IMyShipMergeBlock>();
                program.GridTerminalSystem.GetBlocksOfType(allMergeBlocks);
                foreach (var mergeBlock in allMergeBlocks)
                {
                    string customData = mergeBlock.CustomData;
                    if (customData.Contains(initPhrase))
                    {
                        if (customData.Contains(frontIdentifier))
                        {
                            frontLock.mergeBlock = mergeBlock;
                        }
                        if (customData.Contains(backIdentifier))
                        {
                            backLock.mergeBlock = mergeBlock;
                        }
                    }
                }
                stringBuilder.Append((frontLock.mergeBlock != null ? "found " : "missed ") + "lock | front\n");
                stringBuilder.Append((backLock.mergeBlock != null ? "found " : "missed ") + "lock | back\n");

                List<IMyShipConnector> allConnector = new List<IMyShipConnector>();
                program.GridTerminalSystem.GetBlocksOfType(allConnector);
                foreach (var connector in allConnector)
                {
                    string customData = connector.CustomData;
                    if (customData.Contains(initPhrase))
                    {
                        if (customData.Contains(frontIdentifier))
                        {
                            frontLock.connector = connector;
                        }
                        if (customData.Contains(backIdentifier))
                        {
                            backLock.connector = connector;
                        }
                        if (customData.Contains("start"))
                        {
                            lockStart.Add(connector);
                        }
                    }
                }
                stringBuilder.Append((frontLock.connector != null ? "found " : "missed ") + "connect | front\n");
                stringBuilder.Append((backLock.connector != null ? "found " : "missed ") + "connect | back\n");
                stringBuilder.Append("found " + lockStart.Count + " connector | start\n");
            }

            private void initDrillHead(StringBuilder stringBuilder)
            {
                List<IMyProjector> allProjectors = new List<IMyProjector>();
                program.GridTerminalSystem.GetBlocksOfType(allProjectors);
                projectors.Clear();
                foreach (var projector in allProjectors)
                {
                    if (projector.CustomData.Contains(initPhrase))
                    {
                        projectors.Add(projector);
                    }
                }
                stringBuilder.Append("found " + projectors.Count + " projectors\n");

                List<IMyShipWelder> allWelder = new List<IMyShipWelder>();
                program.GridTerminalSystem.GetBlocksOfType(allWelder);
                minerWelder.Clear();
                foreach (var welder in allWelder)
                {
                    if (welder.CustomData.Contains(initPhrase))
                    {
                        minerWelder.Add(welder);
                    }
                }
                stringBuilder.Append("found " + minerWelder.Count + " welder\n");

                List<IMyShipDrill> allDrills = new List<IMyShipDrill>();
                program.GridTerminalSystem.GetBlocksOfType(allDrills);
                minerDrills.Clear();
                foreach (var drill in allDrills)
                {
                    if (drill.CustomData.Contains(initPhrase))
                    {
                        minerDrills.Add(drill);
                    }
                }
                stringBuilder.Append("found " + minerDrills.Count + " drills\n");
            }

            public void determineState()
            {
                //program.Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                StringBuilder displayText = new StringBuilder("Dtermine State\n");
                displayText.Append("Enable utility check").Append("\n");

                RunState = RunState.Checking;
                states[RunState.Checking].Index = 0;
                resetState(RunState.Checking);

                lockStart.Clear();
                List<IMyShipConnector> allConnector = new List<IMyShipConnector>();
                program.GridTerminalSystem.GetBlocksOfType(allConnector);
                foreach (var connector in allConnector)
                {
                    string customData = connector.CustomData;
                    if (customData.Contains(initPhrase))
                    {
                        if (customData.Contains("start"))
                        {
                            lockStart.Add(connector);
                        }
                    }
                }
                displayText.Append("found " + lockStart.Count + " connector | start\n");

                displayText.Append("\nState Check");
                if (Forward)
                {
                    determineStateForward(displayText);
                }
                else
                {
                    determineStateBackward(displayText);
                }
                displayText.Append("\nGoing into state: " + states[RunState.Moving].Index);
                displayService.writeToDisplays(displayText, false, 1);
            }

            public void determineStateForward(StringBuilder displayText)
            {
                displayText.Append("\nForward");
                if (isFrontLocked(false))
                {
                    displayText.Append("\nFront is locked");
                    if (isBackLocked(false))
                    {
                        displayText.Append("\nBack is locked");
                        if (PistonRailUtilities.isLimitReached(pistonsArm, true, null))
                        {
                            displayText.Append("\nArm is extended");
                            states[RunState.Moving].Index = MinerStates.LOCK_FRONT;
                        }
                        else
                        {
                            displayText.Append("\nArm is not extended");
                            states[RunState.Moving].Index = MinerStates.LOCK_BACK;
                        }
                    }
                    else
                    {
                        displayText.Append("\nBack is unlocked");
                        if (PistonRailUtilities.isLimitReached(pistonsArm, true, null))
                        {
                            displayText.Append("\nArm is extended");
                            states[RunState.Moving].Index = MinerStates.STOP_ARM_AND_EXTEND_BACK;
                        }
                        else
                        {
                            displayText.Append("\nArm is not extended");
                            states[RunState.Moving].Index = MinerStates.RETRACT_BACK;
                        }
                    }
                }
                else
                {
                    displayText.Append("\nFront is unlocked");
                    if (isBackLocked(false))
                    {
                        displayText.Append("\nBack is locked");
                        if (PistonRailUtilities.isLimitReached(pistonsArm, true, null))
                        {
                            displayText.Append("\nArm is extended");
                            if (PistonRailUtilities.isLimitReached(pistonsFront, true, null))
                            {
                                if (frontLock.connector.Status != MyShipConnectorStatus.Connectable)
                                {
                                    displayText.Append("\nCan't go further, switching direction");
                                    Forward = !Forward;
                                    states[RunState.Moving].Index = MinerStates.RETRACT_FRONT;
                                }
                                else
                                {
                                    states[RunState.Moving].Index = MinerStates.STOP_ARM_AND_EXTEND_FRONT;
                                }
                            }
                            else
                            {
                                states[RunState.Moving].Index = MinerStates.STOP_ARM_AND_EXTEND_FRONT;
                            }
                        }
                        else
                        {
                            displayText.Append("\nArm is not extended");
                            states[RunState.Moving].Index = MinerStates.CHECK_WELDER;
                        }
                    }
                    else
                    {
                        displayText.Append("\nBack is unlocked");
                        states[RunState.Moving].Index = MinerStates.STOP_ARM_AND_EXTEND_BACK;
                    }
                }
            }
            public void determineStateBackward(StringBuilder displayText)
            {
                displayText.Append("\nBackward");
                if (isFrontLocked(false))
                {
                    displayText.Append("\nFront is locked");
                    if (isBackLocked(false))
                    {
                        displayText.Append("\nBack is locked");
                        if (PistonRailUtilities.isLimitReached(pistonsArm, true, null))
                        {
                            displayText.Append("\nArm is extended");
                            states[RunState.Moving].Index = MinerStates.UNLOCK_FRONT;
                        }
                        else
                        {
                            if (startIsLocked())
                            {
                                Forward = true;
                                determineState();
                            }
                            else
                            {
                                displayText.Append("\nArm is not extended");
                                states[RunState.Moving].Index = MinerStates.STOP_ARM;
                            }
                        }
                    }
                    else
                    {
                        displayText.Append("\nBack is unlocked");
                        if (PistonRailUtilities.isLimitReached(pistonsArm, true, null))
                        {
                            displayText.Append("\nArm is extended");
                            states[RunState.Moving].Index = MinerStates.STOP_ARM_AND_EXTEND_BACK;
                        }
                        else
                        {
                            displayText.Append("\nArm is not extended");
                            states[RunState.Moving].Index = MinerStates.RETRACT_BACK;
                        }
                    }
                }
                else
                {
                    displayText.Append("\nFront is unlocked");
                    if (isBackLocked(false))
                    {
                        displayText.Append("\nBack is locked");
                        if (PistonRailUtilities.isLimitReached(pistonsFront, true, null))
                        {
                            if (frontLock.connector.Status != MyShipConnectorStatus.Connectable)
                            {
                                states[RunState.Moving].Index = MinerStates.RETRACT_BACK;
                            }
                        }
                        if (PistonRailUtilities.isLimitReached(pistonsArm, true, null))
                        {
                            displayText.Append("\nArm is extended");
                            states[RunState.Moving].Index = MinerStates.RETRACT_FRONT;
                        }
                        else
                        {
                            displayText.Append("\nArm is not extended");
                            states[RunState.Moving].Index = MinerStates.RETRACT_FRONT;
                        }
                    }
                    else
                    {
                        displayText.Append("\nBack is unlocked");
                        if (PistonRailUtilities.isLimitReached(pistonsArm, true, null))
                        {
                            displayText.Append("\nArm is extended");
                            states[RunState.Moving].Index = MinerStates.STOP_ARM_AND_EXTEND_BACK;
                        }
                        else
                        {
                            displayText.Append("\nArm is not extended");
                            states[RunState.Moving].Index = MinerStates.STOP_ARM_AND_EXTEND_FRONT;
                        }
                    }
                }
            }

            public void move()
            {
                displayService.resetCache(3);
                StringBuilder stringBuilder = new StringBuilder("Run Stats\n---------\n");
                addStatus(stringBuilder);

                if (StepLimitation != 0 && RunState == RunState.Moving && (states[RunState.Moving].State == null || !states[RunState.Moving].State.MoveNext()))
                {
                    StringBuilder stepText = new StringBuilder();
                    stringBuilder.Append("step into " + states[RunState.Moving].Index).Append("\n");
                    stepText.Append("\nCurrent action:\n");
                    stepText.Append("\nStep: " + states[RunState.Moving].Index + "\n");

                    resetState(RunState.Moving);

                    if (Forward)
                    {
                        stepText.Append("Moving forward:\n");
                        minerStates.moveForward(stepText);
                    }
                    else
                    {
                        stepText.Append("Moving backward:\n");
                        minerStates.moveBackward(stepText);
                    }

                    displayService.writeToDisplays(stepText, false, 5);
                }


                if (RunState == RunState.Checking && (states[RunState.Checking].State == null || !states[RunState.Checking].State.MoveNext()))
                {
                    StringBuilder utilityText = new StringBuilder("Utilty status\n------------\n");
                    utilityText.Append("Step " + states[RunState.Checking].Index).Append("\n");

                    resetState(RunState.Checking);

                    if (Forward && !CompactMode)
                    {
                        minerStates.checkForward(utilityText);
                    }
                    else
                    {
                        minerStates.checkBackward(utilityText);
                    }

                    displayService.writeToDisplays(utilityText, false, 4);
                }
                if (RunState == RunState.Resetting && (states[RunState.Resetting].State == null || !states[RunState.Resetting].State.MoveNext()))
                {

                    StringBuilder resettingText = new StringBuilder("Resetting status\n------------\n");
                    resettingText.Append("Step " + states[RunState.Resetting].Index).Append("\n");

                    resetState(RunState.Resetting);

                    minerStates.reset(resettingText);

                    displayService.writeToDisplays(resettingText, false, 6);
                }

                displayService.writeToDisplays(stringBuilder, false, 2);
            }

            private void resetState(RunState state)
            {
                if (states[state].State != null)
                {
                    states[state].State.Dispose();
                    states[state].State = null;
                }
            }

            public bool extendDrill()
            {
                bool wasExtended = false;
                if (!PistonRailUtilities.isPistonMaxLimit(pistonsDrills))
                {
                    foreach (var piston in pistonsDrills)
                    {
                        piston.MaxLimit = piston.MaxLimit + (1 / pistonsDrills.Count);
                    }
                    wasExtended = true;
                }
                else
                {
                    foreach (var piston in pistonsFront)
                    {
                        if (piston.MaxLimit < 10)
                        {
                            piston.MaxLimit = piston.MaxLimit + (1 / pistonsFront.Count);
                            piston.MinLimit = piston.MinLimit + (1 / pistonsFront.Count);
                            wasExtended = true;
                        }
                    }
                    foreach (var piston in pistonsBack)
                    {
                        if (piston.MaxLimit < 10)
                        {
                            piston.MaxLimit = piston.MaxLimit + (1 / pistonsFront.Count);
                            piston.MinLimit = piston.MinLimit + (1 / pistonsFront.Count);
                            wasExtended = true;
                        }
                    }
                }
                return wasExtended;
            }


            public bool startIsLocked()
            {
                foreach (var connector in lockStart)
                {
                    if (connector.Status == MyShipConnectorStatus.Connected)
                    {
                        if (connector.OtherConnector.CustomData.Contains(initPhrase) && connector.OtherConnector.CustomData.Contains(backIdentifier))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public bool setLockFrontStatus(bool status, StringBuilder stringBuilder)
            {
                return PistonRailUtilities.setLockStatus(frontLock, status, stringBuilder);
            }

            public bool setLockBackStatus(bool status, StringBuilder stringBuilder)
            {
                return PistonRailUtilities.setLockStatus(backLock, status, stringBuilder);
            }

            public bool isFrontLocked(bool autoConnect)
            {
                return PistonRailUtilities.isLockInState(frontLock, true, autoConnect);
            }

            public bool isBackLocked(bool autoConnect)
            {
                return PistonRailUtilities.isLockInState(backLock, true, autoConnect);
            }

            public void setWelderStatus(bool status, StringBuilder stringBuilder)
            {
                if (stringBuilder != null)
                {
                    stringBuilder.Append("Set welders to " + (status ? "on" : "off")).Append("\n");
                }
                foreach (var welder in minerWelder)
                {
                    welder.Enabled = status;
                }
            }

            public void setDrillStatus(bool status, StringBuilder stringBuilder)
            {
                stringBuilder.Append("Set drills to " + (status ? "on" : "off")).Append("\n");
                foreach (var drill in minerDrills)
                {
                    drill.Enabled = status;
                }
            }

            public void setProjectorStatus(bool status, StringBuilder stringBuilder)
            {
                stringBuilder.Append("Set projectors to " + (status ? "on" : "off")).Append("\n");
                foreach (var projector in projectors)
                {
                    projector.Enabled = status;
                }
            }

            public void addStatus(StringBuilder stringBuilder)
            {
                stringBuilder.Append("State: " + RunState).Append("\n");
                stringBuilder.Append("Forward: " + Forward).Append("\n");
                stringBuilder.Append("Lock start is connected: " + startIsLocked()).Append("\n");
                stringBuilder.Append("Step: " + (states[RunState].Index)).Append("\n");
                stringBuilder.Append("Compact Mode: " + CompactMode).Append("\n");
                stringBuilder.Append("Limit: " + StepLimitation).Append("\n");
                stringBuilder.Append("Build Steps: " + BuildSteps).Append("\n");
                stringBuilder.Append("Continue Building: " + (ExtendRail && PistonRailUtilities.getBuildableBlockCount(projectors) > 0 && (BuildSteps == -1 || BuildSteps > 0))).Append("\n");
                stringBuilder.Append("Countinued Mining: " + ContinueMove).Append("\n");
                stringBuilder.Append("Waiting State: " + (states[RunState].State == null ? "None" : "" + states[RunState].State.Current)).Append("\n");
                stringBuilder.Append("Build rail: " + ExtendRail).Append("\n");
                stringBuilder.Append("Buildable blocks: " + PistonRailUtilities.getBuildableBlockCount(projectors)).Append("\n");
            }

            public void turnOff()
            {
                StringBuilder stringBuilder = new StringBuilder("Turning Off\n----------------\n");
                setWelderStatus(false, stringBuilder);
                setDrillStatus(false, stringBuilder);
                setProjectorStatus(false, stringBuilder);
                PistonRailUtilities.extendPiston(pistonsArm, 0);
                PistonRailUtilities.extendPiston(pistonsFront, 0);
                PistonRailUtilities.extendPiston(pistonsBack, 0);
                PistonRailUtilities.extendPiston(pistonsWelder, 0);
                PistonRailUtilities.extendPiston(pistonsDrills, 0);
                displayService.writeToDisplays(stringBuilder, true, 3);
                //program.Runtime.UpdateFrequency |= UpdateFrequency.Once;
            }

            public RunState getRunState()
            {
                return RunState;
            }

            public void setRunState(RunState runState)
            {
                RunState = runState;
            }

            public Dictionary<RunState, StateTracker> getStates()
            {
                return states;
            }

            public void switchDirection()
            {
                Forward = !Forward;
                RunState = RunState.Checking;
                states[RunState].Index = 0;
            }

            public void reset()
            {
                RunState = RunState.Resetting;
                states[RunState].Index = 0;
            }

            public string save()
            {
                StringBuilder storageText = new StringBuilder();
                storageText.Append("RunState").Append("=").Append(RunState).Append(";");
                storageText.Append("Forward").Append("=").Append(Forward).Append(";");
                storageText.Append("CompactMode").Append("=").Append(CompactMode).Append(";");
                storageText.Append("StepLimitation").Append("=").Append(StepLimitation).Append(";");
                storageText.Append("BuildSteps").Append("=").Append(BuildSteps).Append(";");
                storageText.Append("ExtendRail").Append("=").Append(ExtendRail).Append(";");
                storageText.Append("ContinueMove").Append("=").Append(ContinueMove);

                return storageText.ToString();
            }

            public bool load(string storageText)
            {
                program.Echo("found configuration: " + storageText);
                string[] storageTextParts = storageText.Split(';');
                foreach(string storageTextPart in storageTextParts)
                {
                    string[] property = storageTextPart.Split('=');

                    bool value;
                    int valueInt;
                    switch (property[0])
                    {
                        case "RunState":
                            RunState.TryParse(property[1], out RunState);
                            break;
                        case "Forward":
                            bool.TryParse(property[1], out value);
                            Forward = value;
                            break;
                        case "CompactMode":
                            bool.TryParse(property[1], out value);
                            CompactMode = value;
                            break;
                        case "StepLimitation":
                            int.TryParse(property[1], out valueInt);
                            StepLimitation = valueInt;
                            break;
                        case "BuildSteps":
                            int.TryParse(property[1], out valueInt);
                            BuildSteps = valueInt;
                            break;
                        case "ExtendRail":
                            bool.TryParse(property[1], out value);
                            ExtendRail = value;
                            break;
                        case "ContinueMove":
                            bool.TryParse(property[1], out value);
                            ContinueMove = value;
                            break;
                        default:
                            break;
                    }
                }
                if (RunState != RunState.Stopped)
                {
                    program.Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                }
                return true;
            }
        }
    }
}
