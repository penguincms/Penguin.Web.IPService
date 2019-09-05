using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Penguin.Web.IPServices.Arin
{
    public class ArinBlacklist
    {
        public string Property
        {
            set
            {
                if(!Properties.Any())
                {
                    Properties = new List<string>()
                    {
                        value
                    };
                } else
                {
                    throw new Exception("Can not set individual property more than once, or after a property list has been set");
                }
            }
        }
        public List<string> Properties { get; set; } = new List<string>();
        public string Value { get; set; }
        public MatchMethod MatchMethod { get; set; }
    }

    public enum MatchMethod
    {
        Regex,
        Exact,
        Contains,
        CaseInsensitiveContains
    }
}
