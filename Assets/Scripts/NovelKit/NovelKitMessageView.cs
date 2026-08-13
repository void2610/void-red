using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using Novel.Runtime;
using Novel.View;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// novel-kit のINovelView実装
/// 文字送りの進行はnovel-kitのTextRevealEngineに委譲し、TMPへの反映と打鍵音・インジケーターだけを持つ
/// </summary>
public class NovelKitMessageView : MonoBehaviour, INovelView, INovelPlaybackSettings
{
    [SerializeField] private GameObject window;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI messageLabel;
    [SerializeField] private GameObject nextIndicator;
    [SerializeField] private RectTransform choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private Button autoButton;
    [SerializeField] private TextMeshProUGUI autoButtonText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Color autoButtonNormalColor = Color.white;
    [SerializeField] private Color autoButtonActiveColor = Color.yellow;
    [SerializeField] private float charsPerSecond = 33f;
    [SerializeField] private float autoAdvanceDelay = 3f;
    [SerializeField] private bool skipUnread = true;
    [SerializeField] private float shakeAmplitude = 3f;
    [SerializeField] private float waveAmplitude = 4f;
    [SerializeField] private float waveSpeed = 6f;
    [SerializeField] private float indicatorBounceDuration = 1f;
    [SerializeField] private float indicatorBounceAmplitude = 5f;
    [SerializeField] private Vector2 indicatorOffset = new(30f, 5f);
    [SerializeField] private string typingSeName = "Dialog2";
    [SerializeField] private NovelKitAudioChannel audioChannel;

    /// <summary>
    /// スキップが要求された（確認ダイアログは購読側が担当する）
    /// </summary>
    public Observable<Unit> OnSkipRequested => _onSkipRequested;

    public bool IsAutoMode => _engine.Auto;

    public float CharsPerSecond => charsPerSecond;
    // 打ち終わり時点のSE残量を足して、オートでもSEを鳴らし切ってから進むようにする
    public float AutoAdvanceDelay => autoAdvanceDelay + _pendingSeSeconds;
    public bool SkipUnread => skipUnread;

    private readonly Subject<Unit> _onSkipRequested = new();

    private TextRevealEngine _engine;
    private MotionHandle _indicatorMotion;
    private float _pendingSeSeconds;
    private bool _isWaitingForAdvance;

    public void ToggleAutoMode() => SetAutoMode(!_engine.Auto);

    public void RequestSkip() => _onSkipRequested.OnNext(Unit.Default);

    public void SetMessageWindowVisible(bool visible) => window.SetActive(visible);

    public void Advance()
    {
        // 送り待ち中のオート解除だけを横取りする。文字送り中は全文表示の要求として通す
        if (_engine.Auto && _isWaitingForAdvance)
        {
            SetAutoMode(false);
            return;
        }

        _engine.RequestAdvance();
    }

    /// <summary>
    /// 選択肢に当たるまで送りを飛ばす
    /// </summary>
    public void BeginSkip()
    {
        SetAutoMode(false);
        _engine.Skip = true;
    }

    public async UniTask ShowMessageAsync(NovelLine line, CancellationToken ct)
    {
        window.SetActive(true);
        nameLabel.text = line.DisplayName ?? "";
        HideNextIndicator();

        var tokens = NovelTagLexer.Parse(line.Text);
        _engine.Build(tokens);

        messageLabel.text = NovelDisplayText.Build(tokens);
        messageLabel.ForceMeshUpdate();
        var tmpTotal = messageLabel.textInfo.characterCount;
        messageLabel.maxVisibleCharacters = 0;

        using var animCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var anim = AnimateEffectsAsync(animCts.Token).SuppressCancellationThrow();
        var seCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        SeManager.Instance.PlaySeLoop(typingSeName, cancellationToken: seCts.Token).Forget();

        try
        {
            await _engine.RevealOnlyAsync(line.IsAlreadyRead,
                v => messageLabel.maxVisibleCharacters = Mathf.Min(v, tmpTotal), ct);
        }
        finally
        {
            // 打鍵音は送り待ちに入る前に止める
            seCts.Cancel();
            seCts.Dispose();
            animCts.Cancel();
        }

        await anim;   // 演出側の例外をここで回収する

        ShowNextIndicator();
        _isWaitingForAdvance = true;
        _pendingSeSeconds = audioChannel.SeRemainingSeconds;
        try
        {
            await _engine.WaitForAdvanceAsync(line.IsAlreadyRead, ct);
        }
        finally
        {
            _pendingSeSeconds = 0f;
            _isWaitingForAdvance = false;
            HideNextIndicator();
        }
    }

    public async UniTask<int> ShowChoicesAsync(IReadOnlyList<string> options, CancellationToken ct)
    {
        // 選択肢はプレイヤーの判断が要るのでスキップを打ち切る
        _engine.Skip = false;

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

    // shake/wave の頂点アニメ。区間はエンジンが可視index単位で算出済み
    private async UniTask AnimateEffectsAsync(CancellationToken ct)
    {
        var shake = _engine.ShakeSpans;
        var wave = _engine.WaveSpans;
        if (shake.Count == 0 && wave.Count == 0) return;

        while (!ct.IsCancellationRequested)
        {
            messageLabel.ForceMeshUpdate();
            var info = messageLabel.textInfo;
            var visible = messageLabel.maxVisibleCharacters;

            ApplyOffset(info, shake, visible, isWave: false);
            ApplyOffset(info, wave, visible, isWave: true);

            for (var m = 0; m < info.meshInfo.Length; m++)
            {
                messageLabel.UpdateGeometry(info.meshInfo[m].mesh, m);
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, ct);
        }
    }

    private void ApplyOffset(TMP_TextInfo info, IReadOnlyList<(int start, int end)> ranges, int visible, bool isWave)
    {
        foreach (var (start, end) in ranges)
        {
            for (var i = start; i < end && i < visible && i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;

                var offset = isWave
                    ? new Vector3(0f, Mathf.Sin(Time.time * waveSpeed + i * 0.5f) * waveAmplitude, 0f)
                    : new Vector3(UnityEngine.Random.Range(-shakeAmplitude, shakeAmplitude),
                        UnityEngine.Random.Range(-shakeAmplitude, shakeAmplitude), 0f);

                var verts = info.meshInfo[ch.materialReferenceIndex].vertices;
                var vi = ch.vertexIndex;
                for (var k = 0; k < 4; k++) verts[vi + k] += offset;
            }
        }
    }

    private void ShowNextIndicator()
    {
        nextIndicator.SetActive(true);
        PositionIndicatorAtLastCharacter();

        _indicatorMotion.TryCancel();

        var rt = (RectTransform)nextIndicator.transform;
        var originalPos = rt.anchoredPosition;
        _indicatorMotion = LMotion.Create(0f, 1f, indicatorBounceDuration)
            .WithLoops(-1, LoopType.Yoyo)
            .WithEase(Ease.InOutSine)
            .Bind(t =>
            {
                var pos = originalPos;
                pos.y += Mathf.Sin(t * Mathf.PI) * indicatorBounceAmplitude;
                rt.anchoredPosition = pos;
            })
            .AddTo(this);
    }

    private void HideNextIndicator()
    {
        _indicatorMotion.TryCancel();
        nextIndicator.SetActive(false);
    }

    // 最後の可視文字の右下に付ける。行数や文量で位置が変わるため毎回測り直す
    private void PositionIndicatorAtLastCharacter()
    {
        messageLabel.ForceMeshUpdate();
        var textInfo = messageLabel.textInfo;
        if (textInfo.characterCount == 0) return;

        var lastIndex = textInfo.characterCount - 1;
        while (lastIndex >= 0)
        {
            var info = textInfo.characterInfo[lastIndex];
            if (info.isVisible && !char.IsWhiteSpace(info.character)) break;
            lastIndex--;
        }
        if (lastIndex < 0) return;

        var lastChar = textInfo.characterInfo[lastIndex];
        var worldPos = messageLabel.rectTransform.TransformPoint(new Vector3(lastChar.topRight.x, lastChar.bottomRight.y, 0f));
        var rt = (RectTransform)nextIndicator.transform;
        var localPos = ((RectTransform)rt.parent).InverseTransformPoint(worldPos);
        rt.anchoredPosition = new Vector2(localPos.x, localPos.y) + indicatorOffset;
    }

    private void SetAutoMode(bool on)
    {
        _engine.Auto = on;
        autoButtonText.color = on ? autoButtonActiveColor : autoButtonNormalColor;
    }

    private void Awake()
    {
        _engine = new TextRevealEngine(this, new UnityFrameClock());
        autoButton.onClick.AddListener(ToggleAutoMode);
        skipButton.onClick.AddListener(RequestSkip);
        autoButtonText.color = autoButtonNormalColor;
    }

    private void OnDestroy()
    {
        _indicatorMotion.TryCancel();
        _onSkipRequested.Dispose();
    }
}
