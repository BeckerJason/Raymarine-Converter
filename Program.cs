using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: RaymarineConvert input.csv|input.gpx output.txt waypointgroupname");
            return;
        }

        string inputPath = args[0];
        string outputPath = args[1];
        string waypointGroupName = args[2];

        if (!File.Exists(inputPath))
        {
            Console.WriteLine("Input file not found: " + inputPath);
            return;
        }

        var culture = CultureInfo.InvariantCulture;
        var waypoints = new List<(double lat, double lon, string name)>();

        string ext = Path.GetExtension(inputPath).ToLowerInvariant();

        if (ext == ".csv")
        {
            Console.WriteLine("Reading CSV...");
            foreach (var raw in File.ReadAllLines(inputPath))
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 3)
                {
                    Console.WriteLine("Skipping invalid line: " + line);
                    continue;
                }

                double lat = double.Parse(parts[0], culture);
                double lon = double.Parse(parts[1], culture);
                string name = parts[2].Trim();

                waypoints.Add((lat, lon, name));
            }
        }
        else if (ext == ".gpx")
        {
            Console.WriteLine("Reading GPX...");

            XDocument doc = XDocument.Load(inputPath);
            XNamespace ns = doc.Root.GetDefaultNamespace();

            var wpts = doc.Descendants(ns + "wpt");

            foreach (var w in wpts)
            {
                double lat = double.Parse(w.Attribute("lat").Value, culture);
                double lon = double.Parse(w.Attribute("lon").Value, culture);

                string name =
                    (string)w.Element(ns + "name") ??
                    (string)w.Element(ns + "desc") ??
                    "WP";

                name = name.Trim();

                waypoints.Add((lat, lon, name));
            }
        }
        else
        {
            Console.WriteLine("Unsupported input type. Use .csv or .gpx");
            return;
        }

        if (waypoints.Count == 0)
        {
            Console.WriteLine("No waypoints found.");
            return;
        }

        var output = new List<string>();

        // ---- RayTech Required Header (first 10 lines) ----
        output.Add("*********** RAYTECH WAPOINT AND ROUTE TXT FILE --DO NOT EDIT THIS LINE!!!! ***********");
        output.Add("*********** The first 10 lines of this file are reserved *****************");
        output.Add("*********** The waypoint data is comma delimited in the order of: ***********");
        output.Add("*********** Loc,Name,Lat,Long,Rng,Bear,Bmp,Fixed,Locked,Notes,Rel,RelSet,RcCount,RcRadius,Show,RcShow,SeaTemp,Depth,Time,MarkedForTransfer,GUID*********");
        output.Add("*********** Following the waypoint data is the route data: ********");
        output.Add("*********** Route data is also comma delimited in the order of:***********");
        output.Add("*********** RouteName,Visible,MarkedForTransfer,NumMarks, Guid***********");
        output.Add("*********** MarkName,Cog,Eta,Length,PredictedDrift,PredictedSet,PredictedSog,PredictedTime,PredictedTwa,PredictedTwd,PredictedTws***********");
        output.Add("*****************************************************************************************************************");
        output.Add("************************************ END HEADER ****************************************************************");

        int guidCounter = 1;

        foreach (var wp in waypoints)
        {
            string guid = $"GUID-{guidCounter:0000}";
            guidCounter++;

            string line = string.Format(culture,
                "{0},{1},{2:0.000000000000000},{3:0.000000000000000},0,0,3,1,0,,,1,1,0,1,0,-32678,65535,0,1,{4}",
                waypointGroupName,
                SanitizeName(wp.name),
                wp.lat,
                wp.lon,
                guid
            );

            output.Add(line);
        }

        File.WriteAllLines(outputPath, output);

        Console.WriteLine($"Done! Wrote {waypoints.Count} waypoints to {outputPath}");
    }

    static string SanitizeName(string name)
    {
        // Raymarine prefers <=16 chars — truncate if needed
        if (name.Length > 16)
            name = name.Substring(0, 16);

        // Remove commas to avoid breaking format
        return name.Replace(",", " ");
    }
}
