namespace PvPAnnouncer.Interfaces;

public interface IPlayerStateTracker : IEventPublisher
{
    void CheckSoundState();
    bool CheckCNClient();
    bool CheckKRClient();
}