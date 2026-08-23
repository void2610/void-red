using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 対話フェーズの View
/// 4 つの対話コマンドを選ばせ、相手の反応をカットインで見せる
/// </summary>
public class DialoguePhaseView : BasePhaseView
{
    [SerializeField] private DialogueChoicesView choicesView;
    [SerializeField] private DialogueCutInView cutInView;

    [Header("立ち絵")]
    [SerializeField] private DialoguePortraitView portraitView;
    [SerializeField] private Sprite playerPortraitSprite;

    private ParticipantData _target;

    public void SetChoicesInteractable(bool interactable) => choicesView.SetInteractable(interactable);

    public void SetCommandAvailability(Func<int, bool> isAvailable) => choicesView.SetAvailability(isAvailable);

    /// <summary>
    /// 対話コマンドが選ばれるまで待つ。戻り値は DialogueCommand の並び順
    /// </summary>
    public async UniTask<int> WaitForCommandAsync()
    {
        choicesView.Show();
        try
        {
            return await choicesView.WaitForSelectionAsync();
        }
        finally
        {
            choicesView.Hide();
        }
    }

    /// <summary>
    /// 対話の相手を切り替える (立ち絵とカットイン素材)
    /// </summary>
    public async UniTask SetTargetAsync(ParticipantData target)
    {
        _target = target;
        await portraitView.ChangePortrait(target != null ? target.Portrait : playerPortraitSprite);
    }

    /// <summary>相手のセリフをカットインで見せる</summary>
    public async UniTask ShowTargetLineAsync(string text)
    {
        portraitView.SlideOut();
        await cutInView.PlayCutInAsync(_target.Portrait, _target.CutInSprite, text);
        portraitView.SlideIn();
    }

    /// <summary>主人公のセリフをカットインで見せる</summary>
    public async UniTask ShowPlayerLineAsync(string text)
    {
        portraitView.SlideOut();
        await cutInView.PlayPlayerCutInAsync(text);
        portraitView.SlideIn();
    }

    public override void Show()
    {
        base.Show();
        portraitView.SlideIn();
    }

    public override void Hide()
    {
        portraitView.SlideOut();
        base.Hide();
    }
}
