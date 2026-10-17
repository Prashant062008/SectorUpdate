using CsvHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace SingaporeSectorUpdate
{
    public class AtlasExcel
    {
        private static readonly string csvInputPath = @"C:\Users\kmr.prashant\Downloads\cached_route.csv";
        private static readonly string xmlOutputPath = @"D:\CODE\Atlas.xml";

        private static readonly Dictionary<string, OriginInfo> originDestMap =
            new Dictionary<string, OriginInfo>(StringComparer.OrdinalIgnoreCase);

        public static void GenerateSectorFile()
        {
            if (!File.Exists(csvInputPath))
            {
                Console.WriteLine("CSV file not found.");
                return;
            }

            

            // Skip header
            using (var reader = new StreamReader(csvInputPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    string airlines = csv.GetField("airlines");
                    string origin = csv.GetField("fromCity");
                    string destination = csv.GetField("toCity");

                    AddToMap(origin, destination, airlines);
                    AddToMap(destination, origin, airlines);
                }
            }

            DataSet ds = new DataSet("NewDataSet");
            DataTable dt = new DataTable("Table");

            dt.Columns.Add("Origin");
            dt.Columns.Add("Destination");
            dt.Columns.Add("EngineID");
            

            // Collect all airline codes
            HashSet<string> airlineColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in originDestMap.Values)
            {
                foreach (var airline in item.Airlines.Keys)
                    airlineColumns.Add(airline);
            }

            foreach (string airline in airlineColumns.OrderBy(x => x))
            {
                dt.Columns.Add("Airline_" + airline);
            }

            foreach (var item in originDestMap.OrderBy(x => x.Key))
            {
                DataRow dr = dt.NewRow();

                dr["Origin"] = item.Key;

                dr["Destination"] = string.Join(",",
                    item.Value.Destinations.OrderBy(x => x));

                dr["EngineID"] = "Atlas";

                foreach (var airline in item.Value.Airlines)
                {
                    dr["Airline_" + airline.Key] = string.Join(",",
                        airline.Value.OrderBy(x => x));
                }

                dt.Rows.Add(dr);
            }

            ds.Tables.Add(dt);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true
            };

            using (XmlWriter writer = XmlWriter.Create(xmlOutputPath, settings))
            {
                ds.WriteXml(writer, XmlWriteMode.WriteSchema);
            }

            Console.WriteLine("XML generated successfully at: " + xmlOutputPath);
        }

        private static void AddToMap(string origin, string destination, string airlines)
        {
            if (!originDestMap.ContainsKey(origin))
                originDestMap[origin] = new OriginInfo();

            originDestMap[origin].Destinations.Add(destination);

            if (string.IsNullOrWhiteSpace(airlines))
                return;

            foreach (string airline in airlines.Split(',')
                                               .Select(x => x.Trim())
                                               .Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!originDestMap[origin].Airlines.ContainsKey(airline))
                {
                    originDestMap[origin].Airlines[airline] =
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                originDestMap[origin].Airlines[airline].Add(destination);
            }
        }
    }

    public class OriginInfo
    {
        public HashSet<string> Destinations { get; set; }
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, HashSet<string>> Airlines { get; set; }
            = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    }
}