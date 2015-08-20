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

namespace SteelCityAutomatonCommands{
    public class AutomatonCommand{
        public string command { get; set; }
        public string label {get; set;}
        public CommandResult result { get; set; }

        public AutomatonCommand(){
            command = this.GetType().Name;
            result = null;
            label = null;
        }

        public virtual void Setup(JObject json){

            PreSetup();

            command = this.GetType().Name;
            result = new CommandResult(command);
            foreach (JToken token in json.Children()){
                var property = token as JProperty;
                if (property != null){
                    Type t = this.GetType();
                    PropertyInfo pi = t.GetProperty(property.Name);
                    if (pi != null) pi.SetValue(this, Convert.ChangeType(property.Value, pi.PropertyType));
                }
            }
            result.label = label;
/*            
#if DEBUG
            Console.WriteLine("[Automaton Command] : " + command);
#endif
*/
            PostSetup();
        }

        public virtual void PreSetup(){

        }

        public virtual void PostSetup(){
            //ListProperties();
        }

        public Dictionary<string,string> classesList()
        {
            Dictionary<string, string> classes = new Dictionary<string, string>();
            foreach (Type type in Assembly.GetCallingAssembly().GetTypes().Where(type => type.IsSubclassOf(this.GetType())).ToList())
            {
                AutomatonCommand cmd = (AutomatonCommand)Activator.CreateInstance(type);
                string json = cmd.serialise();
                List<string> parts = json.Substring(1,json.Length - 2).Split(',').ToList();
                parts.Remove("\"label\":null");
                parts.Remove("\"result\":null");
                parts = parts.OrderBy(q => q).ToList();
                int i = Array.FindIndex(parts.ToArray(), element => element.StartsWith("\"command\":"));
                if(i > 0)
                {
                    string c = parts[i];
                    parts[i] = parts[0];
                    parts[0] = c;
                }
                classes.Add(type.Name, "{" + string.Join(",",parts) + "}");
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

        public void ListProperties(){
            Console.WriteLine("Properties");
            foreach (PropertyInfo p in this.GetType().GetProperties()){
                if (p.GetValue(this, null) != null){
                    Console.WriteLine("{0} : {1}", p.Name, p.GetValue(this, null).ToString());
                }
            }
        }

        public string serialise(){
            var json = new JavaScriptSerializer().Serialize(this);
            return json;
        }

        public virtual bool Excecute(Automaton am, GridClient client, bool force){
            return true;
        }

        public virtual string CommandType(){
            return this.GetType().Name;
        }
    }

    public class login : AutomatonCommand{
        public override bool Excecute(Automaton am, GridClient client, bool force){
            string response = "";
            result.success = am.Login(out response);
            result.data = response;
            return result.success;
        }
    }

    public class logout : AutomatonCommand{
        public override bool Excecute(Automaton am, GridClient client, bool force){
            result.success = am.Logout();
            return result.success;
        }
    }

    //MISC COMMANDS

    public class get_dialogs : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (am.total_dialogs > 0)
            {
                result.success = true;
                result.data = am.get_dialogs();
            }
            else result.message = "No Dialogs";
            return true;
        }
    }

    public class get_permission_requests : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (am.permission_requests > 0)
            {
                result.success = true;
                result.data = am.getPermissionRequests();
            }
            else result.message = "No Permission Requests";
            return true;
        }
    }

    public class reply_to_permission_request : AutomatonCommand
    {
        public int perms { get; set; }
        public string uuid { get; set; }
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            UUID scr;
            if (UUID.TryParse(uuid, out scr))
            {
                string msg;
                result.success = am.replyToPermissionRequest(uuid, perms, out msg);
                result.message = msg;
            }
            else result.message = "Invalid UUID";
            return true;
        }
    }

    public class reply_to_dialog : AutomatonCommand
    {
        public int channel { get; set; }
        public int button_index { get; set; }
        public string message { get; set; }
        public string uuid { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            UUID target;
            if (UUID.TryParse(uuid, out target))
            {

                if (message != "ignore" && button_index >= 0)
                {
                    client.Self.ReplyToScriptDialog(channel, button_index, message, target);
                }
                am.clear_dialog(target);
                result.success = true;
            }
            return true;
        }
    }

    public class play_sound : AutomatonCommand
    {
        public string uuid { get; set; }

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            UUID sound;
            if (UUID.TryParse(uuid, out sound))
            {
                client.Sound.PlaySound(sound);
                result.success = true;
                result.data = sound.ToString();
            }
            else result.message = "Invalid uuid for sound";
            return true;
        }
    }

    public class set_home : AutomatonCommand
    {
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            client.Self.SetHome();
            result.success = true;
            return true;
        }
    }

    public class upload_image_web : AutomatonCommand
    {
        AutoResetEvent UploadCompleteEvent = new AutoResetEvent(false);
        public string url { get; set; }
        public string name { get; set; }
        public int timeout { get; set; }

        private DateTime start;
        private UUID TextureID = UUID.Zero;

        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            List<string> data = new List<string>();

            if (name == null || name == "") name = "texture";
            TextureID = UUID.Zero;

            Console.WriteLine("Loading image " + url);
            byte[] jpeg2k = loadImage(url);
            if (jpeg2k == null)
            {
                Console.WriteLine("Failed to compress image to JPEG2000");
                return true;
            }
            Console.WriteLine("Finished compressing image to JPEG2000, uploading...");
            start = DateTime.Now;
            DoUpload(jpeg2k, name, client);

            if (UploadCompleteEvent.WaitOne(timeout, false))
            {
                if (TextureID != UUID.Zero)
                {
                    result.success = true;
                    data.Add(TextureID.ToString());
                }
                else result.success = false;
                Console.WriteLine(String.Format("Texture upload {0}: {1}", (TextureID != UUID.Zero) ? "succeeded" : "failed",
                    TextureID));
            }
            else result.success = false;
            data.Add(client.Self.Balance.ToString());
            result.data = data;
            return true;
        }

        private void DoUpload(byte[] UploadData, string FileName, GridClient Client)
        {
            if (UploadData != null)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(FileName);

                Client.Inventory.RequestCreateItemFromAsset(UploadData, name, "Uploaded with TestClient",
                    AssetType.Texture, InventoryType.Texture, Client.Inventory.FindFolderForType(AssetType.Texture),
                    delegate(bool success, string status, UUID itemID, UUID assetID)
                    {
                        Console.WriteLine(String.Format(
                            "RequestCreateItemFromAsset() returned: Success={0}, Status={1}, ItemID={2}, AssetID={3}",
                            success, status, itemID, assetID));

                        TextureID = assetID;
                        Console.WriteLine(String.Format("Upload took {0}", DateTime.Now.Subtract(start)));
                        UploadCompleteEvent.Set();
                    }
                );
            }
        }
        private byte[] loadImage(string url)
        {
            string urll = url.ToLower();
            byte[] UploadData;
            if (urll.EndsWith(".jpg") || urll.EndsWith(".png") || urll.EndsWith(".bmp") || urll.EndsWith(".gif"))
            {
                try
                {
                    Bitmap image = GetBitmapFromURL(url);

                    int oWidth = image.Width;
                    int oHeight = image.Height;

                    if (!Helper.isPowerOfTwo((uint)oWidth) || !Helper.isPowerOfTwo((uint)oHeight))
                    {
                        int nWidth = Helper.nearestPowerofTwo(oWidth);
                        int nHeight = Helper.nearestPowerofTwo(oHeight);
                        Bitmap resized = new Bitmap(nWidth, nHeight, image.PixelFormat);
                        Graphics graphics = Graphics.FromImage(resized);

                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.InterpolationMode =
                           System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(image, 0, 0, nWidth, nHeight);

                        image.Dispose();
                        image = resized;

                        oWidth = nWidth;
                        oHeight = nHeight;
                    }

                    if (oWidth > 1024 || oHeight > 1024)
                    {
                        int newwidth = (oWidth > 1024) ? 1024 : oWidth;
                        int newheight = (oHeight > 1024) ? 1024 : oHeight;

                        Bitmap resized = new Bitmap(newwidth, newheight, image.PixelFormat);
                        Graphics graphics = Graphics.FromImage(resized);

                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.InterpolationMode =
                           System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(image, 0, 0, newwidth, newheight);

                        image.Dispose();
                        image = resized;
                    }
                    UploadData = OpenJPEG.EncodeFromImage(image, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + " SL Image Upload ");
                    return null;
                }
                return UploadData;
            }
            return null;
        }
        private Bitmap GetBitmapFromURL(string url)
        {
            WebRequest req = WebRequest.Create(url);
            WebResponse resp = req.GetResponse();
            Stream respStream =
                resp.GetResponseStream();
            Bitmap bitmap = new Bitmap(respStream);
            return bitmap;
        }
    }

    public class start_animation : AutomatonCommand
    {
        public string uuid { get; set; }
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            UUID anim;
            if (UUID.TryParse(uuid, out anim))
            {
                client.Self.AnimationStart(anim, true);
                result.success = true;
            }
            else result.message = "Invalid UUID";
            return true;
        }
    }

    public class stop_animation : AutomatonCommand
    {
        public string uuid { get; set; }
        public override bool Excecute(Automaton am, GridClient client, bool force)
        {
            if (!client.Network.Connected) { result.message = "Not Connected to grid"; return true; }
            UUID anim;
            if (UUID.TryParse(uuid, out anim))
            {
                client.Self.AnimationStop(anim, true);
                result.success = true;
            }
            else result.message = "Invalid UUID";
            return true;
        }
    }
}
