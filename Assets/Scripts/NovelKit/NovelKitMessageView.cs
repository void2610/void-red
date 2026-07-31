using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// novel-kit のINovelView実装
/// 参考実装のNovelMessageViewは文字送りと送り待ちを1つのawaitに畳んでおり、文字送り音を打ち終わりで止められないため自前で持つ
/// </summary>
public class NovelKitMessageView : MonoBehaviour, INovelView
{
    [SerializeField] private GameObject window;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI messageLabel;
    [SerializeField] private RectTransform choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private float charSpeed = 0.03f;
    [SerializeField] private float autoNextDelay = 3f;
    [SerializeField] private string typingSeName = "Dialog2";

    /// <summary>
    /// スキップが要求された（確認ダイアログは購読側が担当する）
    /// </summary>
    public Observable<Unit> OnSkipRequested => _onSkipRequested;

    public bool IsAutoMode => _isAutoMode;

    private readonly TextProgressController _progress = new();
    private readonly Subject<Unit> _onSkipRequested = new();

    private bool _isAutoMode;
    private bool _isSkipMode;

    public void ToggleAutoMode() => SetAutoMode(!_isAutoMode);

    public void RequestSkip() => _onSkipRequested.OnNext(Unit.Default);

    public void SetMessageWindowVisible(bool visible) => window.SetActive(visible);

    public void Advance()
    {
        // オート中の送り入力はオートを解除する（意図しない自動進行を止める）
        if (_isAutoMode && _progress.IsWaitingForNext)
        {
            SetAutoMode(false);
            return;
        }

        _progress.AdvanceToNext();
    }

    /// <summary>
    /// 選択肢に当たるまで送りを飛ばす
    /// </summary>
    public void BeginSkip()
    {
        _isSkipMode = true;
        SetAutoMode(false);
        _progress.ForceComplete();
    }

    public async UniTask ShowMessageAsync(NovelLine line, CancellationToken ct)
    {
        window.SetActive(true);
        nameLabel.text = line.DisplayName ?? "";

        var message = NovelDisplayText.Build(NovelTagLexer.Parse(line.Text));

        if (_isSkipMode)
        {
            // 全文を即座に出し、待たずに次へ（1フレームだけ送って表示を反映させる）
            messageLabel.text = message;
            messageLabel.maxVisibleCharacters = int.MaxValue;
            await UniTask.NextFrame(ct);
            return;
        }

        var typingToken = _progress.BeginTyping();

        SeManager.Instance.PlaySeLoop(typingSeName, cancellationToken: _progress.DialogSeToken).Forget();

        try
        {
            await messageLabel.RichTextTypewriterAnimation(message, charSpeed, typingToken);
        }
        catch (OperationCanceledException)
        {
            // 文字送り中の送り入力でキャンセルされた場合は全文を即座に表示
            messageLabel.maxVisibleCharacters = int.MaxValue;
        }

        // SEループもここで停止する
        _progress.CompleteTyping();

        await WaitForAdvanceAsync();
    }

    public async UniTask<int> ShowChoicesAsync(IReadOnlyList<string> options, CancellationToken ct)
    {
        // 選択肢はプレイヤーの判断が要るのでスキップを打ち切る
        _isSkipMode = false;

        var tcs = new UniTaskCompletionSource<int>();
        var spawned = new List<GameObject>(options.Count);

        for (var i = 0; i < options.Count; i++)
        {
            var index = i;
            var button = Instantiate(choiceButtonPrefab, choiceContainer);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = options[i];
            button.onClick.AddListener(() => tcs.TrySetResult(index));
            spawned.Add(button.gameObject);
        }

        try
        {
            using (ct.Register(() => tcs.TrySetCanceled()))
                return await tcs.Task;
        }
        finally
        {
            foreach (var go in spawned) Destroy(go);
        }
    }

    public void ClearMessage()
    {
        nameLabel.text = "";
        messageLabel.text = "";
    }

    // オート中は一定時間で自動送り。待機中に解除されたら手動待ちへ落とす
    private async UniTask WaitForAdvanceAsync()
    {
        while (_isAutoMode)
        {
            await _progress.WaitForNextWithTimeout(autoNextDelay);
            if (!_progress.IsWaitingForNext) return;
        }

        await _progress.WaitForNext();
    }

    private void SetAutoMode(bool on)
    {
        if (_isAutoMode == on) return;

        _isAutoMode = on;

        // 待機中の切り替えは進行中のタイムアウトを取り消して待ち方を組み直す
        if (_progress.IsWaitingForNext) _progress.CancelWait();
    }

    private void OnDestroy()
    {
        _onSkipRequested.Dispose();
        _progress.Dispose();
    }
}
