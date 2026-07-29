using Cysharp.Threading.Tasks;
using Novel.Integration;
using Novel.Runtime;
using Novel.View;
using R3;
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
        // RootLifetimeScopeを持たない単体再生でも動くよう、入力を自前で供給する
        builder.Register<InputActionsProvider>(Lifetime.Singleton);

        builder.RegisterNovelKit();
        builder.RegisterComponent(view).As<INovelView>().AsSelf();
        builder.RegisterInstance<ICharacterCatalog>(catalog);
        builder.RegisterEntryPoint<NovelKitStarter>().WithParameter(scenarioKey);
        builder.RegisterEntryPoint<NovelKitAdvanceInput>();
    }
}

/// <summary>
/// 起動時に指定シナリオの再生を開始する
/// </summary>
public class NovelKitStarter : IStartable
{
    private readonly INovelScenarioRunner _runner;
    private readonly string _scenarioKey;

    public NovelKitStarter(INovelScenarioRunner runner, string scenarioKey)
    {
        _runner = runner;
        _scenarioKey = scenarioKey;
    }

    public void Start()
    {
        _runner.PlayAsync(_scenarioKey, default).Forget();
    }
}

/// <summary>
/// 送り入力をNovelMessageViewに橋渡しする
/// </summary>
public class NovelKitAdvanceInput : IStartable, System.IDisposable
{
    private readonly NovelMessageView _view;
    private readonly InputActionsProvider _inputActionsProvider;
    private readonly CompositeDisposable _disposables = new();

    public NovelKitAdvanceInput(NovelMessageView view, InputActionsProvider inputActionsProvider)
    {
        _view = view;
        _inputActionsProvider = inputActionsProvider;
    }

    public void Start()
    {
        _inputActionsProvider.UI.Advance.OnPerformedAsObservable()
            .Subscribe(_ => _view.Advance())
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
