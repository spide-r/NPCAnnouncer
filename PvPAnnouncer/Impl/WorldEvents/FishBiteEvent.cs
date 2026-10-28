using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Interfaces.PvPEvents;

namespace PvPAnnouncer.Impl.WorldEvents;

public class FishBiteEvent : PvPEvent
{
    public FishBiteEvent()
    {
        Name = "Fish Biting";
        Id = "FishBiteEvent";
    }

    public override bool InvokeRule(IMessage message)
    {
        if (message is FishMessage f) return f.State == 1;

        return false;
    }
}