using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace SingaporeSectorUpdate
{
    class A3Sector
    {
        private static readonly string xmlInputPath = @"D:\CloudCache\LCCValidSectors\Aegean_Routelist.xml";
        private static readonly string xmlOutputPath = @"D:\CODE\Aegean.xml";

        private static readonly Dictionary<string, HashSet<string>> originDestMap = new();
        private static readonly HashSet<string> processedPairs = new();

        public static void GenerateSectorFile()
        {
            XmlDocument doc = new();
            doc.Load(xmlInputPath);

            // Pick all <OD> nodes
            XmlNodeList odNodes = doc.SelectNodes("//OD");

            foreach (XmlNode od in odNodes)
            {
                string origin = od.Attributes["o"]?.Value?.Trim();
                string destination = od.Attributes["d"]?.Value?.Trim();

                if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination))
                    continue;

                string pairKey = GetSortedKey(origin, destination);
                if (!processedPairs.Contains(pairKey))
                {
                    AddToMap(origin, destination);
                    AddToMap(destination, origin);
                    processedPairs.Add(pairKey);
                }
            }

            // Build DataSet
            DataSet ds = new("NewDataSet");
            DataTable dt = new("Table");
            dt.Columns.Add("Origin", typeof(string));
            dt.Columns.Add("Destination", typeof(string));
            dt.Columns.Add("EngineID", typeof(string));

            foreach (var entry in originDestMap.OrderBy(e => e.Key))
            {
                DataRow dr = dt.NewRow();
                dr["Origin"] = entry.Key;
                dr["Destination"] = string.Join(",", entry.Value.OrderBy(x => x));
                dr["EngineID"] = "Aegean";
                dt.Rows.Add(dr);
            }

            ds.Tables.Add(dt);

            using var writer = XmlWriter.Create(xmlOutputPath, new XmlWriterSettings { Indent = true });
            ds.WriteXml(writer, XmlWriteMode.WriteSchema);

            Console.WriteLine("✅ XML generated successfully at: " + xmlOutputPath);
        }

        private static string GetSortedKey(string a, string b) =>
            string.Compare(a, b, StringComparison.OrdinalIgnoreCase) < 0 ? $"{a}-{b}" : $"{b}-{a}";

        private static void AddToMap(string origin, string destination)
        {
            if (!originDestMap.ContainsKey(origin))
                originDestMap[origin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            originDestMap[origin].Add(destination);
        }
    }
}
