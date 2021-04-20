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
using System.Threading.Tasks;

namespace Penguin.Web.IPServices
{
    /// <summary>
    /// A service for checking TXT source files provided by arin
    /// </summary>
    public class ArinTXTService : ArinBaseService
    {
        /// <summary>
        /// Constructs a new instance of the XML service using the paths provided for the XML source files
        /// </summary>
        /// <param name="NetTxtPath">The ARIN TXT dump for NET</param>
        /// <param name="OrgTxtPath">The ARIN TXT dump for ORG</param>
        public ArinTXTService(string NetTxtPath, string OrgTxtPath) : base(OrgTxtPath, NetTxtPath)
        {
        }

        /// <summary>
        /// Using the provided blacklist entry list, this method populates the internal blacklist with relevant IP information for
        /// Determining later if the information associated with an IP address fails a blacklist check
        /// </summary>
        /// <param name="BlackLists">A list of blacklist entries describing what to block</param>
        /// <param name="reportProgress">A method used to return progress information during the load</param>
        /// <returns>A task that will complete when the blacklist has fully loaded</returns>
        public async Task<LoadCompletionArgs> LoadBlacklist(IEnumerable<ArinBlacklist> BlackLists, IProgress<(string, float)> reportProgress = null)
        {
            List<ArinBlacklist> BlackList = BlackLists.ToList();


            return await Task.Run(() =>
            {

                List<Dictionary<string, string>> matchingOrgs = MatchingOrgs(BlackList, this.OrgPath, new Progress<(string, float)>((t) =>
                {
                    reportProgress?.Report(("TXT: " + t.Item1, t.Item2));
                }));

                List<Dictionary<string, string>> matchingNets = MatchingNets(BlackList, matchingOrgs, this.NetPath, new Progress<(string, float)>((t) =>
                {
                    reportProgress?.Report(("TXT: " + t.Item1, t.Item2));
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

                this.BlackList.IsLoaded = true;

                return toReturn;
            });
        }

        private static IEnumerable<Dictionary<string, string>> FindMatches(IEnumerable<Dictionary<string, string>> NextBlock, List<ArinBlacklist> BlackListEntries, Func<Dictionary<string, string>, bool> AdditionalCriteria = null)
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
                        foreach (string property in thisBlacklistEntry.Properties)
                        {
                            if (block.TryGetValue(property, out string netVal))
                            {
                                if (CheckProperty(netVal, thisBlacklistEntry.Value, thisBlacklistEntry.MatchMethod))
                                {
                                    toReturn.Add(block);
                                }
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

        /// <summary>
        /// Returns the company that the given IP address is registered to. This is blocking so it shouldn't be used for large lists if time is critical
        /// </summary>
        /// <param name="ReportProgress">A method used to return progress information during the load</param>
        /// <param name="Ips">Any number of IP addresses</param>
        /// <returns>An IEnumerable containing tuples with the organization name and IP tied to it</returns>
        public override IEnumerable<(string OrgName, string IP)> FindOwner(IProgress<(string, float)> ReportProgress, params string[] Ips)
        {
            if (Ips is null)
            {
                throw new ArgumentNullException(nameof(Ips));
            }

            HashSet<BigInteger> toFind = new HashSet<BigInteger>();
            Dictionary<BigInteger, string> Mapping = new Dictionary<BigInteger, string>();

            foreach (string ip in Ips)
            {
                BigInteger val = IPRegistration.IpToInt(ip);

                if (!Mapping.ContainsKey(val))
                {
                    toFind.Add(val);
                    Mapping.Add(val, ip);
                }
            }

            BlockTxtReader reader = new BlockTxtReader(NetPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("TXT: NET", f));
            }));

            ConcurrentBag<FindOwnerContainer> matchBlocks = new ConcurrentBag<FindOwnerContainer>();

            Parallel.ForEach(reader.Blocks(), (block) =>
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

            Dictionary<string, List<FindOwnerContainer>> matchLookup = new Dictionary<string, List<FindOwnerContainer>>();

            foreach (FindOwnerContainer lookup in matchBlocks)
            {
                string org = lookup.Block["OrgID"];
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

            reader = new BlockTxtReader(OrgPath, new Progress<float>((f) =>
            {
                ReportProgress.Report(("TXT: ORG", f));
            }));

            ConcurrentBag<(string OrgName, string IP)> toReturn = new ConcurrentBag<(string OrgName, string IP)>();
            Parallel.ForEach(reader.Blocks(), (block) =>
            {
                if (matchLookup.TryGetValue(block["OrgID"], out List<FindOwnerContainer> collection))
                {
                    foreach (FindOwnerContainer container in collection)
                    {
                        toReturn.Add((container.IP, block["OrgName"]));
                    }
                }
            });

            return toReturn;
        }

        private struct FindOwnerContainer
        {
            public string IP { get; set; }
            public Dictionary<string, string> Block { get; set; }

            public FindOwnerContainer(string ip, Dictionary<string, string> block)
            {
                IP = ip;
                Block = block;
            }
        }

        private static List<Dictionary<string, string>> MatchingNets(List<ArinBlacklist> BlackListEntries, List<Dictionary<string, string>> MatchingOrgs, string NetXmlPath, IProgress<(string, float)> ReportProgress)
        {
            BlockTxtReader reader = new BlockTxtReader(NetXmlPath,
                new Progress<float>((f) =>
                {
                    ReportProgress.Report(("Net", f));
                }));

            HashSet<string> orgMatch = new HashSet<string>();

            foreach (string name in MatchingOrgs.Select(m => m["OrgID"]))
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