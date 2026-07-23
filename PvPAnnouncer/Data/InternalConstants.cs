using System.Collections.Generic;

namespace PvPAnnouncer.Data;

public abstract class InternalConstants : ImguiTools
{
    public const string MessageTag = "NPCAnnouncer";
    public const uint PvPAnnouncerDevIcon = 73282;
    public const string PvPAnnouncerDevName = "NPCAnnouncer Dev";
    public const string DefaultSoundPath = "sound/voice/vo_line/8205794";
    public const string ErrorContactDev = "Uh oh! You shouldn't see this! Contact the NPCAnnouncer dev!";

    public const string Untranslated =
        "Untranslated Text! Contact the NPCAnnouncer developer if you wish to contribute!";

    public static readonly List<string> LimitBreakListStr =
    [
        "WhatPower", "PotentMagicks", "WhatAClash", "ThrillingBattle", "NeitherSideHoldingBack", "BattleElectrifying",
        "MjGameChanger", "MjBoldMove", "MjHeatingUp", "AbsoluteBrutality", "SuchFerocity", "SomethingsComing",
        "ThisEndsHere",
        "RunBeastRun", "DanceForMe", "ThePowerToTranscend", "StillFightInYou", "ChangedRoutine",
        "VauntedFortitude", "BoundingFromWallToWallMasc", "AssaultedRefMasc", "FeralOnslaught", "FelineFerocity",
        "LitheAndLethal",
        "BBEmbiggening", "KaboomBBSpecial", "UnusedBombarianPress", "BBDesprate", "DischargeAether", "DGSteps",
        "DGFeverPich", "SRBringsWorkToLife",
        "ChimericalFoe", "FeralPowersWeapon", "PunishingAttackFusion", "VFWickedWeapon", "VFUnleashedAether",
        "EvenMoreAether", "LightheadedFem",
        "DBTaggingIn", "Fusion", "HoldingNothingBack", "TyrannyTimeMasc", "UnleashedFullMightMasc", "ArmLifeOwnMasc",
        "ArmSlitheringOutDisgustingMasc", "CruelCoil",
        "VictoryWithinReach", "ReleaseYourPotential", "YouHaveThem", "ForTheHouseOfLeveilleur", "FocusReleaseAlphinaud",
        "SoItComesToThis", "TheoryInPractice",
        "GiveItEverythingAlisaie", "ShowThemWhatYoureMadeOf", "ExpectedWhenSerious", "KeepUpTheOffensive",
        "JustGettingStarted", "Fall", "ThereIsNoEscapeAlisaie", "KeepThemOnTheirHeels",
        "DanceOfTheStars", "ThereIsNoEscapeYshtola", "AndNowToReturnTheFavor", "HoldNothingBack", "HaveThemNowEstinien",
        "EndTheirStruggles",
        "GiveItEverything", "DeclareIntentions", "MakeAStandOurselves", "FinishThemTooMuch", "PointsAndAll",
        "FocusRelease", "TimeHasCome", "ShowMeWhatYouCanDo",
        "FinishingTouchKrile", "GotThemNowWuk", "CantPassChanceWuk", "WillStopThemWuk", "NoHoldingBackWukMahjong",
        "NoHoldingBackWuk", "Impressive", "IntentionsKnown",
        "EvenTheOdds", "HaveThemStrike", "ShowThemTheirPlace", "ThereYouHaveThem"
    ];
}