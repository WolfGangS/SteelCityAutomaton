using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using OpenMetaverse;
using SimpleWebServer;

using SteelCityAutomaton;
using SteelCityAutomatonServiceCommands;
using SteelCityAutomatonCommands;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Web.Script.Serialization;

namespace SteelCityd
{
    class Program
    {
        private static Dictionary<string, Automaton> automatons = new Dictionary<string, Automaton>();
        private readonly HttpListener _listener = new HttpListener();
#if DEBUG
        private static string url = "http://localhost:1759/";
#else
        private static string url = "http://*:1759/";
#endif

        private static Timer timer;

        private static string accessToken = "";

        private static bool webPage = false;

        static void Main(string[] args)
        {
            bool exit = false;

            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-token":
                    case "-t":
                        if(i < args.Length - 1)
                        {
                            ++i;
                            accessToken = args[i].ToLower();
                        }
                        break;
                    case "-tg":
                    case "-tokengen":
                        accessToken = UUID.Random().ToString();
                        break;
                    case "-l":
                    case "-list":
                        AutomatonCommand atm = new AutomatonCommand();
                        string[] lines = atm.classesList().Values.ToArray().OrderBy(q => q).ToArray();
                        File.WriteAllLines("automaton_commands.txt", lines, Encoding.UTF8);

                        ServiceCommand svc = new ServiceCommand();
                        lines = svc.classesList().Values.ToArray();
                        File.WriteAllLines("service_commands.txt", lines, Encoding.UTF8);

                        exit = true;
                        break;
                    case "-w":
                    case "-web":
                        webPage = true;
                        break;
                }
            }
            Console.WriteLine("----==[Access Token]==----");
            Console.WriteLine(accessToken);
            Console.WriteLine("--------------------------");
            WebServer ws = null;
            if(!exit){
                ws = new WebServer(GetHttpResponse, url);
                ws.Run();
                Console.WriteLine("Starting Service on " + url + ". Press a key to quit.");
            }

            string arg;
//#if DEBUG
            /*
            GridClient test = new GridClient() ;
            foreach (FieldInfo p in test.Settings.GetType().GetFields())
            {
                if (p.GetValue(test.Settings) != null)
                {
                    Console.WriteLine("{0} : {1}", p.Name, p.GetValue(test.Settings).ToString());
                }
            }
            test = null;
            */
            //Process.Start(url);
//#endif
            timer = new Timer(timer_tick, null, 0, 1000);

            while (!exit)
            {
                arg = Console.ReadLine();
                if (arg == "exit") exit = true;
                else if (arg == "help") Console.WriteLine("THIS APPLICATION USES A WEB API, USE IT FUCKWAD.");
            }
            if(ws != null)ws.Stop();
            foreach (var pair in automatons)
            {
                pair.Value.Logout();
            }
            
            timer.Dispose();

            automatons.Clear();
        }

        public static void timer_tick(object state)
        {
            foreach (var pair in automatons)
            {
                pair.Value.tick();
            }
        }

        public static string GetHttpResponse(HttpListenerRequest request)
        {
            string response = "";

            string body;
            using (var breader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                body = breader.ReadToEnd();
            }

            switch (request.HttpMethod)
            {
                case "POST":
                    JObject json;
                    try
                    {
                        json = JObject.Parse(body);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[JSON]:{0}:{1}", e.Message, e.StackTrace);
                        return "{\"error\":\"Invalid Json\"}";
                    }
                    string[] path = request.Url.AbsolutePath.ToLower().Split('/');
                    path = path.Where(w => w != path[0]).ToArray();
                    string at = "";
                    if (path.Length >= 1)
                    {
                        if(accessToken.Length > 0)
                        {
                            if (path.Length > 1)
                            {
                                if(path[0].ToLower() != accessToken)
                                {
                                    response = "{\"error\":\"token invalid\"}";
                                    break;
                                }
                                else
                                {
                                    at = path[0];
                                    List<string> pathl = new List<string>(path);
                                    pathl.RemoveAt(0);
                                    path = pathl.ToArray();
                                }
                            }
                            else
                            {
                                response = "{\"error\":\"token error\"}";
                                break;
                            }
                        }
                        switch (path[0])
                        {
                            case "service":

                                List<CommandResult> results = new List<CommandResult>();

                                if (json["commands"].GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                                {
                                    JArray commands = (JArray)json["commands"];
                                    foreach (JObject command in commands)
                                    {
                                        var type = Type.GetType("SteelCityAutomatonServiceCommands." + ((string)command["command"]).ToLower());
                                        if (type != null)
                                        {
                                            ServiceCommand cmd = (ServiceCommand)Activator.CreateInstance(type);
                                            cmd.Setup(command);
                                            cmd.Excecute(automatons,at);
                                            results.Add(cmd.result);
                                        }
                                        else
                                        {
                                            CommandResult result = new CommandResult(((string)command["command"]).ToLower());
                                            result.success = false;
                                            result.data = "unrecognized command";
                                            results.Add(result);
                                            Console.WriteLine("CMD NOT RECOGNIZED - {0} : {1}", ((string)command["command"]).ToLower(), "null");
                                        }
                                    }
                                    CommandResponse resp = new CommandResponse((string)json["label"],results);
                                    response = resp.serialise();
                                }
                                break;
                            case "automaton":
                                if (path.Length > 1)
                                {
                                    string session = path[1];
                                    if (automatons.ContainsKey(at + session))
                                    {
                                        response = automatons[at + session].processCommands(json);
                                    }
                                    else response = "{\"error\":\"unrecognized session id\"}";
                                }
                                else response = "{\"error\":\"missing session id\"}";
                                break;
                            default:
                                Console.WriteLine("------Parts------");
                                foreach (string s in path) Console.WriteLine(s);
                                Console.WriteLine("-----------------");
                                response = "{\"error\":\"unrecognized endpoint, not automaton or service\"}";
                                break;
                        }
                    }
                    else response = "{\"error\":\"unrecognized endpoint, not specifed\"}";
                    break;
                case "GET":
                    if(webPage)
                    {
                        string pathf = request.Url.AbsolutePath.ToLower();
                        if (pathf.Length < 3) pathf = "BasicWebTest.html";
                        pathf = "Resources/" + pathf;
                        //string f = @"Resources/BasicWebTest.html";
                        if (File.Exists(pathf))response =  File.ReadAllText(pathf);
                        else response = "{\"error\":\"BasicWebTest Missing\"}";
                    }
                    else response = "{\"error\":\"Web Page not enabled\"}";
                    break;
                default:
                    response = "{\"error\":\"Unsuported Method\"}";
                    break;
            }

            return response;
        }

    }
}