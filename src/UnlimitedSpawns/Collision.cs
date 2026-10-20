using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace UnlimitedSpawns
{
    public partial class UnlimitedSpawns
    {
        private readonly List<GhostEntry> _ghosts = new();
        private int _ghostCursor;
        private bool _roundFrozen;
        private float _separationDistanceSquared = 100f * 100f;

        private struct GhostEntry
        {
            public int Slot;
            public float NextCheckTime;
        }

        private void TrackGhost(int slot)
        {
            for (int i = 0; i < _ghosts.Count; i++)
            {
                if (_ghosts[i].Slot == slot)
                {
                    GhostEntry existing = _ghosts[i];
                    existing.NextCheckTime = 0f;
                    _ghosts[i] = existing;
                    return;
                }
            }

            _ghosts.Add(new GhostEntry
            {
                Slot = slot,
                NextCheckTime = 0f,
            });
        }

        private void ClearGhosts()
        {
            _ghosts.Clear();
            _ghostCursor = 0;
        }

        private void RemoveGhostAt(int index)
        {
            int last = _ghosts.Count - 1;
            if (index < last)
            {
                _ghosts[index] = _ghosts[last];
            }

            _ghosts.RemoveAt(last);

            if (_ghostCursor > index)
            {
                _ghostCursor--;
            }
            else if (_ghostCursor >= _ghosts.Count)
            {
                _ghostCursor = 0;
            }
        }

        private void OnTick()
        {
            int ghostCount = _ghosts.Count;
            if (ghostCount == 0 || _roundFrozen)
            {
                return;
            }

            float now = Server.CurrentTime;
            float interval = Config.MinCheckInterval;
            float separationSquared = _separationDistanceSquared;

            for (int attempts = 0; attempts < ghostCount; attempts++)
            {
                if (_ghostCursor >= _ghosts.Count)
                {
                    _ghostCursor = 0;
                }

                if (_ghosts.Count == 0)
                {
                    return;
                }

                int index = _ghostCursor;
                GhostEntry entry = _ghosts[index];
                _ghostCursor++;

                if (entry.NextCheckTime > now)
                {
                    continue;
                }

                if (!TryGetAlivePawn(entry.Slot, out CCSPlayerPawn? subject) || subject == null)
                {
                    RemoveGhostAt(index);
                    return;
                }

                Vector? origin = subject.AbsOrigin;
                if (origin == null)
                {
                    entry.NextCheckTime = now + interval;
                    _ghosts[index] = entry;
                    return;
                }

                float sx = origin.X;
                float sy = origin.Y;

                if (IsOverlappingAnyLivingPawn(subject, sx, sy, separationSquared))
                {
                    entry.NextCheckTime = now + interval;
                    _ghosts[index] = entry;
                    return;
                }

                SetCollisionGroup(subject, CollisionGroup.COLLISION_GROUP_PLAYER);
                RemoveGhostAt(index);
                return;
            }
        }

        private static bool IsOverlappingAnyLivingPawn(
            CCSPlayerPawn subject,
            float sx,
            float sy,
            float separationSquared)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!player.IsValid || !player.PawnIsAlive)
                {
                    continue;
                }

                CCSPlayerPawn? other = player.PlayerPawn.Value;
                if (other == null || !other.IsValid || other.Handle == subject.Handle)
                {
                    continue;
                }

                if (other.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    continue;
                }

                Vector? otherOrigin = other.AbsOrigin;
                if (otherOrigin == null)
                {
                    continue;
                }

                float dx = sx - otherOrigin.X;
                float dy = sy - otherOrigin.Y;
                if ((dx * dx) + (dy * dy) < separationSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetAlivePawn(int slot, out CCSPlayerPawn? pawn)
        {
            pawn = null;
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || !player.IsValid || !player.PawnIsAlive)
            {
                return false;
            }

            CCSPlayerPawn? value = player.PlayerPawn.Value;
            if (value == null || !value.IsValid || value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                return false;
            }

            if (value.Collision == null)
            {
                return false;
            }

            pawn = value;
            return true;
        }

        private static void SetCollisionGroup(CCSPlayerPawn pawn, CollisionGroup group)
        {
            if (!pawn.IsValid || pawn.Collision == null)
            {
                return;
            }

            byte groupByte = (byte)group;
            pawn.Collision.CollisionGroup = groupByte;
            pawn.Collision.CollisionAttribute.CollisionGroup = groupByte;
            new VirtualFunctionVoid<nint>(pawn.Handle, GameData.GetOffset("CBaseEntity_CollisionRulesChanged"))
                .Invoke(pawn.Handle);
        }
    }
}
