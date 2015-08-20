using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class get_status : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            result.data = am;
            result.success = true;
            result.message = "status of bot";
            return true;
        }
    }

    public class get_region_data : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            result.data = new RegionData(client);
            return true;
        }
    }

    public class get_region_data_2 : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            result.data = new RegionData(client,true);
            return true;
        }
    }

    public class get_object_list : AutomatonCommand
    {
        public float radius { get; set; }

        private Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        private AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            if (radius < 1) radius = 20;
            Vector3 location = client.Self.SimPosition;
            List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    Vector3 pos = prim.Position;
                    return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < radius));
                }
            );

            bool complete = RequestObjectProperties(prims, 250, client);

            Dictionary<string, string> objs = new Dictionary<string, string>();

            //List<ObjectBones> objs = new List<ObjectBones>();
            
            foreach(Primitive p in prims)
            {
                objs.Add(p.ID.ToString(),(p.Properties != null ? p.Properties.Name : "null"));
            }

            result.data = objs;
            result.success = true;

            return true;
        }

        private bool RequestObjectProperties(List<Primitive> objects, int msPerRequest, GridClient client)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for
            uint[] localids = new uint[objects.Count];

            lock (PrimsWaiting)
            {
                PrimsWaiting.Clear();

                for (int i = 0; i < objects.Count; ++i)
                {
                    localids[i] = objects[i].LocalID;
                    PrimsWaiting.Add(objects[i].ID, objects[i]);
                }
            }

            client.Objects.SelectObjects(client.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }

        void Objects_OnObjectProperties(object sender, ObjectPropertiesEventArgs e)
        {
            lock (PrimsWaiting)
            {
                Primitive prim;
                if (PrimsWaiting.TryGetValue(e.Properties.ObjectID, out prim))
                {
                    prim.Properties = e.Properties;
                }
                PrimsWaiting.Remove(e.Properties.ObjectID);

                if (PrimsWaiting.Count == 0)
                    AllPropertiesReceived.Set();
            }
        }
    }

    public class get_attachment_list : AutomatonCommand
    {
        public string uuid { get; set; }

        private Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        private AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            UUID target;
            if (UUID.TryParse(uuid, out target))
            {
                Vector3 location = client.Self.SimPosition;
                List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                    delegate(Primitive prim)
                    {
                        return (prim.IsAttachment);
                    }
                );

                List<Primitive>p_prims = new List<Primitive>();
                List<Primitive>np_prims = new List<Primitive>();
                foreach (Primitive p in prims)
                {
                    if (p.Properties == null) np_prims.Add(p);
                    else p_prims.Add(p);
                }

                if (np_prims.Count > 0)
                {
                    bool complete = RequestObjectProperties(np_prims, 250, client);
                }
                
                p_prims.AddRange(np_prims);

                prims = p_prims;


                Dictionary<string, string> objs = new Dictionary<string, string>();

                //List<ObjectBones> objs = new List<ObjectBones>();

                foreach (Primitive p in prims)
                {
                    if(p.Properties != null)
                    {
                        if (p.Properties.OwnerID == target)
                        {
                            objs.Add(p.ID.ToString(), p.Properties.Name);
                        }
                    }
                }

                result.data = objs;
                result.success = true;
            }
            else result.message = "Invalid uuid";
            return true;
        }

        private bool RequestObjectProperties(List<Primitive> objects, int msPerRequest, GridClient client)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for
            uint[] localids = new uint[objects.Count];

            lock (PrimsWaiting)
            {
                PrimsWaiting.Clear();

                for (int i = 0; i < objects.Count; ++i)
                {
                    localids[i] = objects[i].LocalID;
                    PrimsWaiting.Add(objects[i].ID, objects[i]);
                }
            }

            client.Objects.SelectObjects(client.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }

        void Objects_OnObjectProperties(object sender, ObjectPropertiesEventArgs e)
        {
            lock (PrimsWaiting)
            {
                Primitive prim;
                if (PrimsWaiting.TryGetValue(e.Properties.ObjectID, out prim))
                {
                    prim.Properties = e.Properties;
                }
                PrimsWaiting.Remove(e.Properties.ObjectID);

                if (PrimsWaiting.Count == 0)
                    AllPropertiesReceived.Set();
            }
        }
    }

    public class get_obj_data : get_object_data { }
    public class get_object_data : AutomatonCommand
    {
        public string uuid { get; set; }


        Primitive Waiting = new Primitive();
        private AutoResetEvent PropertiesReceived = new AutoResetEvent(false);

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            UUID prim_id;
            if (UUID.TryParse(uuid, out prim_id))
            {
                Waiting = client.Network.CurrentSim.ObjectsPrimitives.Find(
                    delegate(Primitive prim)
                    {
                        return (prim.ID == prim_id);
                    }
                );
                bool complete = RequestObjectProperties(Waiting, 250, client);
                result.data = new SL_Object(Waiting);
            }
            else result.data = "Invalid uuid";
            return true;
        }

        private bool RequestObjectProperties(Primitive obj, int msPerRequest, GridClient client)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for

            client.Objects.SelectObject(client.Network.CurrentSim, obj.LocalID);

            return PropertiesReceived.WaitOne(2000 + msPerRequest, false);
        }

        void Objects_OnObjectProperties(object sender, ObjectPropertiesEventArgs e)
        {
            lock (Waiting)
            {
                Waiting.Properties = e.Properties;
                PropertiesReceived.Set();
            }
        }
    }

    //Data Stuctures

    public class SL_Object
    {
        public string uuid { get; private set; }
        public uint local_id { get; private set; }
        public string name { get; private set; }
        public string creator_id { get; private set; }

        public SL_Object(Primitive prim)
        {
            uuid = prim.ID.ToString();
            local_id = prim.LocalID;
            name = prim.Properties.Name;

            creator_id = prim.Properties.CreatorID.ToString();
        }
    }
     
    public class RegionData
    {
        public string name { get; private set; }
        public uint x { get; private set; }
        public uint y { get; private set; }
        public RegionAgent[] agents { get; private set; }
        public RegionParcel[] parcels { get; private set; }

        public RegionData(GridClient client)
        {
            name = client.Network.CurrentSim.Name;
            uint xi, yi;
            Utils.LongToUInts(client.Network.CurrentSim.Handle, out xi, out yi);
            x = xi;
            y = yi;
            agents = RegionAgent.agentsFromList(client.Network.CurrentSim.AvatarPositions);
            parcels = RegionParcel.parcelsFromList(client.Network.CurrentSim.Parcels);
        }

        public RegionData(GridClient client,bool Obj)
        {
            name = client.Network.CurrentSim.Name;
            uint xi, yi;
            Utils.LongToUInts(client.Network.CurrentSim.Handle, out xi, out yi);
            x = xi;
            y = yi;
            agents = RegionAgent.agentsFromList(client.Network.CurrentSim.ObjectsAvatars);
            parcels = RegionParcel.parcelsFromList(client.Network.CurrentSim.Parcels);
        }
    }

    public class RegionParcel
    {
        public string name { get; private set; }
        public string description { get; private set; }
        private UUID group_uuid = UUID.Zero;
        public string group_id { get { return group_uuid.ToString(); } set { } }
        private UUID owner_uuid = UUID.Zero;
        public string owner_id { get { return owner_uuid.ToString(); } set { } }

        public RegionParcel(Parcel p)
        {
            name = p.Name;
            description = p.Desc;
            group_uuid = p.GroupID;
            owner_uuid = p.OwnerID;
        }

        public static RegionParcel[] parcelsFromList(InternalDictionary<int, Parcel> ps)
        {
            List<RegionParcel> parcels = new List<RegionParcel>();
            ps.ForEach(delegate(Parcel p)
            {

            });


            return parcels.ToArray();
        }

    }

    public class RegionAgent
    {
        public string name { get; set; }
        private UUID uuid = UUID.Zero;
        public string user_id { get { return uuid.ToString(); } set { } }
        private Vector3 pos;
        public string position { get { return pos.ToString(); } set { } }

        public RegionAgent(Avatar av)
        {
            name = av.Name;
            uuid = av.ID;
            pos = av.Position;
        }

        public RegionAgent(UUID k, Vector3 p)
        {
            pos = p;
            uuid = k;
        }

        public static RegionAgent[] agentsFromList(InternalDictionary<uint,Avatar> avs)
        {
            List<RegionAgent> agents = new List<RegionAgent>();
            avs.ForEach(delegate(KeyValuePair<uint, Avatar> pair)
            {
                agents.Add(new RegionAgent(pair.Value));
            });
            return agents.ToArray();
        }

        public static RegionAgent[] agentsFromList(InternalDictionary<UUID, Vector3> avs)
        {
            List<RegionAgent> agents = new List<RegionAgent>();
            avs.ForEach(delegate(KeyValuePair<UUID, Vector3> pair)
            {
                agents.Add(new RegionAgent(pair.Key, pair.Value));
            });
            return agents.ToArray();
        }
    }
}
