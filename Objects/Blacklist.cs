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


        public IReadOnlyList<IPAnalysis> Analysis { 
            get
            {
                return _Analysis.ToList();
            }
            set
            {
                _Analysis = new ConcurrentBag<IPAnalysis>();

                if(value is null)
                {
                    return;
                }

                foreach(IPAnalysis thisAnalysis in value)
                {
                    _Analysis.Add(thisAnalysis);
                }
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
            foreach(IPAnalysis analysis in analyses)
            {
                Add(analysis);
            }
        }
    }
}