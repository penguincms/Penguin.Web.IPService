using Penguin.Web.Objects;
using System.Collections.Concurrent;

namespace Penguin.Web.IPServices.Objects
{
    /// <summary>
    /// Represents the a blacklist
    /// </summary>
    public class Blacklist
    {
        /// <summary>
        /// Any IP analysis needed to check for IPs that fall in blacklisted ranges
        /// </summary>
        public ConcurrentBag<IPAnalysis> Analysis { get; set; } = new ConcurrentBag<IPAnalysis>();

        /// <summary>
        /// If the function that loads this blacklist has completed
        /// </summary>
        public bool IsLoaded { get; set; }
    }
}