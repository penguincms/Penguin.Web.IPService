using Penguin.Web.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Objects
{
    public class BlacklistStatus
    {
        public BlacklistState State { get; set; }
        public List<IPAnalysis> Matches { get; set; } = new List<IPAnalysis>();
    }
    public enum BlacklistState
    {
        NotLoaded,
        Pass,
        Fail
    }
}
