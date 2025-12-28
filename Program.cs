using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: RaymarineConvert input.(csv|gpx) output.(txt|rwf) waypointgroupname");
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

        // 1) Read input as a simple list of waypoints
        var waypoints = LoadWaypoints(inputPath, culture);

        if (waypoints.Count == 0)
        {
            Console.WriteLine("No waypoints found in input.");
            return;
        }

        string outExt = Path.GetExtension(outputPath).ToLowerInvariant();

        if (outExt == ".rwf")
        {
            WriteRwf(outputPath, waypointGroupName, waypoints, culture);
        }
        else
        {
            // default: RayTech TXT file
            WriteRaytechTxt(outputPath, waypointGroupName, waypoints, culture);
        }

        Console.WriteLine($"Done! Wrote {waypoints.Count} waypoints to {outputPath}");
    }

    // ---------- waypoint record ----------
    public record Wp(double Lat, double Lon, string Name);

    // ---------- Load from CSV or GPX ----------

    static List<Wp> LoadWaypoints(string inputPath, CultureInfo culture)
    {
        var waypoints = new List<Wp>();
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

                waypoints.Add(new Wp(lat, lon, name));
            }
        }
        else if (ext == ".gpx")
        {
            Console.WriteLine("Reading GPX...");
            XDocument doc = XDocument.Load(inputPath);
            XNamespace ns = doc.Root.GetDefaultNamespace();

            foreach (var w in doc.Descendants(ns + "wpt"))
            {
                double lat = double.Parse(w.Attribute("lat").Value, culture);
                double lon = double.Parse(w.Attribute("lon").Value, culture);

                string name =
                    (string)w.Element(ns + "name") ??
                    (string)w.Element(ns + "desc") ??
                    "WP";

                waypoints.Add(new Wp(lat, lon, name.Trim()));
            }
        }
        else
        {
            Console.WriteLine("Unsupported input type (use .csv or .gpx).");
        }

        return waypoints;
    }

    // ---------- Sanitize waypoint name for Raymarine ----------

    static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            name = "WP";

        name = name.Trim();

        // Raymarine tends to like <= 16 characters
        if (name.Length > 16)
            name = name.Substring(0, 16);

        // Commas would break the format
        return name.Replace(",", " ");
    }

    // ---------- Existing RayTech TXT writer ----------

    static void WriteRaytechTxt(string outputPath, string groupName, List<Wp> waypoints, CultureInfo culture)
    {
        var output = new List<string>();

        // Header (first 10 lines)
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
        double time = DateTime.UtcNow.ToOADate(); // similar style number, but any double works

        foreach (var wp in waypoints)
        {
            string guid = $"GUID-{guidCounter:0000}";
            guidCounter++;

            string line = string.Format(culture,
                "{0},{1},{2:0.000000000000000},{3:0.000000000000000},0,0,3,1,0,,,1,1,0,1,0,-32678,65535,{4:0.000000000000000},1,{5}",
                groupName,
                SanitizeName(wp.Name),
                wp.Lat,
                wp.Lon,
                time,
                guid
            );

            output.Add(line);
        }

        File.WriteAllLines(outputPath, output);
    }

    // ---------- NEW: Raymarine .rwf writer ----------

    static void WriteRwf(string outputPath, string groupName, List<Wp> waypoints, CultureInfo culture)
    {
        var output = new List<string>();
        int i = 0;
        double time = DateTime.UtcNow.ToOADate();

        foreach (var wp in waypoints)
        {
            string guid = $"GUID-{(i + 1):0000}";
            string name = SanitizeName(wp.Name);

            output.Add($"[Wp{i}]");
            output.Add($"Loc={groupName}");
            output.Add($"Name={name}");
            output.Add(string.Format(culture, "Lat={0:0.000000000000000}", wp.Lat));
            output.Add(string.Format(culture, "Long={0:0.000000000000000}", wp.Lon));
            output.Add("Rng=0.000000000000000");
            output.Add("Bear=0.000000000000000");
            output.Add("Bmp=3");
            output.Add("Fixed=1");
            output.Add("Locked=0");
            output.Add("Notes=");
            output.Add("Rel=");
            output.Add("RelSet=1");
            output.Add("RcCount=1");
            output.Add("RcRadius=0.000000000000000");
            output.Add("Show=1");
            output.Add("RcShow=0");
            output.Add("SeaTemp=-32678.000000000000000");
            output.Add("Depth=65535.000000000000000");
            output.Add(string.Format(culture, "Time={0:0.000000000000000}", time));
            output.Add($"GUID={guid}");
            output.Add(""); // blank line between waypoints

            i++;
        }

        File.WriteAllLines(outputPath, output);
    }
}
