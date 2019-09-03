using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Arin
{
    public class ArinBlacklist
    {
        public string Property { get; set; }
        public string Value { get; set; }
        public MatchMethod MatchMethod { get; set; }
    }

    public enum MatchMethod
    {
        Regex,
        Exact,
        Contains
    }
}
