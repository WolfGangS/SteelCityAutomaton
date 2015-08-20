using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SteelCityAutomaton{
    public class CommandResult{
        public string command { get; private set; }
        public object data { get; set; }
        public string message { get; set; }
        public bool success { get; set; }
        public string label { get; set; }

        public CommandResult(string _command){
            command = _command;
            success = false;
            data = null;
            message = null;
            label = null;
        }
    }

    public class CommandResponse{
        public string label { get; private set; }
        public CommandResult[] results { get; set; }

        public CommandResponse(string _label, List<CommandResult> _results){
            label = _label;
            results = _results.ToArray();
        }

        public CommandResponse(string _label, CommandResult[] _results){
            label = _label;
            results = _results;
        }

        public string serialise(){
            var json = new JavaScriptSerializer().Serialize(this);
            return json;
        }
    }
}
