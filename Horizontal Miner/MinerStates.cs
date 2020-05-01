using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public class MinerStates
        {
            private bool startedBuilding = false;

            public MinerControl minerControl;
            public DisplayService displayService;
            // 19
            public const int
                COUNT = 10,
                LOCK_BACK = 0,
                CHECK_WELDER = 11,
                EXTEND_WELDER = 9,
                RETRACT_WELDER = 12,                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
                UNLOCK_FRONT = 1,
                RETRACT_FRONT = 2,
                EXTEND_ARM = 13,
                LOCK_FRONT = 4,
                UNLOCK_BACK = 5,
                RETRACT_BACK = 6,
                RETRACT_ARM = 8,
                TURN = 14,
                EXTEND_DRILL = 15,
                CHECK_MINING_RETURN = 16,
                EXTEND_FRONT = 17,
                STOP_ARM = 18,
                STOP_ARM_AND_EXTEND_FRONT = 3,
                STOP_ARM_AND_EXTEND_BACK = 7;

            public const int
                ACTIVATE_DRILLS = 0,
                ACTIVATE_PROJECTOR_AND_WELDER = 1,
                ADJUST_START = 4;
            public MinerStates(MinerControl minerControl, DisplayService displayService)
            {
                this.displayService = displayService;
                this.minerControl = minerControl;
            }

            public void moveForward(StringBuilder stepText)
            {
                switch (minerControl.states[RunState.Moving].Index)
                {
                    case CHECK_WELDER:
                        minerControl.setWelderStatus(false, stepText);
                        if (extendRail())
                        {
                            startedBuilding = true;
                            minerControl.states[RunState.Moving].Index = EXTEND_WELDER;
                        }
                        else
                        {
                            startedBuilding = false;
                            minerControl.states[RunState.Moving].Index = RETRACT_WELDER;
                        }
                        break;

                    case EXTEND_WELDER:
                        stepText.Append("Extending welder").Append("\n");
                        stepText.Append("- Extending welder").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsWelder, MinerControl.pistonSpeedWelder);
                        stepText.Append("Wait for welder pistons ...").Append("\n");
                        minerControl.states[RunState.Moving].State = waitForLimit(minerControl.pistonsWelder, UNLOCK_FRONT, true);
                        break;

                    case RETRACT_WELDER:
                        stepText.Append("Retract welder").Append("\n");
                        stepText.Append("- Retract welder").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsWelder, MinerControl.pistonSpeedWelder);
                        stepText.Append("Wait for welder pistons ...").Append("\n");
                        minerControl.states[RunState.Moving].State = waitForLimit(minerControl.pistonsWelder, UNLOCK_FRONT, false);
                        break;

                    case UNLOCK_FRONT:
                        stepText.Append("Unlock front").Append("\n");
                        stepText.Append("- Bringing welder to desired state").Append("\n");
                        minerControl.setWelderStatus(PistonRailUtilities.getBuildableBlockCount(minerControl.projectors) > 0 && minerControl.ExtendRail, stepText);
                        stepText.Append("- Activating Projector").Append("\n");
                        minerControl.setProjectorStatus(true, stepText);
                        stepText.Append("- Unlock front").Append("\n");
                        minerControl.setLockFrontStatus(false, stepText);
                        stepText.Append("Wait for front unlock ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForConnectionState(minerControl.frontLock, false, RETRACT_FRONT, minerControl, displayService);
                        break;

                    case RETRACT_FRONT:
                        stepText.Append("Retract front").Append("\n");
                        stepText.Append("- Retract front").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsFront, MinerControl.pistonSpeedLocks);
                        stepText.Append("Wait for front ...").Append("\n");
                        minerControl.states[RunState.Moving].State = waitForLimit(minerControl.pistonsFront, EXTEND_ARM, false);
                        break;

                    case EXTEND_ARM:
                        stepText.Append("Extend arm").Append("\n");
                        stepText.Append("- Extend arm").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsArm, minerControl.pistonSpeedPerEntity * 15);
                        stepText.Append("Wait for arm ...").Append("\n");
                        minerControl.states[RunState.Moving].State = waitForLimit(minerControl.pistonsArm, CHECK_MINING_RETURN, true);
                        break;

                    case CHECK_MINING_RETURN:
                        stepText.Append("Check mining return").Append("\n");
                        if (minigReturn())
                        {
                            minerControl.states[RunState.Moving].Index = COUNT;
                        }
                        else
                        {
                            minerControl.states[RunState.Moving].Index = STOP_ARM_AND_EXTEND_FRONT;
                        }
                        break;

                    case STOP_ARM_AND_EXTEND_FRONT:
                        stepText.Append("Stop arm and extend front").Append("\n");
                        stepText.Append("- Extend front").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsFront, MinerControl.pistonSpeedLocks);
                        stepText.Append("Wait for front ...").Append("\n");
                        minerControl.states[RunState.Moving].State = waitForLimit(minerControl.pistonsFront, LOCK_FRONT, true);
                        break;

                    case LOCK_FRONT:
                        stepText.Append("Lock front").Append("\n");
                        // Error handling if vehicle exceeded builded structure
                        if (minerControl.frontLock.connector.Status != MyShipConnectorStatus.Connectable)
                        {
                            stepText.Append("Find new state, can't continue ...").Append("\n");
                            minerControl.determineState();
                            break;
                        }
                        stepText.Append("- Turning off welder").Append("\n");
                        minerControl.setWelderStatus(false, stepText);
                        stepText.Append("- Lock front").Append("\n");
                        minerControl.setLockFrontStatus(true, stepText);
                        stepText.Append("Wait for front lock ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForConnectionState(minerControl.frontLock, true, UNLOCK_BACK, minerControl, displayService);
                        break;

                    case UNLOCK_BACK:
                        stepText.Append("Unlock back").Append("\n");
                        stepText.Append("- Unlock back").Append("\n");
                        minerControl.setLockBackStatus(false, stepText);
                        stepText.Append("Wait for back unlock ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForConnectionState(minerControl.backLock, false, RETRACT_BACK, minerControl, displayService);
                        break;

                    case RETRACT_BACK:
                        stepText.Append("Retract back").Append("\n");
                        stepText.Append("- Retract back").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsBack, MinerControl.pistonSpeedLocks * 2);
                        stepText.Append("- Retract back").Append("\n");
                        minerControl.states[RunState.Moving].State = waitForLimit(minerControl.pistonsBack, RETRACT_ARM, false);
                        break;

                    case RETRACT_ARM:
                        stepText.Append("retract arm").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsArm, minerControl.pistonSpeedPerEntity * 30);
                        stepText.Append("wait for arm").Append("\n");
                        minerControl.states[RunState.Moving].State = waitForLimit(minerControl.pistonsArm, STOP_ARM_AND_EXTEND_BACK, false);
                        break;

                    case STOP_ARM_AND_EXTEND_BACK:
                        stepText.Append("Extend Back").Append("\n");
                        stepText.Append("- Extend Back").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsBack, MinerControl.pistonSpeedLocks);
                        stepText.Append("Wait for back ...").Append("\n");
                        minerControl.states[RunState.Moving].State = waitForLimit(minerControl.pistonsBack, LOCK_BACK, true);
                        break;

                    case LOCK_BACK:
                        stepText.Append("Lock back").Append("\n");
                        stepText.Append("- Lock back").Append("\n");
                        minerControl.setLockBackStatus(true, stepText);
                        stepText.Append("Wait for back lock ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForConnectionState(minerControl.backLock, true, COUNT, minerControl, displayService);
                        break;

                    case COUNT:
                        if (minerControl.StepLimitation > 0)
                        {
                            minerControl.StepLimitation--;
                        }
                        if (startedBuilding)
                        {
                            minerControl.BuildSteps--;
                            startedBuilding = false;
                        }
                        minerControl.states[RunState.Moving].Index = TURN;
                        break;

                    case TURN:
                        if (PistonRailUtilities.getBuildableBlockCount(minerControl.projectors) > 0)
                        {
                            if (extendRail())
                            {
                                minerControl.states[RunState.Moving].Index = CHECK_WELDER;
                            }
                            else
                            {
                                if (minerControl.ContinueMove)
                                {
                                    minerControl.states[RunState.Moving].Index = EXTEND_DRILL;
                                }
                            }
                        }
                        else
                        {
                            minerControl.states[RunState.Moving].Index = CHECK_WELDER;
                        }
                        break;

                    case EXTEND_DRILL:
                        stepText.Append("Extending drill").Append("\n");
                        stepText.Append("- Extending drill").Append("\n");
                        minerControl.extendDrill();
                        stepText.Append("- Switching direction").Append("\n");
                        minerControl.switchDirection();
                        stepText.Append("- Activating projector").Append("\n");
                        minerControl.setProjectorStatus(true, stepText);
                        // going to backward
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsDrills, RETRACT_ARM, true, minerControl, displayService);
                        break;

                    default:
                        stepText.Append("Unknown state, restarting").Append("\n");
                        minerControl.determineState();
                        break;
                }
            }
            public void moveBackward(StringBuilder stepText)
            {
                /*
                0 => LOCK_FRONT
                1 => UNLOCK_BACK
                2 => RETRACT_BACK
                8 => EXTEND_ARM
                3 => STOP_ARM_AND_EXTEND_BACK
                4 => LOCK_BACK
                5 => UNLOCK_FRONT
                6 => RETRACT_FRONT
                7 => STOP_ARM_AND_EXTEND_FRONT
                */
                switch (minerControl.states[RunState.Moving].Index)
                {
                    case UNLOCK_BACK:
                        stepText.Append("Unlock back").Append("\n");
                        stepText.Append("- Unlock back").Append("\n");
                        minerControl.setLockBackStatus(false, stepText);
                        stepText.Append("Wait for back unlock ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForConnectionState(minerControl.backLock, false, RETRACT_BACK, minerControl, displayService);
                        break;

                    case RETRACT_BACK:
                        stepText.Append("Retract back").Append("\n");
                        stepText.Append("- Disable welder").Append("\n");
                        minerControl.setWelderStatus(false, stepText);
                        stepText.Append("- Disable Projector").Append("\n");
                        minerControl.setProjectorStatus(false, stepText);
                        stepText.Append("- Retract back").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsBack, MinerControl.pistonSpeedLocks);
                        stepText.Append("Wait for back ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsBack, EXTEND_ARM, false, minerControl, displayService);
                        break;

                    case EXTEND_ARM:
                        stepText.Append("Extend arm").Append("\n");
                        stepText.Append("- Extend arm").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsArm, minerControl.pistonSpeedPerEntity * 30);
                        stepText.Append("Wait for arm ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsArm, STOP_ARM_AND_EXTEND_BACK, true, minerControl, displayService);
                        break;

                    case STOP_ARM_AND_EXTEND_BACK:
                        stepText.Append("Stop arm and extend back").Append("\n");
                        stepText.Append("- Disable welder").Append("\n");
                        minerControl.setWelderStatus(false, stepText);
                        stepText.Append("- Disable Projector").Append("\n");
                        minerControl.setProjectorStatus(false, stepText);
                        stepText.Append("- Stop arm").Append("\n");
                        PistonRailUtilities.resetPiston(minerControl.pistonsArm);
                        stepText.Append("- Extend back").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsBack, MinerControl.pistonSpeedLocks);
                        stepText.Append("Wait for back ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsBack, LOCK_BACK, true, minerControl, displayService);
                        break;

                    case LOCK_BACK:
                        stepText.Append("Lock back").Append("\n");
                        stepText.Append("- Lock back").Append("\n");
                        minerControl.setLockBackStatus(true, stepText);
                        stepText.Append("Wait for back lock ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForConnectionState(minerControl.backLock, true, CHECK_WELDER, minerControl, displayService);
                        break;

                    case CHECK_WELDER:
                        if (PistonRailUtilities.isLimitReached(minerControl.pistonsWelder, false, null))
                        {
                            minerControl.states[RunState.Moving].Index = UNLOCK_FRONT;
                        }
                        else
                        {
                            minerControl.states[RunState.Moving].Index = RETRACT_WELDER;
                        }
                        break;

                    case RETRACT_WELDER:
                        stepText.Append("Retract welder").Append("\n");
                        stepText.Append("- Retract welder").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsWelder, MinerControl.pistonSpeedWelder);
                        stepText.Append("Wait for welder pistons ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsWelder, UNLOCK_FRONT, false, minerControl, displayService);
                        break;

                    case UNLOCK_FRONT:
                        stepText.Append("Unlock front").Append("\n");
                        stepText.Append("- Unlock front").Append("\n");
                        minerControl.setLockFrontStatus(false, stepText);
                        stepText.Append("Wait for front unlock ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForConnectionState(minerControl.frontLock, false, RETRACT_FRONT, minerControl, displayService);
                        break;

                    case RETRACT_FRONT:
                        stepText.Append("Retract front").Append("\n");
                        stepText.Append("- Retract front").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsFront, MinerControl.pistonSpeedLocks);
                        stepText.Append("Wait for front ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsFront, RETRACT_ARM, false, minerControl, displayService);
                        break;

                    case RETRACT_ARM:
                        stepText.Append("Retract arm").Append("\n");
                        stepText.Append("- Retract arm").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsArm, minerControl.pistonSpeedPerEntity * 15);
                        stepText.Append("Wait for arm ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsArm, STOP_ARM, false, minerControl, displayService);
                        break;

                    case STOP_ARM:
                        stepText.Append("Stop arm").Append("\n");
                        stepText.Append("- Resetting arm pistons").Append("\n");
                        PistonRailUtilities.resetPiston(minerControl.pistonsArm);
                        minerControl.states[RunState.Moving].Index = TURN;
                        break;

                    case EXTEND_FRONT:
                        stepText.Append("Extend front").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsFront, MinerControl.pistonSpeedLocks);
                        stepText.Append("wait for front").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsFront, LOCK_FRONT, true, minerControl, displayService);
                        break;

                    case LOCK_FRONT:
                        stepText.Append("Lock front").Append("\n");
                        stepText.Append("- Lock front").Append("\n");
                        minerControl.setLockFrontStatus(true, stepText);
                        stepText.Append("Wait for front lock ...").Append("\n");
                        minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForConnectionState(minerControl.frontLock, true, UNLOCK_BACK, minerControl, displayService);
                        break;

                    case COUNT:
                        if (minerControl.StepLimitation > 0)
                        {
                            minerControl.StepLimitation--;
                        }
                        minerControl.states[RunState.Moving].Index = TURN;
                        break;

                    case TURN:
                        if (minerControl.startIsLocked())
                        {
                            if (minerControl.ContinueMove)
                            {
                                minerControl.states[RunState.Moving].Index = EXTEND_DRILL;
                            }
                            else
                            {
                                minerControl.reset();
                            }
                        }
                        else
                        {
                            minerControl.states[RunState.Moving].Index = EXTEND_FRONT;
                        }
                        break;

                    case EXTEND_DRILL:
                        stepText.Append("Extending drill").Append("\n");
                        stepText.Append("- Extending drill").Append("\n");
                        if (minerControl.extendDrill())
                        {
                            PistonRailUtilities.extendPiston(minerControl.pistonsDrills, MinerControl.pistonSpeedHorizontalDrillTotal / minerControl.pistonsDrills.Count);
                            stepText.Append("- Switching direction").Append("\n");
                            minerControl.switchDirection();
                            stepText.Append("Wait for drill ...").Append("\n");
                            // going to Forward
                            minerControl.states[RunState.Moving].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsDrills, CHECK_WELDER, true, minerControl, displayService);
                        }
                        else
                        {
                            minerControl.reset();
                        }
                        break;

                    default:
                        break;
                }
            }
            public void checkForward(StringBuilder utilityText)
            {
                switch (minerControl.states[RunState.Checking].Index)
                {
                    case ACTIVATE_DRILLS:
                        utilityText.Append("Turning on drills").Append("\n");
                        minerControl.setDrillStatus(true, utilityText);
                        utilityText.Append("Extending drill pistons").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsDrills, minerControl.pistonSpeedPerEntityDrill);
                        utilityText.Append("Waiting for drill pistons extended").Append("\n");
                        minerControl.states[RunState.Checking].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsDrills, 2, true, minerControl, displayService);
                        break;

                    // skipped
                    case ACTIVATE_PROJECTOR_AND_WELDER:
                        utilityText.Append("Turning on projector").Append("\n");
                        minerControl.setProjectorStatus(false, utilityText);
                        utilityText.Append("Extending welder pistons").Append("\n");
                        if (PistonRailUtilities.getBuildableBlockCount(minerControl.projectors) > 0 && minerControl.ExtendRail)
                        {
                            if (!PistonRailUtilities.isLimitReached(minerControl.pistonsArm, true, null))
                            {
                                PistonRailUtilities.extendPiston(minerControl.pistonsWelder, MinerControl.pistonSpeedWelder);
                                utilityText.Append("Waiting for welder pistons extended").Append("\n");
                                minerControl.states[RunState.Checking].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsWelder, 2, true, minerControl, displayService);
                            }
                            else
                            {
                                minerControl.states[RunState.Checking].Index = ADJUST_START;
                            }
                        }
                        else
                        {
                            minerControl.states[RunState.Checking].Index = 2;
                        }
                        break;

                    // skipped
                    case ADJUST_START:
                        PistonRailUtilities.retractPiston(minerControl.pistonsArm, minerControl.pistonSpeedPerEntity * 20);
                        minerControl.states[RunState.Checking].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsArm, ACTIVATE_PROJECTOR_AND_WELDER, false, minerControl, displayService);
                        break;

                    case 2:
                        utilityText.Append("Turning on projector").Append("\n");
                        minerControl.setProjectorStatus(true, utilityText);
                        utilityText.Append("Turning " + (minerControl.ExtendRail ? "on" : "off") + " welder").Append("\n");
                        minerControl.setWelderStatus(minerControl.ExtendRail, utilityText);
                        minerControl.states[RunState.Checking].Index = 3;
                        break;

                    default:
                        utilityText.Append("Leaving utility check process").Append("\n");
                        minerControl.RunState = RunState.Moving;
                        break;
                }
            }
            public void checkBackward(StringBuilder utilityText)
            {
                switch (minerControl.states[RunState.Checking].Index)
                {
                    case 0:
                        utilityText.Append("Turning on drills").Append("\n");
                        minerControl.setDrillStatus(true, utilityText);
                        utilityText.Append("Extending drill pistons").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsDrills, minerControl.pistonSpeedPerEntityDrill);
                        utilityText.Append("Waiting for drill pistons extended").Append("\n");
                        minerControl.states[RunState.Checking].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsDrills, 1, true, minerControl, displayService);
                        break;
                    case 1:
                        utilityText.Append("Turning off projector").Append("\n");
                        utilityText.Append("Turning off welder").Append("\n");
                        minerControl.setProjectorStatus(false, utilityText);
                        minerControl.setWelderStatus(false, utilityText);
                        utilityText.Append("Retracting welder pistons").Append("\n");
                        PistonRailUtilities.extendPiston(minerControl.pistonsWelder, -1 * MinerControl.pistonSpeedWelder);
                        utilityText.Append("Waiting for welder pistons retracted").Append("\n");
                        minerControl.states[RunState.Checking].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsWelder, 2, false, minerControl, displayService);
                        break;

                    default:
                        utilityText.Append("Leaving utility check process").Append("\n");
                        minerControl.RunState = RunState.Moving;
                        break;
                }
            }
            public void reset(StringBuilder resettingText)
            {
                switch (minerControl.states[RunState.Resetting].Index)
                {
                    case 0:
                        minerControl.setDrillStatus(false, resettingText);
                        minerControl.setWelderStatus(false, resettingText);
                        resettingText.Append("Retracting welder").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsWelder, MinerControl.pistonSpeedWelder);
                        resettingText.Append("Wait for welder pistons retracted").Append("\n");
                        minerControl.states[RunState.Resetting].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsWelder, 1, false, minerControl, displayService);
                        break;
                    case 1:
                        resettingText.Append("Setting limits").Append("\n");
                        PistonRailUtilities.setMaxLimit(minerControl.pistonsDrills, 1f);
                        resettingText.Append("Retracting drills").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsDrills, MinerControl.pistonSpeedWelder);
                        resettingText.Append("Wait for drill pistons retracted").Append("\n");
                        minerControl.states[RunState.Resetting].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsDrills, 2, false, minerControl, displayService);
                        break;
                    case 2:
                        resettingText.Append("Retracting vehicle").Append("\n");
                        resettingText.Append("Setting limits").Append("\n");
                        PistonRailUtilities.setMaxLimit(minerControl.pistonsMount, 1f);
                        if (minerControl.isFrontLocked(false))
                        {
                            PistonRailUtilities.setMinLimit(minerControl.pistonsFront, 1f);
                        }
                        else
                        {
                            PistonRailUtilities.setMinLimit(minerControl.pistonsFront, 0f);
                        }
                        if (minerControl.isBackLocked(false))
                        {
                            PistonRailUtilities.setMinLimit(minerControl.pistonsBack, 1f);
                        }
                        else
                        {
                            PistonRailUtilities.setMinLimit(minerControl.pistonsBack, 0f);
                        }
                        PistonRailUtilities.retractPiston(minerControl.pistonsMount, MinerControl.pistonSpeedLocks);
                        resettingText.Append("Wait for drill pistons retracted").Append("\n");
                        minerControl.states[RunState.Resetting].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsMount, 3, false, minerControl, displayService);
                        break;
                    case 3:
                        PistonRailUtilities.resetPiston(minerControl.pistonsMount);
                        PistonRailUtilities.setMinLimit(minerControl.pistonsMount, 0f);
                        if (minerControl.isFrontLocked(false) && minerControl.isBackLocked(false))
                        {
                            resettingText.Append("Both locked").Append("\n");
                            resettingText.Append("Unlocking front").Append("\n");
                            minerControl.setLockFrontStatus(false, resettingText);
                            resettingText.Append("Wait for front lock unlocked").Append("\n");
                            minerControl.states[RunState.Resetting].State = PistonRailUtilities.waitForConnectionState(minerControl.frontLock, false, 4, minerControl, displayService);
                        }
                        minerControl.states[RunState.Resetting].Index = 4;
                        break;
                    case 4:
                        if (!minerControl.isBackLocked(false))
                        {
                            resettingText.Append("Retracting back").Append("\n");
                            PistonRailUtilities.retractPiston(minerControl.pistonsBack, MinerControl.pistonSpeedLocks);
                            resettingText.Append("Waiting for retracting back").Append("\n");
                            minerControl.states[RunState.Resetting].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsBack, 5, false, minerControl, displayService);
                        }
                        if (!minerControl.isFrontLocked(false))
                        {
                            resettingText.Append("Retracting front").Append("\n");
                            PistonRailUtilities.retractPiston(minerControl.pistonsFront, MinerControl.pistonSpeedLocks);
                            resettingText.Append("Waiting for retracting front").Append("\n");
                            minerControl.states[RunState.Resetting].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsFront, 5, false, minerControl, displayService);
                        }
                        break;
                    case 5:
                        resettingText.Append("Retracting arm").Append("\n");
                        PistonRailUtilities.retractPiston(minerControl.pistonsArm, minerControl.pistonSpeedPerEntity * 20);
                        resettingText.Append("Waiting for retracting arm").Append("\n");
                        minerControl.states[RunState.Resetting].State = PistonRailUtilities.waitForPistonsToLimit(minerControl.pistonsArm, 6, false, minerControl, displayService);
                        break;
                    default:
                        minerControl.turnOff();
                        minerControl.RunState = RunState.Stopped;
                        break;
                }
            }
            private bool extendRail()
            {
                return minerControl.ExtendRail && PistonRailUtilities.getBuildableBlockCount(minerControl.projectors) > 0 && (minerControl.BuildSteps == -1 || minerControl.BuildSteps > 0);
            }
            private bool minigReturn()
            {
                return (!minerControl.ExtendRail || minerControl.BuildSteps == 0) && PistonRailUtilities.getBuildableBlockCount(minerControl.projectors) > 0;
            }

            private IEnumerator<bool> waitForLimit(List<IMyPistonBase> pistons, int newState, bool isMaxLimit)
            {
                return PistonRailUtilities.waitForPistonsToLimit(pistons, newState, isMaxLimit, minerControl, displayService);
            }
        }
    }
}
