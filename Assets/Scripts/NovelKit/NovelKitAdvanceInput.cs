using System;
using Cysharp.Threading.Tasks;
using R3;
using VContainer.Unity;
using Void2610.SettingsSystem;
using Void2610.UnityTemplate;

/// <summary>
/// 送り / オート / スキップの入力をNovelKitMessageViewに橋渡しする
/// </summary>
public class NovelKitAdvanceInput : IStartable, IDisposable
{
    private readonly NovelKitMessageView _view;
    private readonly InputActionsProvider _inputActionsProvider;
    private readonly IConfirmationDialog _confirmationDialog;
    private readonly CompositeDisposable _disposables = new();

    public NovelKitAdvanceInput(NovelKitMessageView view, InputActionsProvider inputActionsProvider,
        IConfirmationDialog confirmationDialog)
    {
        _view = view;
        _inputActionsProvider = inputActionsProvider;
        _confirmationDialog = confirmationDialog;
    }

    private async UniTaskVoid ConfirmAndSkip()
    {
        var confirmed = await _confirmationDialog.ShowDialog("現在のシナリオをスキップしますか？", "スキップ", "キャンセル");
        if (!confirmed) return;

        _view.BeginSkip();
    }

    public void Start()
    {
        _inputActionsProvider.UI.Advance.OnPerformedAsObservable()
            .Subscribe(_ => _view.Advance())
            .AddTo(_disposables);

        // 確認ダイアログ等が開いている間は誤爆させない
        _inputActionsProvider.Novel.Auto.OnPerformedAsObservable()
            .Where(_ => !BaseWindowView.HasActiveWindows)
            .Subscribe(_ => _view.ToggleAutoMode())
            .AddTo(_disposables);

        _inputActionsProvider.Novel.Skip.OnPerformedAsObservable()
            .Where(_ => !BaseWindowView.HasActiveWindows)
            .Subscribe(_ => _view.RequestSkip())
            .AddTo(_disposables);

        _view.OnSkipRequested
            .Subscribe(_ => ConfirmAndSkip().Forget())
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
