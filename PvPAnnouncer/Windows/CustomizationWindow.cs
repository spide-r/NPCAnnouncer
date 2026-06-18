using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using PvPAnnouncer.Data;

namespace PvPAnnouncer.Windows;

public class CustomizationWindow : Window, IDisposable
{
    private readonly Configuration _configuration;

    public CustomizationWindow(Configuration pluginConfiguration) : base(
        "Character, Event, and Voiceline Management")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 225),
        };
        _configuration = pluginConfiguration;
    }

    private int _activeEventsSelectedItem;
    private int _disabledEventsSelectedItem;
    private string[] _activeEventsArr = [];
    private string[] _activeEventsArrInternal = [];
    private string[] _disabledEventsArr = [];
    private string[] _disabledEventsArrInternal = [];
    private int _selection;
    private string _shoutcaster = "";

    public override void Draw()
    {
        var blEvents = _configuration.BlacklistedEvents;

        ImGui.TextWrapped(
            "If you need to edit or delete a custom shoutcast, or ensure that a pre-made shoutcast is never shown to you, you can do so here:");

        if (ImGui.Button("Mute, Delete & Edit Shoutcast")) PluginServices.VoicelineManagementWindow.Toggle();

        ImGui.TextWrapped("If you wish to browse existing shoutcasts, you can do so here:");
        if (ImGui.Button("Open Full Shoutcast List")) PluginServices.LoadedVoicelineWindow.Toggle();

        if (ImGui.CollapsingHeader("Voiceline Creation & Event Editing"))
        {
            ImGui.TextWrapped(
                "- In order for the plugin to play a voiceline, it needs an audio file and a text transcription.");
            ImGui.TextWrapped(
                "- While some voice line audio is transcribed neatly, most audio is independent from its transcription.");
            ImGui.TextWrapped(
                "- We must connect the dots ourselves in order to create a full shoutcast that the plugin can use.");


            ImGui.TextWrapped("Shoutcast creation can be done here:");
            if (ImGui.Button("Create Shoutcasts")) PluginServices.VoicelineCreationWindow.Toggle();

            ImGui.TextWrapped(
                "Once we have created a few shoutcasts, we must \"map\" the shoutcast to an associated event. This allows the plugin to select it when an event is triggered.\nThis can be done here:");

            if (ImGui.Button("Add & Remove Shoutcasts for Events")) PluginServices.VoicelineMappingWindow.Toggle();
        }

        ImGui.Separator();
        ImGui.TextWrapped(
            "Please feel free to share these presets anywhere! I'd love to see what you end up creating! " +
            "Who knows, your work may end up finding its way into the plugin. ;D");
        ImGui.TextWrapped("I can be contacted in the Dalamud discord in the \"PvPAnnouncer\" help forum ");

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Import & Export"))
        {
            ShowImportExport();
        }

        if (ImGui.CollapsingHeader("Enable & Disable Events"))
        {
            var activeEvents = new List<string>();
            var activeEventsInternal = new List<string>();
            foreach (var e in PluginServices.PvPEventBroker.GetPvPEvents())
            {
                var eventId = e.Id;
                if (!blEvents.Contains(eventId))
                {
                    activeEvents.Add(e.Name);
                    activeEventsInternal.Add(eventId);
                }
            }


            List<string> listDisabledInternal = [];
            List<string> listDisabledPublic = [];
            foreach (var internalName in blEvents)
            {
                var e = PluginServices.PvPEventBroker.GetEvent(internalName);
                if (e == null) continue;

                listDisabledInternal.Add(internalName);
                listDisabledPublic.Add(e.Name);
            }

            _activeEventsArr = activeEvents.ToArray();
            _activeEventsArrInternal = activeEventsInternal.ToArray();
            ImGui.Text("Enabled Events:");
            ImGui.ListBox("###EnabledEvents", ref _activeEventsSelectedItem, _activeEventsArr);
            if (ImGui.Button("Disable"))
                if (_activeEventsSelectedItem < _activeEventsArrInternal.Length)
                {
                    _configuration.BlacklistedEvents.Add(_activeEventsArrInternal[_activeEventsSelectedItem]);
                    _configuration.Save();
                }

            _disabledEventsArrInternal = listDisabledInternal.ToArray();
            _disabledEventsArr = listDisabledPublic.ToArray();
            ImGui.Text("Disabled Events:");
            ImGui.ListBox("###DisabledEvents", ref _disabledEventsSelectedItem, _disabledEventsArr);
            if (ImGui.Button("Enable"))
                if (_disabledEventsSelectedItem < _disabledEventsArrInternal.Length)
                {
                    _configuration.BlacklistedEvents.Remove(_disabledEventsArrInternal[_disabledEventsSelectedItem]);
                    _configuration.Save();
                }
        }

        if (ImGui.CollapsingHeader("Event Tester###Testerheader")) EventTester();


        if (ImGui.CollapsingHeader("Event Queue###QueueHeader")) EventQueue();

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Config Reset"))
        {
            if (ImguiTools.CtrlShiftButton("Reset Custom Voicelines"))
            {
                PluginServices.Config.CustomShoutcasts.Clear();
                PluginServices.Config.Save();
                PluginServices.ConfigManager.ReloadConfig();
                PluginServices.ChatGui.Print("Reset Custom Voicelines!");
            }

            ImGui.SameLine();
            if (ImguiTools.CtrlShiftButton("Reset Custom Mapping"))
            {
                PluginServices.Config.MappingOverride.Clear();
                PluginServices.Config.Save();
                PluginServices.ConfigManager.ReloadConfig();
                PluginServices.ChatGui.Print("Reset Custom Mapping!");
            }
        }
    }

    private void CopyValues(Dictionary<string, string> customVoicelines,
        Dictionary<string, string> customMappings,
        Dictionary<string, string> customEvents)
    {
        var customStuff = new Dictionary<string, Dictionary<string, string>>
        {
            {"shoutcasts", customVoicelines},
            {"mapping", customMappings},
            {"events", customEvents}
        };
        var text = PluginServices.JsonLoader.ProcessObjectForExport(customStuff);
        PluginServices.PluginLog.Verbose($"Output: {text}");
        ImGui.SetClipboardText(text);
        PluginServices.NotificationManager.AddNotification(new Notification()
        {
            Title = "Copied!",
            Content = "Successfully copied to the clipboard!"
        });
    }

    private void ShowImportExport()
    {
        var single = _configuration.ExportSingleCharacter;

        if (ImGui.Checkbox("Export Custom Values for a Single Character", ref single))
        {
            _configuration.ExportSingleCharacter = single;
            _configuration.Save();
        }

        var selection = _selection;
        if (single)
        {
            var scList = PluginServices.ShoutcastRepository.GetShoutcasters().Where(CasterHasChanges)
                .ToList();
            ImGui.TextWrapped("Select Character to Export:");
            if (ImGui.ListBox("###Chars", ref selection,
                    scList)) //slow and sloppy, but due to low list count it doesnt matter
            {
                _selection = selection;
                _shoutcaster = scList[selection];
            }
        }


        if (ImGui.Button("Export Custom Shoutcasts"))
            CopyValues(
                single && !_shoutcaster.IsNullOrEmpty()
                    ? GetShoutsForShoutcaster(_shoutcaster)
                    : PluginServices.Config.CustomShoutcasts, [], []);

        ImGui.SameLine();

        if (ImGui.Button("Export Shoutcast Mapping"))
            CopyValues([],
                single && !_shoutcaster.IsNullOrEmpty()
                    ? GetCustomMappingForShoutcaster(_shoutcaster)
                    : PluginServices.Config.MappingOverride, []);

        ImGui.SameLine();

        if (ImGui.Button("Export Both"))
            CopyValues(
                single && !_shoutcaster.IsNullOrEmpty()
                    ? GetShoutsForShoutcaster(_shoutcaster)
                    : PluginServices.Config.CustomShoutcasts,
                single && !_shoutcaster.IsNullOrEmpty()
                    ? GetCustomMappingForShoutcaster(_shoutcaster)
                    : PluginServices.Config.MappingOverride, []);

        ImGui.Separator();
        if (ImGui.Button("Import From Clipboard"))
        {
            var b64Clip = ImGui.GetClipboardText(); //new Dictionary<string, Dictionary<string, string>>
            PluginServices.PluginLog.Verbose($"Found {b64Clip}");
            var impVl = 0;
            var impMap = 0;
            try
            {
                var dict = PluginServices.JsonLoader
                    .ProcessStringForImport<Dictionary<string, Dictionary<string, string>>>(b64Clip);
                if (dict.TryGetValue("shoutcasts", out var deserializedSc))
                {
                    foreach (var keyValuePair in deserializedSc)
                    {
                        if (_configuration.DupeVoicelineChoice == 1 &&
                            PluginServices.ShoutcastRepository.ContainsKey(keyValuePair.Key)) // skip
                            continue;

                        PluginServices.Config.CustomShoutcasts[keyValuePair.Key] = keyValuePair.Value;
                        impVl++;
                    }
                }

                if (dict.TryGetValue("mapping", out var deserializedMapping))
                {
                    //todo - silent error where shoutcasts that dont exist are either ignored or added - potential room for improvement?
                    foreach (var keyValuePair in deserializedMapping)
                        switch (_configuration.DupeMappingChoice)
                        {
                            // skip stuff already overriden 
                            case 1 when PluginServices.Config.MappingOverride.ContainsKey(keyValuePair.Key):
                                continue;
                            case 1:
                                PluginServices.Config.MappingOverride[keyValuePair.Key] =
                                    keyValuePair.Value; //no overriden values, add freely
                                impMap++;
                                break;
                            // override - we dont care whats in our config
                            case 2:
                                PluginServices.Config.MappingOverride[keyValuePair.Key] = keyValuePair.Value;
                                impMap++;
                                break;
                            // either no dupe or we want to merge 
                            case 0:
                            {
                                var current =
                                    PluginServices.JsonLoader.ConvertJsonToMappingDelta(
                                        PluginServices.Config.MappingOverride.GetValueOrDefault(keyValuePair.Key,
                                            "{}"));
                                var currentToAdd = current["add"];
                                var currentToRemove = current["remove"];
                                var toMerge = PluginServices.JsonLoader.ConvertJsonToMappingDelta(
                                    keyValuePair.Value);
                                var toMergeAdd = toMerge["add"];
                                var toMergeRemove = toMerge["remove"];
                                currentToAdd.AddRange(toMergeAdd);
                                currentToRemove.AddRange(toMergeRemove);
                                currentToAdd.RemoveAll(currentToRemove
                                    .Contains); //should clean up any footguns where someone somehow has the same value in both their add and remove areas
                                currentToAdd = currentToAdd.Distinct().ToList();
                                currentToRemove = currentToRemove.Distinct().ToList();
                                PluginServices.Config.MappingOverride[keyValuePair.Key] = PluginServices.JsonLoader
                                    .ConvertMappingDeltaToJson(keyValuePair.Key, currentToAdd, currentToRemove)
                                    .ToJsonString();
                                impMap++;
                                break;
                            }
                        }
                }


                PluginServices.Config.Save();
                PluginServices.ConfigManager.ReloadConfig();
                PluginServices.ChatGui.Print(
                    $"Successfully Imported {impVl} voicelines and mapping for {impMap} events!");
            }
            catch (Exception e)
            {
                PluginServices.PluginLog.Warning(e, "Oops!");
                PluginServices.NotificationManager.AddNotification(new Notification
                {
                    Content = "Error: " + e.Message,
                    Type = NotificationType.Error,
                    Title = "Error Importing Clipboard Data!"
                });
            }
        }

        var overrideVl = _configuration.DupeVoicelineChoice == 0;
        var skipVl = _configuration.DupeVoicelineChoice == 1;
        var mergeMap = _configuration.DupeMappingChoice == 0;
        var skipMap = _configuration.DupeMappingChoice == 1;
        var overrideMap = _configuration.DupeMappingChoice == 2;

        ImGui.Text("Voiceline Import Options:");
        if (ImGui.RadioButton("Import and Override Duplicate Voicelines (Default)", overrideVl))
        {
            _configuration.DupeVoicelineChoice = 0;
            _configuration.Save();
        }

        if (ImGui.RadioButton("Don't import Duplicate Voicelines", skipVl))
        {
            _configuration.DupeVoicelineChoice = 1;
            _configuration.Save();
        }

        ImGui.Text("Event Mapping Import Options:");
        if (ImGui.RadioButton("Combine Event Mapping (Default)", mergeMap))
        {
            _configuration.DupeMappingChoice = 0;
            _configuration.Save();
        }

        if (ImGui.RadioButton("Preserve Existing Event Mapping", skipMap))
        {
            _configuration.DupeMappingChoice = 1;
            _configuration.Save();
        }

        ImGuiComponents.HelpMarker(
            "If you have made modifications to an event, this option will ensure that the import does not touch those modifications.");

        if (ImGui.RadioButton("Override Existing Event Mapping", overrideMap))
        {
            _configuration.DupeMappingChoice = 2;
            _configuration.Save();
        }
    }


    private Dictionary<string, string> GetShoutsForShoutcaster(string caster)
    {
        var customShouts = PluginServices.Config.CustomShoutcasts.Where(sh =>
        {
            var sc = PluginServices.ShoutcastRepository.GetShoutcast(sh.Key);
            return sc != null && sc.Shoutcaster.Equals(caster);
        });
        return customShouts.ToDictionary(sh => sh.Key, sh => sh.Value);
    }

    private bool CasterHasChanges(string caster)
    {
        return GetCustomMappingForShoutcaster(caster).Count > 0 || GetShoutsForShoutcaster(caster).Count > 0;
    }

    private Dictionary<string, string> GetCustomMappingForShoutcaster(string caster)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (eventId, mapJson) in PluginServices.Config.MappingOverride)
        {
            var delta = PluginServices.JsonLoader.ConvertJsonToMappingDelta(mapJson);
            var add = delta["add"].Where(sh =>
            {
                var sc = PluginServices.ShoutcastRepository.GetShoutcast(sh);
                return sc != null && sc.Shoutcaster.Equals(caster);
            }).ToList();

            var remove = delta["remove"].Where(sh =>
            {
                var sc = PluginServices.ShoutcastRepository.GetShoutcast(sh);
                return sc != null && sc.Shoutcaster.Equals(caster);
            }).ToList();

            if (add.Count == 0 && remove.Count == 0) continue;

            var toExport = PluginServices.JsonLoader.ConvertMappingDeltaToJson(eventId, add, remove).ToJsonString();
            dict.Add(eventId, toExport);
        }

        return dict;
    }


    private void EventTester()
    {
        ImGui.Text("Event Tester");
        ImGui.TextWrapped(
            "This Simulates these events happening in real pvp, using your configuration settings (except for cooldown between announcements.");
        var i = 1;
        foreach (var ev in PluginServices.PvPEventBroker.GetPvPEvents())
        {
            if (ImGui.Button(ev.Name))
            {
                try
                {
                    PluginServices.Announcer.ReceiveEvent(true, ev);
                    PluginServices.Announcer.ClearQueue();
                }
                catch (Exception e)
                {
                    PluginServices.PluginLog.Error(e, "Issue sending custom event!!!");
                }
            }

            if (i % 4 != 0)
            {
                ImGui.SameLine();
            }

            i++;
        }
    }

    private void EventQueue()
    {
        ImGui.TextWrapped("Here are the last 10 triggered events and their voicelines:");
        foreach (var lastTrigger in PluginServices.Announcer.GetLastTriggers()) ImGui.TextWrapped(lastTrigger);
    }

    public void Dispose()
    {
    }
}