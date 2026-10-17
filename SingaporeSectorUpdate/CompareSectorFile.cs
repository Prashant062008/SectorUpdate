using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace SingaporeSectorUpdate
{
    class CompareSectorFile
    {
        private static readonly string xmlFile1Path = @"D:\Code\Aegean.xml";
        private static readonly string xmlFile2Path = @"D:\Code\Olympic.xml";
        private static readonly string outputXmlPath = @"D:\Code\ComparedSectors.xml";

        public static void CompareSectorXmlFiles()
        {
            var file1Map = LoadDirectionalPairs(xmlFile1Path);
            var file2Map = LoadDirectionalPairs(xmlFile2Path);

            // Merge both maps into one combined map: Origin => Set of Destinations
            var combined = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            void Merge(Dictionary<string, HashSet<string>> source)
            {
                foreach (var kvp in source)
                {
                    if (!combined.ContainsKey(kvp.Key))
                        combined[kvp.Key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var dest in kvp.Value)
                        combined[kvp.Key].Add(dest);
                }
            }

            Merge(file1Map);
            Merge(file2Map);

            WriteToXml(combined, outputXmlPath);
            Console.WriteLine("✅ XML comparison file generated: " + outputXmlPath);
        }

        private static Dictionary<string, HashSet<string>> LoadDirectionalPairs(string xmlPath)
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(xmlPath))
            {
                Console.WriteLine($"⚠ File not found: {xmlPath}");
                return map;
            }

            DataSet ds = new DataSet();
            ds.ReadXml(xmlPath);

            if (!ds.Tables.Contains("Table")) return map;

            foreach (DataRow row in ds.Tables["Table"].Rows)
            {
                string origin = row["Origin"]?.ToString()?.Trim();
                string dests = row["Destination"]?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(dests)) continue;

                foreach (var destination in dests.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)))
                {
                    if (!map.ContainsKey(origin))
                        map[origin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    map[origin].Add(destination);
                }
            }

            return map;
        }

        private static void WriteToXml(Dictionary<string, HashSet<string>> map, string path)
        {
            using var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true });
            writer.WriteStartDocument();
            writer.WriteStartElement("NewDataSet");

            foreach (var entry in map.OrderBy(x => x.Key))
            {
                writer.WriteStartElement("Table");
                writer.WriteElementString("Origin", entry.Key);
                writer.WriteElementString("Destination", string.Join(",", entry.Value.OrderBy(x => x)));
                writer.WriteElementString("EngineID", "Singapore");
                writer.WriteEndElement(); // </Table>
            }

            writer.WriteEndElement(); // </NewDataSet>
            writer.WriteEndDocument();
        }
    }
}
