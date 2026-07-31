using Novel.Addressables;
using Novel.Assets;
using Novel.Integration;
using Novel.Runtime;
using Novel.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// novel-kit を配線するLifetimeScope
/// 再生するシナリオは通常ストーリー進行から決まり、scenarioKeyOverride を入れた時だけ単体再生になる
/// </summary>
public class NovelKitLifetimeScope : LifetimeScope
{
    [SerializeField] private NovelMessageView view;
    [SerializeField] private NovelKitPortraitView portraitView;
    [SerializeField] private NovelKitBackgroundView backgroundView;
    [SerializeField] private NovelKitAudioChannel audioChannel;
    [SerializeField] private ScriptableCharacterCatalog catalog;

    [Tooltip("空ならストーリー進行に従う。入れるとそのシナリオを単体再生し、進行もシーン遷移も行わない")]
    [SerializeField] private string scenarioKeyOverride = "";

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
        builder.RegisterComponent(audioChannel).As<IAudioChannel>();

        builder.RegisterInstance<ICharacterCatalog>(catalog);
        builder.RegisterEntryPoint<NovelKitStarter>().WithParameter(scenarioKeyOverride);
        builder.RegisterEntryPoint<NovelKitAdvanceInput>();
    }
}
