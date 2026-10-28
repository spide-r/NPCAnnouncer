using System.Collections.Generic;
using PvPAnnouncer.Data;

namespace PvPAnnouncer.Interfaces;

public interface IRestrictionRepository
{
    public HashSet<uint> GetRestrictedDuties(string shoutcaster);
    public List<ShoutcasterRestriction> GetShoutcasterRestrictions();
    public void SetShoutcasterRestriction(string restrictionId, ShoutcasterRestriction restriction);
    public void Clear();
}