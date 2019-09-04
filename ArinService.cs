using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Objects;
using Penguin.Web.Objects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;

namespace Penguin.Web.IPServices
{
    public class ArinService : ArinBaseService
    {
        private int ToComplete { get; set; }
        ArinTXTService txtService { get; set; }
        ArinXMLService xmlService { get; set; }

        public override bool IsLoaded
        {
            get
            {
                return ToComplete == 0;
            }
            protected set
            {
            }
        }

        public bool IsMatch(IPAddress address)
        {
            foreach(IPAnalysis analysis in Analysis)
            {
                if (analysis.IsMatch(address))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<(string OrgName, string IP)> FindOwner(params string[] Ips)
        {
            foreach((string OrgName, string Ip) in txtService.FindOwner(Ips))
            {
                yield return (OrgName, Ip);
            }
            foreach ((string OrgName, string Ip) in xmlService.FindOwner(Ips))
            {
                yield return (OrgName, Ip);
            }
        }

        public ArinService(IEnumerable<ArinBlacklist> BlackLists, string NetTxtPath, string OrgTxtPath, string NetXmlPath, string OrgXmlPath, Action<string, float> reportProgress = null, Action<LoadCompletionArgs> loadCompleted = null) : base(null, null, loadCompleted, reportProgress)
        {
            ToComplete = -2;

            BackgroundWorker XmlWorker = new BackgroundWorker();
            BackgroundWorker TxtWorker = new BackgroundWorker();

            XmlWorker.DoWork += this.XmlWorker_DoWork;
            TxtWorker.DoWork += this.TxtWorker_DoWork;

            
            XmlWorker.RunWorkerAsync(new WorkerArgs()
            {
                NetPath = NetXmlPath,
                OrgsPath = OrgXmlPath,
                BlackList = BlackLists.ToList(),
                ReportProgress = (s, f) => {
                    reportProgress.Invoke($"Xml:{s}", f);
                }
            });

            TxtWorker.RunWorkerAsync(new WorkerArgs()
            {
                OrgsPath = OrgTxtPath,
                NetPath = NetTxtPath,
                BlackList = BlackLists.ToList(),
                ReportProgress = (s, f) => {
                    reportProgress.Invoke($"Txt:{s}", f);
                }
            });

        }



        private void TxtWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgs args = (e.Argument as WorkerArgs);

            txtService = new ArinTXTService(args.BlackList, args.NetPath, args.OrgsPath, args.ReportProgress, (loadCompletionArgs) => {
                foreach (IPAnalysis i in loadCompletionArgs.Analysis)
                {
                    Analysis.Add(i);
                }

                ToComplete++;

                if (IsLoaded)
                {
                    LoadCompleted?.Invoke(e.Result as LoadCompletionArgs);
                }

            });


        }
        private void XmlWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgs args = (e.Argument as WorkerArgs);

            xmlService = new ArinXMLService(args.BlackList, args.NetPath, args.OrgsPath, args.ReportProgress, (loadCompletionArgs) => {
                foreach (IPAnalysis i in loadCompletionArgs.Analysis)
                {
                    Analysis.Add(i);
                }

                ToComplete++;

                if (IsLoaded)
                {
                    LoadCompleted?.Invoke(e.Result as LoadCompletionArgs);
                }

            });
        }

    }
}
