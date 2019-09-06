using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Objects;
using Penguin.Web.Objects;
using Penguin.Web.Registrations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Penguin.Web.IPServices
{
    /// <summary>
    /// Base abstract for ARIN services
    /// </summary>
    public abstract class ArinBaseService
    {
        /// <summary>
        /// The path to the Organization file
        /// </summary>
        protected string OrgPath { get; set; }

        /// <summary>
        /// The path to the Net file
        /// </summary>
        protected string NetPath { get; set; }

        internal ArinBaseService(string orgPath, string netPath)
        {
            OrgPath = orgPath;
            NetPath = netPath;
        }

        /// <summary>
        /// Loaded blacklist information
        /// </summary>
        protected Blacklist BlackList { get; set; } = new Blacklist();

        /// <summary>
        /// Checks the given IP against the black list
        /// </summary>
        /// <param name="address">The IP to check</param>
        /// <returns>An object representing the blacklist status and any applicable matches</returns>
        public BlacklistStatus CheckIP(string address) => CheckIP(IPRegistration.ParseIp(address));

        /// <summary>
        /// Checks the given IP against the black list
        /// </summary>
        /// <param name="address">The IP to check</param>
        /// <returns>An object representing the blacklist status and any applicable matches</returns>
        public BlacklistStatus CheckIP(IPAddress address)
        {
            BigInteger toCheck = IPRegistration.IpToInt(address);

            BlacklistStatus toReturn = new BlacklistStatus();

            if (!(BlackList?.IsLoaded ?? false))
            {
                toReturn.State = BlacklistState.NotLoaded;
            }
            else
            {
                toReturn.State = BlacklistState.Pass;

                foreach (IPAnalysis analysis in this.BlackList.Analysis)
                {
                    if (analysis.IsMatch(toCheck))
                    {
                        toReturn.Matches.Add(analysis);
                        toReturn.State = BlacklistState.Fail;
                    }
                }
            }

            return toReturn;
        }

        /// <summary>
        /// Returns the company that the given IP address is registered to. This is blocking so it shouldn't be used for large lists if time is critical
        /// </summary>
        /// <param name="Ips">Any number of IP addresses</param>
        /// <returns>An IEnumerable containing tuples with the organization name and IP tied to it</returns>
        public IEnumerable<(string OrgName, string IP)> FindOwner(params string[] Ips) => this.FindOwner(null, Ips);

        /// <summary>
        /// Returns the company that the given IP address is registered to. This is blocking so it shouldn't be used for large lists if time is critical
        /// </summary>
        /// <param name="ReportProgress">A method used to return progress information during the load</param>
        /// <param name="Ips">Any number of IP addresses</param>
        /// <returns>An IEnumerable containing tuples with the organization name and IP tied to it</returns>
        public abstract IEnumerable<(string OrgName, string IP)> FindOwner(IProgress<(string, float)> ReportProgress, params string[] Ips);

        /// <summary>
        /// Compares a block property against a blacklist property value using the given match method
        /// </summary>
        /// <param name="value">The property of the block entry</param>
        /// <param name="against">The property specified on the blacklist</param>
        /// <param name="method">The type of check method to use</param>
        /// <returns>If its a match</returns>
        public static bool CheckProperty(string value, string against, MatchMethod method)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            switch (method)
            {
                case MatchMethod.Regex:
                    if (Regex.IsMatch(value, against))
                    {
                        return true;
                    }
                    break;

                case MatchMethod.Contains:
                    if (value.Contains(against))
                    {
                        return true;
                    }
                    break;

                case MatchMethod.Exact:
                    if (string.Equals(value, against))
                    {
                        return true;
                    }
                    break;

                case MatchMethod.CaseInsensitiveContains:
                    if (value.IndexOf(against, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                    break;

                default:
                    throw new NotImplementedException($"{nameof(MatchMethod)} value {method.ToString()} is not implemented");
            }

            return false;
        }
    }
}