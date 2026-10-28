using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Interfaces.PvPEvents;

namespace PvPAnnouncer.Impl.WorldEvents;

public class LevelUpEvent : PvPEvent
{
    public LevelUpEvent()
    {
        Name = "Leveling Up";
        Id = "LevelUpEvent";
    }


    public override bool InvokeRule(IMessage message)
    {
        return message is LevelUpMessage;
    }
}