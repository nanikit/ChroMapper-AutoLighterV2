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

1. Create a `Directory.Build.props.user` file in the solution root (if it doesn't exist)
2. Set your ChroMapper installation path:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
    <PropertyGroup>
        <ChroMapperPath>YOUR_PATH_TO_CHROMAPPER</ChroMapperPath>
    </PropertyGroup>
</Project>
```

**Note:** The `Directory.Build.props.user` file is git-ignored and contains your local paths.

### Building

```bash
  dotnet build AutoLighterV2.sln
```

The compiled DLL will be in `AutoLighterV2/bin/Debug/` or `AutoLighterV2/bin/Release/`.

### Installation

Copy the compiled `AutoLighterV2.dll` to your ChroMapper plugins folder:
`[ChroMapper]/ChroMapper_Data/Plugins/`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
This project uses the ChroMapper-Automapper Plugin by Loloppe as a starting point, which was also licensed under the MIT License.

## Acknowledgments

- ChroMapper-Automapper Plugin by Loloppe
- ChroMapper by Caeden117 and contributors

