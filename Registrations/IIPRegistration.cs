using System;
using System.Collections.Generic;
using System.Text;

namespace IPRangeBreakdown.Registrations
{
    internal interface IIPRegistration
    {
        bool IsMatch(string IPAddress);
    }
}
