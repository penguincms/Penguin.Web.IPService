using Loxifi;
using Penguin.Web.Objects;
using Penguin.Web.Registrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Penguin.Web.IPService
{
    internal static class IPService
    {
        private static List<IIPRegistration> IPRegistrations { get; set; }

        private static List<IPAnalysis> DiscoveredRanges { get; set; }

        public static void AddAnalysis(IPAnalysis analysis)
        {
            DiscoveredRanges.Add(analysis);
        }

        public const string ANALYSIS_FILENAME = "IP.cache";

        //This is going to get really slow, really fast. This should be Async and incremental
        [Obsolete]
        private static void SaveAnalysis()
        {
            IFormatter formatter = new BinaryFormatter();

            Stream stream = new FileStream(ANALYSIS_FILENAME, FileMode.Create, FileAccess.Write);

            formatter.Serialize(stream, DiscoveredRanges);

            stream.Close();
        }

        private static void TryLoadAnalysis()
        {
            if (DiscoveredRanges is null)
            {
                if (File.Exists(ANALYSIS_FILENAME))
                {
                    LoadAnalysis();
                }
                else
                {
                    DiscoveredRanges = new List<IPAnalysis>();
                }
            }
        }

        [Obsolete]
        private static void LoadAnalysis()
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(ANALYSIS_FILENAME, FileMode.Open, FileAccess.Read);
            DiscoveredRanges = (List<IPAnalysis>)formatter.Deserialize(stream);
        }

        public const string BLACKLIST_FILENAME = "Blacklist.config";

        static IPService()
        {
            if (File.Exists(BLACKLIST_FILENAME))
            {
                string[] BlacklistLines = File.ReadAllLines(BLACKLIST_FILENAME);

                IPRegistrations = new List<IIPRegistration>();

                foreach (string line in BlacklistLines)
                {
                    if (line.Trim().StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.Contains("/"))
                    {
                        IPRegistrations.Add(new CIDRRegistration(line));
                    }
                    else if (line.Contains("-"))
                    {
                        IPRegistrations.Add(new RangeRegistration(line));
                    }
                    else
                    {
                        IPRegistrations.Add(new SingleIPRegistration(line));
                    }
                }
            }
        }

        public const double QueryTimeout = 2000;

        private static DateTime LastQuery { get; set; }

        private static object QueryLock { get; set; } = new object();

        public static async Task<IPAnalysis> QueryIP(IPAddress Ip)
        {
            IPAnalysis? analysis = null;

            Monitor.Enter(QueryLock);

            try
            {
                TryLoadAnalysis();

                foreach (IPAnalysis discoveredAnalysis in DiscoveredRanges)
                {
                    if (discoveredAnalysis.IsMatch(Ip))
                    {
                        analysis = discoveredAnalysis;
                        break;
                    }
                }

                if (analysis is null)
                {
                    IPAnalysis nanalysis = new()
                    {
                        DiscoveryDate = DateTime.Now
                    };

                    if (LastQuery != DateTime.MinValue && (DateTime.Now - LastQuery).TotalMilliseconds < QueryTimeout)
                    {
                        System.Threading.Thread.Sleep((int)(QueryTimeout - (DateTime.Now - LastQuery).TotalMilliseconds));
                    }

                    StringBuilder processResponse = new();

                    _ = await ProcessRunner.StartAsync(new ProcessSettings(Path.Combine(Directory.GetCurrentDirectory(), "Whois", "whosip.exe"))
                    {
                        WorkingDirectory = Directory.GetCurrentDirectory(),
                        Arguments = Ip.ToString(),
                        StdOutWrite = (s, e) => processResponse.Append(s)
                    });

                    string Response = processResponse.ToString();

                    string[] lines = Response.Split('\r').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();

                    foreach (string line in lines)
                    {
                        string key = line.To(":").ToLower().Trim();
                        string value = line.From(":").Trim();

                        switch (key)
                        {
                            case "whois source":
                                nanalysis.WhoisSource = value;
                                break;

                            case "ip address":
                                nanalysis.IpAddress = value;
                                break;

                            case "country":
                                nanalysis.Country = value;
                                break;

                            case "network name":
                                nanalysis.NetworkName = value;
                                break;

                            case "owner name":
                                nanalysis.OwnerName = value;
                                break;

                            case "cidr":
                                nanalysis.CIDR = value.Split(",").Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                                break;

                            case "from ip":
                                nanalysis.FromIp = value;
                                break;

                            case "to ip":
                                nanalysis.ToIp = value;
                                break;

                            case "allocated":
                                nanalysis.Allocated = value == "Yes";
                                break;

                            case "contact name":
                                nanalysis.ContactName = value;
                                break;

                            case "address":
                                nanalysis.Address = value;
                                break;

                            case "email":
                                nanalysis.Email = value;
                                break;

                            case "abuse email":
                                nanalysis.AbuseEmail = value;
                                break;

                            case "phone":
                                nanalysis.Phone = value;
                                break;

                            case "fax":
                                nanalysis.Fax = value;
                                break;
                        }
                    }

                    analysis = nanalysis;

                    LastQuery = DateTime.Now;

                    AddAnalysis(analysis.Value);

                    SaveAnalysis();
                }


                return analysis.Value;
            }
            finally
            {
                Monitor.Exit(QueryLock);
            }
        }

        public static bool IsBlacklisted(IPAddress Ip)
        {
            if (IPRegistrations != null)
            {
                foreach (IIPRegistration iPRegistration in IPRegistrations)
                {
                    if (iPRegistration.IsMatch(IPRegistration.IpToInt(Ip)))
                    {
                        return true;
                    }
                }
            }

            _ = QueryIP(Ip);

            return false;
        }
    }
}