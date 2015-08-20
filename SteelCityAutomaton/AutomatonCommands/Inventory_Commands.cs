using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using SteelCityAutomaton;

using OpenMetaverse;

namespace SteelCityAutomatonCommands
{
    public class inv_load_folder : AutomatonCommand
    {
        private Inventory inv;
        public string path { get; set; }
        public bool hidden { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            if (string.IsNullOrEmpty(path)) path = "";

            string[] pth = path.Split('/');
            if (client.Inventory.Store == null)
            {
                result.message = "inventory not loaded";
                return true;
            }

            inv = client.Inventory.Store;

            InventoryFolder currentFolder = inv.RootFolder;
            if (currentFolder == null)
            {
                result.message = "inventory not loaded";
                return true;
            }
            for (int i = 0; i < pth.Length; ++i)
            {
                string nextName = pth[i];
                if (string.IsNullOrEmpty(nextName) || nextName == ".")
                    continue; // Ignore '.' and blanks, stay in the current directory.

                //List<InventoryBase> currentContents = Inventory.GetContents(currentFolder);
                List<InventoryBase> currentContents = inv.GetContents(currentFolder);
                // Try and find an InventoryBase with the corresponding name.
                bool found = false;
                foreach (InventoryBase item in currentContents)
                {
                    // Allow lookup by UUID as well as name:
                    if (item.Name == nextName || item.UUID.ToString() == nextName)
                    {
                        found = true;
                        if (item is InventoryFolder)
                        {
                            currentFolder = item as InventoryFolder;
                        }
                        else
                        {
                            result.message = "invalid path";
                            return true;
                        }
                    }
                }
                if (!found)
                {
                    result.message = "invalid path";
                    return true;
                }
            }

            List<InventoryBase> contents = inv.GetContents(currentFolder);

            client.Inventory.RequestFolderContents(currentFolder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByName);
            result.success = true;
            return true;
        }
    }

    public class get_inv_folders : AutomatonCommand
    {
        private Inventory inv;
        public string path { get; set; }
        public string filter { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            string res = string.Empty;
            if(path == null || path.Length < 1)path = "";
            if (filter == null || filter.Length < 1) filter = "";
            path = path.Trim().Trim('/');
            InventoryNode folder = FindFolder(client,path);
            if (folder != null)
            {
                foreach (var f in folder.Nodes.Values)
                {
                    if (f.Data is InventoryFolder && !f.Data.Name.StartsWith("."))
                    {
                        res += f.Data.Name + ",";
                    }
                }
                result.success = true;
                result.data = res.TrimEnd(',');
            }
            else result.message = "Path Doesn't Exist";
            return true;
        }

        public InventoryNode FindFolder(GridClient client, string path)
        {
            return FindFolderInternal(client.Inventory.Store.RootNode, "/", "/" + Regex.Replace(path, @"^[/\s]*(.*)[/\s]*", @"$1").ToLower());
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
    }

    public class get_inv_items : AutomatonCommand
    {
        private Inventory inv;
        public string path { get; set; }
        public string filter { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            string res = string.Empty;
            if (path == null || path.Length < 1) path = "";
            if (filter == null || filter.Length < 1) filter = "";
            InventoryNode folder = FindFolder(client, path);
            client.Inventory.RequestFolderContents(folder.Data.UUID, folder.Data.OwnerID, true, true, InventorySortOrder.SystemFoldersToTop & InventorySortOrder.ByName);
            if (folder != null)
            {
                foreach (var f in folder.Nodes.Values)
                {
                    if (/*!(f.Data is InventoryFolder) && */!f.Data.Name.StartsWith("."))
                    {
                        res += f.Data.Name + ",";
                    }
                }
                result.success = true;
                result.data = res.TrimEnd(',');
            }
            else result.message = "Path Doesn't Exist";
            return true;
        }

        public InventoryNode FindFolder(GridClient client, string path)
        {
            return FindFolderInternal(client.Inventory.Store.RootNode, "/", "/" + Regex.Replace(path, @"^[/\s]*(.*)[/\s]*", @"$1").ToLower());
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
    }
}
