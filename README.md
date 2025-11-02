# AutoLighter V2

ChroMapper plugin for automatic light event generation based on notes, arcs, and walls.
This is a port of my Python autolighter, which has been used for over a year in my private Discord bot.

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

