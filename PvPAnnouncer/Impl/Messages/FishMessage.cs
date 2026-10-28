using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl.Messages;

/*
 * 0 - Slip
 * 1 - Hook
 * 2 - Catch
 */
public class FishMessage(uint state) : IMessage
{
    public uint State { get; } = state;
}