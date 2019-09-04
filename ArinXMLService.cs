using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Arin.Readers;
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

namespace Penguin.Web.IPServices
{
    public class ArinXMLService : ArinBaseService
    {

        public async Task<LoadCompletionArgs> LoadBlacklist(IEnumerable<ArinBlacklist> BlackLists, IProgress<(string, float)> reportProgress = null)
        {
            List<ArinBlacklist> BlackList = BlackLists.ToList();

            return await new Task<LoadCompletionArgs>(() => {

                NetXmlReader netReader = new NetXmlReader(NetPath);

                PropertyInfo[] netProps = typeof(Net).GetProperties().Where(p => BlackList.Select(a => a.Property).Contains(p.Name)).ToArray();

                List<Org> matchingOrgs = MatchingOrgs(BlackList, OrgPath, reportProgress);

                List<Net> matchingNets = MatchingNets(BlackList, matchingOrgs, NetPath, reportProgress);

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
        public IEnumerable<(string OrgName, string IP)> FindOwner(Action<string, float> ReportProgress, params string[] Ips)
        {
            List<string> toFind = Ips.ToList();

            NetXmlReader readerX = new NetXmlReader(NetPath);
            readerX.ReportProgress = (f) =>
            {
                ReportProgress.Invoke("Net", f);
            };

            List<(string Ip, Net Block)> matchBlocks = new List<(string Ip, Net Block)>();

            foreach (Net block in readerX.Blocks())
            {
                foreach (IPAnalysis ipa in GetAnalysis(block))
                {
                    foreach (string ip in toFind.ToList())
                    {
                        if (ipa.IsMatch(IPAddress.Parse(ip)))
                        {
                            toFind.Remove(ip);
                            matchBlocks.Add((ip, block));
                        }
                    }
                }
            }

            OrgXmlReader readerO = new OrgXmlReader(OrgPath);
            readerO.ReportProgress = (f) =>
            {
                ReportProgress.Invoke("Org", f);
            };

            foreach (Org block in readerO.Blocks())
            {
                foreach ((string Ip, Net mblock) in matchBlocks)
                {
                    if (block.Handle == mblock.OrgHandle)
                    {
                        yield return (Ip, mblock.OrgHandle);

                    }
                }
            }
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

        private static IEnumerable<T> FindMatches<T>(IEnumerable<T> Blocks, List<ArinBlacklist> BlackListEntries, Func<T, bool> AdditionalCriteria = null)
        {
            PropertyInfo[] props = typeof(T).GetProperties().Where(p => BlackListEntries.Select(a => a.Property).Contains(p.Name)).ToArray();

            foreach (T block in Blocks)
            {
                if (AdditionalCriteria?.Invoke(block) ?? false)
                {
                    yield return block;

                }
                {
                    foreach (ArinBlacklist thisBlacklistEntry in BlackListEntries)
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


                                else if (thisBlacklistEntry.MatchMethod == MatchMethod.Regex)
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
        }
        private static List<Org> MatchingOrgs(List<ArinBlacklist> BlackListEntries, string OrgXmlPath, IProgress<(string, float)> ReportProgress)
        {
            OrgXmlReader reader = new OrgXmlReader(OrgXmlPath);

            if (ReportProgress != null)
            {
                reader.ReportProgress = p =>
                {
                    ReportProgress.Report(("Org", p));
                };
            }

            return FindMatches(reader.Blocks(), BlackListEntries).ToList();
        }
        private static List<Net> MatchingNets(List<ArinBlacklist> BlackListEntries, List<Org> MatchingOrgs, string NetXmlPath, IProgress<(string, float)> ReportProgress)
        {
            NetXmlReader reader = new NetXmlReader(NetXmlPath);

            if (ReportProgress != null)
            {
                reader.ReportProgress = p =>
                {
                    ReportProgress.Report(("Net", p));
                };
            }

            return FindMatches(reader.Blocks(), BlackListEntries, n => {
                return MatchingOrgs.Any(o => o.Handle == n.OrgHandle);
            }).ToList();
        }

    }
}
