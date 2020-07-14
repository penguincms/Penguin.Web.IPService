using Penguin.Web.Registrations;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Penguin.Web.Objects
{
    /// <summary>
    /// Contains information pulled from the ARIN data dumps about a relevant IP
    /// </summary>
    [Serializable]
    public struct IPAnalysis : IIPRegistration
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public DateTime DiscoveryDate { get; set; }
        public string IpAddress { get; set; }
        public string Country { get; set; }
        public string NetworkName { get; set; }
        public string OwnerName { get; set; }
        public string[] CIDR { get; set; }
        public string FromIp { get; set; }
        public string ToIp { get; set; }
        public bool Allocated { get; set; }
        public string ContactName { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string AbuseEmail { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string WhoisSource { get; set; }
        public string OrgID { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        [NonSerialized]
        private List<IIPRegistration> Registrations;

        /// <summary>
        /// Checks if the given IP is described by this analysis
        /// </summary>
        /// <param name="IPAddress">The IP to check</param>
        /// <returns>If the IP is represented by this analysis</returns>
        public bool IsMatch(System.Net.IPAddress IPAddress)
        {
            if (IPAddress is null)
            {
                throw new ArgumentNullException(nameof(IPAddress));
            }

            return IsMatch(IPRegistration.IpToInt(IPAddress));
        }

        /// <summary>
        /// Checks if the given IP is described by this analysis
        /// </summary>
        /// <param name="IPAddress">The IP to check</param>
        /// <returns>If the IP is represented by this analysis</returns>
        public bool IsMatch(BigInteger IPAddress)
        {
            if (Registrations is null)
            {
                Registrations = new List<IIPRegistration>();

                if (CIDR != null)
                {
                    foreach (string cidr in CIDR)
                    {
                        if (!string.IsNullOrWhiteSpace(cidr))
                        {
                            Registrations.Add(new CIDRRegistration(cidr));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(FromIp) && !string.IsNullOrWhiteSpace(ToIp))
                {
                    Registrations.Add(new RangeRegistration($"{FromIp}-{ToIp}"));
                }
            }

            foreach (IIPRegistration registration in Registrations)
            {
                if (registration.IsMatch(IPAddress)) { return true; }
            }

            return false;
        }
    }
}