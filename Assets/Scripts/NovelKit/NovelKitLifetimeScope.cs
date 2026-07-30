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
    [SerializeField] private NovelMessageView view;
    [SerializeField] private NovelKitPortraitView portraitView;
    [SerializeField] private NovelKitBackgroundView backgroundView;
    [SerializeField] private ScriptableCharacterCatalog catalog;
    [SerializeField] private string scenarioKey = "prologue";

    // Addressablesのアドレスがアセットパスそのものなので、ここまでを前置してシナリオ側のキーを短くする
    private const string SPRITE_ADDRESS_ROOT = "Assets/Sprites/";

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterNovelKit();
        builder.RegisterComponent(view).As<INovelView>().AsSelf();

        // novel-kit の既定 (Resourcesロード + 警告用no-op表示) を後勝ちで上書きする
        builder.RegisterInstance<ISpriteLoader>(new AddressablesSpriteLoader(SPRITE_ADDRESS_ROOT));
        builder.RegisterComponent(portraitView).As<IPortraitView>();
        builder.RegisterComponent(backgroundView).As<IBackgroundView>();

        builder.RegisterInstance<ICharacterCatalog>(catalog);
        builder.RegisterEntryPoint<NovelKitStarter>().WithParameter(scenarioKey);
        builder.RegisterEntryPoint<NovelKitAdvanceInput>();
    }
}
