# Raymarine-Converter
Convert Files to Raymarine readable Files

# Use
`RaymarineConvert input.csv|input.gpx output.txt waypointgroupname`

# Raymarine RayTech Navigator Use
## Importing TXT for conversion to RWF
### Raymarine expects your .TXT file in C:\Archive or any other root\Archive
- File
- Import/Export Routes and Waypoints
- Import Routes and Waypoints
- Import from File
- Import from Comma Delimited File (even though the files are .TXT)
- Select your TXT file


# Goal
The goal of this software was to go from Garmin to Raymarine C90

I did: 
- Export from Garmin to ADM (if it cant do GPX)
- Convert from ADM to GPX with https://www.gpsvisualizer.com/
- Convert from GPX to WayPoint and Routes GPX in BaseCamp
- Convert from GPX to TXT with RaymarineConverter
- Convert from TXT to FSH in RayTech Navigator

### Notes
I used https://www.gpsvisualizer.com/ To convert from the initial ADM file to GPX
