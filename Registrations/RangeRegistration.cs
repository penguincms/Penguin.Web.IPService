using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;

namespace Penguin.Web.Registrations
{
    internal class RangeRegistration : IPRegistration
    {
        public BigInteger From { get; set; }

        public BigInteger To { get; set; }

        public RangeRegistration(string Range)
        {

            string start = Range.Split('-')[0];
            string end = Range.Split('-')[1];



            From = IpToInt(ParseIp(start));
            To = IpToInt(ParseIp(end));
        }



        public override bool IsMatch(BigInteger IPAddress)
        {
            return IPAddress >= From && IPAddress <= To;
        }
    }
}
