using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace UnlimitedSpawns
{
    public class PluginConfig : BasePluginConfig
    {
        /// <summary>
        /// Horizontal distance (game units) a ghosted pawn must keep from every other living pawn before player collision is restored.
        /// </summary>
        [JsonPropertyName("separation_distance")]
        public float SeparationDistance { get; set; } = 100f;

        /// <summary>
        /// Minimum seconds between distance checks for the same ghosted pawn.
        /// </summary>
        [JsonPropertyName("min_check_interval")]
        public float MinCheckInterval { get; set; } = 1f;

        public void Update()
        {
            if (SeparationDistance < 1f)
            {
                SeparationDistance = 1f;
            }

            if (MinCheckInterval < 0.1f)
            {
                MinCheckInterval = 0.1f;
            }
        }
    }

    public partial class UnlimitedSpawns : IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            Config.Update();
            _separationDistanceSquared = Config.SeparationDistance * Config.SeparationDistance;
        }
    }
}
