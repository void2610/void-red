#if UNITY_EDITOR || DEVELOPMENT_BUILD || LIMINAL_PALETTE_FORCE_ENABLE
using VContainer;
using VContainer.Unity;
using Void2610.LiminalPalette.Integration.VContainer;

/// <summary>
/// 開発時専用のデバッグ DI スコープ
/// DebugBootstrap が各シーン読込み時に動的生成し、VContainerSettings の RootLifetimeScope を親として構築される
/// このプロジェクトは asmdef を持たないため、defineConstraints の代わりにファイル全体を #if で本番除外する
/// </summary>
public sealed class DebugLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<LiminalPaletteEntryPoint>();
        builder.Register<GameProgressDebugCommands>(Lifetime.Singleton);
        builder.Register<SceneDebugCommands>(Lifetime.Singleton);
    }
}
#endif
