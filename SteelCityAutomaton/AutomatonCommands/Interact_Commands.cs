using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using System.Threading.Tasks;
using SteelCityAutomaton;

namespace SteelCityAutomatonCommands
{
    public class touch : AutomatonCommand
    {
        public string uuid { get; set; }
        public override bool Excecute(SteelCityAutomaton.Automaton am, OpenMetaverse.GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            UUID target;
            result.data = uuid;
            if (UUID.TryParse(uuid, out target))
            {
                Primitive targetPrim = client.Network.CurrentSim.ObjectsPrimitives.Find(
                    delegate(Primitive prim)
                    {
                        return prim.ID == target;
                    }
                );

                if (targetPrim != null)
                {
                    client.Self.Touch(targetPrim.LocalID);
                    result.success = true;
                }
                else result.message = "Couldn't find prim";
            }
            else result.message = "Invalid uuid";
            return true;
        }
    }

    public class sit_on : sit { }
    public class sit : AutomatonCommand
    {
        public string uuid { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            if (uuid == null)
            {
                client.Self.SitOnGround();
                result.success = true;
            }
            else
            {
                UUID target;
                if (UUID.TryParse(uuid, out target))
                {
                    client.Self.RequestSit(target, Vector3.Zero);
                    client.Self.Sit();
                    result.success = true;
                }
            }
            return true;
        }
    }
    public class stand : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            Console.WriteLine("[Stand] {0}",client.Self.Stand());
            result.success = true;
            return true;
        }
    }
}
