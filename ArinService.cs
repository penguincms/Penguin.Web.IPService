using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Arin.Readers;
using Penguin.Web.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Penguin.Web.IPServices
{
    public class ArinService
    {
        private BackgroundWorker LoadWorker { get; set; }
        public bool IsLoaded { get; protected set; }
        private List<IPAnalysis> Analysis { get; set; }
        public ArinService(IEnumerable<ArinBlacklist> BlackLists, string NetXmlPath, string OrgXmlPath, Action<string, float> reportProgress = null)
        {
            Analysis = new List<IPAnalysis>();

            LoadWorker = new BackgroundWorker();

            LoadWorker.DoWork += this.LoadWorker_DoWork;
            LoadWorker.RunWorkerCompleted += this.LoadWorker_RunWorkerCompleted;

            LoadWorker.RunWorkerAsync(new BlacklistLoadArgs()
            {
                BlackListEntries = BlackLists.ToList(),
                NetXmlPath = NetXmlPath,
                OrgXmlPath = OrgXmlPath,
                ReportProgress = reportProgress
            });
        }

        private void LoadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Analysis = (e.Result as BlacklistLoadComplete).Analyis;

            IsLoaded = true;
        }

        private static IEnumerable<T> FindMatches<T>(IEnumerable<T> Blocks, List<ArinBlacklist> BlackListEntries, Func<T, bool> AdditionalCriteria = null)
        {
            PropertyInfo[] props = typeof(T).GetProperties().Where(p => BlackListEntries.Select(a => a.Property).Contains(p.Name)).ToArray();

            foreach (T block in Blocks)
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

                            if (AdditionalCriteria?.Invoke(block) ?? false)
                            {
                                yield return block;

                            } else if (thisBlacklistEntry.MatchMethod == MatchMethod.Regex)
                            {
                                if (Regex.IsMatch(netVal, thisBlacklistEntry.Value))
                                {
                                    yield return block;
                                }
                            }
                            else if (thisBlacklistEntry.MatchMethod == MatchMethod.Contains)
                            {
                                if(netVal.Contains(thisBlacklistEntry.Property))
                                {
                                    yield return block;
                                }
                            } else if(thisBlacklistEntry.MatchMethod == MatchMethod.Exact)
                            {
                                if(string.Equals(netVal, thisBlacklistEntry.Value))
                                {
                                    yield return block;
                                }
                            } else
                            {
                                throw new NotImplementedException($"{nameof(MatchMethod)} value {thisBlacklistEntry.MatchMethod.ToString()} is not implemented");
                            }
                        }
                    }
                }
            }
        }
        private static List<Org> MatchingOrgs(List<ArinBlacklist> BlackListEntries, string OrgXmlPath, Action<string, float> ReportProgress)
        {
            OrgReader reader = new OrgReader(OrgXmlPath);

            if(ReportProgress != null)
            {
                reader.ReportProgress = p =>
                {
                    ReportProgress.Invoke("Org", p);
                };
            }

            return FindMatches(reader.Blocks(), BlackListEntries).ToList();
        }
        private static List<Net> MatchingNets(List<ArinBlacklist> BlackListEntries, List<Org> MatchingOrgs, string NetXmlPath, Action<string, float> ReportProgress)
        {
            NetReader reader = new NetReader(NetXmlPath);

            if (ReportProgress != null)
            {
                reader.ReportProgress = p =>
                {
                    ReportProgress.Invoke("Net", p);
                };
            }

            return FindMatches(reader.Blocks(), BlackListEntries, n => {
                return MatchingOrgs.Any(o => o.Handle == n.OrgHandle);
            }).ToList();
        }
        private void LoadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BlacklistLoadArgs args = e.Argument as BlacklistLoadArgs;

            NetReader netReader = new NetReader(args.NetXmlPath);

            PropertyInfo[] netProps = typeof(Net).GetProperties().Where(p => args.BlackListEntries.Select(a => a.Property).Contains(p.Name)).ToArray();

            List<Org> matchingOrgs = MatchingOrgs(args.BlackListEntries, args.OrgXmlPath, args.ReportProgress);

            List<Net> matchingNets = MatchingNets(args.BlackListEntries, matchingOrgs, args.NetXmlPath, args.ReportProgress);

            BlacklistLoadComplete toReturn = new BlacklistLoadComplete();

            foreach(Net n in matchingNets)
            {
                toReturn.Analyis.Add(new IPAnalysis()
                {
                    FromIp = n.StartAddress.ToString(),
                    ToIp = n.EndAddress.ToString()
                });
            }

            e.Result = toReturn;
        }

        private class BlacklistLoadArgs
        {
            public List<ArinBlacklist> BlackListEntries { get; set; }
            public string NetXmlPath { get; set; }
            public string OrgXmlPath { get; set; }

            public Action<string, float> ReportProgress { get; set; }
        }

        private class BlacklistLoadComplete
        {
            public List<IPAnalysis> Analyis { get; set; }
        }
    }
}
