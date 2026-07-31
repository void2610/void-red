using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
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

    private bool _isConfirming;

    public NovelKitAdvanceInput(NovelKitMessageView view, InputActionsProvider inputActionsProvider,
        IConfirmationDialog confirmationDialog)
    {
        _view = view;
        _inputActionsProvider = inputActionsProvider;
        _confirmationDialog = confirmationDialog;
    }

    // ボタンとキーの両方から要求が来るため、確認中の再要求は捨てる
    private async UniTask ConfirmAndSkip()
    {
        if (_isConfirming) return;

        _isConfirming = true;
        try
        {
            var confirmed = await _confirmationDialog.ShowDialog("現在のシナリオをスキップしますか？", "スキップ", "キャンセル");
            if (confirmed) _view.BeginSkip();
        }
        finally
        {
            _isConfirming = false;
        }
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
            .Subscribe(_ => ConfirmAndSkip().Forget(Debug.LogException))
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
