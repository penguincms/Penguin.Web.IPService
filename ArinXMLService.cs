using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Arin.Readers;
using Penguin.Web.IPServices.Objects;
using Penguin.Web.Objects;
using Penguin.Web.Registrations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;

namespace Penguin.Web.IPServices
{
    /// <summary>
    /// A service for checking XML source files provided by arin
    /// </summary>
    public class ArinXMLService : ArinBaseService
    {
        private struct FindOwnerContainer
        {
            public Net Block { get; set; }
            public string IP { get; set; }

            public FindOwnerContainer(string ip, Net block)
            {
                IP = ip;
                Block = block;
            }
        }

        /// <summary>
        /// Constructs a new instance of the XML service using the paths provided for the XML source files
        /// </summary>
        /// <param name="NetXmlPath">The ARIN XML dump for NET</param>
        /// <param name="OrgXmlPath">The ARIN XML dump for ORG</param>
        public ArinXMLService(string NetXmlPath, string OrgXmlPath) : base(OrgXmlPath, NetXmlPath)
        {
        }

        /// <summary>
        /// Returns the company that the given IP address is registered to. This is blocking so it shouldn't be used for large lists if time is critical
        /// </summary>
        /// <param name="ReportProgress">A method used to return progress information during the load</param>
        /// <param name="Ips">Any number of IP addresses</param>
        /// <returns>An IEnumerable containing tuples with the organization name and IP tied to it</returns>
        public override IEnumerable<(string OrgName, string IP)> FindOwner(IProgress<(string, float)> ReportProgress, params string[] Ips)
        {
            HashSet<BigInteger> toFind = new HashSet<BigInteger>();
            Dictionary<BigInteger, string> Mapping = new Dictionary<BigInteger, string>();

            foreach (string ip in Ips.Distinct())
            {
                BigInteger val = IPRegistration.IpToInt(ip);
                if (!Mapping.ContainsKey(val))
                {
                    toFind.Add(val);
                    Mapping.Add(val, ip);
                }
            }

            NetXmlReader readerX = new NetXmlReader(NetPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("XML: NET", f));
            }));

            ConcurrentBag<FindOwnerContainer> matchBlocks = new ConcurrentBag<FindOwnerContainer>();

            Parallel.ForEach<Net>(readerX.Blocks(),
            block =>
            {
                foreach (IPAnalysis ipa in GetAnalysis(block))
                {
                    foreach (BigInteger ip in toFind)
                    {
                        if (ipa.IsMatch(ip))
                        {
                            matchBlocks.Add(new FindOwnerContainer(Mapping[ip], block));
                        }
                    }
                }
            });

            OrgXmlReader readerO = new OrgXmlReader(OrgPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("XML: ORG", f));
            }));

            Dictionary<string, List<FindOwnerContainer>> matchLookup = new Dictionary<string, List<FindOwnerContainer>>();

            foreach (FindOwnerContainer lookup in matchBlocks)
            {
                string org = lookup.Block.OrgHandle;
                List<FindOwnerContainer> collection;
                if (matchLookup.TryGetValue(org, out collection))
                {
                    collection.Add(lookup);
                }
                else
                {
                    matchLookup.Add(org, new List<FindOwnerContainer>()
                    {
                        lookup
                    });
                }
            }

            ConcurrentBag<(string OrgName, string IP)> toReturn = new ConcurrentBag<(string OrgName, string IP)>();

            Parallel.ForEach(readerO.Blocks(), block =>
            {
                if (matchLookup.TryGetValue(block.Handle, out List<FindOwnerContainer> collection))
                {
                    foreach (FindOwnerContainer container in collection)
                    {
                        toReturn.Add((container.IP, block.Name));
                    }
                }
            });

            return toReturn;
        }

        /// <summary>
        /// Using the provided blacklist entry list, this method populates the internal blacklist with relevant IP information for
        /// Determining later if the information associated with an IP address fails a blacklist check
        /// </summary>
        /// <param name="BlackLists">A list of blacklist entries describing what to block</param>
        /// <param name="reportProgress">A method used to return progress information during the load</param>
        /// <returns>A task that will complete when the blacklist has fully loaded</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
        public async Task<LoadCompletionArgs> LoadBlacklist(IEnumerable<ArinBlacklist> BlackLists, IProgress<(string, float)> reportProgress = null)
        {
            List<ArinBlacklist> BlackList = BlackLists.ToList();

            return await Task.Run(() =>
            {
                NetXmlReader netReader = new NetXmlReader(NetPath);

                PropertyInfo[] netProps = typeof(Net).GetProperties().Where(p => BlackList.SelectMany(a => a.Properties).Contains(p.Name)).ToArray();

                List<Org> matchingOrgs = MatchingOrgs(BlackList, OrgPath, new Progress<(string, float)>((t) =>
                {
                    reportProgress?.Report(("XML: " + t.Item1, t.Item2));
                }));

                List<Net> matchingNets = MatchingNets(BlackList, matchingOrgs, NetPath, new Progress<(string, float)>((t) =>
                {
                    reportProgress?.Report(("XML: " + t.Item1, t.Item2));
                }));

                LoadCompletionArgs toReturn = new LoadCompletionArgs();

                foreach (Net n in matchingNets)
                {
                    foreach (IPAnalysis ip in GetAnalysis(n))
                    {
                        toReturn.Analysis.Add(ip);
                    }
                }

                this.BlackList.AddRange(toReturn.Analysis);
                this.BlackList.IsLoaded = true;

                return toReturn;
            });
        }

        /// <summary>
        /// Converts the given NET object to an IP-Analysis (only relevant information is implemented)
        /// </summary>
        /// <param name="n">The NET object to convert</param>
        /// <returns>A list of IPAnalysis representing the ranges specified in the NET</returns>
        protected List<IPAnalysis> GetAnalysis(Net n)
        {
            if (n is null)
            {
                throw new ArgumentNullException(nameof(n));
            }

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
            PropertyInfo[] props = typeof(T).GetProperties().Where(p => BlackListEntries.SelectMany(a => a.Properties).Contains(p.Name)).ToArray();
            List<ArinBlacklist> entriesToCheck = BlackListEntries.Where(b => props.Any(p => b.Properties.Contains(p.Name))).ToList();
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
                            foreach (string property in thisBlacklistEntry.Properties)
                            {
                                if (thisProperty.Name != property)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (CheckProperty(thisProperty.GetValue(block)?.ToString(), thisBlacklistEntry.Value, thisBlacklistEntry.MatchMethod))
                                    {
                                        toReturn.Add(block);
                                    }
                                }
                            }
                        }
                    }
                }
            });

            return toReturn;
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

            return FindMatches(reader.Blocks(), BlackListEntries, n =>
            {
                return orgMatch.Contains(n.OrgHandle);
            }).ToList();
        }

        private static List<Org> MatchingOrgs(List<ArinBlacklist> BlackListEntries, string OrgXmlPath, IProgress<(string, float)> ReportProgress)
        {
            OrgXmlReader reader = new OrgXmlReader(OrgXmlPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("ORG", f));
            }));

            return FindMatches(reader.Blocks(), BlackListEntries).ToList();
        }
    }
}