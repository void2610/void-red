using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Void2610.UnityTemplate;

/// <summary>
/// オークションの進行役
/// フェーズの実体は IAuctionPhase 群が持ち、ここは「今の状態で実行できるフェーズを回す」だけを担う
/// フェーズを増やすときは AuctionFlow に足す
/// </summary>
public class AuctionPresenter : IStartable, IDisposable
{
    /// <summary>いま走っているフェーズ (進行が止まったときの手掛かり)</summary>
    public string CurrentPhaseName { get; private set; } = "";
    private readonly AuctionContext _context;
    private readonly IReadOnlyList<IAuctionPhase> _phases;
    private readonly CancellationTokenSource _cts = new();
    private readonly PhaseLoopGuard _guard = new();

    public AuctionPresenter(AuctionSceneView view, AuctionSession session, GameProgressService progress, SceneTransitionManager sceneTransition)
    {
        _context = new AuctionContext(session, view, progress, sceneTransition);
        _phases = AuctionFlow.CreatePhases();
    }

    private async UniTask RunAsync(CancellationToken ct)
    {
        try
        {
            while (_context.Session.Phase != AuctionPhase.Finished)
            {
                var phase = _phases.FirstOrDefault(p => p.CanRun(_context.Session));
                if (phase == null) throw new InvalidOperationException($"進行できるフェーズが無い: {_context.Session.Phase}");

                CurrentPhaseName = phase.GetType().Name;
                if (_guard.IsStuck(CurrentPhaseName, _context.Session.Phase, _context.Session.CurrentLotIndex)) throw new InvalidOperationException($"進行が同じ状態で止まっている: {_guard.Describe()}");

                await phase.RunAsync(_context, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (InvalidOperationException) when (ct.IsCancellationRequested)
        {
            // シーン破棄でボタンの Observable が完了する。進行の中断として扱う
        }
        catch (Exception e)
        {
            // 進行が黙って止まると画面が操作できないまま戻れなくなる。ログに残してロビーへ帰す
            Debug.LogError($"[AuctionPresenter] 進行が停止した ({CurrentPhaseName}): {e}");
            await ReturnToLobbyAsync();
        }
    }

    /// <summary>
    /// 進行が続けられなくなったときの逃げ道。シーンが生きているうちにロビーへ戻す
    /// </summary>
    private async UniTask ReturnToLobbyAsync()
    {
        if (_cts.IsCancellationRequested) return;

        try
        {
            await _context.SceneTransition.TransitionToSceneWithFade(SceneType.Home);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuctionPresenter] ロビーへ戻れなかった: {e}");
        }
    }

    public void Start()
    {
        _context.View.Initialize(_context.Session);
        BgmManager.Instance.PlayBGM("Battle");
        RunAsync(_cts.Token).Forget();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

/// <summary>
/// フェーズの並び順。上から順に「今実行できるもの」が選ばれる
/// </summary>
public static class AuctionFlow
{
    public static IReadOnlyList<IAuctionPhase> CreatePhases() => new IAuctionPhase[]
    {
        new ThemeAnnouncePhase(),
        new DialoguePhase(),
        new BiddingPhase(),
        new CompetitionPhase(),
        new RevealPhase(),
        new LotResultPhase(),
        new BaptismPhase(),
        new GameOverPhase(),
        new LotRevealPhase(),
    };
}
