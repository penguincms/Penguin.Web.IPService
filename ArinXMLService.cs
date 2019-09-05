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
    public class ArinXMLService : ArinBaseService
    {

        public async Task<LoadCompletionArgs> LoadBlacklist(IEnumerable<ArinBlacklist> BlackLists, IProgress<(string, float)> reportProgress = null)
        {
            List<ArinBlacklist> BlackList = BlackLists.ToList();

            return await Task.Run(() => {

                NetXmlReader netReader = new NetXmlReader(NetPath);

                PropertyInfo[] netProps = typeof(Net).GetProperties().Where(p => BlackList.Select(a => a.Property).Contains(p.Name)).ToArray();

                List<Org> matchingOrgs = MatchingOrgs(BlackList, OrgPath, new Progress<(string, float)>((t) => {

                    reportProgress.Report(("XML: " + t.Item1, t.Item2));
                }));

                List<Net> matchingNets = MatchingNets(BlackList, matchingOrgs, NetPath, new Progress<(string, float)>((t) => {
                    reportProgress.Report(("XML: " + t.Item1, t.Item2));
                }));

                LoadCompletionArgs toReturn = new LoadCompletionArgs();

                foreach (Net n in matchingNets)
                {
                    foreach (IPAnalysis ip in GetAnalysis(n))
                    {
                        toReturn.Analysis.Add(ip);
                    }
                }


                this.BlackList.Analysis = toReturn.Analysis;
                this.BlackList.IsLoaded = true;

                return toReturn;
            });
        }

        public ArinXMLService(string NetXmlPath, string OrgXmlPath) : base(OrgXmlPath, NetXmlPath)
        {


        }

        public IEnumerable<(string OrgName, string IP)> FindOwner(params string[] Ips) => this.FindOwner(null, Ips);
        public IEnumerable<(string OrgName, string IP)> FindOwner(IProgress<(string, float)> ReportProgress, params string[] Ips)
        {
            List<string> toFind = Ips.ToList();

            NetXmlReader readerX = new NetXmlReader(NetPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("XML: NET", f));
            }));


            ConcurrentBag<(string Ip, Net Block)> matchBlocks = new ConcurrentBag<(string Ip, Net Block)>();

            Parallel.ForEach<Net>(readerX.Blocks(), 
            block =>
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



            OrgXmlReader readerO = new OrgXmlReader(OrgPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("XML: ORG", f));
            }));

            ConcurrentBag<(string OrgName, string IP)> bag = new ConcurrentBag<(string OrgName, string IP)>();

            Parallel.ForEach(readerO.Blocks(), block =>
            {
                foreach ((string Ip, Net mblock) in matchBlocks)
                {
                    if (block.Handle == mblock.OrgHandle)
                    {
                        bag.Add((Ip, mblock.OrgHandle));
                    }
                }
            });

            return bag;
        }

        public List<IPAnalysis> GetAnalysis(Net n)
        {
            List<IPAnalysis> toReturn = new List<IPAnalysis>();

            if (n.NetBlocks.Any())
            {
                foreach (NetBlock nb in n.NetBlocks)
                {
                    toReturn.Add(new IPAnalysis()
                    {
                        FromIp = nb.StartAddress.ToString(),
                        ToIp = nb.EndAddress.ToString(),
                        OwnerName = n.Name,
                        OrgID = n.OrgHandle
                    });
                }
            }
            else
            {
                toReturn.Add(new IPAnalysis()
                {
                    FromIp = n.StartAddress.ToString(),
                    ToIp = n.EndAddress.ToString(),
                    OwnerName = n.Name,
                    OrgID = n.OrgHandle
                });
            }
            return toReturn;
        }

        private static IEnumerable<T> FindMatches<T>(IEnumerable<T> Next, List<ArinBlacklist> BlackListEntries, Func<T, bool> AdditionalCriteria = null) where T : class
        {
            PropertyInfo[] props = typeof(T).GetProperties().Where(p => BlackListEntries.Select(a => a.Property).Contains(p.Name)).ToArray();
            List<ArinBlacklist> entriesToCheck = BlackListEntries.Where(b => props.Any(p => p.Name == b.Property)).ToList();
            ConcurrentBag<T> toReturn = new ConcurrentBag<T>();

            Parallel.ForEach(Next, block =>
            {
                if (AdditionalCriteria?.Invoke(block) ?? false)
                {
                    toReturn.Add(block);

                }
                {
                    foreach (ArinBlacklist thisBlacklistEntry in entriesToCheck)
                    {
                        foreach (PropertyInfo thisProperty in props)
                        {
                            if (thisProperty.Name != thisBlacklistEntry.Property)
                            {
                                continue;
                            }
                            else
                            {
                                string netVal = thisProperty.GetValue(block)?.ToString();

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
                }
            });

            return toReturn;
        }
        private static List<Org> MatchingOrgs(List<ArinBlacklist> BlackListEntries, string OrgXmlPath, IProgress<(string, float)> ReportProgress)
        {
            OrgXmlReader reader = new OrgXmlReader(OrgXmlPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("ORG", f));
            }));



            return FindMatches(reader.Blocks(), BlackListEntries).ToList();
        }
        private static List<Net> MatchingNets(List<ArinBlacklist> BlackListEntries, List<Org> MatchingOrgs, string NetXmlPath, IProgress<(string, float)> ReportProgress)
        {
            NetXmlReader reader = new NetXmlReader(NetXmlPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("Net", f));
            }));

            HashSet<string> orgMatch = new HashSet<string>();

            foreach (string name in MatchingOrgs.Select(m => m.Handle))
            {
                orgMatch.Add(name);
            }

            return FindMatches(reader.Blocks(), BlackListEntries, n => {
                return orgMatch.Contains(n.OrgHandle);
            }).ToList();
        }

    }
}
