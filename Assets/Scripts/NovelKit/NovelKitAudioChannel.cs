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

    public void PlayBgm(string bgmKey) { }
    public void StopBgm() { }

    // 再生完了まで待つとセリフ送りが止まるため、鳴らし始めたら即座に次へ進める
    public UniTask PlaySeAsync(string seKey, CancellationToken ct)
    {
        seManager.PlaySe(seKey);
        return UniTask.CompletedTask;
    }

    public async UniTask PlaySeLoopAsync(string seKey, float interval, int count, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            seManager.PlaySe(seKey);
            if (i < count - 1) await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);
        }
    }
}
