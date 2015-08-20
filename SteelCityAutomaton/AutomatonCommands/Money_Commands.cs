using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class get_balance : AutomatonCommand
    {
        public override bool Excecute(SteelCityAutomaton.Automaton am, OpenMetaverse.GridClient client, bool force)
        {
            result.data = client.Self.Balance;
            result.success = true;
            return true;
        }
    }

    public class pay : AutomatonCommand
    {
        public string uuid { get; set; }
        public int amount { get; set; }
        public string description { get; set; }

        public override bool Excecute(SteelCityAutomaton.Automaton am, OpenMetaverse.GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            UUID target;
            if (UUID.TryParse(uuid, out target) && amount > 0)
            {
                client.Self.GiveAvatarMoney(target, amount,description);
                result.success = true;
            }
            else result.message = "Invalid key or ammount";
            return true;
        }
    }
}