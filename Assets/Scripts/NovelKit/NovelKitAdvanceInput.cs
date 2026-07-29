using System;
using Novel.View;
using R3;
using VContainer.Unity;

/// <summary>
/// 送り入力をNovelMessageViewに橋渡しする
/// </summary>
public class NovelKitAdvanceInput : IStartable, IDisposable
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
