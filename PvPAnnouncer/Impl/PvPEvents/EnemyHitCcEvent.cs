using Lumina.Excel.Sheets;
using PvPAnnouncer.Impl.Messages;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Interfaces.PvPEvents;

namespace PvPAnnouncer.Impl.PvPEvents;

public class EnemyHitCcEvent : PvPActionEvent
{
    public EnemyHitCcEvent()
    {
        Name = "Hit with a Crowd Control Effect (Stun, Sleep, etc)";
        Id = "EnemyHitCcEvent";
    }

    //no current way to check/validate detrimental effect was actually from an enemy - no big issue rn since detrimental, but something to think about
    public override bool InvokeRule(IMessage message)
    {
        if (message is AppliedStatusMessage enemyAppliedStatusEvent)
        {
            var status = PluginServices.DataManager.Excel.GetSheet<Status>()
                .GetRow((uint) enemyAppliedStatusEvent.status);
            if (status.StatusCategory == 2) // detrimental
            {
                if (status.LockMovement) // probably stun/sleep
                {
                    return true;
                }

                if (status.HitEffect.RowId == 4) // probably slow
                {
                    return true;
                }
            }
        }

        return false;
    }
}