using RaymarineConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

class Program
{
    public record Wp(double Lat, double Lon, string Name);

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
        var waypoints = FileConverter.LoadWaypoints(inputPath, culture);

        if (waypoints.Count == 0)
        {
            Console.WriteLine("No waypoints found in input.");
            return;
        }

        string outExt = Path.GetExtension(outputPath).ToLowerInvariant();

        bool includeRoute = false;

        if (outExt == ".rwf")
        {
            Console.Write("Generate route in file order? (y/n): ");
            var key = Console.ReadKey();
            Console.WriteLine();
            includeRoute = key.KeyChar is 'y' or 'Y';

            FileConverter.WriteRwf(outputPath, waypointGroupName, waypoints, includeRoute, culture);
        }
        else if (outExt == ".fsh")
        {
            Console.Write("Generate route in file order? (y/n): ");
            var key = Console.ReadKey();
            Console.WriteLine();
            includeRoute = key.KeyChar is 'y' or 'Y';

            FileConverter.WriteFsh(outputPath, waypointGroupName, waypoints, includeRoute, culture);
        }
        else
        {
            FileConverter.WriteRaytechTxt(outputPath, waypointGroupName, waypoints, culture);
        }

        Console.WriteLine($"Done! Wrote {waypoints.Count} waypoint(s) to {outputPath}");
    }

    

    

    

}
