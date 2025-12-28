using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Program;

namespace RaymarineConverter
{
    internal static class FileConverter
    {
        public static void WriteFsh(string outputPath, string groupName, List<Wp> waypoints, bool includeRoute, CultureInfo culture)
        {
            groupName = Helper.SanitizeName(groupName);

            //
            // 1. Build waypoint binary block (matches RayTech field ordering we observed)
            //
            var wpStream = new MemoryStream();
            var w = new BinaryWriter(wpStream);

            double now = DateTime.UtcNow.ToOADate();

            for (int i = 0; i < waypoints.Count; i++)
            {
                var wp = waypoints[i];

                string name = Helper.SanitizeName(wp.Name);
                string guid = $"GUID-{(i + 1):0000}";

                // --- This structure layout matches real .fsh blocks we examined ---

                // Lat / Lon / Time
                w.Write(Helper.DoubleBytes(wp.Lat));
                w.Write(Helper.DoubleBytes(wp.Lon));
                w.Write(Helper.DoubleBytes(now));

                // Placeholder doubles (range/bearing/etc) — RayTech writes zeros too
                for (int j = 0; j < 6; j++)
                    w.Write(Helper.DoubleBytes(0.0));

                // Flags
                w.Write((byte)1);    // visible
                w.Write((byte)0);    // locked flag

                // Name & Group & GUID (16, 16, 36 bytes)
                w.Write(Helper.GetBytes(name, 16));
                w.Write(Helper.GetBytes(groupName, 16));
                w.Write(Helper.GetBytes(guid, 36));
            }

            byte[] waypointBlock = Helper.PadTo512(wpStream.ToArray());


            //
            // 2. Build route block (if requested)
            //
            byte[] routeBlock = Array.Empty<byte>();

            if (includeRoute && waypoints.Count > 0)
            {
                var rs = new MemoryStream();
                var br = new BinaryWriter(rs);

                string routeGuid = "ROUTE-0001";
                string routeName = groupName;

                // Route Name (16)
                br.Write(Helper.GetBytes(routeName, 16));

                // Flags
                br.Write((byte)1); // visible
                br.Write((byte)1); // marked for transfer

                // Number of marks
                br.Write((ushort)waypoints.Count);

                // Route GUID (36)
                br.Write(Helper.GetBytes(routeGuid, 36));

                // Mark names (16 bytes each)
                foreach (var wp in waypoints)
                    br.Write(Helper.GetBytes(Helper.SanitizeName(wp.Name), 16));

                routeBlock = Helper.PadTo512(rs.ToArray());
            }


            //
            // 3. Build the flash DIRECTORY
            //
            var dir = new MemoryStream();
            var bw = new BinaryWriter(dir);

            // "RL90 FLASH FILE\n"
            bw.Write(Helper.GetBytes("RL90 FLASH FILE", 16));

            // Number of objects
            ushort objectCount = (ushort)(includeRoute ? 2 : 1);
            bw.Write(objectCount);

            // Reserved pad
            bw.Write((ushort)0);

            int offset = 512; // data starts after header

            // Entry 0 = Waypoints object
            bw.Write((ushort)1);   // object type (1=waypoints in our samples)
            bw.Write(offset);
            bw.Write(waypointBlock.Length);

            offset += waypointBlock.Length;

            if (includeRoute)
            {
                // Entry 1 = Route object
                bw.Write((ushort)2);   // route object type ID observed
                bw.Write(offset);
                bw.Write(routeBlock.Length);
                offset += routeBlock.Length;
            }

            byte[] directoryBlock = Helper.PadTo512(dir.ToArray());


            //
            // 4. Assemble entire FSH file
            //
            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            fs.Write(directoryBlock);
            fs.Write(waypointBlock);

            if (includeRoute)
                fs.Write(routeBlock);
        }
        // -------- RayTech TXT Writer --------

        public static void WriteRaytechTxt(string outputPath, string groupName, List<Wp> waypoints, CultureInfo culture)
        {
            var output = new List<string>();

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

            double time = DateTime.UtcNow.ToOADate();
            int guidCounter = 1;

            foreach (var wp in waypoints)
            {
                string guid = $"GUID-{guidCounter:0000}";
                guidCounter++;

                string line = string.Format(culture,
                    "{0},{1},{2:0.000000000000000},{3:0.000000000000000},0,0,3,1,0,,,1,1,0,1,0,-32678,65535,{4:0.000000000000000},1,{5}",
                    groupName,
                    Helper.SanitizeName(wp.Name),
                    wp.Lat,
                    wp.Lon,
                    time,
                    guid);

                output.Add(line);
            }

            File.WriteAllLines(outputPath, output);
        }

        // -------- RWF Writer (with optional route) --------

        public static void WriteRwf(string outputPath, string groupName, List<Wp> waypoints, bool includeRoute, CultureInfo culture)
        {
            var output = new List<string>();
            double time = DateTime.UtcNow.ToOADate();

            // --- Waypoints ---
            for (int i = 0; i < waypoints.Count; i++)
            {
                var wp = waypoints[i];
                string guid = $"GUID-{(i + 1):0000}";
                string name = Helper.SanitizeName(wp.Name);
                string loc = Helper.SanitizeName(groupName);   // clamp / clean group name too

                output.Add($"[Wp{i}]");
                output.Add($"Loc={loc}");
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
                output.Add("Show=1"); // or 0 if you want them hidden by default
                output.Add("RcShow=0");
                output.Add("SeaTemp=-32678.000000000000000");
                output.Add("Depth=65535.000000000000000");
                output.Add(string.Format(culture, "Time={0:0.000000000000000}", time));
                output.Add($"GUID={guid}");
                output.Add(""); // blank line between Wp sections (optional but tidy)
            }

            // --- Route ---
            if (includeRoute && waypoints.Count > 0)
            {
                string routeName = Helper.SanitizeName(groupName);  // e.g. "LIS" / "Long Island Soun"

                output.Add("[Rt0]");
                output.Add($"Name={routeName}");
                output.Add("Visible=1");               // your sample had 0; set 1 if you want it shown
                output.Add("Guid=ROUTE-0001");         // or any GUID-style string you like

                for (int i = 0; i < waypoints.Count; i++)
                {
                    string mkName = Helper.SanitizeName(waypoints[i].Name);

                    output.Add($"Mk{i}={mkName}");
                    output.Add($"Cog{i}=0.000000000000000");
                    output.Add($"Eta{i}=0.000000000000000");
                    output.Add($"Length{i}=0.000000000000000");
                    output.Add($"PredictedDrift{i}=0.000000000000000");
                    output.Add($"PredictedSet{i}=0.000000000000000");
                    output.Add($"PredictedSog{i}=0.000000000000000");
                    output.Add($"PredictedTime{i}=0.000000000000000");
                    output.Add($"PredictedTwa{i}=0.000000000000000");
                    output.Add($"PredictedTwd{i}=0.000000000000000");
                    output.Add($"PredictedTws{i}=0.000000000000000");
                }
            }

            File.WriteAllLines(outputPath, output);
        }
        public static List<Wp> LoadWaypoints(string inputPath, CultureInfo culture)
        {
            var list = new List<Wp>();
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

                    list.Add(new Wp(lat, lon, name));
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

                    list.Add(new Wp(lat, lon, name.Trim()));
                }
            }
            else
            {
                Console.WriteLine("Unsupported input type (use .csv or .gpx).");
            }

            return list;
        }
    }
}
