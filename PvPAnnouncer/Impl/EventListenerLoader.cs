using PvPAnnouncer.Impl.PvEEvents;
using PvPAnnouncer.Impl.PvPEvents;
using PvPAnnouncer.Impl.WorldEvents;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Interfaces.PvPEvents;

namespace PvPAnnouncer.Impl;

public class EventListenerLoader : IEventListenerLoader
{
    private PvPEvent[] StandardPvpEvents { get; } =
    [
        new MatchVictoryEvent(),
        new MatchLossEvent(),
        new AllyBurstedEvent(),
        new AllyDeathEvent(),
        new AllyHitEnemyHardEvent(),
        new AllyHitByLimitBreakEvent(),
        new AllyHitUnderGuardEvent(),
        new AllyLimitBreakEvent(),
        new AllyMitUsedEvent(),
        new AllyPulledByDrkEvent(),
        new AllyResurrectEvent(),
        new AllyZoneOutEvent(),
        new EnemyMissedCc(),
        new MatchStartEvent(),
        new EnteredMechEvent(),
        new DutyRecommenceEvent(),
        new VulnStackDDEvent(),
        new EnemyHitCcEvent(),
        new MaxBattleFeverEvent(),
        new DutyPopEvent(),
        new LevelUpEvent()
    ];

    public void LoadEventListeners()
    {
        PluginServices.PluginLog.Verbose("Loading Standard event listeners");
        foreach (var ee in StandardPvpEvents)
        {
            PluginServices.PvPEventBroker.RegisterListener(ee);
        }
    }
}