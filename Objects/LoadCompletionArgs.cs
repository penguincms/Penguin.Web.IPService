using Penguin.Web.Objects;
using System.Collections.Concurrent;

namespace Penguin.Web.IPServices.Objects
{
    /// <summary>
    /// Contains a list of IP analysis relevant to the provided blacklist during loading
    /// </summary>
    public class LoadCompletionArgs
    {
        internal LoadCompletionArgs()
        {
            Analysis = new ConcurrentBag<IPAnalysis>();
        }

        /// <summary>
        /// A list of IP analysis relevant to the provided blacklist during loading
        /// </summary>
        public ConcurrentBag<IPAnalysis> Analysis { get; set; }
    }
}