using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace UnlimitedSpawns
{
    public partial class UnlimitedSpawns
    {
        private bool _spawnsFilled;
        private int _spawnFillAttempts;

        private void OnMapStart(string mapName)
        {
            _ghostChecksEnabled = false;
            // Map entities need time to exist; hibernation must be off or the timer never fires.
            EnsureSpawns(delaySeconds: 3.0f);
        }

        /// <summary>
        /// Count current T/CT spawn entities and create only the missing ones up to MaxPlayers/2.
        /// Used on map start (delayed) and on plugin hot-reload (immediate).
        /// </summary>
        private void EnsureSpawns(float delaySeconds = 0f)
        {
            _spawnsFilled = false;
            _spawnFillAttempts = 0;

            // Timers do not run while the empty server hibernates.
            Server.ExecuteCommand("sv_hibernate_when_empty 0");

            if (delaySeconds > 0f)
            {
                AddTimer(delaySeconds, TryFillSpawns);
            }
            else
            {
                Server.NextFrame(TryFillSpawns);
            }
        }

        private void TryFillSpawns()
        {
            if (_spawnsFilled)
            {
                RestoreHibernate();
                return;
            }

            int tCount = CountSpawns("info_player_terrorist");
            int ctCount = CountSpawns("info_player_counterterrorist");

            // Map spawn entities are not always present on the first frame after map change.
            if (tCount == 0 && ctCount == 0)
            {
                _spawnFillAttempts++;
                if (_spawnFillAttempts < 64)
                {
                    Server.NextFrame(TryFillSpawns);
                    return;
                }

                Console.WriteLine("[UnlimitedSpawns] No map spawns found; skipping fill.");
                _spawnsFilled = true;
                RestoreHibernate();
                return;
            }

            int targetPerTeam = Server.MaxPlayers / 2;
            int createdT = 0;
            int createdCt = 0;

            if (targetPerTeam > 0)
            {
                createdT = AddMissingSpawns(
                    CollectUniqueTemplates("info_player_terrorist"),
                    tCount,
                    CsTeam.Terrorist,
                    targetPerTeam);
                createdCt = AddMissingSpawns(
                    CollectUniqueTemplates("info_player_counterterrorist"),
                    ctCount,
                    CsTeam.CounterTerrorist,
                    targetPerTeam);
            }

            _spawnsFilled = true;
            RestoreHibernate();

            if (createdT == 0 && createdCt == 0)
            {
                Console.WriteLine(
                    $"[UnlimitedSpawns] Spawn check OK — T={tCount} CT={ctCount} " +
                    $"(target {targetPerTeam} per team, MaxPlayers={Server.MaxPlayers}).");
            }
            else
            {
                Console.WriteLine(
                    $"[UnlimitedSpawns] Added missing spawns — T={tCount}+{createdT} CT={ctCount}+{createdCt} " +
                    $"(target {targetPerTeam} per team, MaxPlayers={Server.MaxPlayers}).");
            }
        }

        private static void RestoreHibernate()
        {
            Server.ExecuteCommand("sv_hibernate_when_empty 1");
        }

        private static int CountSpawns(string designerName)
        {
            int count = 0;
            foreach (SpawnPoint spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(designerName))
            {
                if (spawn.IsValid)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Add only the missing amount of spawn entities, distributed evenly across unique map spawns.
        /// </summary>
        private static int AddMissingSpawns(
            List<SpawnTemplate> templates,
            int currentCount,
            CsTeam team,
            int targetCount)
        {
            if (templates.Count == 0)
            {
                return 0;
            }

            int missing = targetCount - currentCount;
            if (missing <= 0)
            {
                return 0;
            }

            int created = 0;
            int index = 0;
            while (created < missing)
            {
                SpawnTemplate template = templates[index % templates.Count];
                if (!CreateSpawnEntity(
                    team,
                    template.X,
                    template.Y,
                    template.Z,
                    template.Pitch,
                    template.Yaw,
                    template.Roll))
                {
                    break;
                }

                created++;
                index++;
            }

            return created;
        }

        private static List<SpawnTemplate> CollectUniqueTemplates(string designerName)
        {
            var groups = new Dictionary<string, SpawnTemplate>();

            foreach (SpawnPoint spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(designerName))
            {
                if (!spawn.IsValid)
                {
                    continue;
                }

                Vector? origin = spawn.AbsOrigin;
                QAngle? angle = spawn.AbsRotation;
                if (origin == null || angle == null)
                {
                    continue;
                }

                string key = $"{origin.X:F2}|{origin.Y:F2}|{origin.Z:F2}|{angle.X:F2}|{angle.Y:F2}|{angle.Z:F2}";
                if (groups.ContainsKey(key))
                {
                    continue;
                }

                groups[key] = new SpawnTemplate
                {
                    X = origin.X,
                    Y = origin.Y,
                    Z = origin.Z,
                    Pitch = angle.X,
                    Yaw = angle.Y,
                    Roll = angle.Z,
                };
            }

            return groups.Values.ToList();
        }

        private static bool CreateSpawnEntity(
            CsTeam team,
            float x,
            float y,
            float z,
            float pitch,
            float yaw,
            float roll)
        {
            SpawnPoint? entity = team == CsTeam.Terrorist
                ? Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_terrorist")
                : Utilities.CreateEntityByName<CInfoPlayerCounterterrorist>("info_player_counterterrorist");

            if (entity == null)
            {
                return false;
            }

            entity.Teleport(
                new Vector(x, y, z),
                new QAngle(pitch, yaw, roll),
                new Vector(0f, 0f, 0f));
            entity.DispatchSpawn();
            return true;
        }

        private sealed class SpawnTemplate
        {
            public float X;
            public float Y;
            public float Z;
            public float Pitch;
            public float Yaw;
            public float Roll;
        }
    }
}
