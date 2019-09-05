using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Arin.Readers;
using Penguin.Web.IPServices.Extensions;
using Penguin.Web.IPServices.Objects;
using Penguin.Web.Objects;
using System;
using System.Collections.Concurrent;
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

            return await Task.Run(() =>
            {
                List<Dictionary<string, string>> matchingOrgs = MatchingOrgs(BlackList, this.OrgPath, new Progress<(string, float)>((t) => {

                    reportProgress.Report(("TXT: " + t.Item1, t.Item2));
                }));

                List<Dictionary<string, string>> matchingNets = MatchingNets(BlackList, matchingOrgs, this.NetPath, new Progress<(string, float)>((t) => {

                    reportProgress.Report(("TXT: " + t.Item1, t.Item2));
                }));

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



        private static IEnumerable<Dictionary<string, string>> FindMatches(IEnumerable<Dictionary<string,string>> NextBlock, List<ArinBlacklist> BlackListEntries, Func<Dictionary<string, string>, bool> AdditionalCriteria = null)
        {
            ConcurrentBag<Dictionary<string, string>> toReturn = new ConcurrentBag<Dictionary<string, string>>();

            Parallel.ForEach(NextBlock, block =>
            {

                if (AdditionalCriteria?.Invoke(block) ?? false)
                {
                    toReturn.Add(block);

                }
                else
                {
                    foreach (ArinBlacklist thisBlacklistEntry in BlackListEntries)
                    {
                        if (block.TryGetValue(thisBlacklistEntry.Property, out string netVal))
                        {
                            if (string.IsNullOrWhiteSpace(netVal))
                            {
                                continue;
                            }

                            switch (thisBlacklistEntry.MatchMethod)
                            {
                                case MatchMethod.Regex:
                                    if (Regex.IsMatch(netVal, thisBlacklistEntry.Value))
                                    {
                                        toReturn.Add(block);
                                    }
                                    break;
                                case MatchMethod.Contains:
                                    if (netVal.Contains(thisBlacklistEntry.Value))
                                    {
                                        toReturn.Add(block);
                                    }
                                    break;
                                case MatchMethod.Exact:
                                    if (string.Equals(netVal, thisBlacklistEntry.Value))
                                    {
                                        toReturn.Add(block);
                                    }
                                    break;
                                default:
                                    throw new NotImplementedException($"{nameof(MatchMethod)} value {thisBlacklistEntry.MatchMethod.ToString()} is not implemented");

                            }

                        }
                    }
                }
            });

            return toReturn;
        }

        private static List<Dictionary<string, string>> MatchingOrgs(List<ArinBlacklist> BlackListEntries, string OrgTxtPath, IProgress<(string, float)> ReportProgress)
        {
            BlockTxtReader reader = new BlockTxtReader(OrgTxtPath,
                new Progress<float>((f) =>
                {
                    ReportProgress.Report(("Org", f));
                })
            );

            return FindMatches(reader.Blocks(), BlackListEntries).ToList();
        }

        public IEnumerable<(string OrgName, string IP)> FindOwner(params string[] Ips) => this.FindOwner(null, Ips);
        public IEnumerable<(string OrgName, string IP)> FindOwner(IProgress<(string, float)> ReportProgress, params string[] Ips)
        {
            List<string> toFind = Ips.ToList();

            BlockTxtReader reader = new BlockTxtReader(NetPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("TXT: NET", f));
            }));


            ConcurrentBag<(string Ip, Dictionary<string, string> Block)> matchBlocks = new ConcurrentBag<(string Ip, Dictionary<string, string> Block)>();


            Parallel.ForEach(reader.Blocks(), (block) =>
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

            ConcurrentBag<(string OrgName, string IP)> toReturn = new ConcurrentBag<(string OrgName, string IP)>();
            Parallel.ForEach(reader.Blocks(), (block) =>
            {
                foreach ((string Ip, Dictionary<string, string> mblock) in matchBlocks)
                {
                    if (block["OrgID"] == mblock["OrgID"])
                    {
                        toReturn.Add((Ip, block["OrgName"]));

                    }
                }
            });

            return toReturn;
        }

        private static List<Dictionary<string, string>> MatchingNets(List<ArinBlacklist> BlackListEntries, List<Dictionary<string, string>> MatchingOrgs, string NetXmlPath, IProgress<(string, float)> ReportProgress)
        {
            BlockTxtReader reader = new BlockTxtReader(NetXmlPath,
                new Progress<float>((f) =>
                {
                    ReportProgress.Report(("Net", f));
                }));

            HashSet<string> orgMatch = new HashSet<string>();
            
            foreach(string name in MatchingOrgs.Select(m => m["OrgID"]))
            {
                orgMatch.Add(name);
            }

            return FindMatches(reader.Blocks(), BlackListEntries, n =>
            {
                return orgMatch.Contains(n["OrgID"]);
            }).ToList();
        }

        private List<IPAnalysis> GetAnalysis(Dictionary<string, string> block)
        {
            string Range = block["NetRange"];
            int i = Range.IndexOf('-');

            return new List<IPAnalysis>() { new IPAnalysis()
            {
                FromIp = Range.Substring(0, i).Trim(),
                ToIp = Range.Substring(i + 1).Trim(),
                OwnerName = block.TryGetValue("OrgName", out string s) ? s : block["OrgID"],
                OrgID = block["OrgID"]
            } };

        }
    }
}
