using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class get_active_group : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.data = client.Self.ActiveGroup.ToString();
            result.success = true;
            return true;
        }
    }

    public class set_active_group : AutomatonCommand
    {
        public string group { get; set; }
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            /*
            UUID grp;
            if(UUID.TryParse(group,out grp))
            {
                client.Groups.
            }
            */
            //UUID grp = client.g
            return true;
        }
    }
}
