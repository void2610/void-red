using UnityEngine;
using VContainer;
using VContainer.Unity;
using Void2610.SettingsSystem;
using Void2610.UnityTemplate;
using Void2610.UnityTemplate.Discord;
using Void2610.UnityTemplate.Steam;

/// <summary>
/// ルートレベルのLifetimeScope
/// 複数シーンで共有されるサービス（設定管理など）を登録
/// </summary>
public class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private BgmManager bgmManager;
    [SerializeField] private SeManager seManager;
    [SerializeField] private VolumeController volumeController;
    [SerializeField] private AllHelpData allHelpData;
    [SerializeField] private ConfirmationDialogView confirmationDialogView;

    [Header("展示モード")]
    [SerializeField] private ExhibitSettings exhibitSettings;
    [SerializeField] private ExhibitOverlayView exhibitOverlayViewPrefab;

    protected override void Configure(IContainerBuilder builder)
    {
        // データの登録と初期化
        builder.RegisterInstance(allHelpData);
        RegisterAllData();

        // セーブデータ管理
        builder.Register<SaveDataManager>(Lifetime.Singleton);

        // シーン遷移管理（クロスフェード機能付き）
        builder.Register<SceneTransitionManager>(Lifetime.Singleton).AsSelf().As<ISceneTransitionService>();

        // ゲーム進行管理（全機能統合）
        builder.Register<GameProgressService>(Lifetime.Singleton);

        // 進行度と独立したノベル再生の予約 (回想 / デバッグ)
        builder.Register<NovelPlaybackRequest>(Lifetime.Singleton);

        // InputSystem管理
        builder.Register<InputActionsProvider>(Lifetime.Singleton);

        // UIナビゲーション管理
        builder.RegisterEntryPoint<MouseHoverUISelector>();
        builder.RegisterEntryPoint<SafeNavigationManager>();

        // SettingsSystem用のバインド
        builder.RegisterInstance(new SettingsSystemOptions());
        builder.Register<ISettingsDefinition, GameSettingsDefinition>(Lifetime.Singleton);
        builder.Register<ISettingsInputProvider, SettingsInputProvider>(Lifetime.Singleton);
        builder.Register<ConfirmationDialogService>(Lifetime.Singleton).WithParameter(confirmationDialogView).As<IConfirmationDialog>();

        // 設定管理（ISettingsDefinitionに依存）
        builder.RegisterEntryPoint<SettingsManager>().AsSelf();

        // Steam統合サービス
        builder.RegisterEntryPoint<SteamService>().WithParameter(3997140).AsSelf();
        // 展示モード
        builder.RegisterInstance(exhibitSettings);
        builder.Register<IdleDetector>(Lifetime.Singleton);
        builder.RegisterEntryPoint<ExhibitSessionTimerService>().AsSelf();

        // ExhibitIdleServiceはIExhibitOverlayが必要なため、展示モード有効時のみ登録
        if (exhibitSettings.EnableExhibitMode)
        {
            var overlay = Instantiate(exhibitOverlayViewPrefab);
            DontDestroyOnLoad(overlay.gameObject);
            builder.RegisterInstance<IExhibitOverlay>(overlay);
            builder.RegisterEntryPoint<ExhibitIdleService>();
        }

        // Discord統合サービス
        builder.Register<DiscordService>(Lifetime.Singleton)
            .WithParameter(1415132179377160262UL)   // clientId
            .WithParameter("Void Red")               // gameName
            .WithParameter("https://void-red.void2610.dev/") // url
            .WithParameter("プレイ中");              // defaultDetails

        InitializeSingletonMonoBehaviour();
    }

    private void RegisterAllData()
    {
#if UNITY_EDITOR
        // allHelpData.RegisterAllHelps();
#endif
    }

    private void InitializeSingletonMonoBehaviour()
    {
        DontDestroyOnLoad(Instantiate(bgmManager).gameObject);
        DontDestroyOnLoad(Instantiate(seManager).gameObject);
        DontDestroyOnLoad(Instantiate(volumeController).gameObject);
    }
}
