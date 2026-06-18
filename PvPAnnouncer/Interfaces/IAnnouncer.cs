using System.Collections.Generic;
using PvPAnnouncer.Data;
using PvPAnnouncer.Interfaces.PvPEvents;

namespace PvPAnnouncer.Interfaces;

public interface IAnnouncer
{
    void ReceiveEvent(PvPEvent pvpEvent);
    void ReceiveEvent(bool bypass, PvPEvent pvpEvent);
    void PlaySound(string sound);
    void SendBattleTalk(Shoutcast shoutcast);
    void ClearQueue();
    void PlayAndSendBattleTalkForTesting(Shoutcast shoutcast);
    List<string> GetLastTriggers();
    void Dispose();
}