using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net;
using System.Reflection;
using System.Drawing;
using OpenMetaverse;
using OpenMetaverse.Http;
using OpenMetaverse.Imaging;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SteelCityAutomaton;


namespace SteelCityAutomatonServiceCommands
{
    public class ServiceCommand
    {
        public string command {get; set;}
        public CommandResult result {get; set;}

        public ServiceCommand()
        {
            command = this.GetType().Name;
            result = new CommandResult(command);
        }

        public virtual void Setup(JObject json){
            command = this.GetType().Name;
            result = new CommandResult(command);
            foreach(JToken token in json.Children()){
                var property = token as JProperty;
                if (property != null){
                    Type t = this.GetType();
                    PropertyInfo pi = t.GetProperty(property.Name);
                    if (pi != null)pi.SetValue(this, Convert.ChangeType(property.Value,pi.PropertyType));
                }
            }
/*
#if DEBUG
            Console.WriteLine("[Service Command] : " + command);
#endif
*/
            this.PostSetup();
        }

        public virtual void PostSetup()
        {
            //ListProperties();
        }

        public void ListProperties(){
            Console.WriteLine("Properties");
            foreach(PropertyInfo p in this.GetType().GetProperties()){
                if (p.GetValue(this, null) != null){
                    Console.WriteLine("{0} : {1}", p.Name, p.GetValue(this, null).ToString());
                }
            }
        }

        public string serialise(){
            var json = new JavaScriptSerializer().Serialize(this);
            return json;
        }

        public virtual bool Excecute(Dictionary<string, Automaton> automatons,string at)
        {
            return true;
        }

        public virtual string CommandType(){
            return this.GetType().Name;
        }

        public Dictionary<string, string> classesList()
        {
            Dictionary<string, string> classes = new Dictionary<string, string>();
            foreach (Type type in Assembly.GetCallingAssembly().GetTypes().Where(type => type.IsSubclassOf(this.GetType())).ToList())
            {
                ServiceCommand cmd = (ServiceCommand)Activator.CreateInstance(type);
                string json = cmd.serialise();
                List<string> parts = json.Substring(1, json.Length - 2).Split(',').ToList();
                parts.Remove("\"label\":null");
                parts.Remove("\"result\":null");
                parts = parts.OrderBy(q => q).ToList();
                int i = Array.FindIndex(parts.ToArray(), element => element.StartsWith("\"command\":"));
                if (i > 0)
                {
                    string c = parts[i];
                    parts[i] = parts[0];
                    parts[0] = c;
                }
                classes.Add(type.Name, "{" + string.Join(",", parts) + "}");
            }
            return classes;
        }

        public void listclasses()
        {
            foreach (KeyValuePair<string, string> kvp in classesList())
            {
                Console.WriteLine("----==[{0}]==----", kvp.Key);
                Console.WriteLine(kvp.Value);
            }
        }
    }

    public class new_automaton : ServiceCommand
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string password { get; set; }

        public override bool Excecute(Dictionary<string, Automaton> automatons,string at)
        {
            if (firstname == null || firstname.Length < 1 || password.Length < 5)
            {
                result.success = false;
                result.data = "bad user/pass";
                return false;
            }
            foreach (var pair in automatons)
            {
                if(firstname.ToLower() == pair.Value.firstname.ToLower() &&
                    lastname.ToLower() == pair.Value.lastname.ToLower())
                {
                    result.success = false;
                    result.data = pair.Key;
                    result.message = "automaton exists";
                    return false;
                }
            }

            if (lastname == null || lastname.Length < 2) lastname = "Resident";
//#if DEBUG
//            string session = firstname.ToLower() + "." + lastname.ToLower();
//#else
            string session = OpenMetaverse.UUID.Random().ToString();
            while (automatons.ContainsKey(at + session)) session = OpenMetaverse.UUID.Random().ToString();
//#endif
            Automaton am = new Automaton(session, firstname, lastname, password);

            automatons.Add(at + session, am);
            result.data = session;
            result.success = true;
            return true;
        }
    }

    public class list_automatons : ServiceCommand
    {
        public override bool Excecute(Dictionary<string, Automaton> automatons, string at)
        {
            List<am_post> ams = new List<am_post>();
            foreach (var pair in automatons)
            {
                if(pair.Key.StartsWith(at))ams.Add(new am_post(pair.Value));
            }
            result.data = ams.ToArray();
            result.success = true;
            return true;
        }
    }

    public class am_post
    {
        public string session { get; private set; }
        public string firstname { get; private set; }
        public string lastname { get; private set; }
        public bool connected { get; private set; }

        public am_post(Automaton a)
        {
            session = a.session;
            firstname = a.firstname;
            lastname = a.lastname;
            connected = a.connected;
        }
    }

    public class destroy_automaton : ServiceCommand
    {
        public string session { get; set; }

        public override bool Excecute(Dictionary<string, Automaton> automatons, string at)
        {
            result.data = session;
            if(automatons.ContainsKey(at + session))
            {
                automatons[at + session].Logout();
                automatons.Remove(at + session);
                result.success = true;
                result.message = "automaton destroyed";
            }
            else
            {
                result.message = "unrecognized session";
            }
            return true;
        }
    }
}
