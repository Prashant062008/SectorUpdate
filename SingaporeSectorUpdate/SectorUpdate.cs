using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace SingaporeSectorUpdate
{
    class SectorUpdate
    {
        private static readonly string csvFilePath = @"D:\Code\SQ_AirlineProfile_PRD.csv";
        private static readonly string xmlOutputPath = @"D:\CODE\Singapore.xml";

        private static readonly Dictionary<string, HashSet<string>> originDestMap = new();
        private static readonly HashSet<string> processedPairs = new();

        public static void GenerateSectorFile()
        {
            var lines = File.ReadAllLines(csvFilePath);

            for (int i = 1; i < lines.Length; i++) // skip header
            {
                var parts = lines[i].Split(',');

                if (parts.Length < 2) continue;

                string origin = parts[0].Trim();
                string destination = parts[1].Trim();

                if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination)) continue;

                string pairKey = GetSortedKey(origin, destination);
                if (!processedPairs.Contains(pairKey))
                {
                    AddToMap(origin, destination);
                    AddToMap(destination, origin);
                    processedPairs.Add(pairKey);
                }
            }

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
                dr["EngineID"] = "Singapore";
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
