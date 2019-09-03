using System;
using System.Collections.Generic;
using System.Text;

namespace IPRangeBreakdown.Registrations
{
    internal abstract class IPRegistration : IIPRegistration
    {
        public string Source { get; set; }

        public abstract bool IsMatch(string IPAddress);
    }
}
