using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using PvPAnnouncer.Data;
using PvPAnnouncer.Impl;
using PvPAnnouncer.Impl.Rake;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Windows;

public class VoicelineCreationWindow : Window, IDisposable
{
    private readonly IVoicelineCreationController _controller;
    private readonly IVoicelineCreationViewer _viewer;
    private readonly Rake _englishRake;
    private readonly Rake _germanRake;
    private readonly Rake _frenchRake;

    public VoicelineCreationWindow() : base(
        "NPCAnnouncer Voiceline Creation & Editing Window")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 225),
        };
        _controller = new VoicelineCreationController(PluginServices.DataManager);
        _viewer = new VoicelineCreationViewer(PluginServices.VoicelineDataResolver);
        _englishRake = new Rake("en");
        _germanRake = new Rake("de");
        _frenchRake = new Rake("fr");
    }

    //
    // sources of voicelines: 
    // ContentDirectorBattleTalkVO, CutsceneLine

    // sources of text:
    // npcyell, cutsceneLine, instancecontenttextdatarow
    private bool _useIcon;
    private bool _useBackup;
    private bool _editing;


    public void Edit(Shoutcast shoutcast)
    {
        if (!PluginServices.Config.CustomShoutcasts.ContainsKey(shoutcast.Id))
        {
            PluginServices.PluginLog.Warning("Attempted to edit a shoutcast that isn't custom!");
            return;
        }

        _controller.SetShoutcast(shoutcast);
        if (shoutcast.Icon != 0) _useIcon = true;

        _editing = true;
    }

    public override void Draw()
    {
        var current = _controller.GetCurrentShoutcast();
        if (ImGui.CollapsingHeader("Step 1: Character Data"))
        {
            ShowCharacterData();
            _viewer.ShowCharacterData(current);
        }

        if (ImGui.CollapsingHeader("Step 2: Audio Selection"))
        {
            ShowAudioSelection();

            _viewer.ShowSelectedAudio(current);
        }

        if (ImGui.CollapsingHeader("Step 3: Text Selection"))
        {
            ShowTextSelection();

            _viewer.ShowSelectedText(current);
        }

        if (ImGui.CollapsingHeader("Step 4: Announcement Data"))
        {
            ShowAnnouncementMetadata();
            _viewer.ShowMetadata(current);
        }

        if (ImGui.CollapsingHeader("Step 5: Confirm Everything Looks Good"))
        {
            ShowObjectData();
        }

        if (ImGui.CollapsingHeader("Step 6: Save and Reset"))
        {
            try
            {
                var n = current.Shoutcaster;

                if (PluginServices.ShoutcastRepository.ContainsKey(_controller.GetCurrentShoutcast().Id) && !_editing)
                {
                    ImGui.TextColoredWrapped(ImGuiColors.DalamudRed,
                        "Error! The chosen announcement ID is already in use! Please pick another!");
                }
                else
                {
                    if (_editing)
                    {
                        ImGui.TextColoredWrapped(ImGuiColors.DalamudRed,
                            "Important! This will OVERRIDE the current shoutcast \"" +
                            _controller.GetCurrentShoutcast().Id + "\" for " + n + ". Be very sure before saving.");
                        if (ImguiTools.CtrlShiftButton("Save & Close"))
                        {
                            var sc = _controller.BuildAndResetToDefaults();
                            _controller.SaveToConfigAndRegister(sc);
                            _editing = false;
                            IsOpen = false;
                        }
                    }
                    else
                    {
                        if (ImguiTools.CtrlShiftButton("Save & Reset To Blank Character"))
                        {
                            var sc = _controller.BuildAndResetToDefaults();
                            _controller.SaveToConfigAndRegister(sc);
                        }

                        if (ImguiTools.CtrlShiftButton("Save & New Voiceline for " + n))
                        {
                            var sc = _controller.BuildAndResetToCharacterDefaults();
                            _controller.SaveToConfigAndRegister(sc);
                        }
                    }
                }


                if (_editing)
                {
                    if (ImGui.Button("Close Without Saving"))
                    {
                        _controller.ResetToDefaults();
                        _editing = false;
                    }
                }
                else
                {
                    if (ImguiTools.CtrlShiftButton("Reset to Blank Voiceline for " + n))
                        _controller.BuildAndResetToCharacterDefaults();
                }
            }
            catch (InvalidOperationException e)
            {
                PluginServices.ChatGui.PrintError(e.Message, InternalConstants.MessageTag);
            }

            ImGui.Separator();
            if (ImguiTools.CtrlShiftButton("Reset To Defaults Without Saving"))
            {
                _controller.ResetToDefaults();
            }
        }
    }

    private string _attributeBuffer = "";

    private void ShowAnnouncementMetadata()
    {
        var id = _controller.GetCurrentShoutcast().Id;


        if (_editing)
        {
            ImGui.Text("Unique Internal Announcement ID: " + id);
        }
        else
        {
            ImGui.TextWrapped("Unique Internal Announcement ID");
            ImGuiComponents.HelpMarker(
                "This will be the name of the shoutcast you are creating. This is how you reference the voiceline when adding it to events. " +
                "This must be unique across all voicelines. If a duplicate is encountered, one may overwrite the other.");

            if (ImGui.InputText("###Unique Internal Announcement ID", ref id)) _controller.SelectAnnouncementId(id);

            if (PluginServices.ShoutcastRepository.ContainsKey(_controller.GetCurrentShoutcast().Id))
                ImGui.TextColoredWrapped(ImGuiColors.DalamudRed,
                    "Error! This announcement ID is already in use! Please pick another!");


            var lang = PluginServices.Config.Language;
            if (lang == "en" || lang == "de" ||
                lang == "fr") // I do not support JP/cn/kr rake as that would require a bit more work for not a lot of benefit
            {
                var transcription = _viewer.GetText(_controller.GetCurrentShoutcast());

                if (transcription.Length > 0 && !transcription.Equals("Shoutcast Transcription"))
                {
                    if (ImGui.Button("Pick for me"))
                    {
                        if (lang == "en")
                        {
                            var result = _englishRake.Get(transcription);
                            _controller.SelectAnnouncementId(result);
                        }
                        else if (lang == "de")
                        {
                            var result = _germanRake.Get(transcription);
                            _controller.SelectAnnouncementId(result);
                        }
                        else if (lang == "fr")
                        {
                            var result = _frenchRake.Get(transcription);
                            _controller.SelectAnnouncementId(result);
                        }
                    }
                }
                else
                {
                    ImGuiComponents.DisabledButton("Pick for me");
                }

                ImGuiComponents.HelpMarker(
                    "This button makes guess at an accurate Voiceline ID. " +
                    "It isn't always correct, but helpful when making lots of voicelines. " +
                    "There must be a voiceline transcription in order to use this feature. " +
                    "(It uses the RAKE algorithm, not generative AI)");
            }
        }


        uint duration = _controller.GetCurrentShoutcast().Duration;
        ImGui.TextWrapped("Announcement Duration");
        ImGuiComponents.HelpMarker("This value determines how long text will stay on the screen.");

        if (ImGui.SliderUInt("###Announcement Duration", ref duration, 1, 10, default, ImGuiSliderFlags.AlwaysClamp))
        {
            _controller.SetDuration((byte) duration);
        }


        ImGui.TextWrapped("Attributes:");
        ImGuiComponents.HelpMarker("Attributes are optional fields that act as filters. " +
                                   "If an attribute is not enabled in the configuration, the voiceline will never be shown during gameplay. " +
                                   "This is useful for if the line uses pronouns or calls ingame characters by name.");


        var attributeToAdd = _attributeBuffer;
        if (ImGui.InputText("###AddAttrText", ref attributeToAdd))
        {
            _attributeBuffer = attributeToAdd;
        }

        if (ImGui.Button("Add Attribute"))
        {
            if (!attributeToAdd.Trim().IsNullOrEmpty())
            {
                _controller.AddAttribute(attributeToAdd.Trim());
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("Clear Attributes"))
        {
            _controller.SetAttributes([]);
        }
    }

    private void ShowAudioSelection()
    {
        ImGui.TextWrapped(
            "You must pick one source of audio. Battle Talk and Cutscene Line link both audio and text, so they are easier to work with.");
        if (ImGui.Button("Battle Talk"))
        {
            ImGui.OpenPopup("CtrPop");
        }

        ImGuiComponents.HelpMarker("Using this is the easiest, as it auto-fills style, icon, and voiceline duration.");
        ContentDirectorBattleTalkPopUp();


        if (ImGui.Button("Cutscene Line"))
        {
            ImGui.OpenPopup("CutscenePop");
        }

        ImGuiComponents.HelpMarker(
            "This contains ALL cutscene lines filtered by character.");
        CutsceneLinePopup();


        ImGui.Separator();
        if (ImGui.Button("Orphaned Voice Lines"))
        {
            if (PluginServices.VoicelineDataResolver.GetOrphanedLines().Count > 1)
            {
                ImGui.OpenPopup("OrphanedPop");
            }
            else
            {
                PluginServices.ChatGui.PrintError("Game data has not yet loaded! Please try again in a few moments.");
            }
        }

        if (ImGui.CollapsingHeader("What is this?"))
        {
            ImGui.TextWrapped(
                "These voicelines are not connected to any other sheet or transcription. These are the trickiest to use, " +
                "but have a lot of trust voicelines, other battle NPC's, etc. To explore through these easily, " +
                "I recommend clicking on the following button to be taken to a list of all sound files in the game. " +
                "This should link you to the NPC + Cutscene Dialogue, and set you to row 27060. This is where the orphaned voicelines start.");
            if (ImGui.Button("SCD Master list"))
                Util.OpenLink(
                    "https://docs.google.com/spreadsheets/d/1IGnaqBeIuyMH-DjgChIH4M5CWdwIVr5RLWigQ-IgNqQ/edit?gid=1596921809#gid=1596921809&range=A27060");
        }

        ImGui.Separator();
        OrphanedVoLinePopup();
    }

    private void ShowObjectData()
    {
        if (ImGui.Button("Test Current Announcement"))
        {
            _controller.TestCreation();
        }

        _viewer.ShowObject(_controller.GetCurrentShoutcast());

        ImGui.Separator();
    }

    private string _manualTranscriptionBuffer = "";

    private void ShowTextSelection()
    {
        if (ImGui.Button("Instance Text Data Selector")) ImGui.OpenPopup("ICTDPop");

        ImGuiComponents.HelpMarker("This selects from most instance battle text.");
        InstanceContentTextDataPopup();


        if (ImGui.Button("NPC Yell Data Selector")) ImGui.OpenPopup("NPCPop");

        ImGuiComponents.HelpMarker("Trust/Regular NPC Speech Bubbles");
        ShowNpcYellSelectionPopup();

        if (ImGui.CollapsingHeader("I can't find the text in either of the above places!"))
        {
            var manualTranscriptionBuffer = _manualTranscriptionBuffer;
            var useBackup = _useBackup;
            ImGui.TextWrapped("While it is encouraged to use ingame data due to easy translation, " +
                              "sometimes the text isn't present. Use this field to add the transcription yourself.");
            if (ImGui.Checkbox("Manually Transcribe Text", ref useBackup)) _useBackup = useBackup;

            if (useBackup)
            {
                if (ImGui.InputText("###InputBackup", ref manualTranscriptionBuffer, 255))
                {
                    _manualTranscriptionBuffer = manualTranscriptionBuffer;
                    _controller.SetManualTranscription(PluginServices.Config.Language, _manualTranscriptionBuffer);
                }
            }
            else
            {
                _controller.ClearManualTranscription();
            }
        }
    }


    private string _npcYellFilter = "";

    private void ShowNpcYellSelectionPopup()
    {
        var lang = LanguageUtil.LanguageMap.First(p => p.Value.Equals(PluginServices.Config.Language)).Key;

        var filter = _npcYellFilter;

        ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
        if (ImGui.BeginPopupModal("NPCPop"))
        {
            ClosePopupButton();
            ImGui.TextWrapped("Filter");
            if (ImGui.InputText("###NPCYellFilter", ref filter, 300))
            {
                _npcYellFilter = filter;
            }

            if (ImGui.BeginTable("NPCYell###NPCYellTable", 2,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();
                var yells = PluginServices.VoicelineDataResolver.GetNpcYellList(lang);
                foreach (var y in yells)
                {
                    var text = y.Text.ToString();
                    if (!_npcYellFilter.Equals(""))
                    {
                        if (!text.Contains(_npcYellFilter, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(text);
                    ImGui.TableNextColumn();
                    if (ImGui.Button("Select###SelectYell" + y.RowId))
                    {
                        _controller.SelectNpcYell(y.RowId);
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndTable();
            }

            ImGui.EndPopup();
        }
    }

    private string _textDataFilter = "";

    private void InstanceContentTextDataPopup()
    {
        var lang = LanguageUtil.LanguageMap.First(p => p.Value.Equals(PluginServices.Config.Language)).Key;

        ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
        if (ImGui.BeginPopupModal("ICTDPop"))
        {
            ClosePopupButton();
            var filter = _textDataFilter;
            ImGui.TextWrapped("Filter");
            if (ImGui.InputText("###TextDataFilter", ref filter, 300))
            {
                _textDataFilter = filter;
            }

            if (ImGui.BeginTable("Text Data###TextDataTable", 2,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();
                var textData = PluginServices.VoicelineDataResolver.GetInstanceContentTextData(lang);
                foreach (var td in textData)
                {
                    var text = td.Text.ToString();
                    if (!_textDataFilter.Equals(""))
                    {
                        if (!text.Contains(_textDataFilter, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(text);
                    ImGui.TableNextColumn();
                    if (ImGui.Button("Select###SelectTextData" + td.RowId))
                    {
                        _controller.SelectInstanceContentTextDataRow(td.RowId);
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndTable();
            }

            ImGui.EndPopup();
        }
    }


    private string _contentDirectorFilter = "";

    private readonly ExcelSheet<InstanceContentTextData> _ictd =
        PluginServices.DataManager.Excel.GetSheet<InstanceContentTextData>();

    private void ContentDirectorBattleTalkPopUp()
    {
        ImGui.SetNextWindowSize(new Vector2(600, 600), ImGuiCond.FirstUseEver);
        if (ImGui.BeginPopupModal("CtrPop"))
        {
            ClosePopupButton();
            var filter = _contentDirectorFilter;
            ImGui.TextWrapped("Filter");
            if (ImGui.InputText("###CtrFilter", ref filter, 300))
            {
                _contentDirectorFilter = filter;
            }

            if (ImGui.BeginTable("Content DirectorBattleTalk###ContentDirectorBattleTalk", 5,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Style", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("VoLine", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();

                foreach (var td in PluginServices.VoicelineDataResolver.GetCdbtList())
                {
                    var rId = td.Text.RowId;
                    var foundEntry = _ictd.TryGetRow(rId, out var ctrRow);
                    var text = InternalConstants.ErrorContactDev;
                    if (foundEntry)
                    {
                        text = ctrRow.Text.ToString();
                    }

                    var voLine = td.VoLine;
                    var style = td.Style;
                    var icon = td.Icon;
                    var duration = td.Duration;
                    if (!_contentDirectorFilter.Equals(""))
                    {
                        if (!text.Contains(_contentDirectorFilter, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }
                    }

                    if (voLine == 0)
                    {
                        continue;
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(text);
                    ImGui.TableNextColumn();
                    ImGui.Text(style.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(icon.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(voLine.ToString());
                    ImGui.TableNextColumn();
                    //todo this smells funky, review after rewrite
                    if (ImGui.Button("Select###SelectCtr" + voLine))
                    {
                        _controller.SelectBattleTalk(voLine);
                        if (style != 0)
                        {
                            _controller.SelectStyle(style);
                        }

                        if (icon != 0)
                        {
                            _controller.SelectIcon(icon);
                        }

                        if (duration != 0)
                        {
                            _controller.SetDuration(duration);
                        }

                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndTable();
            }

            ImGui.EndPopup();
        }
    }

    private string _chosenChar = "";
    private string _cutsceneLineFilter = "";

    private void CutsceneLinePopup()
    {
        ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
        if (ImGui.BeginPopupModal("CutscenePop"))
        {
            ClosePopupButton();
            if (ImGui.BeginCombo("Character Picker", _chosenChar))
            {
                foreach (var character in PluginServices.VoicelineDataResolver.GetSortedCharacterNames())
                {
                    bool selected = _chosenChar.Equals(character);
                    if (ImGui.Selectable(character, selected))
                    {
                        _chosenChar = character;
                    }
                }

                ImGui.EndCombo();
            }

            ImGuiComponents.HelpMarker(
                "Due to how the game stores voicelines, this character picker is in all caps. This is just cosmetic.");


            if (_chosenChar.Equals(""))
            {
                ImGui.Text("Select A Character!");
            }
            else
            {
                var filter = _cutsceneLineFilter;
                ImGui.TextWrapped("Filter");
                if (ImGui.InputText("###CutsceneLineFilter", ref filter, 300))
                {
                    _cutsceneLineFilter = filter;
                }

                if (ImGui.BeginTable("Character Cutscene Lines", 2,
                        ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable |
                        ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed);

                    ImGui.TableHeadersRow();
                    foreach (var cutsceneLineTag in PluginServices.VoicelineDataResolver.GetCutsceneLineTags()[
                                 _chosenChar])
                    {
                        var text = PluginServices.VoicelineDataResolver.ResolveCutsceneLineWithTag(cutsceneLineTag);
                        if (!_cutsceneLineFilter.Equals(""))
                        {
                            if (!text.Contains(_cutsceneLineFilter, StringComparison.CurrentCultureIgnoreCase))
                            {
                                continue;
                            }
                        }

                        if (text.Equals("No subtitles, report if displayed."))
                        {
                            continue;
                        }

                        if (text.Equals(""))
                        {
                            text = InternalConstants.Untranslated;
                        }


                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        ImGui.TextWrapped(text);

                        ImGui.TableNextColumn();

                        if (ImGui.Button("Select###Select" + cutsceneLineTag))
                        {
                            _controller.SelectCutsceneLine(cutsceneLineTag);
                            ImGui.CloseCurrentPopup();
                        }
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.EndPopup();
        }
    }

    private string GetVoLineToPlay(int selector)
    {
        return "sound/voice/vo_line/" + PluginServices.VoicelineDataResolver.GetOrphanedLines()[selector] + "_" +
               PluginServices.Config.Language + ".scd";
    }

    private int _voLineSelector;
    private string _OrphanedLineSearch = "";
    private bool _jump;

    private void OrphanedVoLinePopup()
    {
        var selector = _voLineSelector;
        ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
        if (ImGui.BeginPopupModal("OrphanedPop"))
        {
            var search = _OrphanedLineSearch;
            ImGui.TextWrapped("Search for a Specific Voiceline ID");
            if (ImGui.InputText("###OrphanedLineSearch", ref search, 300))
            {
                _OrphanedLineSearch = search;
                if (!search.IsNullOrEmpty())
                {
                    var location = PluginServices.VoicelineDataResolver.GetOrphanedLines()
                        .FindIndex(s => s.Contains(search, StringComparison.CurrentCultureIgnoreCase));
                    if (location != -1)
                    {
                        _voLineSelector = location;
                        ImGui.SetScrollHereY();
                    }
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Jump to Voiceline")) _jump = true;


            ClosePopupButton();

            var items = PluginServices.VoicelineDataResolver.GetOrphanedLines();
            if (ImGui.BeginListBox("Voice Lines###OrphanedVoLinesList"))
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var isSelected = _voLineSelector == i;
                    if (ImGui.Selectable(items[i], isSelected)) _voLineSelector = i;

                    if (_jump && i == _voLineSelector)
                    {
                        ImGui.SetScrollHereY();
                        _jump = false;
                    }
                }

                ImGui.EndListBox();
            }

            if (ImGui.Button("Play###OrphanedVoLinesPlay"))
            {
                PluginServices.SoundManager.PlaySound(GetVoLineToPlay(selector));
            }

            ImGui.Separator();
            if (ImGui.Button("Go Back And Play###OrphanedVoLinesBackward"))
            {
                if (selector > 0)
                {
                    _voLineSelector = selector - 1;
                    PluginServices.SoundManager.PlaySound(GetVoLineToPlay(selector - 1));
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Go Forward And Play###OrphanedVoLinesForward"))
            {
                if (selector < PluginServices.VoicelineDataResolver.GetOrphanedLines().Count - 1)
                {
                    _voLineSelector = selector + 1;
                    PluginServices.SoundManager.PlaySound(GetVoLineToPlay(selector + 1));
                }
            }

            var currentVo = PluginServices.VoicelineDataResolver.GetOrphanedLines()[selector];
            if (ImGui.Button("Select Voiceline " + currentVo + "###OrphanedVoLinesSelect" + currentVo))
            {
                _controller.SelectOrphanedVoLine(Convert.ToUInt32(currentVo));
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void ShowCharacterData()
    {
        var name = _controller.GetCurrentShoutcast().Shoutcaster;
        ImGui.Text("Name:");
        if (ImGui.InputText("###Announcer Name", ref name, 100))
        {
            _controller.SelectName(name.Trim());
        }

        var icon = _controller.GetCurrentShoutcast().Icon;
        uint min = 73001;
        uint max = 73287;
        var useIcon = _useIcon;
        if (ImGui.Checkbox("Use Icon", ref useIcon))
        {
            _useIcon = useIcon;
        }

        if (useIcon)
        {
            ImGui.Text("Icon: ");
            if (ImGui.SliderUInt("###AnnouncerIcon", ref icon, min, max, default, ImGuiSliderFlags.AlwaysClamp))
                _controller.SelectIcon(icon);

            ImGui.SameLine();
            if (ImGui.ArrowButton("###GoDownIcon", ImGuiDir.Left))
            {
                var newIcon = icon - 1;
                if (newIcon > max) newIcon = max;
                if (newIcon < min) newIcon = min;

                _controller.SelectIcon(newIcon);
            }

            ImGui.SameLine();
            if (ImGui.ArrowButton("###GoUpIcon", ImGuiDir.Right))
            {
                var newIcon = icon + 1;

                if (newIcon > max) newIcon = max;
                if (newIcon < min) newIcon = min;


                _controller.SelectIcon(newIcon);
            }
        }
        else
        {
            _controller.SelectIcon(0);
        }


        if (_controller.GetCurrentShoutcast().InstanceContentTextDataRow ==
            0) //known issue - no way for me to easily let people override the shoutcast style
        {
            uint[] ss = [0, 6, 7, 11];
            var style = _controller.GetCurrentShoutcast().Style;
            foreach (var styleInt in ss)
            {
                if (ImGui.RadioButton("Style " + styleInt, style == styleInt)) _controller.SelectStyle((byte) styleInt);

                ImGui.SameLine();
            }

            ImGui.NewLine();
            ImGui.TextWrapped(
                "Style 0 is for dialogue, Style 6 is the default linkshell/announcement box, " +
                "Style 7 is black and rounded, and Style 11 is blue and sleek.");
        }
    }

    private void ClosePopupButton()
    {
        if (ImGui.Button("Close Without Selecting###ClosePopupButton")) ImGui.CloseCurrentPopup();

        ImGui.Separator();
    }

    public void Dispose()
    {
    }

    public override void OnClose()
    {
        if (_editing)
        {
            _editing = false;
            _controller.ResetToDefaults();
        }
    }
}