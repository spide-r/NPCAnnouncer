using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using PvPAnnouncer.Data;

namespace PvPAnnouncer.Interfaces;

public interface IJsonLoader
{
    public void ClearAndLoadEmbeddedValuesIntoMemory();
    public void LoadAndMapActionIdEvents();
    public void LoadShoutcasts();
    public void LoadMapping();
    public JsonObject BuildCustomShoutJson(Shoutcast s);
    public Dictionary<string, List<string>> LoadCutsceneLines();
    public Shoutcast ConstructShoutcast(string json);

    [Obsolete("Use ConvertJsonToMappingDelta")]
    public List<string> ConstructMappingFromJson(string json);

    [Obsolete("Use ConvertMappingDeltaToJson")]
    public JsonObject BuildJsonMapping(string eventId, List<string> shouts);

    public Dictionary<string, List<string>> ConvertJsonToMappingDelta(string json);
    public Dictionary<string, string> ConvertJsonToTranslation(string json);
    public Dictionary<string, List<string>> GetDelta(List<string> add, List<string> remove);

    public JsonObject ConvertMappingDeltaToJson(string eventId, List<string> add, List<string> remove);
    public string CompressToGzip(string str);

    public string UncompressToStr(string gzip);

    public string GetJsonObj(object? obj);
    public string ProcessObjectForExport(object? obj);
    public T ProcessStringForImport<T>(string str);
}