using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace UnlimitedSpawns
{
    public partial class UnlimitedSpawns : BasePlugin
    {
        public override string ModuleName => "UnlimitedSpawns";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";
        public override string ModuleDescription => "Fill map spawns to MaxPlayers/2 per team and disable collision until players are clear";

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnTick>(OnTick);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

            if (!hotReload)
            {
                return;
            }

            // Mid-round reload: freeze already ended (or never applies).
            _ghostChecksEnabled = true;
            EnsureSpawns();
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnTick>(OnTick);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            ClearGhosts();
            ClearCustomSpawns();
            RestoreHibernate();
        }

        [GameEventHandler]
        public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _ghostChecksEnabled = false;
            Server.NextFrame(FollowMovedParentSpawns);
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // mp_freezetime 0: round_freeze_end is unreliable — enable checks on round start.
            _ghostChecksEnabled = GetFreezeTime() <= 0;
            if (_ghostChecksEnabled)
            {
                Server.NextFrame(ProcessGhosts);
            }

            UpdateSpawnQuota();
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            _ghostChecksEnabled = true;
            Server.NextFrame(ProcessGhosts);
            return HookResult.Continue;
        }

        private static int GetFreezeTime()
        {
            ConVar? freezeTime = ConVar.Find("mp_freezetime");
            return freezeTime?.GetPrimitiveValue<int>() ?? 0;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid)
            {
                return HookResult.Continue;
            }

            int slot = player.Slot;
            Server.NextFrame(() =>
            {
                if (!TryGetAlivePawn(slot, out CCSPlayerPawn? pawn) || pawn == null)
                {
                    return;
                }

                SetCollisionGroup(pawn, CollisionGroup.COLLISION_GROUP_DISSOLVING);
                TrackGhost(slot);
            });

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            Server.NextFrame(UpdateSpawnQuota);
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            Server.NextFrame(UpdateSpawnQuota);
            return HookResult.Continue;
        }

        private void OnMapEnd()
        {
            _ghostChecksEnabled = false;
            _spawnsFilled = false;
            _spawnFillAttempts = 0;
            ClearGhosts();
            ClearCustomSpawns();
            RestoreHibernate();
        }
    }
}
