using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Interfaces.PvPEvents;

namespace PvPAnnouncer.Impl.WorldEvents;

public class FishSlipEvent : PvPEvent
{
    public FishSlipEvent()
    {
        Name = "Fish Slip";
        Id = "FishSlipEvent";
    }

    public override bool InvokeRule(IMessage message)
    {
        if (message is FishMessage f) return f.State == 0;

        return false;
    }
}