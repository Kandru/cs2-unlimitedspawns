# CounterstrikeSharp - Unlimited Spawns

[![Discord Support](https://img.shields.io/discord/289448144335536138?label=Discord%20Support&color=darkgreen)](https://discord.gg/NtHCk5PWEt)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-unlimitedspawns?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-unlimitedspawns/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-unlimitedspawns](https://img.shields.io/github/issues/Kandru/cs2-unlimitedspawns?color=darkgreen)](https://github.com/Kandru/cs2-unlimitedspawns/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

Lets several players share a map spawn without getting stuck inside each other. After spawn they have no player collision until they are far enough apart; then normal collision comes back.

> [!TIP]
> Please consider a [donation](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG) when you're using this plugin - it only costs you a few dollars and would help me very much!

> [!IMPORTANT]
> Create a GitHub Issue for bugs, features, and improvements. Use Discord for everything else.

## Features

- On spawn, disable player-to-player collision so stacked players can move apart.
- After `mp_freezetime` ends (`round_freeze_end`), check one ghosted player per tick against other living pawns.
- Restore collision once a player is at least `separation_distance` units away from every other living player (default 100).
- Build with Docker (`make debug` / `make release`) - no local .NET install required.

## Requirements

- [MetaMod:Source](https://github.com/alliedmodders/metamod-source/)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

Raise team size / spawnable counts with something like [Max Team Players Override](https://github.com/Kandru/cs2-max-team-players-override) when you want more than the map's usual roster. This plugin only handles collision for players who already share a spawn.

## Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-unlimitedspawns/releases/).
2. Move the `UnlimitedSpawns` folder to `addons/counterstrikesharp/plugins/`.
3. If your CounterStrikeSharp install does not already provide `CBaseEntity_CollisionRulesChanged`, copy `gamedata/unlimitedspawns.json` into `addons/counterstrikesharp/gamedata/`.
4. Restart the server.

## Configuration

The plugin writes `/addons/counterstrikesharp/configs/plugins/UnlimitedSpawns/UnlimitedSpawns.json`.

| Key | Default | Description |
|---|---|---|
| `separation_distance` | `100` | Horizontal distance (units) required from every other living player before collision is restored |
| `min_check_interval` | `1` | Minimum seconds between distance checks for the same player |

## Compile Yourself

Clone the project:

```bash
git clone https://github.com/Kandru/cs2-unlimitedspawns.git
cd cs2-unlimitedspawns
```

Build debug files (development game server):

```bash
make debug
```

Build release files (production game server):

```bash
make release
```

Docker runs the .NET 10 SDK. No local `dotnet` install is required.

## Credits

- [CS2-InfiniteSpawns](https://github.com/Kitof/CS2-InfiniteSpawns) by [Kitof](https://github.com/Kitof) - original infinite-spawn / no-block approach
- [CS2-FullTeamLimit](https://github.com/ShookEagle/CS2-FullTeamFix) by [ShookEagle](https://github.com/ShookEagle)
- [NoBlock](https://github.com/justinnobledev/cs2-spawntools) / [ManifestManah](https://github.com/ManifestManah) - collision-group approach for shared spawns

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
