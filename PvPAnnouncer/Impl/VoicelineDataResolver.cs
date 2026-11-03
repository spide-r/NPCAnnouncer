using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using PvPAnnouncer.Data;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public partial class VoicelineDataResolver : IVoicelineDataResolver
{
    private List<string> _orphanedVoLines = [];
    private List<string> _sortedCharacterNames = [];
    private Dictionary<string, List<string>> _cutsceneLines = new();
    private List<ContentDirectorBattleTalk> _ctrList = [];
    private readonly Dictionary<Language, List<NpcYell>> _npcYellMemo = [];
    private readonly Dictionary<string, string> _cSLineTagDict = []; // me when memoization
    private readonly Dictionary<string, List<string>> _csLineSorted = []; //me when more memoization
    private readonly Dictionary<Language, ExcelSheet<InstanceContentTextData>> _instanceContentTextData = new();
    private bool _cslInit;

    public VoicelineDataResolver(IDataManager dataManager)
    {
        InitOrphanedLines(dataManager);
    }

    public void InitOrphanedLines(IDataManager dataManager)
    {
        var sheet = dataManager.GetSubrowExcelSheet<ContentDirectorBattleTalk>();
        var allUints = sheet.Flatten().Select(k => k.VoLine).ToHashSet();
        PluginServices.PluginLog.Verbose("Starting to load all orphaned lines!");
        Task.Run(() =>
        {
            var results = new List<string>();
            for (uint j = 0; j < 9999999; j++)
            {
                var voLine = $"sound/voice/vo_line/{j}_en.scd";
                if (!dataManager.FileExists(voLine)) continue;

                if (!allUints.Contains(j))
                    // if the VO Line does not exist in sheet, its orphaned so we need to add data ourselves
                    results.Add(j.ToString());
            }

            PluginServices.Framework.RunOnFrameworkThread(() =>
            {
                _orphanedVoLines = results;
                PluginServices.PluginLog.Verbose("Finished loading all orphaned lines!");
            });
        });
    }

    private void InitCutsceneLines()
    {
        if (_cslInit) //sloppy but eh it works
            return;

        _cslInit = true;
        var dataManager = PluginServices.DataManager;
        var lang = PluginServices.Config.Language;
        PluginServices.PluginLog.Verbose("Starting to load all cutscene lines!");
        Task.Run(() =>
        {
            var cutsceneLines = new Dictionary<string, List<string>>();
            var sheets = dataManager.Excel.SheetNames.Where(n =>
                n.StartsWith("cut_scene/") &&
                n.Contains("VoiceMan_")); //basically all cutscenes
            foreach (var sheetName in sheets)
            foreach (var row in dataManager.Excel.GetSheet<CutsceneText>(name: sheetName))
            {
                var tag = row.MessageTag.ExtractText();
                var audio = GetCutsceneSoundPath(tag);
                if (!dataManager.FileExists($"{audio}_{lang}.scd")) continue;

                var split = tag.Split("_");
                var speaker = split[split.Length - 1];
                if (cutsceneLines.TryGetValue(speaker, out var tags))
                    tags.Add(tag);
                else
                    cutsceneLines[speaker] = [tag];
            }

            PluginServices.Framework.RunOnFrameworkThread(() =>
            {
                _cutsceneLines = cutsceneLines;
                PluginServices.PluginLog.Verbose("Finished loading all cutscene lines!");
            });
        });
    }

    private static string GetCutsceneSoundPath(string tag)
    {
        var split = tag.Split("_");
        var number = split[2];
        var secondNumber = split[3];
        var prefix = int.Parse(number[..2]);
        var ex = prefix - 2;
        var exStr = ex == 0 ? "ffxiv" : $"ex{ex}";
        return $"cut/{exStr}/sound/voicem/voiceman_{number}/vo_voiceman_{number}_{secondNumber}_m";
    }

    public List<string> GetOrphanedLines()
    {
        return _orphanedVoLines;
    }

    public Dictionary<string, List<string>> GetCutsceneLineTags()
    {
        InitCutsceneLines();
        return _cutsceneLines;
    }

    public List<string> GetSortedCharacterNames()
    {
        InitCutsceneLines();
        if (_sortedCharacterNames.Count > 0) return _sortedCharacterNames;

        var k = new List<string>(_cutsceneLines.Keys);
        k.Sort();
        _sortedCharacterNames = k;
        return k;
    }

    public string ResolveCutsceneLineWithTag(string tag)
    {
        var tagKey = PluginServices.Config.Language + "_" + tag;
        if (_cSLineTagDict.TryGetValue(tagKey, out var value)) return value;

        var splitLine = tag.Split("_");
        var number = splitLine[2];
        var trimmedNumber = number.Substring(0, 3);
        var csvName = $"cut_scene/{trimmedNumber}/VoiceMan_{number}";
        var lang = LanguageUtil.LanguageMap.First(p => p.Value.Equals(PluginServices.Config.Language)).Key;
        var cutscene = PluginServices.DataManager.Excel.GetSheet<CutsceneText>(lang, csvName);
        var row = cutscene.FirstOrNull(r => r.MessageTag.ToString().Equals(tag));
        var dialogue = InternalConstants.ErrorContactDev;
        if (row != null) dialogue = row.Value.Dialogue.ToMacroString();

        dialogue = CutsceneNameRemovalRegex()
            .Replace(dialogue,
                ""); // any dialogue with (- text_here -) at the start will override the name shown in battletalk
        _cSLineTagDict[tagKey] = dialogue;
        return dialogue;
    }


    public string ResolveTextWithNpcYell(uint npcYell)
    {
        var lang = LanguageUtil.LanguageMap.First(p => p.Value.Equals(PluginServices.Config.Language)).Key;
        var y = PluginServices.DataManager.Excel.GetSheet<NpcYell>(lang);
        return y.TryGetRow(npcYell, out var row) ? row.Text.ToString() : InternalConstants.ErrorContactDev;
    }

    public List<NpcYell> GetNpcYellList(Language lang)
    {
        if (_npcYellMemo.TryGetValue(lang, out var cached)) return cached;

        _npcYellMemo[lang] = PluginServices.DataManager.Excel.GetSheet<NpcYell>(lang).ToList();
        return _npcYellMemo[lang];
    }

    public string? ResolveTextWithIctdRow(uint row)
    {
        var lang = LanguageUtil.LanguageMap.First(p => p.Value.Equals(PluginServices.Config.Language)).Key;
        if (GetInstanceContentTextData(lang).TryGetRow(row, out var ctrRow)) return ctrRow.Text.ToString();

        return InternalConstants.ErrorContactDev;
    }

    public ExcelSheet<InstanceContentTextData> GetInstanceContentTextData(Language lang)
    {
        if (_instanceContentTextData.TryGetValue(lang, out var data)) return data;

        var instanceContent = PluginServices.DataManager.Excel.GetSheet<InstanceContentTextData>(lang);
        _instanceContentTextData[lang] = instanceContent;
        return instanceContent;
    }

    public List<ContentDirectorBattleTalk> GetCdbtList()
    {
        if (_ctrList.Count > 0) return _ctrList;

        _ctrList = PluginServices.DataManager.Excel.GetSubrowSheet<ContentDirectorBattleTalk>().Flatten().ToList();
        return _ctrList;
    }

    public List<string> GetSortedCutsceneLines(string speaker)
    {
        if (_csLineSorted.TryGetValue(speaker, out var cached)) return cached;

        _csLineSorted[speaker] = GetCutsceneLineTags()[speaker]
            .OrderBy(t => ResolveCutsceneLineWithTag(t).Length).ToList();
        return _csLineSorted[speaker];
    }


    [GeneratedRegex(@"^\(-.*-\)")]
    private static partial Regex CutsceneNameRemovalRegex();
}