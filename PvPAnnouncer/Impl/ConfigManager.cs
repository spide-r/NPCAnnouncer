using System;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public class ConfigManager(Configuration pluginConfiguration, IJsonLoader jsonLoader)
{
    public void ReloadConfig()
    {
        jsonLoader.ClearAndLoadEmbeddedValuesIntoMemory();
        ApplyCustomValues();
    }

    public void ApplyCustomValues()
    {
        PluginServices.PluginLog.Verbose("Applying custom values!");
        foreach (var keyValuePair in pluginConfiguration.CustomShoutcasts)
        {
            try
            {
                var sc = keyValuePair.Value;
                var scObj = PluginServices.JsonLoader.ConstructShoutcast(sc);
                PluginServices.ShoutcastRepository.SetShoutcast(keyValuePair.Key, scObj);
            }
            catch (Exception e)
            {
                PluginServices.PluginLog.Error(
                    $"Issue parsing Shoutcast {keyValuePair.Key}: {e.Message} VALUE: {keyValuePair.Value}");
            }
        }

        foreach (var keyValuePair in pluginConfiguration.MappingOverride)
        {
            try
            {
                var jsonMapping = keyValuePair.Value;
                PluginServices.EventShoutcastMapping.ProcessMappingDelta(keyValuePair.Key,
                    PluginServices.JsonLoader.ConvertJsonToMappingDelta(jsonMapping));
            }
            catch (Exception e)
            {
                PluginServices.PluginLog.Error(
                    $"Issue parsing Mapping {keyValuePair.Key}: {e.Message} VALUE: {keyValuePair.Value}");
            }
        }

        foreach (var keyValuePair in pluginConfiguration.CustomEvents)
        {
            //unused - thank goodness
        }
    }

    public void DeleteAndDeregisterShoutcast(string shoutcastId)
    {
        PluginServices.Config.DeleteCustomShoutCast(shoutcastId);
        PluginServices.ShoutcastRepository.DeleteShoutcast(shoutcastId);
        PluginServices.EventShoutcastMapping.PurgeMapping(shoutcastId);
        PurgeShoutFromCustomMapping(shoutcastId);
        PluginServices.Config.Save();
    }

    private void PurgeShoutFromCustomMapping(string shout)
    {
        foreach (var (eventName, value) in PluginServices.Config.MappingOverride)
        {
            var currentList = PluginServices.JsonLoader.ConvertJsonToMappingDelta(value);
            var add = currentList["add"];
            var remove = currentList["remove"];
            add.Remove(shout);
            remove.Remove(shout);
            var currentJson = PluginServices.JsonLoader.ConvertMappingDeltaToJson(eventName, add, remove);
            PluginServices.Config.MappingOverride[eventName] = currentJson.ToJsonString();
        }
    }
}