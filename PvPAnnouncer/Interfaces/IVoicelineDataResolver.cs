using System.Collections.Generic;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace PvPAnnouncer.Interfaces;

public interface IVoicelineDataResolver
{
    List<string> GetOrphanedLines();
    Dictionary<string, List<string>> GetCutsceneLineTags();
    List<string> GetSortedCutsceneLines(string character);
    List<string> GetSortedCharacterNames();
    string ResolveCutsceneLineWithTag(string tag);
    string ResolveTextWithNpcYell(uint npcYell);
    string? ResolveTextWithIctdRow(uint row);
    List<ContentDirectorBattleTalk> GetCdbtList();
    List<NpcYell> GetNpcYellList(Language lang);
    ExcelSheet<InstanceContentTextData> GetInstanceContentTextData(Language lang);
}