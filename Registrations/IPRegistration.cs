﻿using System;
using System.Net;
using System.Numerics;

namespace Penguin.Web.Registrations
{
    internal abstract class IPRegistration : IIPRegistration
    {
        public string Source { get; set; }

        public abstract bool IsMatch(BigInteger IPAddress);

        public static string CleanIP(string Ip)
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
                start += "0";
            }

            if (start.EndsWith(":"))
            {
                start += "0000";
            }

            while (start.Contains(".."))
            {
                start = start.Replace("..", ".0.");
            }

            return start;
        }

        public static IPAddress ParseIp(string Ip)
        {
            return IPAddress.Parse(CleanIP(Ip));
        }

        public static BigInteger IpToInt(string address)
        {
            return IpToInt(ParseIp(address));
        }

        public static BigInteger IpToInt(IPAddress address)
        {
            byte[] AddressBytes = address.GetAddressBytes();

            Array.Reverse(AddressBytes);

            byte[] PaddedAddressBytes = new byte[AddressBytes.Length + 1];

            Array.Copy(AddressBytes, PaddedAddressBytes, AddressBytes.Length);

            return new BigInteger(PaddedAddressBytes);
        }
    }
}