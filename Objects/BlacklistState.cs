using Penguin.Web.Objects;
using System.Collections.Generic;

namespace Penguin.Web.IPServices.Objects
{
    /// <summary>
    /// Contains information relevant to the result of a check of an IP address against the service blacklist
    /// </summary>
    public class BlacklistStatus
    {
        /// <summary>
        /// The status of the black list check
        /// </summary>
        public BlacklistState State { get; set; }

        /// <summary>
        /// Contains any IP Analysis that the blacklist check matched
        /// </summary>
        public List<IPAnalysis> Matches { get; set; } = new List<IPAnalysis>();
    }

    /// <summary>
    /// Represents the result of checking an IP address against a blacklist
    /// </summary>
    public enum BlacklistState
    {
        /// <summary>
        /// The service had not finished loading when the check was performed
        /// </summary>
        NotLoaded,

        /// <summary>
        /// The IP was NOT found in the blacklist
        /// </summary>
        Pass,

        /// <summary>
        /// The IP WAS found in the blacklist
        /// </summary>
        Fail
    }
}