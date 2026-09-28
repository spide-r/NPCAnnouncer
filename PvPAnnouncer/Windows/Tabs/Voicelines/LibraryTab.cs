using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using OtterGui.Widgets;
using PvPAnnouncer.Data;

namespace PvPAnnouncer.Windows.Tabs.Voicelines;

public class LibraryTab : ITab
{
    public ReadOnlySpan<byte> Label => "Library"u8;

    private List<Shoutcast> _allBattleTalks;
    private readonly List<string> toFilter = ["All"];
    private string _textFilter = "";
    private int _filterIndex;

    public void DrawContent()
    {
        _allBattleTalks = new List<Shoutcast>(PluginServices.ShoutcastRepository.GetShoutcasts());
        toFilter.Clear();
        toFilter.Add("All");
        foreach (var s in _allBattleTalks.Where(s => !toFilter.Contains(s.Shoutcaster))) toFilter.Add(s.Shoutcaster);

        if (_filterIndex > toFilter.Count - 1) _filterIndex = 0;

        //above code is... inefficient, but idc since the list will be computationally small

        ImGui.Text("Announcer Filter");
        if (ImGui.BeginCombo("###Announcer Filter", toFilter[_filterIndex]))
        {
            for (var i = 0; i < toFilter.Count; i++)
            {
                var selected = _filterIndex == i;
                if (ImGui.Selectable(toFilter[i], selected)) _filterIndex = i;
            }

            ImGui.EndCombo();
        }

        var filter = _textFilter;
        ImGui.TextWrapped("Text Filter");
        if (ImGui.InputText("###TextFilter", ref filter)) _textFilter = filter;


        // id(voLine/path/)- name - text - button
        if (ImGui.BeginTable("Voicelines", 4,
                ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
        {
            ImGui.TableSetupColumn("Voiceline ID");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Text");
            ImGui.TableSetupColumn("Button");
            ImGui.TableHeadersRow();
            foreach (var bt in _allBattleTalks)
            {
                if (_filterIndex != 0)
                    if (!bt.Shoutcaster.Equals(toFilter[_filterIndex]))
                        continue;

                var text = bt.GetTranscriptionWithGender(PluginServices.Config.Language,
                    PluginServices.Config.WantsAttribute("Feminine Pronouns"), PluginServices.SeStringEvaluator);
                if (text.Equals(""))
                    text = InternalConstants.Untranslated;

                if (!_textFilter.Equals(""))
                    if (!text.Contains(_textFilter, StringComparison.CurrentCultureIgnoreCase))
                        continue;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(bt.Id);
                ImGui.TableNextColumn();
                ImGui.Text(bt.Shoutcaster);
                ImGui.TableNextColumn();


                ImGui.Text(text);
                ImGui.TableNextColumn();

                if (ImGui.Button("Play###Play" + bt.SoundPath))
                {
                    PluginServices.Announcer.SendBattleTalk(bt);
                    PluginServices.Announcer.PlaySound(bt.GetShoutcastSoundPathWithGenderAndLang(
                        PluginServices.Config.Language, PluginServices.Config.WantsAttribute("Feminine Pronouns")));
                }

                ImGui.SameLine();
                if (PluginServices.Config.MutedShouts.Contains(bt.Id))
                {
                    if (ImGui.Button("Unmute###Unmute" + bt.SoundPath))
                    {
                        PluginServices.Config.MutedShouts.Remove(bt.Id);
                        PluginServices.Config.Save();
                    }
                }
                else
                {
                    if (ImGui.Button("Mute###Mute" + bt.SoundPath))
                    {
                        PluginServices.Config.MutedShouts.Add(bt.Id);
                        PluginServices.Config.Save();
                    }
                }

                ImGui.SameLine();
                if (PluginServices.Config.CustomShoutcasts.ContainsKey(bt.Id))
                {
                    if (ImGui.Button("Edit###EditVL" + bt.SoundPath))
                    {
                        PluginServices.VoicelineCreationWindow.Edit(bt);
                        PluginServices.VoicelineCreationWindow.IsOpen = true;
                        PluginServices.VoicelineCreationWindow.BringToFront();
                    }

                    ImGui.SameLine();
                    if (ImguiTools.CtrlShiftButton("Delete###Delete" + bt.SoundPath))
                        PluginServices.ConfigManager.DeleteAndDeregisterShoutcast(bt.Id);
                }
            }

            ImGui.EndTable();
        }
    }
}