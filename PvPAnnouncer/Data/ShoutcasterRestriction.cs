using System.Collections.Generic;

namespace PvPAnnouncer.Data;

public class ShoutcasterRestriction
{
    public string id { get; }
    public string shoutcaster { get; }
    public string restrictionName { get; }
    public List<uint> tt { get; }
    public List<string> alternateNames { get; }

    public ShoutcasterRestriction(string id, string shoutcaster, List<string> alternateNames, string restrictionName,
        List<uint> tt)
    {
        this.id = id;
        this.shoutcaster = shoutcaster;
        this.alternateNames = alternateNames;
        this.restrictionName = restrictionName;
        this.tt = tt;
    }
}