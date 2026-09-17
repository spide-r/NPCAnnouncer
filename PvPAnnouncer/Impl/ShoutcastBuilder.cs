using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using Lumina.Data;
using Lumina.Excel.Exceptions;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using PvPAnnouncer.Data;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public partial class ShoutcastBuilder(IDataManager dataManager) : IShoutcastBuilder
{
    private Shoutcast _instance = NewShoutcast();

    public ShoutcastBuilder FromShoutcast(Shoutcast shoutcast, IDataManager dm)
    {
        var b = new ShoutcastBuilder(dm)
        {
            _instance = shoutcast.CopyShoutcast()
        };
        return b;
    }

    public Shoutcast BuildAndRefreshProperties()
    {
        var result = BuildAndPreserveProperties();
        PluginServices.PluginLog.Verbose($"Replacing {result.Id}");
        _instance = NewShoutcast();
        return result;
    }

    public void RefreshProperties()
    {
        _instance = NewShoutcast();
    }

    public Shoutcast BuildAndPreserveProperties()
    {
        var result = _instance;
        PluginServices.PluginLog.Verbose($"Building shoutcast {result.Id} and preserving properties");


        /*
         * Time to build!
         * - Check for BattleTalkVo, InstanceContentTextDataRow, NpcYell. CutsceneLine
         * - if One is found, try to pull all data we can (even if we override something)
         *
         * Check for a valid name, icon, voice, length, if its valid send it through, if not, error out
         */
        if (result.ContentDirectorBattleTalkVo != 0)
        {
            PluginServices.PluginLog.Verbose($"ContentDirectorBattleTalkVo: {result.ContentDirectorBattleTalkVo}");
            /*
                name: ContentDirectorBattleTalk
                fields:
                - name: Unknown0 -> BattleTalkIcon
                - name: Unknown1 -> Voiceover
                - name: Text
                type: link
                targets: [InstanceContentTextData]
                - name: Unknown3 -> BattleTalkDuration
                - name: Unknown4 -> BattleTalkStyle
            */
            result.SoundPath = "sound/voice/vo_line/" + result.ContentDirectorBattleTalkVo;
            PluginServices.PluginLog.Debug($"BattleTalkVO Sound path: {result.SoundPath}");
            var d = GetContentDirectorBattleTalkAllLanguages(result.ContentDirectorBattleTalkVo);

            if (d.Keys.Count > 0) // record exists in the sheets - attempt to set the other values
            {
                var sheet = dataManager.GetSubrowExcelSheet<ContentDirectorBattleTalk>();

                var t = sheet.SelectMany(row => row)
                    .FirstOrDefault(bt => bt.VoLine == result.ContentDirectorBattleTalkVo);
                if (t.Icon != 0) result.Icon = t.Icon;

                result.Style = t.Style;
                result.Duration = t.Duration;
                result.Transcription = d;
            }
        }

        if (result.InstanceContentTextDataRow != 0)
        {
            PluginServices.PluginLog.Verbose($"InstanceContentTextDataRow: {result.InstanceContentTextDataRow}");

            result.Transcription = GetInstanceContentTextDataAllLang(result.InstanceContentTextDataRow);
        }

        if (result.NpcYell != 0)
        {
            PluginServices.PluginLog.Verbose($"NpcYell: {result.NpcYell}");

            var d = GetNpcYellAllLang(result.NpcYell);
            var sheet = dataManager.GetExcelSheet<NpcYell>();
            var foundEntry = sheet.TryGetRow(result.NpcYell, out var row);
            var duration = foundEntry ? row.BalloonTime : 5;

            result.Duration = (byte) duration;
            result.Transcription = d;
        }

        if (!result.CutsceneLine.Equals("")) // cutsceneLine is not empty and we don't have a soundpath set
        {
            var splitLine = result.CutsceneLine.Split("_");
            var number = splitLine[2];
            var secondNumber = splitLine[3];
            var expac = Convert.ToInt32(number.Substring(0, 2)) - 2;
            if (expac >= 0)
            {
                var ex = expac == 0 ? "ffxiv" : $"ex{expac}";
                var audio = $"cut/{ex}/sound/voicem/voiceman_{number}/vo_voiceman_{number}_{secondNumber}_m";
                result.SoundPath = audio;
                var d = GetCutsceneLineAllLang(result.CutsceneLine);
                result.IsGendered = dataManager.FileExists(result.GetFemSoundPathJa());
                result.Transcription = d;
            }
        }

        //validation order:
        //name, text, soundpath

        if (result.Shoutcaster.Equals(""))
        {
            throw new InvalidOperationException($"No shoutcaster name exists for {result.Id}");
        }

        if (result.Transcription.Count == 0)
        {
            throw new InvalidOperationException($"No text data exists for {result.Id}");
        }

        if (result.SoundPath.Equals(""))
            PluginServices.PluginLog.Error(
                $"Empty sound path for shoutcast {result.Id}: {result.GetShoutcastSoundPathWithLang("ja")}");

        if (!dataManager.FileExists(result.GetShoutcastSoundPathWithLang("ja")))
            PluginServices.PluginLog.Error(
                $"No valid sound path found for shoutcast {result.Id}: {result.GetShoutcastSoundPathWithLang("ja")}");

        return result;
    }

    public Shoutcast BuildAndPreserveCharacter()
    {
        var sc = BuildAndRefreshProperties();
        _instance.Shoutcaster = sc.Shoutcaster;
        _instance.Icon = sc.Icon;
        _instance.Style = sc.Style;
        return sc;
    }

    private Dictionary<string, string> GetCutsceneLineAllLang(string tag)
    {
        var splitLine = tag.Split("_");
        var number = splitLine[2];
        var trimmedNumber = number.Substring(0, 3);
        var csvName = $"cut_scene/{trimmedNumber}/VoiceMan_{number}";
        Dictionary<string, string> dict = new Dictionary<string, string>();

        foreach (var (lang, langStr) in LanguageUtil.LanguageMap)
        {
            try
            {
                var cutscene = dataManager.Excel.GetSheet<CutsceneText>(lang, csvName);
                var row = cutscene.FirstOrNull(r =>
                    r.MessageTag.ExtractText().Equals(tag)); //need extractText for macro string 
                var dialogue = InternalConstants.ErrorContactDev;
                if (row != null)
                {
                    dialogue = row.Value.Dialogue.ToMacroString();
                }

                dialogue = CutsceneNameRemovalRegex()
                    .Replace(dialogue,
                        ""); // any dialogue with (- text_here -) at the start will override the name shown in battletalk
                dict.Add(langStr, dialogue);
                PluginServices.PluginLog.Verbose($"CutsceneLineLang: {langStr}, Transcription: {dialogue}");
            }
            catch (UnsupportedLanguageException)
            {
                PluginServices.PluginLog.Verbose(
                    $"Attempted to pull Cutscene Lang for {langStr} and failed due to unsupported language");
            }
        }

        return dict;
    }

    private Dictionary<string, string> GetNpcYellAllLang(uint yell)
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();
        foreach (var (lang, langStr) in LanguageUtil.LanguageMap)
        {
            try
            {
                var sheet = dataManager.Excel.GetSheet<NpcYell>(lang);
                var foundEntry = sheet.TryGetRow(yell, out var row);
                var text = foundEntry ? row.Text.ToString() : InternalConstants.ErrorContactDev;
                dict[langStr] = text;

                PluginServices.PluginLog.Verbose($"NPCYellLang: {langStr}, Transcription: {text}");
            }
            catch (UnsupportedLanguageException)
            {
                PluginServices.PluginLog.Verbose(
                    $"Attempted to pull NPC Yell for {langStr} and failed due to unsupported language");
            }
        }

        return dict;
    }

    private Dictionary<string, string> GetContentDirectorBattleTalkAllLanguages(uint voiceover)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (lang, langStr) in LanguageUtil.LanguageMap)
        {
            try
            {
                var sheet = dataManager.Excel
                    .GetSubrowSheet<
                        ContentDirectorBattleTalk>(); //ContentDirectorBattleTalk does NOT have a language assigned 
                foreach (var row in sheet)
                {
                    foreach (var talk in row.Where(talk => talk.VoLine == voiceover))
                    {
                        // see https://github.com/NotAdam/Lumina/issues/65 - gotta not use the RowRef and instead pull it manually ourselves
                        var textDataRow = talk.Text.RowId;
                        var instanceContent = dataManager.Excel.GetSheet<InstanceContentTextData>(lang);
                        var foundEntry = instanceContent.TryGetRow(textDataRow, out var ctrRow);
                        var text = foundEntry ? ctrRow.Text.ToString() : InternalConstants.ErrorContactDev;
                        dict[langStr] = text;
                        PluginServices.PluginLog.Verbose(
                            $"CtrDirectorBattleTalkLang: {langStr}, Transcription: {text}, Lang: {talk.ExcelPage.Language}");
                    }
                }
            }
            catch (UnsupportedLanguageException)
            {
                PluginServices.PluginLog.Verbose(
                    $"Attempted to pull CtrDirectorBattleTalk for {langStr} and failed due to unsupported language");
            }
        }

        return dict;
    }


    private Dictionary<string, string> GetInstanceContentTextDataAllLang(uint textDataRow)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (lang, langStr) in LanguageUtil.LanguageMap)
        {
            try
            {
                var sheet = dataManager.Excel.GetSheet<InstanceContentTextData>(lang);
                var foundEntry = sheet.TryGetRow(textDataRow, out var row);
                var text = foundEntry ? row.Text.ToString() : InternalConstants.ErrorContactDev;
                dict[langStr] = text;
                PluginServices.PluginLog.Verbose(
                    $"GetInstanceContentTextDataAllLang: {langStr}, Transcription: {text}");
            }
            catch (UnsupportedLanguageException)
            {
                PluginServices.PluginLog.Verbose(
                    $"Attempted to pull Instance Content Text Data for {langStr} and failed due to unsupported language");
            }
        }

        return dict;
    }

    public IShoutcastBuilder WithId(string id)
    {
        _instance.Id = id;
        return this;
    }

    public IShoutcastBuilder WithIcon(uint icon)
    {
        _instance.Icon = icon;
        return this;
    }

    public IShoutcastBuilder WithTranscription(Dictionary<string, string> transcription)
    {
        _instance.Transcription = transcription;
        return this;
    }

    public IShoutcastBuilder WithDuration(byte duration)
    {
        _instance.Duration = duration;
        return this;
    }

    public IShoutcastBuilder WithStyle(byte style)
    {
        _instance.Style = style;
        return this;
    }

    public IShoutcastBuilder WithShoutcaster(string name)
    {
        _instance.Shoutcaster = name;
        return this;
    }

    public IShoutcastBuilder WithAttributes(List<string> attributes)
    {
        _instance.Attributes = attributes;
        return this;
    }

    public IShoutcastBuilder AddAttribute(string attribute)
    {
        if (!_instance.Attributes.Contains(attribute)) _instance.Attributes.Add(attribute);

        return this;
    }

    public IShoutcastBuilder WithSoundPath(string path)
    {
        _instance.SoundPath = path;
        return this;
    }

    public IShoutcastBuilder WithCutsceneLine(string cutsceneLine)
    {
        _instance.CutsceneLine = cutsceneLine;
        return this;
    }

    public IShoutcastBuilder WithContentDirectorBattleTalkVo(uint contentDirectorBattleTalkVo)
    {
        _instance.ContentDirectorBattleTalkVo = contentDirectorBattleTalkVo;
        return this;
    }

    public IShoutcastBuilder WithNpcYell(uint npcYell)
    {
        _instance.NpcYell = npcYell;
        return this;
    }

    public IShoutcastBuilder WithGendered(bool gendered)
    {
        _instance.IsGendered = gendered;
        return this;
    }

    public Shoutcast GetShoutcastMidConstruction()
    {
        return _instance;
    }

    public IShoutcastBuilder WithInstanceContentTextDataRow(uint instanceContentTextDataRow)
    {
        _instance.InstanceContentTextDataRow = instanceContentTextDataRow;
        return this;
    }

    private static Shoutcast NewShoutcast()
    {
        return new Shoutcast("ShoutcastId", 0, new Dictionary<string, string>()
        {
            {"en", "Shoutcast Transcription"}
        }, 5, 6, "Shoutcaster", [], InternalConstants.DefaultSoundPath, "", 0, 0, 0, false);
    }

    [GeneratedRegex(@"^\(-.*-\)")]
    private static partial Regex CutsceneNameRemovalRegex();
}