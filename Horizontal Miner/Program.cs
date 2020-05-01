using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        private string initPhrase = "miner";
        private readonly MinerControl minerControl;
        private readonly DisplayService displayService;
        private readonly Profiler profiler;

        public Program()
        {
            profiler = new Profiler(this);
            this.displayService = new DisplayService(this, initPhrase);
            this.minerControl = new MinerControl(this, displayService, initPhrase, Storage);
            StringBuilder stringBuilder = new StringBuilder("Commands\n--------\n\n");
            stringBuilder.Append(initPhrase).Append(" start -> Starts the process").Append(initPhrase).Append("(Button 1)\n");
            stringBuilder.Append(initPhrase).Append(" stop -> Stops all moving parts imediatelly").Append("(Button 2)\n");
            stringBuilder.Append(initPhrase).Append(" forward -> sets the movement direction to forward").Append("(Button 3)\n");
            stringBuilder.Append(initPhrase).Append(" backward -> sets the movement direction to backward").Append("(Button 4)\n");
            stringBuilder.Append(initPhrase).Append(" compact -> sets the drill head to compact\n\t retracting all extended parts and disables all tools").Append("\n");
            stringBuilder.Append("(experimental) ").Append(initPhrase).Append(" limit=x -> Sets execution to x steps and then stops\n\t while x is an interger").Append("\n");
            stringBuilder.Append("\nBlock Init\n--------------\n\n");
            stringBuilder.Append("Init phrase: ").Append(initPhrase).Append("\n");
            stringBuilder.Append("Additional phrases: ").Append("top, bottom, arm, welder, grinder").Append("\n");
            displayService.writeToDisplays(stringBuilder, false, 10);
        }

        public void Save()
        {
            Storage = minerControl.save();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument.Length == 0)
            {
                minerControl.move();
            }
            else if (argument.Contains(initPhrase))
            {
                if (argument.Contains("start"))
                {
                    minerControl.RunState = RunState.Moving;
                    minerControl.determineState();
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                }

                if (argument.Contains("stop"))
                {
                    minerControl.RunState = RunState.Stopped;
                    minerControl.turnOff();
                }

                if (argument.Contains("backward"))
                {
                    minerControl.Forward = false;
                    if (minerControl.RunState != RunState.Stopped)
                    {
                        minerControl.RunState = RunState.Stopped;
                        minerControl.turnOff();
                        minerControl.determineState();
                    }
                }

                if (argument.Contains("forward"))
                {
                    minerControl.Forward = true;
                    if (minerControl.RunState != RunState.Stopped)
                    {
                        minerControl.RunState = RunState.Stopped;
                        minerControl.turnOff();
                        minerControl.determineState();
                    }
                }

                if (argument.Contains("compact"))
                {
                    minerControl.CompactMode = true;
                    if (minerControl.RunState != RunState.Stopped)
                    {
                        minerControl.RunState = RunState.Stopped;
                        minerControl.turnOff();
                        minerControl.determineState();
                    }
                }

                if (argument.Contains("limit"))
                {
                    minerControl.StepLimitation = int.Parse(argument.Split('=')[1].Trim());
                }

                if (argument.Contains("init"))
                {
                    minerControl.initEquipment();
                }

                if (argument.Contains("rail"))
                {
                    minerControl.ExtendRail = !minerControl.ExtendRail;
                }

                if (argument.Contains("build"))
                {
                    minerControl.BuildSteps = int.Parse(argument.Split('=')[1].Trim());
                    minerControl.ExtendRail = true;
                }

                if (argument.Contains("toggle"))
                {
                    minerControl.Forward = !minerControl.Forward;
                }

                if (argument.Contains("reset"))
                {
                    minerControl.RunState = RunState.Stopped;
                    minerControl.turnOff();
                    minerControl.RunState = RunState.Resetting;
                }
                // Command processing
            }
            profiler.print();
        }
    }
}
