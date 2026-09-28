using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using Lumina.Data;
using OtterGui.Widgets;

namespace PvPAnnouncer.Windows.Tabs.Settings;

public class LanguageSettingsTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;

    public ReadOnlySpan<byte> Label => "Language"u8;

    public void DrawContent()
    {
        ImGui.Text("Announcer Spoken Language:");
        DoLanguageVoiceSelection();

        ImGui.Text("Announcer Written Language:");
        ImGuiComponents.HelpMarker(
            "Due to how the plugin works, some voice lines do not have text equivalents in game. (specifically Mahjong Lines and Encrypted Voicelines from M12S). They have been manually transcribed to English. If you wish to help translate them to different languages, please contact the Plugin Developer.");

        DoLanguageTextSelection();
    }

    private void DoLanguageVoiceSelection()
    {
        foreach (var keyValuePair in LanguageUtil.LanguageMap)
        {
            var k = keyValuePair.Key;
            var v = keyValuePair.Value;
            if (k == 0) continue;

            var keyName = Enum.GetName(k) ?? "Unknown Language";
            if (!PluginServices.DataManager.FileExists($"sound/voice/vo_line/8205353_{v}.scd")) continue;

            var lang = _configuration.Language;
            if (ImGui.RadioButton(keyName + "###" + "LanguageVoiceSelection" + v, lang.Equals(v)))
            {
                _configuration.Language = v;
                _configuration.Save();
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    private void DoLanguageTextSelection()
    {
        var langs = Enum.GetValues<ClientLanguage>().Cast<ClientLanguage>();

        foreach (var clientLangEnum in langs)
        {
            var enumCodeString = clientLangEnum.ToCode();

            var userFacingLang = Enum.GetName(clientLangEnum) ?? "Unknown Language";

            var configuredTextLang = _configuration.TextLanguage;

            if (ImGui.RadioButton(userFacingLang + "###" + "LanguageTextSelection" + enumCodeString,
                    configuredTextLang.Equals(enumCodeString)))
            {
                _configuration.TextLanguage = enumCodeString;
                _configuration.Save();
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}