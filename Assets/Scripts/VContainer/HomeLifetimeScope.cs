using VContainer;
using VContainer.Unity;

/// <summary>
/// ホームシーン用のLifetimeScope
/// ホームシーン固有のコンポーネントを登録
/// </summary>
public class HomeLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<HomeView>();

        builder.RegisterSettingsFeature();
        builder.RegisterEntryPoint<HelpPresenter>();

        // ホームPresenter
        builder.RegisterEntryPoint<HomePresenter>();
    }
}
