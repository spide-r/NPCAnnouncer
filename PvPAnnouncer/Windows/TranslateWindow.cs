using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using PvPAnnouncer.Data;

namespace PvPAnnouncer.Windows;

public class TranslateWindow : Window, IDisposable
{
    public TranslateWindow() : base(
        "NPC Announcer Translation")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 225)
        };
        foreach (var s in PluginServices.ShoutcastRepository.GetShoutcasters()) _toFilter.Add(s);
    }

    private readonly List<string> _toFilter = ["All"];
    private int _filterIndex;
    private int _shoutcastSelection;
    private string _translationBuffer = "";


    public override void Draw()
    {
        var lang = PluginServices.Config.Language;

        ImGui.Text("Current Language: " + lang);


        ImGui.Separator();
        ImGui.Text("Pick an Announcer and a Voiceline:");
        if (ImGui.BeginCombo("###Announcer Filter", _toFilter[_filterIndex]))
        {
            for (var i = 0; i < _toFilter.Count; i++)
            {
                var selected = _filterIndex == i;
                if (ImGui.Selectable(_toFilter[i], selected))
                {
                    _filterIndex = i;
                    _shoutcastSelection = 0;
                    _translationBuffer = "";
                }
            }

            ImGui.EndCombo();
        }

        var shoutcastSelection = _shoutcastSelection;
        var scInternal = new List<string>();
        var toTranslate = PluginServices.ShoutcastRepository.GetShoutcasts()
            .Where(sc =>
            {
                var filter = _toFilter[_filterIndex];
                return filter.Equals("All") || sc.Shoutcaster.Equals(filter);
            })
            .Where(sc =>
                !sc.Transcription.ContainsKey(lang) ||
                PluginServices.Config.Translations.ContainsKey(sc.Id)
            ).ToList();
        foreach (var e in toTranslate)
        {
            var eventId = e.Id;

            scInternal.Add(eventId);
        }

        if (toTranslate.Count <= 0)
        {
            ImGui.Text(
                $"No lines need to be translated for the chosen language! Either change your settings or pick another announcer!");
            return;
        }

        ImGui.Separator();
        if (ImGui.Combo("###VoicelineCombo", ref shoutcastSelection, scInternal))
        {
            _shoutcastSelection = shoutcastSelection;
            _translationBuffer = "";
        }

        var sc = PluginServices.ShoutcastRepository.GetShoutcast(scInternal[shoutcastSelection]);
        if (sc != null)
        {
            ImGui.Separator();
            ImGui.Text("Name: " + sc.Shoutcaster);
            ImGui.Text("ID: " + sc.Id);
            if (ImGui.Button("Play Sound"))
                PluginServices.Announcer.PlaySound(sc.GetShoutcastSoundPathWithGenderAndLang(
                    PluginServices.Config.Language, PluginServices.Config.WantsAttribute("Feminine Pronouns")));

            var translations = PluginServices.Config.Translations.TryGetValue(sc.Id, out var configTranslation)
                ? PluginServices.JsonLoader.ConvertJsonToTranslation(configTranslation)
                : [];

            ImGui.Text($"Enter a translation:");
            var translationBuffer = _translationBuffer;
            if (ImGui.InputText("###TranslationInput", ref translationBuffer)) _translationBuffer = translationBuffer;

            ImGui.SameLine();

            if (ImGui.Button("Apply Translation"))
            {
                translations[lang] = _translationBuffer;
                PluginServices.Config.Translations[sc.Id] = PluginServices.JsonLoader.GetJsonObj(translations);
                PluginServices.Config.Save();
            }

            ImGui.Text("Current translation: " + translations.GetValueOrDefault(lang, ""));
        }

        ImGui.Separator();
        if (ImGui.Button("Copy All Translations to Clipboard"))
        {
            var text = PluginServices.JsonLoader.ProcessObjectForExport(PluginServices.Config.Translations);
            ImGui.SetClipboardText(text);
            PluginServices.ChatGui.Print("Copied all translations to the clipboard!");
        }

        if (ImguiTools.CtrlShiftButton("Clear All Translations"))
        {
            PluginServices.Config.Translations.Clear();
            PluginServices.Config.Save();
        }
    }


    public void Dispose()
    {
    }
}