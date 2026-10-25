using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace UnlimitedSpawns
{
    public partial class UnlimitedSpawns
    {
        private const string CustomSpawnNamePrefix = "unlimitedspawns_";
        private const string TerroristSpawnDesigner = "info_player_terrorist";
        private const string CtSpawnDesigner = "info_player_counterterrorist";

        private bool _spawnsFilled;
        private int _spawnFillAttempts;
        private readonly List<CustomSpawnEntry> _customSpawns = new();

        private sealed class CustomSpawnEntry
        {
            public required SpawnPoint Entity;
            public required SpawnPoint Parent;
            public CsTeam Team;
            public float ParentX;
            public float ParentY;
            public float ParentZ;
            public float ParentPitch;
            public float ParentYaw;
            public float ParentRoll;
        }

        private void OnMapStart(string mapName)
        {
            _ghostChecksEnabled = false;
            _customSpawns.Clear();
            // Map entities need time to exist; hibernation must be off or the timer never fires.
            EnsureSpawns(delaySeconds: 3.0f);
        }

        private void ClearCustomSpawns() => _customSpawns.Clear();

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
                return;
            }

            Server.NextFrame(TryFillSpawns);
        }

        private void TryFillSpawns()
        {
            if (_spawnsFilled)
            {
                RestoreHibernate();
                return;
            }

            int mapT = CountMapSpawns(TerroristSpawnDesigner);
            int mapCt = CountMapSpawns(CtSpawnDesigner);

            // Map spawn entities are not always present on the first frame after map change.
            if (mapT == 0 && mapCt == 0)
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

            // Hot-reload may leave prior custom entities; remove them and rebuild from map parents.
            RemoveExistingCustomSpawnEntities();
            _customSpawns.Clear();

            int targetPerTeam = Server.MaxPlayers / 2;
            int createdT = 0;
            int createdCt = 0;

            if (targetPerTeam > 0)
            {
                createdT = AddMissingSpawns(CollectMapTemplates(TerroristSpawnDesigner), mapT, CsTeam.Terrorist, targetPerTeam);
                createdCt = AddMissingSpawns(CollectMapTemplates(CtSpawnDesigner), mapCt, CsTeam.CounterTerrorist, targetPerTeam);
            }

            _spawnsFilled = true;
            UpdateSpawnQuota();
            RestoreHibernate();

            if (createdT == 0 && createdCt == 0)
            {
                Console.WriteLine(
                    $"[UnlimitedSpawns] Spawn check OK — map T={mapT} CT={mapCt} " +
                    $"(target {targetPerTeam} per team, MaxPlayers={Server.MaxPlayers}).");
                return;
            }

            Console.WriteLine(
                $"[UnlimitedSpawns] Added missing spawns — map T={mapT}+{createdT} CT={mapCt}+{createdCt} " +
                $"(target {targetPerTeam} per team, MaxPlayers={Server.MaxPlayers}).");
        }

        private static void RemoveExistingCustomSpawnEntities()
        {
            RemoveCustomSpawnsByDesigner(TerroristSpawnDesigner);
            RemoveCustomSpawnsByDesigner(CtSpawnDesigner);
        }

        private static void RemoveCustomSpawnsByDesigner(string designerName)
        {
            // Materialize first: removing while FindAll iterates can skip entities.
            List<SpawnPoint>? toRemove = null;
            foreach (SpawnPoint spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(designerName))
            {
                if (!spawn.IsValid || !IsCustomSpawnName(spawn))
                {
                    continue;
                }

                toRemove ??= new List<SpawnPoint>();
                toRemove.Add(spawn);
            }

            if (toRemove == null)
            {
                return;
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                toRemove[i].Remove();
            }
        }

        private static void RestoreHibernate() => Server.ExecuteCommand("sv_hibernate_when_empty 1");

        private void PruneInvalidCustomSpawns()
        {
            for (int i = _customSpawns.Count - 1; i >= 0; i--)
            {
                CustomSpawnEntry entry = _customSpawns[i];
                if (!entry.Entity.IsValid || !entry.Parent.IsValid)
                {
                    _customSpawns.RemoveAt(i);
                }
            }
        }

        private static int CountMapSpawns(string designerName)
        {
            int count = 0;
            foreach (SpawnPoint spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(designerName))
            {
                if (spawn.IsValid && !IsCustomSpawnName(spawn))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnabledMapSpawns(string designerName)
        {
            int count = 0;
            foreach (SpawnPoint spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(designerName))
            {
                if (spawn.IsValid && !IsCustomSpawnName(spawn) && spawn.Enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsCustomSpawnName(SpawnPoint spawn)
        {
            string? name = spawn.Entity?.Name;
            return !string.IsNullOrEmpty(name) && name.StartsWith(CustomSpawnNamePrefix, StringComparison.Ordinal);
        }

        private static string DesignerNameFor(CsTeam team) =>
            team == CsTeam.Terrorist ? TerroristSpawnDesigner : CtSpawnDesigner;

        /// <summary>
        /// Add only the missing amount of spawn entities, distributed evenly across unique map spawns.
        /// </summary>
        private int AddMissingSpawns(List<SpawnTemplate> templates, int currentCount, CsTeam team, int targetCount)
        {
            int missing = targetCount - currentCount;
            if (missing <= 0 || templates.Count == 0)
            {
                return 0;
            }

            int created = 0;
            int index = 0;
            while (created < missing)
            {
                if (!CreateCustomSpawn(templates[index % templates.Count], team, created))
                {
                    break;
                }

                created++;
                index++;
            }

            return created;
        }

        private static List<SpawnTemplate> CollectMapTemplates(string designerName)
        {
            var groups = new Dictionary<string, SpawnTemplate>();

            foreach (SpawnPoint spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(designerName))
            {
                if (!spawn.IsValid || IsCustomSpawnName(spawn))
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
                if (!groups.TryAdd(key, new SpawnTemplate
                {
                    Parent = spawn,
                    SourceName = spawn.Entity?.Name ?? string.Empty,
                    SourceIndex = (int)spawn.Index,
                }))
                {
                    continue;
                }
            }

            return groups.Values.ToList();
        }

        private bool CreateCustomSpawn(SpawnTemplate template, CsTeam team, int createdIndex)
        {
            SpawnPoint parent = template.Parent;
            if (!parent.IsValid)
            {
                return false;
            }

            Vector? origin = parent.AbsOrigin;
            QAngle? angle = parent.AbsRotation;
            if (origin == null || angle == null)
            {
                return false;
            }

            SpawnPoint? entity = team == CsTeam.Terrorist
                ? Utilities.CreateEntityByName<CInfoPlayerTerrorist>(TerroristSpawnDesigner)
                : Utilities.CreateEntityByName<CInfoPlayerCounterterrorist>(CtSpawnDesigner);

            if (entity == null)
            {
                return false;
            }

            string sourceLabel = string.IsNullOrEmpty(template.SourceName)
                ? $"{(team == CsTeam.Terrorist ? "t" : "ct")}_{template.SourceIndex}"
                : template.SourceName;

            if (entity.Entity != null)
            {
                entity.Entity.Name = $"{CustomSpawnNamePrefix}{sourceLabel}_{createdIndex}";
            }

            entity.Teleport(origin, angle, new Vector(0f, 0f, 0f));
            entity.DispatchSpawn();
            entity.Priority = parent.Priority;
            entity.Type = parent.Type;
            entity.AcceptInput("SetDisabled");

            _customSpawns.Add(new CustomSpawnEntry
            {
                Entity = entity,
                Parent = parent,
                Team = team,
                ParentX = origin.X,
                ParentY = origin.Y,
                ParentZ = origin.Z,
                ParentPitch = angle.X,
                ParentYaw = angle.Y,
                ParentRoll = angle.Z,
            });

            return true;
        }

        /// <summary>
        /// Keep map spawns as-is. Enable customs until teamPlayers+1 spawn slots exist per team.
        /// </summary>
        private void UpdateSpawnQuota()
        {
            if (_customSpawns.Count == 0)
            {
                return;
            }

            PruneInvalidCustomSpawns();
            if (_customSpawns.Count == 0)
            {
                return;
            }

            int tPlayers = 0;
            int ctPlayers = 0;
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!player.IsValid)
                {
                    continue;
                }

                if (player.Team == CsTeam.Terrorist)
                {
                    tPlayers++;
                }
                else if (player.Team == CsTeam.CounterTerrorist)
                {
                    ctPlayers++;
                }
            }

            ApplyTeamSpawnQuota(CsTeam.Terrorist, tPlayers + 1);
            ApplyTeamSpawnQuota(CsTeam.CounterTerrorist, ctPlayers + 1);
        }

        private void ApplyTeamSpawnQuota(CsTeam team, int needed)
        {
            int customToEnable = Math.Max(0, needed - CountEnabledMapSpawns(DesignerNameFor(team)));
            int enabledSoFar = 0;

            for (int i = 0; i < _customSpawns.Count; i++)
            {
                CustomSpawnEntry entry = _customSpawns[i];
                if (entry.Team != team || !entry.Entity.IsValid)
                {
                    continue;
                }

                bool eligible = entry.Parent.IsValid && entry.Parent.Enabled;
                bool shouldEnable = eligible && enabledSoFar < customToEnable;
                if (shouldEnable)
                {
                    enabledSoFar++;
                }

                SetSpawnEnabled(entry.Entity, shouldEnable);
            }
        }

        private static void SetSpawnEnabled(SpawnPoint spawn, bool enabled)
        {
            if (spawn.Enabled == enabled)
            {
                return;
            }

            spawn.AcceptInput(enabled ? "SetEnabled" : "SetDisabled");
        }

        /// <summary>
        /// After round end, map scripts may have moved parent spawns. Mirror that onto customs.
        /// </summary>
        private void FollowMovedParentSpawns()
        {
            if (_customSpawns.Count == 0)
            {
                return;
            }

            for (int i = _customSpawns.Count - 1; i >= 0; i--)
            {
                CustomSpawnEntry entry = _customSpawns[i];
                if (!entry.Entity.IsValid || !entry.Parent.IsValid)
                {
                    _customSpawns.RemoveAt(i);
                    continue;
                }

                Vector? parentOrigin = entry.Parent.AbsOrigin;
                QAngle? parentAngle = entry.Parent.AbsRotation;
                if (parentOrigin == null || parentAngle == null)
                {
                    continue;
                }

                if (parentOrigin.X == entry.ParentX &&
                    parentOrigin.Y == entry.ParentY &&
                    parentOrigin.Z == entry.ParentZ &&
                    parentAngle.X == entry.ParentPitch &&
                    parentAngle.Y == entry.ParentYaw &&
                    parentAngle.Z == entry.ParentRoll)
                {
                    continue;
                }

                entry.Entity.Teleport(parentOrigin, parentAngle, new Vector(0f, 0f, 0f));
                entry.ParentX = parentOrigin.X;
                entry.ParentY = parentOrigin.Y;
                entry.ParentZ = parentOrigin.Z;
                entry.ParentPitch = parentAngle.X;
                entry.ParentYaw = parentAngle.Y;
                entry.ParentRoll = parentAngle.Z;
            }
        }

        private sealed class SpawnTemplate
        {
            public required SpawnPoint Parent;
            public string SourceName = string.Empty;
            public int SourceIndex;
        }
    }
}
