using Novel.Addressables;
using Novel.Assets;
using Novel.Integration;
using Novel.Runtime;
using Novel.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// novel-kit を最小構成で配線するLifetimeScope
/// セリフ送り・立ち絵・背景を担当し、フラグの永続化は未対応
/// </summary>
public class NovelKitLifetimeScope : LifetimeScope
{
    // Addressablesのアドレスがアセットパスそのものなので、ここまでを前置してシナリオ側のキーを短くする
    private const string SpriteAddressRoot = "Assets/Sprites/";

    [SerializeField] private NovelMessageView view;
    [SerializeField] private PortraitStage portraitStage;
    [SerializeField] private DialogBackgroundView backgroundView;
    [SerializeField] private ScriptableCharacterCatalog catalog;
    [SerializeField] private string scenarioKey = "prologue";

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterNovelKit();
        builder.RegisterComponent(view).As<INovelView>().AsSelf();
        builder.RegisterComponent(portraitStage);
        builder.RegisterComponent(backgroundView);

        // novel-kit の警告用no-op実装を後勝ちで上書きし、立ち絵と背景を実表示する
        builder.RegisterInstance<ISpriteLoader>(new AddressablesSpriteLoader(SpriteAddressRoot));
        builder.Register<IPortraitView, NovelKitPortraitView>(Lifetime.Singleton);
        builder.Register<IBackgroundView, NovelKitBackgroundView>(Lifetime.Singleton);

        builder.RegisterInstance<ICharacterCatalog>(catalog);
        builder.RegisterEntryPoint<NovelKitStarter>().WithParameter(scenarioKey);
        builder.RegisterEntryPoint<NovelKitAdvanceInput>();
    }
}
