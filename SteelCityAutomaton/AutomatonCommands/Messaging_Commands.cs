using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class say : chat { }
    public class chat : AutomatonCommand
    {
        public int channel { get; set; }
        public string message { get; set; }
        public string chat_type { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (message != null && client.Network.Connected)
            {
                client.Self.Chat(message, channel, ChatType.Normal);
                result.success = true;
            }
            else
            {
                result.success = false;
                result.message = message;
            }
            return true;
        }
    }

    public class instant_message : im { }
    public class send_im : im { }
    public class im : AutomatonCommand
    {
        public string uuid { get; set; }
        public string message { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            UUID target;
            result.success = false;
            if(UUID.TryParse(uuid, out target))
            {
                if (client.Network.Connected)
                {
                    if (message != null && message.Length > 0)
                    {
                        InstantMessage i = new InstantMessage();
                        i.FromAgentID = target;
                        i.Message = message;
                        am.newIM(i);
                        client.Self.InstantMessage(target, message);
                        result.success = true;
                    }
                    else
                    {
                        result.message = "message null or 0 length [" + message + "]";
                    }
                }
                else
                {
                    result.message = "not connected";
                }
            }
            else
            {
                result.message = "uuid faised";
            }
            return true;
        }
    }

    public class clear_chat_log : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            am.ClearChatLog();
            result.success = true;
            result.message = "cleared chat log";
            return true;
        }
    }

    public class get_chat_log : AutomatonCommand
    {
        public int first { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (first < 0) first = 0;
            result.data = am.GetChatLogFrom(first);
            if (result.data != null) result.success = true;
            return true;
        }
    }

    public class get_im_sessions : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            Dictionary<string, int> ims = am.getIMSessions();
            if (ims.Count > 0) result.success = true;
            result.data = ims;
            return true;
        }
    }

    public class get_im_log : AutomatonCommand
    {
        public int first { get; set; }
        public string uuid { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.data = am.GetIMLogFrom(uuid, first);
            if (result.data != null) result.success = true;
            return true;
        }
    }

    public class clear_im_session : AutomatonCommand
    {
        public string uuid { get; set; }
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.success = am.wipe_im_session(uuid);
            if (!result.success) result.message = "No im session with : " + uuid;
            return true;
        }
    }

    public class remove_im_session : AutomatonCommand
    {
        public string uuid { get; set; }
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.success = am.remove_im_session(uuid);
            if (!result.success) result.message = "No im session with : " + uuid;
            return true;
        }
    }
}
