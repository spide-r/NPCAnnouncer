using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PvPAnnouncer.Impl;
using PvPAnnouncer.Interfaces;
using PvPAnnouncer.Windows;

namespace PvPAnnouncer;

#pragma warning disable 8618
internal class PluginServices
{
    [PluginService] internal static IDataManager DataManager { get; private set; }

    [PluginService] internal static ISigScanner SigScanner { get; private set; }

    [PluginService] internal static ICommandManager CommandManager { get; private set; }

    [PluginService] internal static IChatGui ChatGui { get; private set; }

    [PluginService] internal static IObjectTable ObjectTable { get; private set; }

    [PluginService] internal static IPartyList PartyList { get; private set; }

    [PluginService] internal static IClientState ClientState { get; private set; }

    [PluginService] internal static IPlayerState PlayerState { get; private set; }

    [PluginService] internal static IDalamudPluginInterface DalamudPluginInterface { get; private set; }

    [PluginService] internal static IFramework Framework { get; private set; }

    [PluginService] internal static ICondition Condition { get; private set; }

    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; }

    [PluginService] internal static IPluginLog PluginLog { get; private set; }

    [PluginService] internal static IDutyState DutyState { get; private set; }

    [PluginService] internal static IGameConfig GameConfig { get; private set; }

    [PluginService] internal static INotificationManager NotificationManager { get; private set; }

    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; }

    [PluginService] internal static ISeStringEvaluator SeStringEvaluator { get; private set; }

    [PluginService] internal static ITextureProvider TextureProvider { get; private set; }

    internal static IPvPEventBroker PvPEventBroker { get; private set; }
    internal static IDutyManager DutyManager { get; private set; }
    internal static IAnnouncer Announcer { get; private set; }
    internal static IEventPublisher EventHooksPublisher { get; private set; }
    internal static ISoundManager SoundManager { get; private set; }
    internal static Configuration Config { get; private set; }
    internal static IEventListenerLoader ListenerLoader { get; private set; }
    internal static IPlayerStateTracker PlayerStateTracker { get; private set; }
    internal static IEventShoutcastMapping EventShoutcastMapping { get; private set; }
    internal static IShoutcastRepository ShoutcastRepository { get; private set; }
    internal static IJsonLoader JsonLoader { get; private set; }

    internal static LoadedVoicelineWindow LoadedVoicelineWindow { get; private set; }
    internal static VoicelineCreationWindow VoicelineCreationWindow { get; private set; }
    internal static VoicelineMappingWindow VoicelineMappingWindow { get; private set; }
    internal static CustomizationWindow CustomizationWindow { get; private set; }
    internal static ConfigWindow ConfigWindow { get; private set; }
    internal static MainWindow MainWindow { get; private set; }
    internal static DevWindow DevWindow { get; private set; }
    internal static VoicelineManagementWindow VoicelineManagementWindow { get; private set; }
    internal static ConfigManager ConfigManager { get; private set; }
    internal static IVoicelineDataResolver VoicelineDataResolver { get; private set; }

    internal static void Initialize(IDalamudPluginInterface pluginInterface, WindowSystem window)
    {
        pluginInterface.Create<PluginServices>();
        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        PvPEventBroker = new EventBroker();
        EventHooksPublisher = new EventHooksPublisher();
        SoundManager = new PluginSoundManager();
        PlayerStateTracker = new PlayerStateTracker();
        Config.Initialize(pluginInterface, PlayerStateTracker, GameConfig);
        DutyManager = new DutyManager(PlayerStateTracker);

        EventShoutcastMapping = new EventShoutcastMapping();
        ShoutcastRepository = new ShoutcastRepository(DataManager);

        JsonLoader = new JsonLoader(DataManager, PvPEventBroker,
            ShoutcastRepository, EventShoutcastMapping);
        VoicelineDataResolver = new VoicelineDataResolver(DataManager, JsonLoader);

        JsonLoader.ClearAndLoadEmbeddedValuesIntoMemory();
        ConfigManager = new ConfigManager(Config, JsonLoader);
        ConfigManager.ApplyCustomValues();
        Announcer = new Announcer(EventShoutcastMapping, ShoutcastRepository);
        LoadedVoicelineWindow = new LoadedVoicelineWindow(ShoutcastRepository);
        VoicelineCreationWindow = new VoicelineCreationWindow();
        VoicelineMappingWindow = new VoicelineMappingWindow();
        CustomizationWindow = new CustomizationWindow(Config);
        VoicelineManagementWindow = new VoicelineManagementWindow();
        ConfigWindow = new ConfigWindow(ShoutcastRepository, Config,
            EventShoutcastMapping);
        MainWindow = new MainWindow();
        DevWindow = new DevWindow();
        window.AddWindow(ConfigWindow);
        window.AddWindow(MainWindow);
        window.AddWindow(DevWindow);
        window.AddWindow(LoadedVoicelineWindow);
        window.AddWindow(VoicelineCreationWindow);
        window.AddWindow(VoicelineMappingWindow);
        window.AddWindow(CustomizationWindow);
        window.AddWindow(VoicelineManagementWindow);
        ListenerLoader = new EventListenerLoader();
        ListenerLoader.LoadEventListeners();
    }
}
#pragma warning restore 8618