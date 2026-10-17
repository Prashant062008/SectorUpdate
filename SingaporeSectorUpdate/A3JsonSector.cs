using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;

namespace SingaporeSectorUpdate
{
    class A3JsonSector
    {
        private static readonly string xmlInputPath = @"D:\CloudCache\LCCValidSectors\Olympic_RoutelistJson.Json";
        private static readonly string xmlOutputPath = @"D:\CODE\Olympic.xml";

        private static readonly Dictionary<string, HashSet<string>> originDestMap = new();
        private static readonly HashSet<string> processedPairs = new();

        public static void GenerateSectorFile()
        {
            string jsonContent = File.ReadAllText(xmlInputPath);
            
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement route in root.EnumerateArray())
                {
                    string origin = route.TryGetProperty("Dep", out var depElement) 
                        ? depElement.GetString()?.Trim() 
                        : null;
                    string destination = route.TryGetProperty("Arr", out var arrElement) 
                        ? arrElement.GetString()?.Trim() 
                        : null;

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
                dr["EngineID"] = "Olympic";
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
