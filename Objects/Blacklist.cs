using Penguin.Web.Objects;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
        private ConcurrentBag<IPAnalysis> _Analysis = new ConcurrentBag<IPAnalysis>();

        public ConcurrentBagWrapper<IPAnalysis> Analysis
        {
            get
            {
                return new ConcurrentBagWrapper<IPAnalysis>(_Analysis);
            }
            internal set
            {
                _Analysis = value.Backing;
            }
        }

        /// <summary>
        /// If the function that loads this blacklist has completed
        /// </summary>
        public bool IsLoaded { get; set; }

        public void Add(IPAnalysis analysis)
        {
            _Analysis.Add(analysis);
        }

        public void AddRange(IEnumerable<IPAnalysis> analyses)
        {
            if (analyses is null)
            {
                throw new System.ArgumentNullException(nameof(analyses));
            }

            foreach (IPAnalysis analysis in analyses)
            {
                Add(analysis);
            }
        }
    }
}