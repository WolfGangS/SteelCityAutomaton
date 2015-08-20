using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMetaverse;
using System.Text.RegularExpressions;

namespace SteelCityAutomaton
{
    public class RLVRule
    {
        private Regex rlv_regex = new Regex(@"(?<behaviour>[^:=]+)(:(?<option>[^=]*))?=(?<param>\w+)", RegexOptions.Compiled);

        public string Behaviour { private set; get; }
        public string Option { private set; get; }
        public string Param { private set; get; }
        public UUID Source { private set; get; }
        public string SourceName { private set; get; }

        public RLVRule(ChatEventArgs e)
        {
            if (e.SourceID != null)
            {
                Match reg = rlv_regex.Match(e.Message);
                if (reg.Success)
                {
                    Behaviour = reg.Groups["behaviour"].ToString().ToLower();
                    Option = reg.Groups["option"].ToString().ToLower();
                    Param = reg.Groups["param"].ToString().ToLower();
                    if (Param == "rem") Param = "y";
                    if (Param == "add") Param = "n";
                    Source = e.SourceID;
                    SourceName = e.FromName;
                }
                else throw new ArgumentException("Invalid rlv rule : " + e.Message);
            }
            else throw new ArgumentException("source uuid may not be null.");
        }

        public override string ToString()
        {
            return string.Format("{0} : {1}:{2}={3} [{4}]", SourceName, Behaviour, Option, Param, Source);
        }
    }
}
