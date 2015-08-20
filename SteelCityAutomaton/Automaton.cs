using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net.Http;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Web.Script.Serialization;
using SteelCityAutomatonCommands;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace SteelCityAutomaton
{
    public class Automaton
    {
        private GridClient Client = null;
        private HttpClient httpClient;
        private Dictionary<CallBackEvent, List<CallBackURL>> httpCallbacks;
        public String session {get; set;}
        public String firstname { get; private set; }
        public String lastname { get; private set; }
        private String password;

        public String group_id { get; set; }

        public String sim_name { get; private set; }
        public Vector3 position { get; private set; }
        public bool connected { get; private set; }

        public string avatar_uuid { get; private set; }

        private List<UUID> masters = new List<UUID>();
        public List<UUID> Masters() { return masters; }
        public bool add_master(UUID master)
        {
            if (!masters.Contains(master))
            {
                masters.Add(master);
                return true;
            }
            return false;
        }
        public bool remove_master(UUID master)
        {
            if (masters.Contains(master))
            {
                masters.Remove(master);
                return true;
            }
            return false;
        }

        private Dictionary<string, AutomatonCommand> CommandQueue = new Dictionary<string, AutomatonCommand>();

        private Dictionary<OpenMetaverse.UUID, DialogMenu> Dialogs = new Dictionary<UUID, DialogMenu>();
        public DialogMenu[] get_dialogs(){
            return Dialogs.Values.ToArray();
        }
        public void clear_dialog(UUID id){
            if(Dialogs.ContainsKey(id))Dialogs.Remove(id);
            total_dialogs = Dialogs.Count;
        }
        public int total_dialogs { get; private set; }

        private List<Chat> ChatLog = new List<Chat>();
        public int total_chat { get; private set; }

        private Dictionary<string, TPOffer> TPOffers = new Dictionary<string, TPOffer>();
        public int pending_tp_offers { get; set; }

        private Dictionary<string, IMSession> IMLog = new Dictionary<string, IMSession>();

        private Dictionary<string, PermissionRequest> permissionRequests = new Dictionary<string, PermissionRequest>();
        public int permission_requests { get; private set; }
        public PermissionRequest[] getPermissionRequests()
        {
            return permissionRequests.Values.ToArray();
        }

        public bool replyToPermissionRequest(string id,int perm,out string msg)
        {
            msg = "no such id";
            if (permissionRequests.ContainsKey(id))
            {
                bool res = permissionRequests[id].Respond(Client, perm, out msg);
                permissionRequests.Remove(id);
                permission_requests = permissionRequests.Count;
                return res;
            }
            else return false;
        }
        

        public bool wipe_im_session(string uuid){
            if (IMLog.ContainsKey(uuid))
            {
                IMLog[uuid].wipe();
                sumIMs();
                return true;
            }
            else return false;
        }
        public bool remove_im_session(string uuid){
            if (IMLog.ContainsKey(uuid))
            {
                IMLog[uuid].wipe();
                IMLog.Remove(uuid);
                sumIMs();
                return true;
            }
            else return false;
        }
        public int total_ims { get; set; }

        public int balance { get; private set; }

        private Vector3d autopilot_target;
        private bool autopiloting = false;
        private double autopilot_range = 2.0;

        private Dictionary<UUID, Group> groups = new Dictionary<UUID, Group>();
        public Dictionary<UUID, Group> Groups()
        {
            return groups;
        }


        private AutomatonSettings settings;
        public AutomatonSettings Settings() { return settings; }

        public Automaton(String _Session, String _Firstname, String _Lastname, String _Password)
        {

            session = _Session;
            firstname = _Firstname;
            lastname = _Lastname;
            password = _Password;

            httpClient = new HttpClient();
            httpCallbacks = new Dictionary<CallBackEvent, List<CallBackURL>>();

            settings = new AutomatonSettings();
/*
#if DEBUG
            settings.autotp = true;
#endif
*/
            total_chat = 0;
            total_ims = 0;
            permission_requests = 0;
            total_dialogs = 0;

            balance = 0;

            makeClient();
        }

        private void makeClient()
        {
            if (Client == null)
            {
                Client = new GridClient();

                SetupClient();
                //UnregisterEvents();
                RegisterEvents();
            }
        }

        public bool Login(out string response)
        {
            response = "";

            makeClient();

            if(!Client.Network.Connected)
            {
                if (Client.Network.Login(firstname, lastname, password, "SteelCityAutomaton", "1.0"))
                {
/*
#if DEBUG
                    Console.WriteLine("[LOGIN]Success\nMOTD:{0}", Client.Network.LoginMessage);
#endif
*/
                    response = Client.Network.LoginMessage;
                    avatar_uuid = Client.Self.AgentID.ToString();
                    tick();
                    foreach (FieldInfo p in Client.Settings.GetType().GetFields())
                    {
                        if (p.GetValue(Client.Settings) != null)
                        {
                            Console.WriteLine("{0} : {1}", p.Name, p.GetValue(Client.Settings).ToString());
                        }
                    }
                    //balance = Client.Self.Balance;
                    Client.Self.RequestBalance();
                    //Client.Network.CurrentSim.
                    //Client.Inventory.RequestFolderContents(Client.Inventory.Store.RootFolder.UUID, Client.Self.AgentID, true, false, InventorySortOrder.ByName);
                    return true;
                }
                else
                {
/*
#if DEBUG
                    Console.WriteLine("We were unable to login to Second Life, The Login Server said: {0}", Client.Network.LoginMessage);
#endif
*/
                    response = Client.Network.LoginMessage;
                    tick();
                    return false;
                }
            }
            else return false;
        }
        public bool Logout()
        {
            if (Client != null && Client.Network.Connected)
            {
                Client.Network.Logout();
                tick();
                Client = null;
                makeClient();
                return true;
            }
            else return false;
        }

        public string processCommands(JObject json)
        {
            string response = "";
            List<CommandResult> results = new List<CommandResult>();

            if (json["commands"].GetType() == typeof(Newtonsoft.Json.Linq.JArray))
            {
                JArray commands = (JArray)json["commands"];
                foreach (JObject command in commands)
                {
                    var type = Type.GetType("SteelCityAutomatonCommands." + (string)command["command"]);
                    if (type != null)
                    {
                        AutomatonCommand cmd = (AutomatonCommand)Activator.CreateInstance(type);
                        cmd.Setup(command);
                        if (Client != null) cmd.Excecute(this, Client, false);
                        else cmd.result.message = "Client Not Yet initialized";
                        results.Add(cmd.result);
                    }
                    else
                    {
                        CommandResult result = new CommandResult((string)command["command"]);
                        result.success = false;
                        result.data = "unrecognized command";
                        results.Add(result);
/*                        
#if DEBUG
                        Console.WriteLine("CMD NOT RECOGNIZED - {0} : {1}", command["command"], "null");
#endif
*/
                    }
                }
                CommandResponse resp = new CommandResponse((string)json["label"], results);
                response = resp.serialise();
            }
            return response;
        }

        public void tick()
        {
            if (Client == null)
            {
                connected = false;
                sim_name = null;
                position = Vector3.Zero;
                return;
            }
            connected = Client.Network.Connected;
            if(connected && Client.Network.CurrentSim != null)
            {
                sim_name = Client.Network.CurrentSim.Name;
                position = Client.Self.SimPosition;
                if(autopiloting)
                {
                    Vector3d pos = Client.Self.GlobalPosition;
                    Vector3d com = autopilot_target;
                    if (!Client.Self.Movement.Fly)
                    {
                        pos.Z = 0;
                        com.Z = 0;
                    }
                    double d = Vector3d.Distance(pos, com);
                    if (d <= autopilot_range)
                    {
                        autopiloting = false;
                        Client.Self.AutoPilotCancel();
                    }
                }
            }
            else
            {
                sim_name = null;
                position = Vector3.Zero;
            }
        }

        public void AutoPilot(Vector3d target, double range)
        {
            Client.Self.AutoPilot(target.X, target.Y, target.Z);
            autopiloting = true;
            autopilot_target = target;
            autopilot_range = range;
            tick();
        }
        public void AutoPilotStop()
        {
            autopiloting = false;
            Client.Self.AutoPilotCancel();
        }


        private void SetupClient()
        {
            Client.Settings.CACHE_PRIMITIVES = true;
            //Client.Settings.LOG_LEVEL = Helpers.LogLevel.Debug;
            Client.Settings.LOG_RESENDS = false;
            Client.Settings.STORE_LAND_PATCHES = true;
            Client.Settings.ALWAYS_DECODE_OBJECTS = true;
            Client.Settings.ALWAYS_REQUEST_OBJECTS = true;
            Client.Settings.SEND_AGENT_UPDATES = true;
            Client.Settings.USE_ASSET_CACHE = true;
            Client.Settings.FETCH_MISSING_INVENTORY = true;
/*
#if DEBUG
            Console.WriteLine("Obj Decode : {0}", Client.Settings.ALWAYS_DECODE_OBJECTS);
            Console.WriteLine("Obj Request : {0}", Client.Settings.ALWAYS_REQUEST_OBJECTS);
            Console.WriteLine("Av Tracking : {0}", Client.Settings.AVATAR_TRACKING);
            Console.WriteLine("Prim Cache : {0}", Client.Settings.CACHE_PRIMITIVES);
            Console.WriteLine("Sen Apperance : {0}", Client.Settings.SEND_AGENT_APPEARANCE);
            Console.WriteLine("Agent Updates : {0}", Client.Settings.SEND_AGENT_UPDATES);
            Console.WriteLine("Decode : {0}", Client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK);
            Console.WriteLine("Feth Inv Missing : {0}", Client.Settings.FETCH_MISSING_INVENTORY);
            Console.WriteLine("Http Inv : {0}", Client.Settings.HTTP_INVENTORY);
#endif
*/
        }

        private void RegisterEvents(){
            if (Client == null) return;
            Client.Network.SimChanged += Network_SimChanged;

            Client.Self.IM += Self_IM;
            Client.Self.ChatFromSimulator += Self_ChatFromSimulator;

            Client.Self.ScriptDialog += Self_ScriptDialog;

            Client.Network.RegisterCallback(PacketType.AgentDataUpdate, AgentDataUpdateHandler);

            Client.Objects.AvatarUpdate += new EventHandler<AvatarUpdateEventArgs>(Objects_AvatarUpdate);
            Client.Objects.TerseObjectUpdate += new EventHandler<TerseObjectUpdateEventArgs>(Objects_TerseObjectUpdate);

            Client.Self.ScriptQuestion += _Self_Permission_Request;

            Client.Groups.CurrentGroups += Self_Current_Groups;

            Client.Self.MoneyBalance += Self_MoneyBalance;
            Client.Self.MoneyBalanceReply += Self_MoneyBalanceReply;

        }

        private void Self_MoneyBalanceReply(object sender, MoneyBalanceReplyEventArgs e)
        {
            balance = e.Balance;
        }

        private void Self_MoneyBalance(object sender, BalanceEventArgs e)
        {
            balance = e.Balance;
        }

        private void _Self_Permission_Request(object sender, ScriptQuestionEventArgs e)
        {
            if (permissionRequests.ContainsKey(e.ItemID.ToString())) permissionRequests[e.ItemID.ToString()] = new PermissionRequest(e);
            else permissionRequests.Add(e.ItemID.ToString(), new PermissionRequest(e));
            httpCallback(CallBackEvent.permissions, e.ItemID.ToString(), permissionRequests[e.ItemID.ToString()]);
            permission_requests = permissionRequests.Count;
            //riptPermission.
            //Client.Self.ScriptQuestionReply();
        }

        private void Self_Current_Groups(object sender, CurrentGroupsEventArgs e)
        {
            groups = e.Groups;
        }
        private void AgentDataUpdateHandler(object sender, PacketReceivedEventArgs e)
        {
            AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
            if (p.AgentData.AgentID == e.Simulator.Client.Self.AgentID && p.AgentData.ActiveGroupID != UUID.Zero)
            {
                group_id = p.AgentData.ActiveGroupID.ToString();

                //GroupMembersRequestID = e.Simulator.Client.Groups.RequestGroupMembers(GroupID);
            }
        }
        void Objects_TerseObjectUpdate(object sender, TerseObjectUpdateEventArgs e)
        {
            if (e.Prim.LocalID == Client.Self.LocalID)
            {
                SetDefaultCamera();
            }
        }

        void Objects_AvatarUpdate(object sender, AvatarUpdateEventArgs e)
        {
            if (e.Avatar.LocalID == Client.Self.LocalID)
            {
                SetDefaultCamera();
            }
        }
        public void SetDefaultCamera()
        {
            // SetCamera 5m behind the avatar
            Client.Self.Movement.Camera.LookAt(
                Client.Self.SimPosition + new Vector3(-5, 0, 0) * Client.Self.Movement.BodyRotation,
                Client.Self.SimPosition
            );
        }
        void Network_SimChanged(object sender, SimChangedEventArgs e)
        {
            Dialogs.Clear();
            permissionRequests.Clear();
            permission_requests = 0;
            total_dialogs = 0;
            tick();
        }
        void Self_ScriptDialog(object sender, ScriptDialogEventArgs e)
        {
            if(Dialogs.ContainsKey(e.ObjectID))Dialogs.Remove(e.ObjectID);
            Dialogs.Add(e.ObjectID,new DialogMenu(e));
            httpCallback(CallBackEvent.dialog, e.ObjectID.ToString(), Dialogs[e.ObjectID]);
            total_dialogs = Dialogs.Count;
        }
        void Self_ChatFromSimulator(object sender, ChatEventArgs e){
            string head;
            ConsoleColor col;
            switch(e.Type)
            {
                case ChatType.OwnerSay:
                        head = "OWNER";
                        if (e.Message[0] == '@')
                        {
                            //if(RLV(e))return;
                        }
                        else if(e.Message[0] == '#')
                        {
                            if(e.Message.Length > 1)
                            {
                                string msg = e.Message.Substring(1);
                                JObject json;
                                try
                                {
                                    json = JObject.Parse(msg);
                                }
                                catch (Exception er)
                                {
#if DEBUG
                                    Console.WriteLine("[JSON]:{0}:{1}", er.Message, er.StackTrace);
#endif
                                    return;
                                }
                                string resp = processCommands(json);
                                int chan = 0;
                                if (!int.TryParse((string)json["channel"], out chan)) chan = 175971596;
                                Client.Self.Chat(resp, chan, ChatType.Normal);
                                return;
                            }
                        }
                        col = ConsoleColor.Yellow;
                    break;
                case ChatType.Normal:
                        head = " CHAT";
                        col = ConsoleColor.Green;
                    break;
                case ChatType.Whisper:
                        head = "QUIET";
                        col = ConsoleColor.Yellow;
                    break;
                case ChatType.Shout:
                        head = "SHOUT";
                        col = ConsoleColor.Yellow;
                    break;
                case ChatType.StartTyping:
                case ChatType.StopTyping:
                    return;
                default:
                        head = "DEF";
                        col = ConsoleColor.Gray;
                    break;
            }
/*
#if DEBUG
            string txt = String.Format("[{0}]{1}:{2}", head, e.FromName, e.Message);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[");
            Console.ForegroundColor = col;
            Console.Write(head);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(e.FromName);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(e.Message);
            Console.ResetColor();
            Console.WriteLine("");
#endif
*/
            ++total_chat;
            ChatLog.Add(new Chat(e, ChatLog.Count));
            httpCallback(CallBackEvent.chat, e.SourceID.ToString(), ChatLog.Last());
        }
        void Self_IM(object sender, InstantMessageEventArgs e){
            
            switch(e.IM.Dialog)
            {
                case InstantMessageDialog.MessageFromAgent:
/*
#if DEBUG
                        Console.WriteLine("  IM :{0}:{1}", e.IM.FromAgentName, e.IM.Message);
#endif
*/
                        if (masters.Contains(e.IM.FromAgentID) && e.IM.Message.Length > 1 && e.IM.Message[0] == '¬')
                        {
                            string msg = e.IM.Message.Substring(1);
                            JObject json;
                            try
                            {
                                json = JObject.Parse(msg);
                            }
                            catch (Exception er)
                            {
                                Client.Self.InstantMessage(e.IM.FromAgentID, "invalid json");
                                return;
                            }
                            string res = processCommands(json);
                            if (res.Length > 1000) res = res.Substring(0, 1000);
                            Client.Self.InstantMessage(e.IM.FromAgentID, res);
                            return;
                        }
                        newIM(e.IM);
                    break;
                case InstantMessageDialog.RequestTeleport:
/*
#if DEBUG
                        Console.WriteLine(" LURE:{0}:{1}", e.IM.FromAgentName, e.IM.Message);
#endif
*/
                        if (settings.autotp)
                        {
                            if (masters.Contains(e.IM.FromAgentID)){
                                Client.Self.TeleportLureRespond(e.IM.FromAgentID, e.IM.IMSessionID, true);
                                break;
                            }
                        }
                        if (TPOffers.ContainsKey(e.IM.FromAgentID.ToString())) TPOffers[e.IM.FromAgentID.ToString()] = new TPOffer(e.IM);
                        else TPOffers.Add(e.IM.FromAgentID.ToString(), new TPOffer(e.IM));
                        httpCallback(CallBackEvent.tp_offer, e.IM.FromAgentID.ToString(), TPOffers[e.IM.FromAgentID.ToString()]);
                        pending_tp_offers = TPOffers.Count();
                    break;
                case InstantMessageDialog.RequestLure:
                    Console.WriteLine("Teleport request : {0} {1} {2}", e.IM.FromAgentName, e.IM.Message, e.IM.Timestamp.ToLongTimeString());
                    break;
                default:
                    break;
            }
        }
        public void newIM(InstantMessage i)
        {
            if (IMLog.ContainsKey(i.FromAgentID.ToString())) IMLog[i.FromAgentID.ToString()].newIM(i);
            else IMLog.Add(i.FromAgentID.ToString(), new IMSession(i));
            httpCallback(CallBackEvent.im, new string[] { i.FromAgentID.ToString() }, new JavaScriptSerializer().Serialize(IMLog[i.FromAgentID.ToString()].messages.Last()));
            sumIMs();
        }

        public bool registerAllHttpCallbacks(string tag,string url,out string msg)
        {
            msg = "";
            if (url.Length < 10)
            {
                msg = "short url";
                return false;
            }
            if (url.Substring(url.Length - 1) != "/") url += "/";
            Uri uriResult;
            bool res = Uri.TryCreate(url, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
            if (!res)
            {
                msg = "invalid url";
                return false;
            }
            if (tag.Length < 3)
            {
                msg = "invalid tag";
                return false;
            }
            tag = tag.ToLower();
            foreach (CallBackEvent ev in Enum.GetValues(typeof(CallBackEvent)))
            {
                if (!httpCallbacks.ContainsKey(ev)) httpCallbacks.Add(ev, new List<CallBackURL>());
                bool done = false;
                foreach (CallBackURL cb in httpCallbacks[ev])
                {
                    if (cb.tag == tag)
                    {
                        cb.url = url;
                        done = true;
                    }
                }
                if (!done) httpCallbacks[ev].Add(new CallBackURL(tag, url));
            }
            return true;
        }

        public bool registerHttpCallback(CallBackEvent ev,string tag,string url, out string msg)
        {
            msg = "";
            //Console.WriteLine("url : {0}", url);
            //Console.WriteLine("tag : {0}", tag);
            //Console.WriteLine("ev  : {0}", ev);
            if (url.Length < 10)
            {
                msg = "short url";
                return false;
            }
            //Console.WriteLine("reghttp 0");
            if (url.Substring(url.Length - 1) != "/") url += "/";
            //Console.WriteLine("reghttp 1");
            Uri uriResult;
            bool res = Uri.TryCreate(url, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
            //Console.WriteLine("reghttp 2");
            if(!res)
            {
                msg = "invalid url";
                return false;
            }
            if (ev < CallBackEvent.num_types && ev > 0)
            {
                msg = "not a valid event";
                return false;
            }
            if (tag.Length < 3)
            {
                msg = "invalid tag";
                return false;
            }
            tag = tag.ToLower();
            //Console.WriteLine("reghttp 3");
            if (!httpCallbacks.ContainsKey(ev)){
                httpCallbacks.Add(ev, new List<CallBackURL>());
            }
            //Console.WriteLine("reghttp 4");
            foreach (CallBackURL cb in httpCallbacks[ev])
            {
                if (cb.tag == tag)
                {
                    cb.url = url;
                    return true;
                }
            }
            httpCallbacks[ev].Add(new CallBackURL(tag, url));
            return true;
        }

        public bool unregisterAllHttpCallbacks(string tag)
        {
            tag = tag.ToLower();
            foreach(KeyValuePair<CallBackEvent,List<CallBackURL>> kvp in httpCallbacks)
            {
                int i = kvp.Value.Count;
                while (--i > -1)
                {
                    if (kvp.Value[i].tag == tag) kvp.Value.RemoveAt(i);
                }
            }
            return true;
        }

        public bool unregisterHttpCallback(CallBackEvent ev, string tag, out string msg)
        {
            tag = tag.ToLower();
            msg = "";
            if (ev < CallBackEvent.num_types && ev > 0)
            {
                msg = "not a valid event";
                return false;
            }
            if (!httpCallbacks.ContainsKey(ev))
            {
                msg = "no event registered";
                return false;
            }
            int i = httpCallbacks[ev].Count;
            while(--i > -1)
            {
                if (httpCallbacks[ev][i].tag == tag) httpCallbacks[ev].RemoveAt(i);
            }
            return true;
        }
        public void httpCallback(CallBackEvent ev, string path, object data){
            httpCallback(ev, new string[] { path }, new JavaScriptSerializer().Serialize(data));
        }
        public void httpCallback(CallBackEvent ev,string[] path,string data)
        {
            if (ev >= CallBackEvent.num_types || ev <= 0) return;
            //CallbackEvents = { "im", "chat" };
            if (httpCallbacks.ContainsKey(ev))
            {
                foreach(CallBackURL callback in httpCallbacks[ev])
                {
                    httpClient.PostAsync(callback.url + session + "/" + ev.ToString() + "/" + string.Join("/",path), new StringContent(data, Encoding.UTF8, "application/json"));
                }
            }
        }

        private void sumIMs()
        {
            int total = 0;
            foreach(var i in IMLog)
            {
                total += i.Value.count();
            }
            total_ims = total;
        }

        public Dictionary<string,int> getIMSessions()
        {
            Dictionary<string, int> ims = new Dictionary<string, int>();
            foreach (var pair in IMLog) ims.Add(pair.Key, pair.Value.count());
            return ims;
        }
        public IMSession GetIMLogFrom(string id,int first,int last = -1)
        {
            if (IMLog.ContainsKey(id))
            {
                IMSession resp = IMSession.partialIMSession(IMLog[id], first, last);
                return resp;
            }
            return null;
        }
        public IMSession GetIMLogFromPaged(string id,int page, int perpage)
        {
            int first = page * perpage;
            int last = first + (perpage - 1);
            if (first >= ChatLog.Count) return null;
            if (last >= ChatLog.Count) last = ChatLog.Count - 1;
            return GetIMLogFrom(id, first, last);
        }

        public void ClearChatLog()
        {
            ChatLog = new List<Chat>();
            total_chat = 0;
        }
        public Chat[] GetChatLogFrom(int first, int last = -1)
        {
            if (ChatLog.Count > first)
            {
                int l = last - first;
                if (l < 0 || last < 0 || (l + first) >= ChatLog.Count) l = ChatLog.Count - first;
                return ChatLog.GetRange(first, l).ToArray();
            }
            return null;
        }
        public Chat[] GetChatFromSimulatorPaged(int page, int perpage)
        {
            int first = page * perpage;
            int last = first + (perpage - 1);
            if (first >= ChatLog.Count) return null;
            if (last >= ChatLog.Count) last = ChatLog.Count - 1;
            return GetChatLogFrom(first, last);
        }

        public TPOffer[] GetTPOffers()
        {
            return TPOffers.Values.ToArray();
        }

        public bool AcceptTPOffer(string uuid, bool accept = true)
        {
            UUID id;
            if (TPOffers.ContainsKey(uuid) && UUID.TryParse(uuid,out id))
            {
                TPOffer tp = TPOffers[uuid];
                Client.Self.TeleportLureRespond(UUID.Parse(tp.user_id), UUID.Parse(tp.session_id), accept);
                TPOffers.Remove(uuid);
                pending_tp_offers = TPOffers.Count();
                return true;
            }
            return false;
        }

        public string serialise()
        {
            var json = new JavaScriptSerializer().Serialize(this);
            return json;
        }

        public bool set_group(string grp)
        {
            UUID group;
            if (!UUID.TryParse(grp, out group))
            {
                foreach (var g in groups.Values)
                {
                    if (g.Name.ToLower() == grp)
                    {
                        group = g.ID;
                    }
                }
            }
            if (groups.ContainsKey(group))
            {
                Client.Groups.ActivateGroup(group);
                return true;
            }
            else return false;
        }

    }

    public class Chat{
        public int id = 0;
        public string source { get; private set; }
        public string message { get; private set; }
        public string from_id { get; private set; }
        public string from_name { get; private set; }
        public Int32 timestamp { get; private set; }
        
        public Chat(ChatEventArgs e,int _id){
            id = _id;
            source = e.SourceType.ToString();
            message = e.Message;
            from_id = e.SourceID.ToString();
            from_name = e.FromName;
            timestamp = Helper.unixTimeStamp();
        }
    }
    public class IMSession {
        public string user_id { get; private set; }
        public string user_name { get; private set; }
        public List<IM> messages { get; private set; }
        
        public IMSession()
        {
        }

        public void wipe(){
            messages.Clear();
        }

        public IMSession(InstantMessage i){
            user_id = i.FromAgentID.ToString();
            bool other = false;
            if (i.FromAgentName != null && i.FromAgentName != "")
            {
                user_name = i.FromAgentName;
                other = true;
            }
            messages = new List<IM>();
            messages.Add(new IM(i,other,messages.Count));
        }

        public int count()
        {
            return messages.Count();
        }

        public static IMSession partialIMSession(IMSession session,int first,int last){
            IMSession sesh = new IMSession();
            sesh.user_id = session.user_id;
            sesh.user_name = session.user_name;
            if (session.messages.Count > first)
            {
                int l = last - first;
                if (l < 0 || last < 0 || (l + first) >= session.messages.Count) l = session.messages.Count - first;
                int read = session.messages.Count - (first + l);
                sesh.messages = session.messages.GetRange(first, l);
            }
            else sesh.messages = null;
            return sesh;
        }

        public void newIM(InstantMessage i){
            bool other = false;
            if (i.FromAgentName != null && i.FromAgentName != "")
            {
                user_name = i.FromAgentName;
                other = true;
            }
            messages.Add(new IM(i,other,messages.Count));
        }
    }
    public class IM{
        public string message { get; private set; }
        public Int32 timestamp { get; private set; }
        public bool from_other { get; private set; }
        public int id { get; private set; }
        
        public IM(InstantMessage i,bool _from_other,int _id){
            message = i.Message;
            timestamp = Helper.unixTimeStamp();
            from_other = _from_other;
            id = _id;
        }
    }

    public class TPOffer{
        public string user_id { get; private set; }
        public string user_name { get; private set; }
        public string session_id { get; private set; }
        public string message { get; private set; }

        public TPOffer(InstantMessage im)
        {
            user_id = im.FromAgentID.ToString();
            user_name = im.FromAgentName;
            session_id = im.IMSessionID.ToString();
            message = im.Message;
        }
    }

    public class CallBackURL{
        public string tag { get; private set; }
        public string url { get; set; }
        public CallBackURL(string t, string u){
            tag = t;
            url = u;
        }
    }

    public class DialogMenu{
        public List<string> buttons { get; private set; }
        public int channel { get; private set; }
        public string uuid { get; private set; }
        public string name { get; private set; }
        public string owner_uuid { get; private set; }
        public string message { get; private set; }

        public DialogMenu(ScriptDialogEventArgs e){
            buttons = e.ButtonLabels;
            channel = e.Channel;
            name = e.ObjectName;
            uuid = e.ObjectID.ToString();
            owner_uuid = e.OwnerID.ToString();
            message = e.Message;
        }
    }

    public class PermissionRequest
    {
        public string obj_uuid { get; private set; }
        public string scr_uuid { get; private set; }
        public string obj_name { get; private set; }
        public int perms { get; private set; }
        private UUID regionID;
        private UUID objID;
        private UUID scrID;
        
        public PermissionRequest(ScriptQuestionEventArgs q)
        {
            objID = q.ItemID;
            scrID = q.TaskID;
            scr_uuid = q.ItemID.ToString();
            obj_uuid = q.TaskID.ToString();
            obj_name = q.ObjectName;
            perms = (int)q.Questions;
            regionID = q.Simulator.RegionID;
        }
        
        public bool Respond(GridClient client, int p,out string msg)
        {
            msg = "region error";
            if(client.Network.CurrentSim.RegionID == regionID){
                msg = "";
                client.Self.ScriptQuestionReply(client.Network.CurrentSim, objID, scrID, (ScriptPermission)p);
                return true;
            }
            return false;
        }
    }

    public class AutomatonSettings{
        public bool autotp { get; set; }

        public AutomatonSettings(){
            autotp = false;
        }
    }

    public enum CallBackEvent : int
    {
        none,
        im,
        chat,
        tp_offer,
        tp_request,
        permissions,
        dialog,


        num_types
        //"im", "chat", "tp_offer","tp_request","permission_request","dialog"
    }
    
}
