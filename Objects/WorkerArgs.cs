using Penguin.Web.IPServices.Arin;
using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Objects
{
    public class WorkerArgs
    {
        public List<ArinBlacklist> BlackList { get; set; }
        public Progress<(string, float)> ReportProgress { get; set; }
    }
}
