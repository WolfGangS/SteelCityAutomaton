using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class callback_register : AutomatonCommand
    {
        public string tag { get; set; }
        public string ev { get; set; }
        public string url { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.data = new CallBackURL(tag, url);
            CallBackEvent cev;
            if (Enum.TryParse(ev, false, out cev))
            {
                string msg;
                result.success = am.registerHttpCallback(cev, tag, url, out msg);
                result.message = msg;
            }
            else result.message = "invalid event";
            return true;
        }
    }

    public class callback_register_all : AutomatonCommand
    {
        public string tag { get; set; }
        public string url { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.data = new CallBackURL(tag, url);
            string msg;
            result.success = am.registerAllHttpCallbacks(tag, url, out msg);
            result.message = msg;
            return true;
        }
    }

    public class callback_unregister : AutomatonCommand
    {
        public string tag { get; set; }
        public string ev { get; set; }
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.success = false;
            if (tag.Length > 3)
            {
                CallBackEvent cev;
                if (Enum.TryParse(ev, false, out cev))
                {
                    string msg;
                    result.success = am.unregisterHttpCallback(cev, tag, out msg);
                    result.message = msg;
                }
                else result.message = "invalid event";
            }
            else result.message = "Invalid tag";

            return true;
        }
    }

    public class callback_unregister_all : AutomatonCommand
    {
        public string tag { get; set; }
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.success = false;
            if (tag.Length > 3)
            {
                result.success = am.unregisterAllHttpCallbacks(tag);
            }
            else result.message = "Invalid tag";

            return true;
        }
    }
}
