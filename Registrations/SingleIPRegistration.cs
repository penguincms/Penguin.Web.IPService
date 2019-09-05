using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;

namespace Penguin.Web.Registrations
{
    internal class SingleIPRegistration : IPRegistration
    {
        public SingleIPRegistration(string IP)
        {
            Source = IP;
        }

        public override bool IsMatch(BigInteger IPAddress)
        {
            return IpToInt(ParseIp(Source)).Equals(IPAddress);
        }
    }

}
