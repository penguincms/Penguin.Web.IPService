using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;

namespace Penguin.Web.Registrations
{
    internal abstract class IPRegistration : IIPRegistration
    {
        public string Source { get; set; }

        public abstract bool IsMatch(IPAddress IPAddress);

        public string CleanIP(string Ip)
        {
            string start = Ip.Trim('0');
            while (start.Contains(".0"))
            {
                start = start.Replace(".0", ".");
            }
            if (start.StartsWith("."))
            {
                start = "0" + start;
            }

            if (start.StartsWith(":"))
            {
                start = "0000" + start;
            }

            if (start.EndsWith("."))
            {
                start = start + "0";
            }

            if (start.EndsWith(":"))
            {
                start = start + "0000";
            }

            while(start.Contains(".."))
            {
                start = start.Replace("..", ".0.");
            }

            return start;
        }

        public IPAddress ParseIp(string Ip)
        {
            return IPAddress.Parse(CleanIP(Ip));
        }
        protected BigInteger IpToInt(IPAddress address)
        {
            byte[] AddressBytes = address.GetAddressBytes();

            Array.Reverse(AddressBytes);

            byte[] PaddedAddressBytes = new byte[AddressBytes.Length + 1];

            Array.Copy(AddressBytes, PaddedAddressBytes, AddressBytes.Length);

            return new BigInteger(PaddedAddressBytes);
        }

    }
}
