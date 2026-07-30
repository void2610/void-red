using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using UnityEngine;

/// <summary>
/// novel-kit のIAudioChannel実装
/// 既存のNovelSeManagerへSE再生を委譲する (BGMはノベル側で扱わないため未対応)
/// </summary>
public class NovelKitAudioChannel : MonoBehaviour, IAudioChannel
{
    [SerializeField] private NovelSeManager seManager;

    private bool _bgmWarned;

    public void StopBgm() => WarnBgmUnsupported();
    public void PlayBgm(string bgmKey) => WarnBgmUnsupported();

    // 再生完了まで待つとセリフ送りが止まるため、鳴らし始めたら即座に次へ進める
    public UniTask PlaySeAsync(string seKey, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return UniTask.FromCanceled(ct);

        seManager.PlaySe(seKey);
        return UniTask.CompletedTask;
    }

    public async UniTask PlaySeLoopAsync(string seKey, float interval, int count, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            seManager.PlaySe(seKey);
            if (i < count - 1) await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct);
        }
    }

    // シナリオ側の bgm 呼び出しミスに気付けるよう、無音で捨てず一度だけ警告する
    private void WarnBgmUnsupported()
    {
        if (_bgmWarned) return;

        _bgmWarned = true;
        Debug.LogWarning("[NovelKitAudioChannel] BGM は未対応のため無視した");
    }
}
