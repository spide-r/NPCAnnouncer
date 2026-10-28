using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Interfaces.PvPEvents;

namespace PvPAnnouncer.Impl.WorldEvents;

public class DutyPopEvent : PvPMatchEvent
{
    public DutyPopEvent()
    {
        Name = "Duty Finder Pop";
        Id = "DutyPopEvent";
    }

    public override bool InvokeRule(IMessage message)
    {
        return message is DutyPopMessage;
    }
}