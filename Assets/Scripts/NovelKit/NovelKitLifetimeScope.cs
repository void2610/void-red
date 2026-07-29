using Novel.Integration;
using Novel.Runtime;
using Novel.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// novel-kit を最小構成で配線するLifetimeScope
/// セリフ送りのみを担当し、フラグの永続化やView差し替えは未対応
/// </summary>
public class NovelKitLifetimeScope : LifetimeScope
{
    [SerializeField] private NovelMessageView view;
    [SerializeField] private NovelKitPortraitView portraitView;
    [SerializeField] private NovelKitBackgroundView backgroundView;
    [SerializeField] private ScriptableCharacterCatalog catalog;
    [SerializeField] private string scenarioKey = "prologue";

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterNovelKit();
        builder.RegisterComponent(view).As<INovelView>().AsSelf();
        // novel-kit のWarning実装を後勝ちで上書きし、立ち絵と背景を実表示する
        builder.Register<AddressableImageLoader>(Lifetime.Singleton);
        builder.RegisterComponent(portraitView).As<IPortraitView>();
        builder.RegisterComponent(backgroundView).As<IBackgroundView>();
        builder.RegisterInstance<ICharacterCatalog>(catalog);
        builder.RegisterEntryPoint<NovelKitStarter>().WithParameter(scenarioKey);
        builder.RegisterEntryPoint<NovelKitAdvanceInput>();
    }
}
