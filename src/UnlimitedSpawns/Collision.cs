using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace UnlimitedSpawns
{
    public partial class UnlimitedSpawns
    {
        private readonly List<int> _ghosts = new();
        private readonly List<(int Slot, float X, float Y, CCSPlayerPawn Pawn)> _livingSnapshot = new();
        private bool _ghostChecksEnabled;
        private float _separationDistanceSquared = 150f * 150f;
        private static int _collisionRulesChangedOffset = -1;

        private void TrackGhost(int slot)
        {
            if (_ghosts.Contains(slot))
            {
                return;
            }

            _ghosts.Add(slot);
        }

        private void ClearGhosts() => _ghosts.Clear();

        private void OnTick()
        {
            if (!_ghostChecksEnabled || _ghosts.Count == 0)
            {
                return;
            }

            ProcessGhosts();
        }

        /// <summary>
        /// Snapshot living pawn origins once, then restore collision for every ghost
        /// that is clear of every other living pawn.
        /// </summary>
        private void ProcessGhosts()
        {
            int ghostCount = _ghosts.Count;
            if (ghostCount == 0)
            {
                return;
            }

            FillLivingSnapshot();
            int livingCount = _livingSnapshot.Count;
            if (livingCount == 0)
            {
                _ghosts.Clear();
                return;
            }

            float separationSquared = _separationDistanceSquared;

            for (int g = ghostCount - 1; g >= 0; g--)
            {
                int slot = _ghosts[g];
                int subject = IndexOfSlot(slot);
                if (subject < 0)
                {
                    _ghosts.RemoveAt(g);
                    continue;
                }

                // Alone on the server: nothing to overlap with.
                if (livingCount > 1 && IsOverlapping(subject, separationSquared))
                {
                    continue;
                }

                SetCollisionGroup(_livingSnapshot[subject].Pawn, CollisionGroup.COLLISION_GROUP_PLAYER);
                _ghosts.RemoveAt(g);
            }
        }

        private void FillLivingSnapshot()
        {
            _livingSnapshot.Clear();

            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!player.IsValid || !player.PawnIsAlive)
                {
                    continue;
                }

                CCSPlayerPawn? pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.Collision == null)
                {
                    continue;
                }

                Vector? origin = pawn.AbsOrigin;
                if (origin == null)
                {
                    continue;
                }

                _livingSnapshot.Add((player.Slot, origin.X, origin.Y, pawn));
            }
        }

        private int IndexOfSlot(int slot)
        {
            for (int i = 0; i < _livingSnapshot.Count; i++)
            {
                if (_livingSnapshot[i].Slot == slot)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsOverlapping(int subject, float separationSquared)
        {
            float sx = _livingSnapshot[subject].X;
            float sy = _livingSnapshot[subject].Y;

            for (int i = 0; i < _livingSnapshot.Count; i++)
            {
                if (i == subject)
                {
                    continue;
                }

                float dx = sx - _livingSnapshot[i].X;
                float dy = sy - _livingSnapshot[i].Y;
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
            if (value == null || !value.IsValid || value.LifeState != (byte)LifeState_t.LIFE_ALIVE || value.Collision == null)
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

            if (_collisionRulesChangedOffset < 0)
            {
                _collisionRulesChangedOffset = GameData.GetOffset("CBaseEntity_CollisionRulesChanged");
            }

            byte groupByte = (byte)group;
            pawn.Collision.CollisionGroup = groupByte;
            pawn.Collision.CollisionAttribute.CollisionGroup = groupByte;
            new VirtualFunctionVoid<nint>(pawn.Handle, _collisionRulesChangedOffset).Invoke(pawn.Handle);
        }
    }
}
