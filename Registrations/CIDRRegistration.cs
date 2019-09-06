using System;
using System.Net;
using System.Numerics;

namespace Penguin.Web.Registrations
{
    internal class CIDRRegistration : IPRegistration
    {
        private IPAddress CidrAddress { get; set; }

        private BigInteger CidrAddressBytes { get; set; }

        private int CidrMaskBytes { get; set; }

        public CIDRRegistration(string CIDR)
        {
            Source = CIDR;

            if (string.IsNullOrEmpty(CIDR))
            {
                throw new ArgumentException("Input string must not be null", CIDR);
            }

            string[] parts = CIDR.Split('/');

            CidrAddress = ParseIp(parts[0]);

            if (parts.Length != 2)
            {
                throw new FormatException($"cidrMask was not in the correct format:\nExpected: a.b.c.d/n\nActual: {CIDR}");
            }

            if (!int.TryParse(parts[1], out int netmaskBitCount))
            {
                throw new FormatException($"Unable to parse netmask bit count from {CIDR}");
            }

            if (0 > netmaskBitCount || netmaskBitCount > 32)
            {
                throw new ArgumentOutOfRangeException($"Netmask bit count value of {netmaskBitCount} is invalid, must be in range 0-32");
            }

            CidrAddressBytes = IpToInt(CidrAddress);

            CidrMaskBytes = IPAddress.HostToNetworkOrder(-1 << (32 - netmaskBitCount));
        }

        public override bool IsMatch(BigInteger IPAddress)
        {
            return (IPAddress & CidrMaskBytes) == (CidrAddressBytes & CidrMaskBytes);
        }
    }
}