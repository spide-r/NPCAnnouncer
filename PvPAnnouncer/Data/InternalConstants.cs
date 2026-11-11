using System.Collections.Generic;

namespace PvPAnnouncer.Data;

public abstract class InternalConstants : ImguiTools
{
    public const string MessageTag = "NPCAnnouncer";
    public const uint PvPAnnouncerDevIcon = 73282;
    public const string PvPAnnouncerDevName = "NPCAnnouncer Dev";
    public const string DefaultSoundPath = "sound/voice/vo_line/8205794";
    public const string ErrorContactDev = "Uh oh! You shouldn't see this! Contact the NPCAnnouncer dev!";
    public const string MahjongAttribute = "Contains Mahjong Reference (Non-English)";

    public const string CurrentUpdateMessage =
        "Over 200 new voicelines have been added! Bugs have been fixed, UI has been improved." +
        "\nNew Settings for Trust & Duty support!";

    public const string Untranslated =
        "Untranslated Text! Contact the NPCAnnouncer developer if you wish to contribute!";

    public const string LimitBreakList = "LIMIT_BREAK_LIST";

    public static readonly Dictionary<string, List<string>> TrustNpcNames = new()
    {
        ["Alisaie"] = ["Alisaie", "アリゼー"],
        ["Alphinaud"] = ["Alphinaud", "アルフィノ"],
        ["Arenvald"] = ["Arenvald", "アレンヴァルド"],
        ["Aymeric"] = ["Aymeric", "アイメリク"],
        ["Carvallain"] = ["Carvallain", "カルヴァラン"],
        ["Crystal Exarch"] =
            ["G'raha Tia", "グ・ラハ・ティア", "Crystal Exarch", "exarque du Cristal", "Kristallexarch", "水晶公"],
        ["Emet-Selch"] = ["Emet-Selch", "エメトセルク"],
        ["Estinien"] = ["Estinien", "エスティニアン"],
        ["G'raha Tia"] = ["G'raha Tia", "グ・ラハ・ティア", "Crystal Exarch", "exarque du Cristal", "Kristallexarch", "水晶公"],
        ["Gosetsu"] = ["Gosetsu", "ゴウセツ"],
        ["Haurchefant"] = ["Haurchefant", "オルシュファン"],
        ["Hien"] = ["Hien", "ヒエン"],
        ["Hythlodaeus"] = ["Hythlodaeus", "ヒュトロダエウス"],
        ["J'moldva"] = ["J'moldva", "ジャ・モルバ"],
        ["Koana"] = ["Koana", "コーナ"],
        ["Krile"] = ["Krile", "クルル"],
        ["Lyna"] = ["Lyna", "ライナ"],
        ["Lyse"] = ["Lyse", "リセ"],
        ["Minfilia"] = ["Ryne", "リーン", "Minfilia", "ミンフィリア"],
        ["Pero Roggo"] = ["Pero Roggo", "ペロ・ロッゴ"],
        ["Raubahn"] = ["Raubahn", "ラウバーン"],
        ["Ryne"] = ["Ryne", "リーン", "Minfilia", "ミンフィリア"],
        ["Sphene"] = ["Sphene", "スフェーン"],
        ["Thancred"] = ["Thancred", "サンクレッド"],
        ["Urianger"] = ["Urianger", "ウリエンジェ"],
        ["Varshahn"] = ["Varshahn", "ヴァルシャン"],
        ["Venat"] = ["Venat", "ヴェーネス"],
        ["Wuk Lamat"] = ["Wuk Lamat", "ウクラマト"],
        ["Y'shtola"] = ["Y'shtola", "ヤ・シュトラ"],
        ["Ysayle"] = ["Ysayle", "イゼル"],
        ["Yugiri"] = ["Yugiri", "ユウギリ"],
        ["Zero"] = ["Zero", "ゼロ"],
        ["Zoraal Ja"] = ["Zoraal Ja", "ゾラージャ"]
    };
}