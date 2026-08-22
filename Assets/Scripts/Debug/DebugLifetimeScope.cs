using UnityEngine;
using VContainer;
using VContainer.Unity;
using Void2610.LiminalPalette;
using Void2610.LiminalPalette.Integration.VContainer;

/// <summary>
/// 開発時専用のデバッグ DI スコープ
/// 登録は全て Root シングルトンのみに依存するため、DebugBootstrap 経由で初回に 1 度だけ生成し、
/// DontDestroyOnLoad で全シーンに常駐する (VContainerSettings の RootLifetimeScope を親として構築)
/// VoidRed.Debug asmdef は defineConstraints で本番ビルドから除外される
/// (LiminalPalette 本体のランタイムは本番にも同梱されるが、ProductionGuard と Runtime.Ipc の defineConstraints で無効化される)
/// </summary>
public sealed class DebugLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<LiminalPaletteEntryPoint>();
        builder.Register<GameProgressDebugCommands>(Lifetime.Singleton);
        builder.Register<SceneDebugCommands>(Lifetime.Singleton);
        builder.Register<NovelDebugCommands>(Lifetime.Singleton);
        builder.Register<SettingsDebugCommands>(Lifetime.Singleton);
        builder.Register<AudioDebugCommands>(Lifetime.Singleton);
        builder.Register<UiDebugCommands>(Lifetime.Singleton);
        builder.Register<ExhibitDebugCommands>(Lifetime.Singleton);
        builder.Register<AuctionDebugCommands>(Lifetime.Singleton);
        builder.Register<LobbyDebugCommands>(Lifetime.Singleton);
    }

    protected override void Awake()
    {
        base.Awake();
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);
    }

    protected override void OnDestroy()
    {
        // Reload Domain off では static が Play 終了後も残るため、破棄済みコンテナへの参照を確実に断つ
        LiminalPalette.SetInstanceResolver(null);
        base.OnDestroy();
    }
}
