using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class add_master : AutomatonCommand
    {
        public string uuid { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            UUID master;
            result.data = uuid;
            if (UUID.TryParse(uuid, out master))
            {
                if (am.add_master(master))
                {
                    result.success = true;
                    result.message = "added master";
                }
                else result.message = "already a master";
            }
            else result.message = "not a valid uuid";
            return true;
        }
    }

    public class remove_master : AutomatonCommand
    {
        public string uuid { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            UUID master;
            result.data = uuid;
            if (UUID.TryParse(uuid, out master))
            {
                if (am.remove_master(master))
                {
                    result.success = true;
                    result.message = "removed master";
                }
                else result.message = "not a master";
            }
            else result.message = "not a valid uuid";
            return true;
        }
    }

    public class list_master : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            List<string> masters = new List<string>();
            result.data = am.Masters().ToArray();
            result.message = "list of masters";
            result.success = true;
            return true;
        }
    }
}
