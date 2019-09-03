using IPRangeBreakdown.Registrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Framework.Shared.Extensions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace IPRangeBreakdown
{
    public static class IPService
    {
        static List<IIPRegistration> IPRegistrations { get; set; }

        static List<IPAnalysis> DiscoveredRanges { get; set; }

        public static void AddAnalysis(IPAnalysis analysis)
        {
            DiscoveredRanges.Add(analysis);
        }

        public const string ANALYSIS_FILENAME = "IP.cache";

        //This is going to get really slow, really fast. This should be Async and incremental
        private static void SaveAnalysis()
        {
            IFormatter formatter = new BinaryFormatter();

            Stream stream = new FileStream(ANALYSIS_FILENAME, FileMode.Create, FileAccess.Write);

            formatter.Serialize(stream, DiscoveredRanges);

            stream.Close();
        }

        private static void TryLoadAnalysis()
        {
            if(DiscoveredRanges is null)
            {
                if(File.Exists(ANALYSIS_FILENAME))
                {
                    LoadAnalysis();
                } else
                {
                    DiscoveredRanges = new List<IPAnalysis>();
                }
            }
        }

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
        private static Object QueryLock { get; set; } = new object();
        public static IPAnalysis QueryIP(string Ip)
        {
            IPAnalysis analysis = null;

            lock (QueryLock)
            {

                TryLoadAnalysis();

                foreach(IPAnalysis discoveredAnalysis in DiscoveredRanges)
                {
                    if(discoveredAnalysis.IsMatch(Ip))
                    {
                        analysis = discoveredAnalysis;
                        break;
                    }
                }

                if (analysis is null)
                {
                    analysis = new IPAnalysis()
                    {
                        DiscoveryDate = DateTime.Now
                    };

                    if (LastQuery != DateTime.MinValue && (DateTime.Now - LastQuery).TotalMilliseconds < QueryTimeout)
                    {
                        System.Threading.Thread.Sleep((int)(QueryTimeout - (DateTime.Now - LastQuery).TotalMilliseconds));
                    }

                    string Response = Framework.Console.Process.Run(Path.Combine(Directory.GetCurrentDirectory(), "Whois", "whosip.exe"), Ip);

                    string[] lines = Response.Split('\r').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();

                    foreach (string line in lines)
                    {
                        string key = line.To(":").ToLower().Trim();
                        string value = line.From(":").Trim();

                        switch (key)
                        {
                            case "whois source":
                                analysis.WhoisSource = value;
                                break;
                            case "ip address":
                                analysis.IpAddress = value;
                                break;
                            case "country":
                                analysis.Country = value;
                                break;
                            case "network name":
                                analysis.NetworkName = value;
                                break;
                            case "owner name":
                                analysis.OwnerName = value;
                                break;
                            case "cidr":
                                analysis.CIDR = value.Split(",").Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                                break;
                            case "from ip":
                                analysis.FromIp = value;
                                break;
                            case "to ip":
                                analysis.ToIp = value;
                                break;
                            case "allocated":
                                analysis.Allocated = value == "Yes";
                                break;
                            case "contact name":
                                analysis.ContactName = value;
                                break;
                            case "address":
                                analysis.Address = value;
                                break;
                            case "email":
                                analysis.Email = value;
                                break;
                            case "abuse email":
                                analysis.AbuseEmail = value;
                                break;
                            case "phone":
                                analysis.Phone = value;
                                break;
                            case "fax":
                                analysis.Fax = value;
                                break;

                        }

                    }


                    LastQuery = DateTime.Now;

                    AddAnalysis(analysis);

                    SaveAnalysis();
                }
            }

            return analysis;
        }
        public static bool IsBlacklisted(string Ip)
        {

            if (IPRegistrations != null)
            {
                foreach (IIPRegistration iPRegistration in IPRegistrations)
                {
                    if (iPRegistration.IsMatch(Ip))
                    {
                        return true;
                    }
                }
            }

            IPAnalysis analyzeIP = QueryIP(Ip);

            return false;
        }
    }
}
