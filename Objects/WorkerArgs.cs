using Penguin.Web.IPServices.Arin;
using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Objects
{
    class WorkerArgs
    {
        public List<ArinBlacklist> BlackList { get; set; }
        public string NetPath { get; set; }
        public string OrgsPath { get; set; }
        public Action<string, float> ReportProgress { get; set; }
    }
}
