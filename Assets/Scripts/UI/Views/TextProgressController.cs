using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// テキスト表示の進行状態を管理するpure C#クラス
/// タイピングアニメーションと次への進行待機を制御する
/// </summary>
public class TextProgressController
{
    /// <summary>
    /// SEループ用のキャンセルトークン
    /// </summary>
    public CancellationToken DialogSeToken => _dialogSeCancellationTokenSource?.Token ?? CancellationToken.None;

    private CancellationTokenSource _typingCancellationTokenSource;
    private CancellationTokenSource _dialogSeCancellationTokenSource;
    private CancellationTokenSource _waitCancellationTokenSource;
    private bool _isTyping;
    private bool _isWaitingForNext;

    /// <summary>
    /// タイピングを開始し、キャンセルトークンを返す
    /// </summary>
    public CancellationToken BeginTyping()
    {
        _isTyping = true;
        _isWaitingForNext = false;

        _typingCancellationTokenSource?.Cancel();
        _typingCancellationTokenSource?.Dispose();
        _typingCancellationTokenSource = new CancellationTokenSource();

        // SEループ用のトークンも作成
        _dialogSeCancellationTokenSource?.Cancel();
        _dialogSeCancellationTokenSource?.Dispose();
        _dialogSeCancellationTokenSource = new CancellationTokenSource();

        return _typingCancellationTokenSource.Token;
    }

    /// <summary>
    /// タイピング完了を通知し、待機状態に遷移
    /// </summary>
    public void CompleteTyping()
    {
        _isTyping = false;
        _isWaitingForNext = true;

        _typingCancellationTokenSource?.Dispose();
        _typingCancellationTokenSource = null;

        // SEループを停止
        _dialogSeCancellationTokenSource?.Cancel();
        _dialogSeCancellationTokenSource?.Dispose();
        _dialogSeCancellationTokenSource = null;
    }

    /// <summary>
    /// 次への進行を待つ（手動進行モード用）
    /// </summary>
    public async UniTask WaitForNext()
    {
        _waitCancellationTokenSource = new CancellationTokenSource();
        try
        {
            while (_isWaitingForNext)
            {
                await UniTask.Yield(_waitCancellationTokenSource.Token);
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            _waitCancellationTokenSource?.Dispose();
            _waitCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// タイムアウト付きで次への進行を待つ（自動進行モード用）
    /// </summary>
    public async UniTask WaitForNextWithTimeout(float duration)
    {
        if (!_isWaitingForNext) return;

        _waitCancellationTokenSource = new CancellationTokenSource();
        try
        {
            await UniTask.Delay((int)(duration * 1000), cancellationToken: _waitCancellationTokenSource.Token);

            // タイムアウト後もまだ待機中の場合は自動で進む
            if (_isWaitingForNext) _isWaitingForNext = false;
        }
        catch (System.OperationCanceledException)
        {
            // ユーザーがクリックして手動で進んだ場合
        }
        finally
        {
            _waitCancellationTokenSource?.Dispose();
            _waitCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// ユーザー入力による次への進行
    /// タイピング中の場合はスキップ、待機中の場合は進行
    /// </summary>
    public void AdvanceToNext()
    {
        if (_isTyping)
        {
            // 文字送り中の場合はスキップして待機状態へ
            SkipTyping();
            _isWaitingForNext = true;
            return;
        }

        if (!_isWaitingForNext) return;

        // 待機をキャンセル（自動進行の場合のタイムアウトをキャンセル）
        _waitCancellationTokenSource?.Cancel();

        // 次へ進む
        _isWaitingForNext = false;
    }

    /// <summary>
    /// タイピングアニメーションをスキップ
    /// </summary>
    private void SkipTyping()
    {
        if (!_isTyping) return;

        _typingCancellationTokenSource?.Cancel();
        _typingCancellationTokenSource?.Dispose();
        _typingCancellationTokenSource = null;

        // SEループを停止
        _dialogSeCancellationTokenSource?.Cancel();
        _dialogSeCancellationTokenSource?.Dispose();
        _dialogSeCancellationTokenSource = null;

        _isTyping = false;
    }

    /// <summary>
    /// すべてのキャンセレーショントークンをクリーンアップ
    /// </summary>
    public void Dispose()
    {
        _typingCancellationTokenSource?.Cancel();
        _typingCancellationTokenSource?.Dispose();
        _typingCancellationTokenSource = null;

        _dialogSeCancellationTokenSource?.Cancel();
        _dialogSeCancellationTokenSource?.Dispose();
        _dialogSeCancellationTokenSource = null;

        _waitCancellationTokenSource?.Cancel();
        _waitCancellationTokenSource?.Dispose();
        _waitCancellationTokenSource = null;

        _isTyping = false;
        _isWaitingForNext = false;
    }
}
