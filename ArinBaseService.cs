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
using System.Threading.Tasks;

namespace Penguin.Web.IPServices
{
    public abstract class ArinBaseService
    {
        public string OrgPath { get; set; }
        public string NetPath { get; set; }
        internal ArinBaseService(string orgPath, string netPath)
        {
            OrgPath = orgPath;
            NetPath = netPath;
        }
        public Blacklist BlackList { get; set; } = new Blacklist();

    }
}
