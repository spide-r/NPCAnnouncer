using System.Linq;
using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Interfaces.PvPEvents;

namespace PvPAnnouncer.Impl.PvPEvents;

public class AllyActionEvent : PvPActionEvent
{
    //generic event for specific action and sound pairings

    private uint[] ActionIds { get; }

    public AllyActionEvent(uint[] actionIds, string name = "Action", string id = "internal")
    {
        ActionIds = actionIds;
        Name = name;
        Id = id;
    }

    public override bool InvokeRule(IMessage p)
    {
        if (p is ActionEffectMessage)
        {
            ActionEffectMessage message = (ActionEffectMessage) p;
            if (ActionIds.Contains(message.ActionId))
            {
                return PluginServices.DutyManager.IsMonitoredUser(message.SourceId);
            }
        }

        return false;
    }
}