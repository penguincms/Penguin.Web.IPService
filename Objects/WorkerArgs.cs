using Penguin.Web.IPServices.Arin;
using System;
using System.Collections.Generic;

namespace Penguin.Web.IPServices.Objects
{
    /// <summary>
    /// A class to pass into the task that loads the blacklist internally
    /// </summary>
    public class WorkerArgs
    {
        /// <summary>
        /// The blacklist entries to load
        /// </summary>
        public List<ArinBlacklist> BlackList { get; set; }

        /// <summary>
        /// A delegate to use to report task progress
        /// </summary>
        public Progress<(string, float)> ReportProgress { get; set; }
    }
}