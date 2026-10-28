using System;
using Dalamud.Game.DutyState;

namespace PvPAnnouncer.Interfaces;

public interface IDutyManager : IDisposable
{
    void DutyStarted(IDutyStateEventArgs args);
    void DutyCompleted(IDutyStateEventArgs args);
    bool IsMonitoredUser(int userId);
    bool IsActiveTrustNPC(string npc);
    bool InTrust();
    bool IsMonitoredUser(uint entityId);
}