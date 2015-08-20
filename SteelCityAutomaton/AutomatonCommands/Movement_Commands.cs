using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class turn_to : AutomatonCommand
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }

            if (x == 0 && y == 0 && z == 0) result.message = "invalid/missing arguements";
            else
            {
                Vector3 newDirection;
                newDirection.X = (float)x;
                newDirection.Y = (float)y;
                newDirection.Z = (float)z;
                client.Self.Movement.TurnToward(newDirection);
                client.Self.Movement.SendUpdate(false);
                result.success = true;
            }
            return true;
        }
    }

    public class start_crouching : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.Crouch(true);
            result.success = true;
            return true;
        }
    }

    public class stop_crouching : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.Crouch(false);
            result.success = true;
            return true;
        }
    }

    public class start_running : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.Movement.AlwaysRun = true;
            result.success = true;
            return true;
        }
    }

    public class stop_running : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.Movement.AlwaysRun = false;
            result.success = true;
            return true;
        }
    }

    public class start_autopilot : AutomatonCommand
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public double range { get; set; }
        public bool local { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            if (local)
            {
                OpenMetaverse.Vector3 pos = client.Self.SimPosition;
                x += pos.X;
                y += pos.Y;
                z += pos.Z;
            }
            uint regionX, regionY;
            Utils.LongToUInts(client.Network.CurrentSim.Handle, out regionX, out regionY);
            x += (double)regionX;
            y += (double)regionY;
            if (range <= 0) range = 2;
            am.AutoPilot(new Vector3d(x, y, z), range);
            result.success = true;
            return true;
        }
    }

    public class stop_autopilot : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            am.AutoPilotStop();
            result.success = true;
            return true;
        }
    }

    public class start_jumping : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.Jump(true);
            result.success = true;
            result.message = "how high?";
            return true;
        }
    }

    public class stop_jumping : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.Jump(false);
            result.success = true;
            result.message = "ok";
            return true;
        }
    }

    public class start_flying : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.Fly(true);
            result.success = true;
            result.message = "how high?";
            return true;
        }
    }

    public class stop_flying : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.Fly(false);
            result.success = true;
            result.message = "ok";
            return true;
        }
    }
}
