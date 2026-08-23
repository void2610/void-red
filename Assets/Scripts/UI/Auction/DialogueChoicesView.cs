using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// 対話フェーズの選択肢表示View
/// 4つのボタンを持つ対話選択View
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DialogueChoicesView : MonoBehaviour
{
    [SerializeField] private StaggeredSlideInGroup buttonStagger;
    [SerializeField] private List<Button> choiceButtons;

    private CanvasGroup _canvasGroup;
    private UniTaskCompletionSource<int> _selectionCompletionSource;

    /// <summary>
    /// 選択肢が押されるまで待機する
    /// </summary>
    /// <returns>押された選択肢の番号</returns>
    public UniTask<int> WaitForSelectionAsync() => _selectionCompletionSource.Task;

    public void Show()
    {
        _selectionCompletionSource = new UniTaskCompletionSource<int>();
        foreach (var button in choiceButtons)
            button.interactable = true;
        _canvasGroup.Show();
        buttonStagger.Play();
    }

    /// <summary>
    /// 指定インデックスのボタンのみ選択可能にする
    /// </summary>
    public void SetOnlyAllowed(int allowedIndex)
    {
        for (var i = 0; i < choiceButtons.Count; i++)
            choiceButtons[i].interactable = i == allowedIndex;
    }

    /// <summary>
    /// 使用済みのコマンドを個別に無効化する
    /// </summary>
    public void SetAvailability(System.Func<int, bool> isAvailable)
    {
        for (var i = 0; i < choiceButtons.Count; i++)
            choiceButtons[i].interactable = isAvailable(i);
    }

    public void SetInteractable(bool interactable)
    {
        foreach (var button in choiceButtons) button.interactable = interactable;
    }

    public void Hide()
    {
        buttonStagger.Cancel();
        _selectionCompletionSource?.TrySetCanceled();
        _canvasGroup.Hide();
    }

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.Hide();

        for (var i = 0; i < choiceButtons.Count; i++)
        {
            var index = i;
            choiceButtons[i].OnClickAsObservable()
                .Subscribe(_ => _selectionCompletionSource?.TrySetResult(index))
                .AddTo(this);
        }
    }

    private void OnDestroy()
    {
        _selectionCompletionSource?.TrySetCanceled();
    }
}
