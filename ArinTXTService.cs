using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Arin.Readers;
using Penguin.Web.IPServices.Objects;
using Penguin.Web.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Penguin.Web.IPServices
{
    public class ArinTXTService : ArinBaseService
    {

        public ArinTXTService(IEnumerable<ArinBlacklist> BlackLists, string NetTxtPath, string OrgTxtPath, Action<string, float> reportProgress = null, Action<LoadCompletionArgs> loadCompleted = null) : base(OrgTxtPath, NetTxtPath, loadCompleted, reportProgress)
        {

            LoadWorker = new BackgroundWorker();

            LoadWorker.DoWork += this.LoadWorker_DoWork;
            LoadWorker.RunWorkerCompleted += this.LoadWorker_RunWorkerCompleted;

            LoadWorker.RunWorkerAsync(new WorkerArgs()
            {
                BlackList = BlackLists.ToList(),
                NetPath = NetTxtPath,
                OrgsPath = OrgTxtPath,
                ReportProgress = reportProgress
            });
        }

        private void LoadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Analysis = (e.Result as LoadCompletionArgs).Analysis;

            IsLoaded = true;

            LoadCompleted?.Invoke(e.Result as LoadCompletionArgs);
        }

        private static IEnumerable<Dictionary<string, string>> FindMatches(IEnumerable<Dictionary<string, string>> Blocks, List<ArinBlacklist> BlackListEntries, Func<Dictionary<string, string>, bool> AdditionalCriteria = null)
        {
            foreach (Dictionary<string, string> block in Blocks)
            {
                if (AdditionalCriteria?.Invoke(block) ?? false)
                {
                    yield return block;

                }
                else
                {
                    foreach (ArinBlacklist thisBlacklistEntry in BlackListEntries)
                    {
                        if (block.ContainsKey(thisBlacklistEntry.Property))
                        {
                            string netVal = block[thisBlacklistEntry.Property];



                            if (string.IsNullOrWhiteSpace(netVal))
                            {
                                continue;
                            }

                            if (thisBlacklistEntry.MatchMethod == MatchMethod.Regex)
                            {
                                if (Regex.IsMatch(netVal, thisBlacklistEntry.Value))
                                {
                                    yield return block;
                                }
                            }
                            else if (thisBlacklistEntry.MatchMethod == MatchMethod.Contains)
                            {
                                if (netVal.Contains(thisBlacklistEntry.Value))
                                {
                                    yield return block;
                                }
                            }
                            else if (thisBlacklistEntry.MatchMethod == MatchMethod.Exact)
                            {
                                if (string.Equals(netVal, thisBlacklistEntry.Value))
                                {
                                    yield return block;
                                }
                            }
                            else
                            {
                                throw new NotImplementedException($"{nameof(MatchMethod)} value {thisBlacklistEntry.MatchMethod.ToString()} is not implemented");
                            }

                        }
                    }
                }
            }
        }
        private static List<Dictionary<string, string>> MatchingOrgs(List<ArinBlacklist> BlackListEntries, string OrgTxtPath, Action<string, float> ReportProgress)
        {
            BlockTxtReader reader = new BlockTxtReader(OrgTxtPath);

            if (ReportProgress != null)
            {
                reader.ReportProgress = p =>
                {
                    ReportProgress.Invoke("Org", p);
                };
            }

            return FindMatches(reader.Blocks(), BlackListEntries).ToList();
        }

        public IEnumerable<(string OrgName, string IP)> FindOwner(params string[] Ips)
        {
            List<string> toFind = Ips.ToList();

            BlockTxtReader reader = new BlockTxtReader(NetPath);
            reader.ReportProgress = (f) =>
            {
                this.ReportProgress.Invoke("Net", f);
            };

            List<(string Ip, Dictionary<string, string> Block)> matchBlocks = new List<(string Ip, Dictionary<string, string> Block)>();

            foreach(Dictionary<string, string> block in reader.Blocks())
            {
                foreach(IPAnalysis ipa in GetAnalysis(block))
                {
                    foreach (string ip in toFind.ToList()) {
                        if (ipa.IsMatch(IPAddress.Parse(ip)))
                        {
                            toFind.Remove(ip);
                            matchBlocks.Add((ip, block));
                        }
                    }
                }
            }

            reader = new BlockTxtReader(OrgPath);
            reader.ReportProgress = (f) =>
            {
                this.ReportProgress.Invoke("Org", f);
            };

            foreach (Dictionary<string, string> block in reader.Blocks())
            {
                foreach((string Ip, Dictionary<string, string> mblock) in matchBlocks)
                {
                    if(block["OrgID"] == mblock["OrgID"])
                    {
                        yield return (Ip, block["OrgName"]);

                    }
                }
            }
        }

        private static List<Dictionary<string, string>> MatchingNets(List<ArinBlacklist> BlackListEntries, List<Dictionary<string, string>> MatchingOrgs, string NetXmlPath, Action<string, float> ReportProgress)
        {
            BlockTxtReader reader = new BlockTxtReader(NetXmlPath);

            if (ReportProgress != null)
            {
                reader.ReportProgress = p =>
                {
                    ReportProgress.Invoke("Net", p);
                };
            }

            return FindMatches(reader.Blocks(), BlackListEntries, n =>
            {
                return MatchingOrgs.Any(m => m["OrgID"] == n["OrgID"]);
            }).ToList();
        }
        private void LoadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgs args = e.Argument as WorkerArgs;

            BlockTxtReader netReader = new BlockTxtReader(args.NetPath);

            List<Dictionary<string, string>> matchingOrgs = MatchingOrgs(args.BlackList, args.OrgsPath, args.ReportProgress);

            List<Dictionary<string, string>> matchingNets = MatchingNets(args.BlackList, matchingOrgs, args.NetPath, args.ReportProgress);

            LoadCompletionArgs toReturn = new LoadCompletionArgs();

            foreach (Dictionary<string, string> n in matchingNets)
            {
                string Range = n["NetRange"];

                foreach (IPAnalysis ip in GetAnalysis(n))
                {
                    toReturn.Analysis.Add(ip);
                }
            }

            e.Result = toReturn;
        }

        private List<IPAnalysis> GetAnalysis(Dictionary<string, string> block)
        {
            string Range = block["NetRange"];
            return new List<IPAnalysis>() { new IPAnalysis()
            {
                FromIp = Range.Split('-')[0].Trim(),
                ToIp = Range.Split('-')[1].Trim(),
                OwnerName = block.ContainsKey("OrgName") ? block["OrgName"] : block["OrgID"],
                OrgID = block["OrgID"]
            } };
            
        }
    }
}
