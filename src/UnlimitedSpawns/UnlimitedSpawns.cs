using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace UnlimitedSpawns
{
    public partial class UnlimitedSpawns : BasePlugin
    {
        public override string ModuleName => "UnlimitedSpawns";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";
        public override string ModuleDescription => "Disable player collision after spawn until players are clear of each other";

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnTick>(OnTick);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnTick>(OnTick);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            ClearGhosts();
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _roundFrozen = true;
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            _roundFrozen = false;
            return HookResult.Continue;
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

        private void OnMapEnd()
        {
            _roundFrozen = false;
            ClearGhosts();
        }
    }
}
