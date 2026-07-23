using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dalamud.Plugin.Services;
using PvPAnnouncer.Data;
using PvPAnnouncer.Impl.PvPEvents;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer.Impl;

public class JsonLoader(
    IDataManager dataManager,
    IPvPEventBroker pvPEventBroker,
    IShoutcastRepository shoutcastRepository,
    IEventShoutcastMapping eventShoutcastMapping) : IJsonLoader
{
    public void ClearAndLoadEmbeddedValuesIntoMemory()
    {
        shoutcastRepository.Clear();
        eventShoutcastMapping.Clear();
        LoadShoutcasts();
        //todo bad design - shoutcasts needs to be loaded w/ the above method before the below methods can complete - divide responsibility?
        LoadAndMapActionIdEvents();
        LoadMapping();
    }


    private static string ReadEmbeddedFile(string jsonFile)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resourceName = "PvPAnnouncer.json." + jsonFile;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return "";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public void LoadShoutcasts()
    {
        var shoutJ = ReadEmbeddedFile("shoutcast.json");
        var r = JsonNode.Parse(shoutJ);
        if (r is JsonArray j)
        {
            foreach (var shout in j)
            {
                var sh = ConstructShoutcast(shout?.ToString() ?? "");
                PluginServices.PluginLog.Verbose($"Constructed {sh.Id} with {sh.SoundPath}");
                shoutcastRepository.SetShoutcast(sh.Id, sh);
            }
        }
    }

    public void LoadAndMapActionIdEvents()
    {
        var eventsJ = ReadEmbeddedFile("event.json");
        var r = JsonNode.Parse(eventsJ);
        if (r is JsonArray j)
        {
            foreach (var customEvent in j)
            {
                if (customEvent != null)
                {
                    var name = customEvent["name"]?.GetValue<string>();
                    var id = customEvent["id"]?.GetValue<string>();
                    var eventType = customEvent["eventType"]?.GetValue<string>();
                    var actionIds = customEvent["actionIds"]?.AsArray().Select(x => (uint) (x ?? 0)).ToArray();
                    var shouts = customEvent["shouts"]?.Deserialize<List<string>>() ?? [];
                    if (id == null)
                    {
                        continue;
                    }

                    MapEvent(id, shouts);
                    switch (eventType)
                    {
                        case "AllyActionEvent":
                        {
                            var newAllyActionEvent =
                                new AllyActionEvent(actionIds ?? [], name ?? "Action", id ?? "UnknownEvent");
                            pvPEventBroker.RegisterListener(newAllyActionEvent);
                            break;
                        }
                        case "EnemyActionEvent":
                        {
                            var newEnemyActionEvent =
                                new EnemyActionEvent(actionIds ?? [], name ?? "Action", id ?? "UnknownEvent");
                            pvPEventBroker.RegisterListener(newEnemyActionEvent);
                            break;
                        }
                    }
                }
                //PluginServices.PluginLog.Verbose($"Constructed {sh.Id} with {sh.SoundPath} and attr {sh.Attributes}: {customEvent}");
            }
        }
    }

    private void MapEvent(string id, List<string> shouts)
    {
        List<string> newShoutList = [];
        foreach (var shout in shouts)
        {
            if (shout.Equals("LIMIT_BREAK_LIST"))
            {
                foreach (var se in InternalConstants.LimitBreakListStr)
                {
                    if (shoutcastRepository.ContainsKey(se))
                    {
                        newShoutList.Add(se);
                    }
                    else
                    {
                        PluginServices.PluginLog.Warning($"{se} not found in shoutcast repository!");
                    }
                }
            }
            else
            {
                PluginServices.PluginLog.Verbose($"Shout {shout} for {id} added.");
                newShoutList.Add(shout);
            }
        }

        eventShoutcastMapping.ReplaceMapping(id, newShoutList);
    }

    public void LoadMapping()
    {
        var mappingJ = ReadEmbeddedFile("mapping.json");
        var r = JsonNode.Parse(mappingJ);
        if (r is JsonArray j)
        {
            foreach (var mapping in j)
            {
                var id = mapping?["eventId"]?.GetValue<string>();
                var shouts = mapping?["shouts"]?.Deserialize<List<string>>();
                if (id == null || shouts == null)
                {
                    continue;
                }

                MapEvent(id, shouts);
            }
        }
    }

    public Dictionary<string, List<string>> LoadCutsceneLines()
    {
        var cs = ReadEmbeddedFile("csl.json");
        var cutsceneLines = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(cs.Trim()) ?? [];
        return cutsceneLines;
    }

    [Obsolete("Use ConvertJsonToMappingDelta")]
    public List<string> ConstructMappingFromJson(string json)
    {
        var r = JsonNode.Parse(json);
        var shouts = r?["shouts"]?.Deserialize<List<string>>();
        return shouts ?? [];
    }

    public Dictionary<string, List<string>> ConvertJsonToMappingDelta(string json)
    {
        var r = JsonNode.Parse(json);
        var add = r?["add"]?.Deserialize<List<string>>();
        var remove = r?["remove"]?.Deserialize<List<string>>();
        var newObj = new Dictionary<string, List<string>>
        {
            {"add", add ?? []},
            {"remove", remove ?? []}
        };
        return newObj;
    }

    public Dictionary<string, string> ConvertJsonToTranslation(string json)
    {
        var r = JsonNode.Parse(json);
        var dict = r?.Deserialize<Dictionary<string, string>>();
        return dict ?? [];
    }

    public Dictionary<string, List<string>> GetDelta(List<string> add, List<string> remove)
    {
        var newObj = new Dictionary<string, List<string>>
        {
            {"add", add ?? []},
            {"remove", remove ?? []}
        };
        return newObj;
    }

    [Obsolete("Use ConvertMappingDeltaToJson")]
    public JsonObject BuildJsonMapping(string eventId, List<string> shouts)
    {
        var j = new JsonObject
        {
            ["eventId"] = eventId
        };
        var shoutsArray = new JsonArray();

        foreach (var shout in shouts.Where(shout => !shout.Equals(""))) shoutsArray.Add(shout);

        if (shoutsArray.Count > 0) j["shouts"] = shoutsArray;

        return j;
    }

    public JsonObject ConvertMappingDeltaToJson(string eventId, List<string> add, List<string> remove)
    {
        var j = new JsonObject
        {
            ["eventId"] = eventId
        };
        var addArray = new JsonArray();
        foreach (var shout in add.Where(shout => !shout.Equals(""))) addArray.Add(shout);
        if (addArray.Count > 0) j["add"] = addArray;

        var removeArray = new JsonArray();
        foreach (var shout in remove.Where(shout => !shout.Equals(""))) removeArray.Add(shout);
        if (removeArray.Count > 0) j["remove"] = removeArray;

        return j;
    }

    public Shoutcast ConstructShoutcast(string json)
    {
        ShoutcastBuilder builder = new ShoutcastBuilder(dataManager);
        var j = JsonNode.Parse(json);
        if (j == null)
        {
            return builder.BuildAndRefreshProperties();
        }

        if (j["id"] != null)
        {
            builder.WithId(j["id"]!.GetValue<string>());
        }

        if (j["icon"] != null)
        {
            builder.WithIcon(Convert.ToUInt32(j["icon"]!.GetValue<string>()));
        }

        if (j["transcription"] != null)
        {
            var dict = j["transcription"]?.Deserialize<Dictionary<string, string>>();
            builder.WithTranscription(dict ?? []);
        }


        if (j["duration"] != null)
        {
            builder.WithDuration(Convert.ToByte(j["duration"]!.GetValue<string>()));
        }

        if (j["style"] != null)
        {
            builder.WithStyle(Convert.ToByte(j["style"]!.GetValue<string>()));
        }

        if (j["shoutcaster"] != null)
        {
            builder.WithShoutcaster(j["shoutcaster"]!.GetValue<string>());
        }

        if (j["attributes"] != null)
        {
            var att = j["attributes"]!.Deserialize<List<string>>();
            builder.WithAttributes(att ?? []);
        }

        if (j["soundPath"] != null)
        {
            builder.WithSoundPath(j["soundPath"]!.GetValue<string>());
        }

        if (j["cutsceneLine"] != null)
        {
            builder.WithCutsceneLine(j["cutsceneLine"]!.GetValue<string>());
        }

        if (j["contentDirectorBattleTalkVo"] != null)
        {
            builder.WithContentDirectorBattleTalkVo(
                Convert.ToUInt32(j["contentDirectorBattleTalkVo"]!.GetValue<string>()));
        }

        if (j["npcYell"] != null)
        {
            builder.WithNpcYell(Convert.ToUInt32(j["npcYell"]!.GetValue<string>()));
        }

        if (j["instanceContentTextDataRow"] != null)
        {
            builder.WithInstanceContentTextDataRow(
                Convert.ToUInt32(j["instanceContentTextDataRow"]!.GetValue<string>()));
        }

        return builder.BuildAndRefreshProperties();
    }

    public JsonObject BuildCustomShoutJson(Shoutcast s)
    {
        var j = new JsonObject();

        if (!s.Id.Equals(""))
        {
            j["id"] = s.Id;
        }

        if (s.Icon != 0)
        {
            j["icon"] = s.Icon.ToString();
        }

        if (!s.Shoutcaster.Equals(""))
        {
            j["shoutcaster"] = s.Shoutcaster;
        }

        if (s.Duration != 0)
        {
            j["duration"] = s.Duration.ToString();
        }


        j["style"] = s.Style.ToString();

        var hasSourceRef = s.NpcYell != 0 || s.ContentDirectorBattleTalkVo != 0 ||
                           !s.CutsceneLine.Equals("") || s.InstanceContentTextDataRow != 0;
        if (s.Transcription.Count > 0 && !hasSourceRef) //dont bother serializing something we'll re-serialize at load
        {
            j["transcription"] = JsonSerializer.SerializeToNode(s.Transcription);
        }

        if (s.Attributes.Count > 0)
        {
            j["attributes"] = JsonSerializer.SerializeToNode(s.Attributes);
        }

        if (!s.SoundPath.Equals(""))
        {
            j["soundPath"] = s.SoundPath;
        }

        if (!s.CutsceneLine.Equals(""))
        {
            j["cutsceneLine"] = s.CutsceneLine;
        }

        if (s.ContentDirectorBattleTalkVo != 0)
        {
            j["contentDirectorBattleTalkVo"] = s.ContentDirectorBattleTalkVo.ToString();
        }

        if (s.NpcYell != 0)
        {
            j["npcYell"] = s.NpcYell.ToString();
        }

        if (s.InstanceContentTextDataRow != 0)
        {
            j["instanceContentTextDataRow"] = s.InstanceContentTextDataRow.ToString();
        }

        if (s.IsGendered)
        {
            j["isGendered"] = true;
        }

        return j;
    }

    public string CompressToGzip(string str)
    {
        var inputBytes = Encoding.UTF8.GetBytes(str);
        var outputStream = new MemoryStream();
        var gZipStream = new GZipStream(outputStream, CompressionMode.Compress);
        gZipStream.Write(inputBytes, 0, inputBytes.Length);
        gZipStream.Close();
        var outputBytes = outputStream.ToArray();

        var outputStr = Convert.ToBase64String(outputBytes);
        return outputStr;
    }


    public string UncompressToStr(string b64)
    {
        var inputBytes = Convert.FromBase64String(b64);
        var st = new MemoryStream(inputBytes);
        var outputStream = new MemoryStream();
        var gZipStream = new GZipStream(st, CompressionMode.Decompress);
        gZipStream.CopyTo(outputStream);
        var outputBytes = outputStream.ToArray();

        var outputStr = Encoding.UTF8.GetString(outputBytes);
        return outputStr;
    }

    public string GetJsonObj(object? obj)
    {
        var str = JsonSerializer.Serialize(obj);
        return str;
    }


    public string ProcessObjectForExport(object? obj)
    {
        var json = GetJsonObj(obj);
        var compressed = CompressToGzip(json);
        return compressed;
    }

    public T ProcessStringForImport<T>(string b64)
    {
        var uncompressed = UncompressToStr(b64);
        var obj = JsonSerializer.Deserialize<T>(uncompressed);
        return obj;
    }
}