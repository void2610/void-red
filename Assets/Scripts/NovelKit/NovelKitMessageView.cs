using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
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
    [SerializeField] private string typingSeName = "Dialog2";

    private readonly TextProgressController _progress = new();

    public void Advance() => _progress.AdvanceToNext();

    public void SetMessageWindowVisible(bool visible) => window.SetActive(visible);

    public async UniTask ShowMessageAsync(NovelLine line, CancellationToken ct)
    {
        window.SetActive(true);
        nameLabel.text = line.DisplayName ?? "";

        var message = NovelDisplayText.Build(NovelTagLexer.Parse(line.Text));
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

        await _progress.WaitForNext();
    }

    public async UniTask<int> ShowChoicesAsync(IReadOnlyList<string> options, CancellationToken ct)
    {
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

    private void OnDestroy()
    {
        _progress.Dispose();
    }
}
