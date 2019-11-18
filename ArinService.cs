using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Objects;
using Penguin.Web.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Penguin.Web.IPServices
{
    /// <summary>
    /// Wraps the ORG and XML services
    /// </summary>
    public class ArinService : ArinBaseService
    {
        private ArinTXTService TxtService { get; set; }
        private ArinXMLService XmlService { get; set; }


        public void LoadBlacklist(Blacklist blacklist)
        {
            this.BlackList = blacklist;
        }

        /// <summary>
        /// Using the provided blacklist entry list, this method populates the internal blacklist with relevant IP information for
        /// Determining later if the information associated with an IP address fails a blacklist check
        /// </summary>
        /// <param name="BlackLists">A list of blacklist entries describing what to block</param>
        /// <param name="reportProgress">A method used to return progress information during the load</param>
        /// <returns>A task that will complete when the blacklist has fully loaded</returns>
        public async Task LoadBlacklist(IEnumerable<ArinBlacklist> BlackLists, IProgress<(string, float)> reportProgress = null)
        {
            Task[] loads = new Task[2];

            this.BlackList = new Blacklist();

            loads[0] = Task.Run(async () =>
                {
                    LoadCompletionArgs Result = await TxtService.LoadBlacklist(BlackLists, reportProgress);

                    foreach (IPAnalysis ip in Result.Analysis)
                    {
                        this.BlackList.Add(ip);
                    }
                }
            );

            loads[1] = Task.Run(async () =>
            {
                LoadCompletionArgs Result = await XmlService.LoadBlacklist(BlackLists, reportProgress);

                foreach (IPAnalysis ip in Result.Analysis)
                {
                    this.BlackList.Add(ip);
                }
            });

            await Task.WhenAll(loads);

            this.BlackList.IsLoaded = true;
        }

        /// <summary>
        /// Returns the company that the given IP address is registered to. This is blocking so it shouldn't be used for large lists if time is critical
        /// </summary>
        /// <param name="ReportProgress">A method used to return progress information during the load</param>
        /// <param name="Ips">Any number of IP addresses</param>
        /// <returns>An IEnumerable containing tuples with the organization name and IP tied to it</returns>
        public override IEnumerable<(string OrgName, string IP)> FindOwner(IProgress<(string, float)> ReportProgress, params string[] Ips)
        {
            foreach ((string OrgName, string Ip) in XmlService.FindOwner(ReportProgress, Ips))
            {
                yield return (OrgName, Ip);
            }

            foreach ((string OrgName, string Ip) in TxtService.FindOwner(ReportProgress, Ips))
            {
                yield return (OrgName, Ip);
            }
        }

        /// <summary>
        /// Constructs a new instance of this service
        /// </summary>
        /// <param name="NetTxtPath">The path of the ARIN NET .txt dump</param>
        /// <param name="OrgTxtPath">The path of the ARIN ORG .txt dump</param>
        /// <param name="NetXmlPath">The path of the ARIN NET .xml dump</param>
        /// <param name="OrgXmlPath">The path of the ARIN ORG .xml dump</param>
        public ArinService(string NetTxtPath, string OrgTxtPath, string NetXmlPath, string OrgXmlPath) : base(null, null)
        {
            TxtService = new ArinTXTService(NetTxtPath, OrgTxtPath);
            XmlService = new ArinXMLService(NetXmlPath, OrgXmlPath);
        }
    }
}