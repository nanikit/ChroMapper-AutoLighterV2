# Beat Saber AutoLighter V2

ChroMapper plugin for automatic light event generation based on notes, arcs, and walls.
When available, it also uses bookmarks to generate boost events and to align ring events and color changes with downbeats.

This is a port of my Python autolighter, which has been used for over a year in my private Discord bot.

## Installation
1. Download `AutoLighterV2.dll`
2. Place in ChroMapper `Plugins` folder
3. Restart ChroMapper

## Usage
1. Open your map in ChroMapper
2. Open the Plugins menu by pressing `Tab` and select `AutoLighter V2` from the list
3. Configure the settings as desired, defaults should work fine for most maps (to reset to defaults, press the `Reset` button)
4. Click `Autolight` to generate the light events

If you want to copy the generated lightevents to all other difficulties, use the `Sync To All Diffs` button.

### Usage Notes
- The plugin will overwrite existing light events in the map. By clicking the `Sync To All Diffs` button, existing light events in ALL other difficulties will also be overwritten
- If you want the lights more calm, increase the `Anti Flicker` threshold
- If you want strobes generated, enable the `Use Strobes` option and place more than 2 short walls closer than 0.25 beats apart
- Bookmarks on full beats are considered to find the downbeat to align ring events and color changes. Boost Mode 1 also tries to snap boost events to close bookmarks
- Boost events are disabled by default. If you enable it, don't forget to set boost colors so the boost lights are not black
- If you have long walls timed to vocals consider the `Use Long Walls` option to use them for light generation as well
- If you have short decoration walls spaced apart further than strobe walls, `Wall Sprinkles` will light them

## Setup for Development

### Prerequisites

- ChroMapper installed

### Configuration

1. Create an `AutoLighterV2/AutoLighterV2.csproj.user` file (if it doesn't exist)
2. Set your ChroMapper installation path:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <ChroMapperDir>YOUR_PATH_TO_CHROMAPPER</ChroMapperDir>
  </PropertyGroup>
</Project>
```

**Note:** The `*.csproj.user` file is git-ignored and contains your local paths.

### Building

```bash
  dotnet build AutoLighterV2.sln
```

The compiled DLL is written to `AutoLighterV2/bin/Release/netstandard2.1/` (or `bin/Dev/Plugins/` for Debug) and, when `ChroMapperDir` is set, is automatically copied into `[ChroMapperDir]/Plugins/`. Restart ChroMapper to load the new build.

VS Code: run the **Build** task (`Ctrl+Shift+B`).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
This project uses the ChroMapper-Automapper Plugin by Loloppe as a starting point, which was also licensed under the MIT License.

## Acknowledgments

- ChroMapper-Automapper Plugin by Loloppe
- ChroMapper by Caeden117 and contributors

