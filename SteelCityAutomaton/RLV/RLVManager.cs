using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using OpenMetaverse;

namespace SteelCityAutomaton
{
    public class RLVManager
    {
        public bool Enabled { get; private set; }
        GridClient Client;
        Automaton Am;

        public RLVManager(Automaton am, GridClient client)
        {
            Enabled = false;
            Client = client;
            Am = am;
        }

        public void Enable()
        {
            Enabled = true;
        }

        public void Disable()
        {
            Enabled = false;
            //TODO : Cleanup
        }

        public bool TryProcessCMD(ChatEventArgs e)
        {
            if (!Enabled || !e.Message.StartsWith("@")) return false;

            foreach (string cmd in e.Message.Substring(1).Split(','))
            {
                RLVRule rule = new RLVRule(e);

                switch (rule.Behaviour)
                {
                    case "version":
                        int chan = 0;
                        if (int.TryParse(rule.Param, out chan) && chan > 0)
                        {
                            Respond(chan, "RestrainedLife viewer v1.23 (SteelCity Automaton - Alpha)");
                        }
                        break;

                    case "versionnew":
                        chan = 0;
                        if (int.TryParse(rule.Param, out chan) && chan > 0)
                        {
                            Respond(chan, "RestrainedLove viewer v1.23 (SteelCity Automaton - Alpha)");
                        }
                        break;


                    case "versionnum":
                        if (int.TryParse(rule.Param, out chan) && chan > 0)
                        {
                            Respond(chan, "1230100");
                        }
                        break;

                    case "getgroup":
                        if (int.TryParse(rule.Param, out chan) && chan > 0)
                        {
                            UUID gid = Client.Self.ActiveGroup;
                            if (Am.Groups().ContainsKey(gid))
                            {
                                Respond(chan, Am.Groups()[gid].Name);
                            }
                        }
                        break;

                    case "setgroup":
                        {
                            if (rule.Param == "force")
                            {
                                Am.set_group(rule.Option);
                            }
                        }
                        break;

                    //TODO MORE

                    case "getinv":
                        if (int.TryParse(rule.Param, out chan) && chan > 0)
                        {
                            string res = string.Empty;
                            InventoryNode folder = FindFolder(rule.Option);
                            if (folder != null)
                            {
                                foreach (var f in folder.Nodes.Values)
                                {
                                    if (f.Data is InventoryFolder && !f.Data.Name.StartsWith("."))
                                    {
                                        res += f.Data.Name + ",";
                                    }
                                }
                            }

                            Respond(chan, res.TrimEnd(','));
                        }
                        break;

                    default:
                        Console.WriteLine("Invalid rlv behaviour:");
                        Console.WriteLine(rule.ToString());
                        break;
                }
            }
            return true;
        }

        #region #RLV FOLDER FUNCTIONS
        public InventoryNode RLVRootFolder()
        {
            foreach (var rn in Client.Inventory.Store.RootNode.Nodes.Values)
            {
                if (rn.Data.Name == "#RLV" && rn.Data is InventoryFolder)
                {
                    return rn;
                }
            }
            return null;
        }

        public InventoryNode FindFolder(string path)
        {
            var root = RLVRootFolder();
            if (root == null) return null;

            return FindFolderInternal(root, "/", "/" + Regex.Replace(path, @"^[/\s]*(.*)[/\s]*", @"$1").ToLower());
        }

        protected InventoryNode FindFolderInternal(InventoryNode currentNode, string currentPath, string desiredPath)
        {
            if (desiredPath == currentPath)
            {
                return currentNode;
            }
            foreach (var n in currentNode.Nodes.Values)
            {
                if (n.Data.Name.StartsWith(".")) continue;

                var res = FindFolderInternal(n, (currentPath == "/" ? currentPath : currentPath + "/") + n.Data.Name.ToLower(), desiredPath);
                if (res != null)
                {
                    return res;
                }
            }
            return null;
        }
        #endregion


        private void Respond(int chan, string msg)
        {
            Client.Self.Chat(msg, chan, ChatType.Normal);
        }
    }
}
