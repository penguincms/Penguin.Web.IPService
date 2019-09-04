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
using System.Threading.Tasks;

namespace Penguin.Web.IPServices
{
    public class ArinService : ArinBaseService
    {
        ArinTXTService TxtService { get; set; }
        ArinXMLService XmlService { get; set; }

        public bool IsBlacklisted(IPAddress address)
        {
            foreach(IPAnalysis analysis in this.BlackList.Analysis)
            {
                if (analysis.IsMatch(address))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task LoadBlacklist(IEnumerable<ArinBlacklist> BlackLists, IProgress<(string, float)> reportProgress)
        {
            Task[] loads = new Task[2];

            loads[0] = TxtService.LoadBlacklist(BlackLists, reportProgress).ContinueWith(t =>
            {
                foreach(IPAnalysis ip in t.Result.Analysis)
                {
                    this.BlackList.Analysis.Add(ip);
                }
            });

            loads[1] = XmlService.LoadBlacklist(BlackLists, reportProgress).ContinueWith(t => {
                foreach (IPAnalysis ip in t.Result.Analysis)
                {
                    this.BlackList.Analysis.Add(ip);
                }
            });

            await Task.WhenAll();

            
        }


        public IEnumerable<(string OrgName, string IP)> FindOwner(params string[] Ips)
        {
            foreach((string OrgName, string Ip) in TxtService.FindOwner(Ips))
            {
                yield return (OrgName, Ip);
            }
            foreach ((string OrgName, string Ip) in XmlService.FindOwner(Ips))
            {
                yield return (OrgName, Ip);
            }
        }

        public ArinService(string NetTxtPath, string OrgTxtPath, string NetXmlPath, string OrgXmlPath) : base(null, null)
        {

            TxtService = new ArinTXTService(NetTxtPath, OrgTxtPath);
            XmlService = new ArinXMLService(NetXmlPath, OrgXmlPath);

        }



        //private void TxtWorker_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    WorkerArgs args = (e.Argument as WorkerArgs);

        //    TxtService = new ArinTXTService(args.BlackList, args.NetPath, args.OrgsPath, args.ReportProgress, (loadCompletionArgs) => {
        //        foreach (IPAnalysis i in loadCompletionArgs.Analysis)
        //        {
        //            BlackList.Analysis.Add(i);
        //        }

        //        ToComplete++;

        //        if (BlackList.IsLoaded)
        //        {
        //            LoadCompleted?.Invoke(e.Result as LoadCompletionArgs);
        //        }

        //    });


        //}
        //private void XmlWorker_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    WorkerArgs args = (e.Argument as WorkerArgs);

        //    XmlService = new ArinXMLService(args.BlackList, args.NetPath, args.OrgsPath, args.ReportProgress, (loadCompletionArgs) => {
        //        foreach (IPAnalysis i in loadCompletionArgs.Analysis)
        //        {
        //            BlackList.Analysis.Add(i);
        //        }

        //        ToComplete++;

        //        if (BlackList.IsLoaded)
        //        {
        //            LoadCompleted?.Invoke(e.Result as LoadCompletionArgs);
        //        }

        //    });
        //}

    }
}
