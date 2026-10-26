# CounterstrikeSharp - Unlimited Spawns

[![Discord Support](https://img.shields.io/discord/289448144335536138?label=Discord%20Support&color=darkgreen)](https://discord.gg/NtHCk5PWEt)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-unlimitedspawns?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-unlimitedspawns/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-unlimitedspawns](https://img.shields.io/github/issues/Kandru/cs2-unlimitedspawns?color=darkgreen)](https://github.com/Kandru/cs2-unlimitedspawns/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

> [!TIP]
> Please consider a [donation](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG) when you're using this plugin - it took a long time to build and improve - it only costs you a few dollars and would help me very much!

Extra spawn points in Counter-Strike 2. Maps only come with a few places to spawn and this makes it impossible to play with more players then the mapper originally thought of. This plugin adds more spawn points for both teams and automatically disables collision to avoid getting stuck in one another until they moved away from each other. This plug-in also works with the new Rush game mode where the spawn points do change for each round inside the map.

## Installation

1. Install [Metamod:Source](https://www.metamodsource.net/downloads.php?branch=dev).
2. Install [CounterStrikeSharp](https://docs.cssharp.dev/).
3. Download the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-unlimitedspawns/releases/) and copy the `UnlimitedSpawns` folder into `/game/csgo/addons/counterstrikesharp/plugins/`.
4. If CounterStrikeSharp does not already provide `CBaseEntity_CollisionRulesChanged`, copy `gamedata/unlimitedspawns.json` into `/game/csgo/addons/counterstrikesharp/gamedata/`.
5. Restart the server. Config is created at `addons/counterstrikesharp/configs/plugins/UnlimitedSpawns/UnlimitedSpawns.json`.

To run teams larger than the map was built for, also raise the team size limit, for example with [Max Team Players Override](https://github.com/Kandru/cs2-max-team-players-override).

## Build

```bash
make debug      # dockerized dotnet build
make release    # dockerized dotnet publish
```

Uses `mcr.microsoft.com/dotnet/sdk:10.0` — no project Dockerfile.

## Config

`addons/counterstrikesharp/configs/plugins/UnlimitedSpawns/UnlimitedSpawns.json` (generated on first run). Disable the plugin in CounterStrikeSharp if you do not want Unlimited Spawns at all.

| Key | Default | Meaning |
| --- | --- | --- |
| `separation_distance` | `150` | How far (in ingame units) a player must be from every other living player before collision is turned back on |

## Credits

- [CS2-InfiniteSpawns](https://github.com/Kitof/CS2-InfiniteSpawns) by [Kitof](https://github.com/Kitof) — original infinite-spawn / no-block approach
- [CS2-FullTeamLimit](https://github.com/ShookEagle/CS2-FullTeamFix) by [ShookEagle](https://github.com/ShookEagle)
- [NoBlock](https://github.com/justinnobledev/cs2-spawntools) / [ManifestManah](https://github.com/ManifestManah) — collision-group approach for shared spawns

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
