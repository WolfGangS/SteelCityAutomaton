using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelCityAutomatonCommands
{
    public class get_client_settings : AutomatonCommand
    {
        public override bool Excecute(SteelCityAutomaton.Automaton am, OpenMetaverse.GridClient client, bool force)
        {
            result.data = client.Settings;
            result.success = true;
            return true;
        }
    }
}
