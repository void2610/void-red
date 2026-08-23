using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// フェーズViewの共通基底クラス
/// CanvasGroupベースでフェード表示/非表示を切り替える
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class BasePhaseView : MonoBehaviour
{
    private const float DEFAULT_FADE_DURATION = 0.3f;

    protected CanvasGroup CanvasGroup { get; private set; }

    public virtual void Show() => CanvasGroup.FadeIn(DEFAULT_FADE_DURATION);

    public virtual void Hide() => CanvasGroup.FadeOut(DEFAULT_FADE_DURATION);

    protected virtual void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
        // 初期状態は即時非表示
        CanvasGroup.Hide();
    }
}
