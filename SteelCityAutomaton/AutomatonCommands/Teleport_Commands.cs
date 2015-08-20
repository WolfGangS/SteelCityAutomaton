using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class offer_tp : offer_teleport { }
    public class offer_teleport : AutomatonCommand
    {
        public string user_id { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            result.data = user_id;
            UUID target;
            if (UUID.TryParse(user_id, out target))
            {
                client.Self.SendTeleportLure(target);
                result.success = true;
            }
            else result.message = "invalid uuid";
            return true;
        }
    }

    public class tp_home : teleport_home { }
    public class teleport_home : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            result.success = client.Self.GoHome();
            return true;
        }
    }

    public class get_tp_offers : get_teleport_offers { }
    public class get_teleport_offers : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            result.data = am.GetTPOffers();
            result.success = true;
            return true;
        }
    }

    public class accept_tp_offer : accept_teleport_offer { }
    public class accept_tp : accept_teleport_offer { }
    public class accept_teleport_offer : AutomatonCommand
    {
        public string user_id { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            result.data = user_id;
            if (am.AcceptTPOffer(user_id)) result.success = true;
            else result.message = "Unrecognized uuid";
            return true;
        }
    }

    public class decline_tp_offer : decline_teleport_offer { }
    public class decline_tp : decline_teleport_offer { }
    public class decline_teleport_offer : AutomatonCommand
    {
        public string user_id { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            result.data = user_id;
            if (am.AcceptTPOffer(user_id,false)) result.success = true;
            else result.message = "Unrecognized uuid";
            return true;
        }
    }
}
