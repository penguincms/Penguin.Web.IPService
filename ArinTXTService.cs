using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Arin.Readers;
using Penguin.Web.IPServices.Extensions;
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
using System.Threading.Tasks;
using TaskExtensions = Penguin.Web.IPServices.Extensions.TaskExtensions;

namespace Penguin.Web.IPServices
{
    public class ArinTXTService : ArinBaseService
    {

        public ArinTXTService(string NetTxtPath, string OrgTxtPath, Action<LoadCompletionArgs> loadCompleted = null) : base(OrgTxtPath, NetTxtPath)
        {


        }

        public async Task<LoadCompletionArgs> LoadBlacklist(IEnumerable<ArinBlacklist> BlackLists, IProgress<(string, float)> reportProgress = null)
        {
            List<ArinBlacklist> BlackList = BlackLists.ToList();

            return await new Task<LoadCompletionArgs>(() =>
            {
                List<Dictionary<string, string>> matchingOrgs = MatchingOrgs(BlackList, this.OrgPath, reportProgress);

                List<Dictionary<string, string>> matchingNets = MatchingNets(BlackList, matchingOrgs, this.NetPath, reportProgress);

                LoadCompletionArgs toReturn = new LoadCompletionArgs();

                foreach (Dictionary<string, string> n in matchingNets)
                {
                    string Range = n["NetRange"];

                    foreach (IPAnalysis ip in GetAnalysis(n))
                    {
                        toReturn.Analysis.Add(ip);
                    }
                }

                return toReturn;
            });
        }



        private static IEnumerable<Dictionary<string, string>> FindMatches(Func<Task<Dictionary<string, string>>> NextBlock, List<ArinBlacklist> BlackListEntries, Func<Dictionary<string, string>, bool> AdditionalCriteria = null)
        {

            foreach (Dictionary<string, string> thisBlock in NextBlock.YieldNext())
            {

                if (AdditionalCriteria?.Invoke(thisBlock) ?? false)
                {
                    yield return thisBlock;

                }
                else
                {
                    foreach (ArinBlacklist thisBlacklistEntry in BlackListEntries)
                    {
                        if (thisBlock.ContainsKey(thisBlacklistEntry.Property))
                        {
                            string netVal = thisBlock[thisBlacklistEntry.Property];

                            if (string.IsNullOrWhiteSpace(netVal))
                            {
                                continue;
                            }

                            if (thisBlacklistEntry.MatchMethod == MatchMethod.Regex)
                            {
                                if (Regex.IsMatch(netVal, thisBlacklistEntry.Value))
                                {
                                    yield return thisBlock;
                                }
                            }
                            else if (thisBlacklistEntry.MatchMethod == MatchMethod.Contains)
                            {
                                if (netVal.Contains(thisBlacklistEntry.Value))
                                {
                                    yield return thisBlock;
                                }
                            }
                            else if (thisBlacklistEntry.MatchMethod == MatchMethod.Exact)
                            {
                                if (string.Equals(netVal, thisBlacklistEntry.Value))
                                {
                                    yield return thisBlock;
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

        private static List<Dictionary<string, string>> MatchingOrgs(List<ArinBlacklist> BlackListEntries, string OrgTxtPath, IProgress<(string, float)> ReportProgress)
        {
            BlockTxtReader reader = new BlockTxtReader(OrgTxtPath,
                new Progress<float>((f) =>
                {
                    ReportProgress.Report(("Org", f));
                })
            );

            return FindMatches(reader.GetNextBlock, BlackListEntries).ToList();
        }

        public IEnumerable<(string OrgName, string IP)> FindOwner(params string[] Ips) => this.FindOwner(null, Ips);
        public IEnumerable<(string OrgName, string IP)> FindOwner(IProgress<(string, float)> ReportProgress, params string[] Ips)
        {
            List<string> toFind = Ips.ToList();

            BlockTxtReader reader = new BlockTxtReader(NetPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("TXT: NET", f));
            }));


            List<(string Ip, Dictionary<string, string> Block)> matchBlocks = new List<(string Ip, Dictionary<string, string> Block)>();


            TaskExtensions.WhileNext(reader.GetNextBlock, (block) =>
            {
                foreach (IPAnalysis ipa in GetAnalysis(block))
                {
                    foreach (string ip in toFind.ToList())
                    {
                        if (ipa.IsMatch(IPAddress.Parse(ip)))
                        {
                            matchBlocks.Add((ip, block));
                        }
                    }
                }
            });

            reader = new BlockTxtReader(OrgPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("TXT: ORG", f));
            }));


            foreach (Dictionary<string, string> block in new Func<Task<Dictionary<string, string>>>(reader.GetNextBlock).YieldNext())
            {
                foreach ((string Ip, Dictionary<string, string> mblock) in matchBlocks)
                {
                    if (block["OrgID"] == mblock["OrgID"])
                    {
                        yield return (Ip, block["OrgName"]);

                    }
                }
            }
        }

        private static List<Dictionary<string, string>> MatchingNets(List<ArinBlacklist> BlackListEntries, List<Dictionary<string, string>> MatchingOrgs, string NetXmlPath, IProgress<(string, float)> ReportProgress)
        {
            BlockTxtReader reader = new BlockTxtReader(NetXmlPath,
                new Progress<float>((f) =>
                {
                    ReportProgress.Report(("Net", f));
                }));


            return FindMatches(reader.GetNextBlock, BlackListEntries, n =>
            {
                return MatchingOrgs.Any(m => m["OrgID"] == n["OrgID"]);
            }).ToList();
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
