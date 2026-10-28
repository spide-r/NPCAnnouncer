using System.Collections.Generic;
using System.Linq;
using PvPAnnouncer.Data;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public class RestrictionRepository : IRestrictionRepository
{
    private readonly Dictionary<string, ShoutcasterRestriction> _restrictions = new();

    public HashSet<uint> GetRestrictedDuties(string shoutcaster)
    {
        return _restrictions.Values
            .Where(r => PluginServices.Config.Restrictions.Contains(r.id))
            .Where(r => r.shoutcaster.Equals(shoutcaster) || r.alternateNames.Contains(shoutcaster))
            .SelectMany(r => r.duties)
            .ToHashSet();
    }

    public List<ShoutcasterRestriction> GetShoutcasterRestrictions()
    {
        return _restrictions.Values.ToList();
    }

    public void SetShoutcasterRestriction(string restrictionId, ShoutcasterRestriction restriction)
    {
        _restrictions[restrictionId] = restriction;
    }

    public void Clear()
    {
        _restrictions.Clear();
    }
}