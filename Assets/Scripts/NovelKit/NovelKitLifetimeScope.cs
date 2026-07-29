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
    [SerializeField] private ScriptableCharacterCatalog catalog;
    [SerializeField] private string scenarioKey = "prologue";

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterNovelKit();
        builder.RegisterComponent(view).As<INovelView>().AsSelf();
        builder.RegisterInstance<ICharacterCatalog>(catalog);
        builder.RegisterEntryPoint<NovelKitStarter>().WithParameter(scenarioKey);
        builder.RegisterEntryPoint<NovelKitAdvanceInput>();
    }
}
