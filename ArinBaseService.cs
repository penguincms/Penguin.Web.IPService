using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Arin.Readers;
using Penguin.Web.IPServices.Objects;
using Penguin.Web.Objects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Penguin.Web.IPServices
{
    public abstract class ArinBaseService
    {
        public string OrgPath { get; set; }
        public string NetPath { get; set; }
        internal ArinBaseService(string orgPath, string netPath, Action<LoadCompletionArgs> loadComplete, Action<string, float> reportProgress)
        {
            ReportProgress = reportProgress;
            LoadCompleted = loadComplete;
            OrgPath = orgPath;
            NetPath = netPath;
        }
        internal Action<string,float> ReportProgress { get; set; }
        internal Action<LoadCompletionArgs> LoadCompleted { get; set; }
        protected BackgroundWorker LoadWorker { get; set; }
        public virtual bool IsLoaded { get; protected set; }
        protected ConcurrentBag<IPAnalysis> Analysis { get; set; } = new ConcurrentBag<IPAnalysis>();

    }
}
