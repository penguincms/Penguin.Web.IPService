using Penguin.Web.Objects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Objects
{
    public class LoadCompletionArgs
    {
        public LoadCompletionArgs()
        {
            Analysis = new ConcurrentBag<IPAnalysis>();
        }
        public ConcurrentBag<IPAnalysis> Analysis { get; set; }
    }
}
