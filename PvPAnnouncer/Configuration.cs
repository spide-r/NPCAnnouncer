using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Game;
using Dalamud.Game.Config;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using PvPAnnouncer.Data;
using PvPAnnouncer.Interfaces;

namespace PvPAnnouncer
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        [NonSerialized] private IDalamudPluginInterface? _pluginInterface;

        [Obsolete] public Personalization VoicelineSettings = Personalization.MetemAnnouncer;

        public bool NewConfig { get; set; } = true;
        public int RepeatVoiceLineQueue { get; set; } = 3;
        public int ClearEventsAfter { get; set; } = 2;
        public int ClearVoicelinesAfter { get; set; } = 5;
        public int RepeatEventCommentaryQueue { get; set; } = 3;
        public int AnimationDelayFactor { get; set; } = 250;

        public int CooldownSeconds { get; set; } = 15;

        public HashSet<string> DesiredAttributes { get; set; } = [];

        public bool Notify { get; set; } = true;

        public List<string> BlacklistedEvents { get; set; } = [];

        public bool Disabled { get; set; } = false;
        public bool Muted { get; set; } = false;

        public bool HideBattleText { get; set; } = false;
        public bool WantsIcon { get; set; } = true;

        public int Percent { get; set; } = 70;

        public bool ShowNotification { get; set; } = false;

        public int DupeVoicelineChoice { get; set; } = 0;
        public int DupeMappingChoice { get; set; } = 0;
        public bool ExportSingleCharacter { get; set; } = false;

        public bool Overworld { get; set; } = false;
        public bool PvE { get; set; } = true;
        public bool PvECrits { get; set; } = false;
        public bool PvP { get; set; } = true;
        public bool WolvesDen { get; set; } = false;

        public bool PartyMembersPvP { get; set; } = false;
        public bool PartyMembersPvE { get; set; } = false;


        public Dictionary<string, string> CustomShoutcasts { get; set; } = [];
        public Dictionary<string, string> CustomEvents { get; set; } = []; //unused for now
        public Dictionary<string, string> MappingOverride { get; set; } = [];

        public List<string> MutedShouts { get; set; } = [];


        public string Language { get; set; } = "en";
        public string TextLanguage { get; set; } = "en";
        public int Version { get; set; } = 8;

        public void AddCustomShoutCast(string shoutcastId, string shoutcastJson)
        {
            CustomShoutcasts[shoutcastId] = shoutcastJson;
        }

        public void DeleteCustomShoutCast(string shoutcastId)
        {
            CustomShoutcasts.Remove(shoutcastId);
        }

        public void Initialize(IDalamudPluginInterface pluginInterface, IPlayerStateTracker ps, IGameConfig gameConfig)
        {
            _pluginInterface = pluginInterface;
            MigrateOldPluginConfig();

            if (NewConfig && DesiredAttributes.Count < 1) // confirmed new config w/o anything set
            {
                if (ps.CheckCNClient())
                {
                    Language = "chs";
                    TextLanguage = "chs";
                }
                else if (ps.CheckKRClient())
                {
                    Language = "kr";
                    TextLanguage = "kr";
                }
                else
                {
                    // global client
                    gameConfig.TryGet(SystemConfigOption.CutsceneMovieVoice, out uint configuredLang);
                    gameConfig.TryGet(SystemConfigOption.Language, out uint clientLang);
                    PluginServices.PluginLog.Info($"CutsceneMovieVoice: {configuredLang}");
                    PluginServices.PluginLog.Info($"Client Language: {clientLang}");
                    var sw = configuredLang > 99 ? clientLang : configuredLang;
                    try
                    {
                        Language = ((ClientLanguage) sw).ToCode();
                        TextLanguage = ((ClientLanguage) clientLang).ToCode();
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }

                DesiredAttributes.Add("Metem");
                NewConfig = false;
                _pluginInterface?.SavePluginConfig(this);
            }
        }

        private void MigrateOldPluginConfig()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (Version == 0) //porting due to changes in voice line personalization system
            {
                Version++;
            }

            if (Version == 1) // Port due to new internal name for pvp events
            {
                if (BlacklistedEvents.Count > 0)
                {
                    List<string> oldBl = [];
                    oldBl.AddRange(BlacklistedEvents);
                    BlacklistedEvents.Clear();
                    var oldDict = new Dictionary<string, string>
                    {
                        {"Marksman's Spite (Wicked Thunder)", "AllyMchLBEvent"},
                        {"Seraphism (Wicked Thunder)", "AllySchLbEvent"},
                        {"Tenebrae Lemurum (Wicked Thunder)", "AllyRprLBEvent"},
                        {"Blota", "AllyPullEvent"},
                        {"Rising Phoenix & Flare Star", "AllyFireEvent"},
                        {"Full Swing, Wind's Reply (Brute Bomber)", "AllyKBActionEvent"},
                        {"Swift (Howling Blade)", "AllySwiftEvent"},
                        {"Biolysis (Honey B. Lovely)", "AllyBiolysisEvent"},
                        {"Instant Kills (Brute Bomber + Black Cat)", "AllyInstantKillEvent"},
                        {"Contradance (Honey B. Lovely)", "AllyDncLBEvent"},
                        {"Contradance from enemies.", "EnemyDncLBEvent"},
                        {"Flarethrower (Rival Wings)", "AllyFlarethrowerEvent"},
                        {"Flarethrower from Enemies (Rival Wings)", "EnemyFlarethrowerEvent"},
                        {"Deaths", "AllyDeathEvent"},
                        {"Hit Enemy Hard", "AllyHitEnemyHardEvent"},
                        {"Hit hard by enemy", "AllyHitHardEvent"},
                        {"Hit while under guard", "AllyHitUnderGuardEvent"},
                        {"Limit Breaks", "AllyLimitBreakEvent"},
                        {"Mitigation used", "AllyMitUsedEvent"},
                        {"Pulled By Dark Knight", "AllyPulledByDrkEvent"},
                        {"Resurrection", "AllyResurrectEvent"},
                        {"Fall Damage", "AllyZoneOutEvent"},
                        {"Enemies Fail to hit CC", "EnemyMissedCcEvent"},
                        {"Matches Ending", "MatchEndEvent"},
                        {"Matches Started", "MatchStartEvent"},
                        {"Entered Rival Wings Mech", "EnteredMechEvent"},
                        {"Stormy Weather", "MatchStormyWeatherEvent"},
                        {"Battle High V / Flying High Gained", "MaxBattleFeverEvent"},
                    };
                    foreach (var oldName in oldBl)
                    {
                        if (oldDict.TryGetValue(oldName, out var value))
                        {
                            BlacklistedEvents.Add(value);
                        }
                    }
                }

                Version++;
            }

            if (Version == 2) //2 week spoiler embargo had a personalization Spoiler value of 15 - to remove any wonkyness, remake the personalization int without 15
            {
                Version++;
                ShowNotification = false;
            }

            if (Version == 3)
            {
                // Event Changes
                if (BlacklistedEvents.Contains("AllyHitHardEvent"))
                {
                    BlacklistedEvents.Remove("AllyHitHardEvent");
                    BlacklistedEvents.Add("AllyHitByLimitBreakEvent");
                }

                ShowNotification = true;
                Version++;
            }

            if (Version == 4)
            {
                // Personalization rework

                ShowNotification = true;
                Version++;
            }

            if (Version == 5) // fix ja/jp bug
            {
                if (Language.Equals("jp"))
                {
                    Language = "ja";
                }

                Version++;
            }

            if (Version == 6)
            {
                ShowNotification = true;
                var values = (Personalization[]) Enum.GetValues(typeof(Personalization));

                foreach (var p in values)
                {
                    if (!VoicelineSettings.HasFlag(p))
                    {
                        continue;
                    }

                    switch (p)
                    {
                        case Personalization.FemPronouns:
                            DesiredAttributes.Add("Feminine Pronouns");
                            break;
                        case Personalization.MascPronouns:
                            DesiredAttributes.Add("Masculine Pronouns");
                            break;
                        case Personalization.BlackCat:
                            DesiredAttributes.Add("Black Cat");
                            break;
                        case Personalization.HoneyBLovely:
                            DesiredAttributes.Add("Honey B. Lovely");
                            break;
                        case Personalization.BruteBomber:
                            DesiredAttributes.Add("Brute Bomber");
                            break;
                        case Personalization.WickedThunder:
                            DesiredAttributes.Add("Wicked Thunder");
                            break;
                        case Personalization.DancingGreen:
                            DesiredAttributes.Add("Dancing Green");
                            break;
                        case Personalization.SugarRiot:
                            DesiredAttributes.Add("Sugar Riot");
                            break;
                        case Personalization.BruteAbominator:
                            DesiredAttributes.Add("Brute Abominator");
                            break;
                        case Personalization.HowlingBlade:
                            DesiredAttributes.Add("Howling Blade");
                            break;
                        case Personalization.VampFatale:
                            DesiredAttributes.Add("Vamp Fatale");
                            break;
                        case Personalization.DeepBlueRedHot:
                            DesiredAttributes.Add("Deep Blue & Red Hot");
                            break;
                        case Personalization.Tyrant:
                            DesiredAttributes.Add("The Tyrant");
                            break;
                        case Personalization.President:
                            DesiredAttributes.Add("The President");
                            break;
                        case Personalization.MetemAnnouncer:
                            DesiredAttributes.Add("Metem");
                            break;
                        case Personalization.AlphinaudAnnouncer:
                            DesiredAttributes.Add("Alphinaud");
                            break;
                        case Personalization.AlisaieAnnouncer:
                            DesiredAttributes.Add("Alisaie");
                            break;
                        case Personalization.ThancredAnnouncer:
                            DesiredAttributes.Add("Thancred");
                            break;
                        case Personalization.UriangerAnnouncer:
                            DesiredAttributes.Add("Urianger");
                            break;
                        case Personalization.YshtolaAnnouncer:
                            DesiredAttributes.Add("Y'shtola");
                            break;
                        case Personalization.EstinienAnnouncer:
                            DesiredAttributes.Add("Estinien");
                            break;
                        case Personalization.GrahaAnnouncer:
                            DesiredAttributes.Add("G'raha Tia");
                            break;
                        case Personalization.KrileAnnouncer:
                            DesiredAttributes.Add("Krile");
                            break;
                        case Personalization.WukLamatAnnouncer:
                            DesiredAttributes.Add("Wuk Lamat");
                            break;
                        case Personalization.KoanaAnnouncer:
                            DesiredAttributes.Add("Koana");
                            break;
                        case Personalization.BakoolJaJaAnnouncer:
                            DesiredAttributes.Add("Bakool Ja Ja");
                            break;
                        case Personalization.ErenvilleAnnouncer:
                            DesiredAttributes.Add("Erenville");
                            break;
                        case Personalization.ZenosAnnouncer:
                            DesiredAttributes.Add("Zenos");
                            break;
                        case Personalization.None:
                        default:
                            break;
                    }
                }

                Version++;
            }


            if (Version == 7)
            {
                ShowNotification = true;
                Version++;
            }

            if (Version == 8)
            {
                ShowNotification = true;
                Version++;
            }

            if (Version == 9)
            {
                ShowNotification = true;
                Version++;
            }

            _pluginInterface?.SavePluginConfig(this);
#pragma warning restore CS0612 // Type or member is obsolete
        }

        public bool WantsAttribute(string attribute)
        {
            return DesiredAttributes.Contains(attribute);
        }


        public bool WantsAllAttributes(List<string> attributes)
        {
            foreach (var p in attributes)
            {
                if (!DesiredAttributes.Contains(p))
                {
                    PluginServices.PluginLog.Verbose($"Attribute {p} not found.");
                    return false;
                }
            }

            return true;
        }

        public void SetAttribute(string attribute)
        {
            DesiredAttributes.Add(attribute);
        }

        public void RemoveAttribute(string attribute)
        {
            DesiredAttributes.Remove(attribute);
        }

        public void ToggleAttribute(string attribute, bool set)
        {
            if (set)
            {
                DesiredAttributes.Add(attribute);
            }
            else
            {
                DesiredAttributes.Remove(attribute);
            }
        }

        public void Save()
        {
            _pluginInterface?.SavePluginConfig(this);
        }
    }
}