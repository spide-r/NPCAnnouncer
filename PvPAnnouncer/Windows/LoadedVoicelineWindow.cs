using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using PvPAnnouncer.Data;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Windows;

public class LoadedVoicelineWindow : Window, IDisposable
{
    private readonly List<Shoutcast> _allBattleTalks;
    private List<string> toFilter = ["All"];
    private string _textFilter = "";
    private int _filterIndex;

    public LoadedVoicelineWindow(IShoutcastRepository shoutcastRepository) : base(
        "Loaded Voice Lines", ImGuiWindowFlags.AlwaysVerticalScrollbar)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 225),
        };
        foreach (var s in PluginServices.ShoutcastRepository.GetShoutcasters()) toFilter.Add(s);


        _allBattleTalks = new List<Shoutcast>(shoutcastRepository.GetShoutcasts());
    }

    public void Dispose()
    {
    }


    public override void Draw()
    {
        ImGui.Text("Announcer Filter");
        if (ImGui.BeginCombo("###Announcer Filter", toFilter[_filterIndex]))
        {
            for (var i = 0; i < toFilter.Count; i++)
            {
                bool selected = (_filterIndex == i);
                if (ImGui.Selectable(toFilter[i], selected))
                {
                    _filterIndex = i;
                }
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
                {
                    if (!bt.Shoutcaster.Equals(toFilter[_filterIndex]))
                    {
                        continue;
                    }
                }

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

                if (ImGui.Button("Play###" + bt.SoundPath))
                {
                    PluginServices.Announcer.SendBattleTalk(bt);
                    PluginServices.Announcer.PlaySound(bt.GetShoutcastSoundPathWithGenderAndLang(
                        PluginServices.Config.Language, PluginServices.Config.WantsAttribute("Feminine Pronouns")));
                }
            }

            ImGui.EndTable();
        }
    }
}