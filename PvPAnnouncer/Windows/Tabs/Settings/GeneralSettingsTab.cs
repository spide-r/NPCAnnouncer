using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using OtterGui.Widgets;

namespace PvPAnnouncer.Windows.Tabs.Settings;

public class GeneralSettingsTab : ITab
{
    private readonly Configuration _configuration = PluginServices.Config;

    public ReadOnlySpan<byte> Label => "General"u8;

    public void DrawContent()
    {
        DrawMainToggles();
    }

    private void DrawMainToggles()
    {
        ImGui.TextWrapped("Where should announcers be heard?");
        ImGui.Separator();
        DrawYap();
        ImGui.NewLine();

        ImGui.TextWrapped("Should Announcers react to other players?");
        ImGui.Separator();
        DrawReact();
        ImGui.NewLine();

        ImGui.TextWrapped("How should these announcements be displayed?");
        ImGui.Separator();
        DrawDisplay();
    }

    private void DrawYap()
    {
        var wolvesDen = _configuration.WolvesDen;
        var pvp = _configuration.PvP;
        var pve = _configuration.PvE;
        var fieldOps = _configuration.FieldOps;
        var overworld = _configuration.Overworld;


        if (ImGui.Checkbox("Wolves Den", ref wolvesDen))
        {
            _configuration.WolvesDen = wolvesDen;
            _configuration.Save();
        }


        if (ImGui.Checkbox("Overworld Areas", ref overworld))
        {
            _configuration.Overworld = overworld;
            _configuration.Save();
        }

        if (ImGui.Checkbox("PvP Instances", ref pvp))
        {
            _configuration.PvP = pvp;
            _configuration.Save();
        }

        if (ImGui.Checkbox("PvE Instances", ref pve))
        {
            _configuration.PvE = pve;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Field Operations", ref fieldOps))
        {
            _configuration.FieldOps = fieldOps;
            _configuration.Save();
        }
    }

    private void DrawReact()
    {
        var partyPvE = _configuration.PartyMembersPvE;
        var partyPvP = _configuration.PartyMembersPvP;

        if (ImGui.Checkbox("React to my party in PvP", ref partyPvP))
        {
            _configuration.PartyMembersPvP = partyPvP;
            _configuration.Save();
        }

        if (ImGui.Checkbox("React to my party in PvE", ref partyPvE))
        {
            _configuration.PartyMembersPvE = partyPvE;
            _configuration.Save();
        }
    }

    private void DrawDisplay()
    {
        var hideBattleText = _configuration.HideBattleText;
        var notify = _configuration.Notify;
        var icon = _configuration.WantsIcon;
        var pveCrits = _configuration.PvECrits;
        if (ImGui.Checkbox(
                "Allow comments on critical hits during PvE",
                ref pveCrits))
        {
            _configuration.PvECrits = pveCrits;
            _configuration.Save();
        }

        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Warning, this may make announcers extra chatty depending on your settings");
        if (ImGui.Checkbox("Show Announcer Portrait", ref icon))
        {
            _configuration.WantsIcon = icon;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Notify when Voice Volume is muted", ref notify))
        {
            _configuration.Notify = notify;
            _configuration.Save();
        }

        if (ImGui.Checkbox("Hide Battle Text", ref hideBattleText))
        {
            _configuration.HideBattleText = hideBattleText;
            _configuration.Save();
        }
    }
}