using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;
using Void2610.UnityTemplate;

/// <summary>
/// 1 階層分のオークションを旧 UI 資産 (感情ホイール / 入札ウィンドウ / 対話カットイン / 天秤) で進行させる
/// テーマ公開 → 5 ロット (対話 → 入札 → 開示 → 競合) → 洗礼 or ゲームオーバー
/// </summary>
public class AuctionPresenter : IStartable, IDisposable
{
    private readonly AuctionSceneView _scene;
    private readonly AuctionSession _session;
    private readonly GameProgressService _progress;
    private readonly SceneTransitionManager _sceneTransition;
    private readonly CancellationTokenSource _cts = new();
    private readonly EmotionBid _draft = new();

    private EmotionType _selectedEmotion = EmotionType.Joy;
    private WonLot _chosen;

    public AuctionPresenter(AuctionSceneView scene, AuctionSession session, GameProgressService progress, SceneTransitionManager sceneTransition)
    {
        _scene = scene;
        _session = session;
        _progress = progress;
        _sceneTransition = sceneTransition;
    }

    private async UniTask RunAsync(CancellationToken ct)
    {
        try
        {
            await _scene.Theme.DisplayThemeWithKeywords(_session.Floor.ThemeTitle, true);
            while (!_session.IsLastLot) await RunLotAsync(ct);
            _session.FinishLots();

            if (_session.IsPlayerGameOver)
            {
                await RunGameOverAsync(ct);
                return;
            }
            await RunBaptismAsync(ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (InvalidOperationException) when (ct.IsCancellationRequested)
        {
            // シーン破棄でボタンの Observable が完了する。進行の中断として扱う
        }
    }

    private async UniTask RunLotAsync(CancellationToken ct)
    {
        var lot = _session.BeginNextLot();
        _scene.RefreshParticipants();
        await _scene.Announcement.DisplayAnnouncement($"第 {_session.CurrentLotIndex + 1} 競売\n『{lot.Title}』", 1.6f);
        _scene.Auction.Show();
        _scene.Auction.ShowLot(lot);

        await RunDialogueAsync(ct);
        await RunBiddingAsync(ct);

        var reveal = _session.LastReveal;
        var rivalTop = reveal.Bidders.Where(b => !b.IsPlayer).Select(b => b.SubmittedBid.Total).DefaultIfEmpty(0).Max();
        _scene.Auction.ShowBids(BidBreakdown(_session.Player.SubmittedBid), rivalTop);
        _scene.ShowParticipantBids(reveal);
        await UniTask.Delay(900, cancellationToken: ct);

        AuctionParticipant winner;
        if (reveal.IsTie)
        {
            await _scene.Auction.ShowResultAsync(false, true, false, RivalColor());
            winner = await RunCompetitionAsync(ct);
        }
        else
        {
            winner = _session.ResolveReveal();
            await _scene.Auction.ShowResultAsync(winner != null && winner.IsPlayer, false, winner == null, RivalColor());
        }

        _scene.RefreshParticipants();
        _scene.HighlightWinner(winner);
        await _scene.Announcement.DisplayAnnouncement(winner != null ? $"{winner.DisplayName} が落札" : "流札", 1.4f);
        _scene.Auction.Clear();
        _scene.Auction.Hide();
    }

    private async UniTask RunDialogueAsync(CancellationToken ct)
    {
        _scene.Dialogue.Show();
        var target = _session.Rivals.First(r => r.CanBid);
        await SelectTargetAsync(target);

        while (true)
        {
            _scene.SetTargetSelectable(true);
            _scene.Dialogue.SetCommandAvailability(i => _session.CanUseDialogue(CurrentTarget(), (DialogueCommand)i));
            _scene.Auction.SetConfirmInteractable(true);

            var picked = await UniTask.WhenAny(
                _scene.Dialogue.WaitForCommandAsync(),
                _scene.WaitTargetChangedAsync(ct),
                _scene.Auction.OnBiddingConfirmed.FirstAsync(ct).AsUniTask());

            if (picked.winArgumentIndex == 2) break;
            if (picked.winArgumentIndex == 1)
            {
                await SelectTargetAsync(_scene.SelectedTarget);
                continue;
            }

            var command = (DialogueCommand)picked.result1;
            var current = CurrentTarget();
            if (!_session.CanUseDialogue(current, command)) continue;

            _scene.SetTargetSelectable(false);
            _scene.Auction.SetConfirmInteractable(false);
            var outcome = _session.UseDialogue(current, command);
            await PlayDialogueOutcomeAsync(outcome, ct);
        }

        _scene.SetTargetSelectable(false);
        _scene.Dialogue.Hide();
        _session.EnterBidding();
    }

    private async UniTask PlayDialogueOutcomeAsync(DialogueOutcome outcome, CancellationToken ct)
    {
        await _scene.Dialogue.ShowPlayerLineAsync(PlayerLine(outcome.Command));
        await _scene.Dialogue.ShowTargetLineAsync(outcome.Line);

        if (outcome.ObservedTotal.HasValue)
            await _scene.Announcement.DisplayAnnouncement($"{outcome.Target.DisplayName} の入札予定: {outcome.ObservedTotal.Value} 枚", 1.5f);
        else if (!outcome.Success)
            await _scene.Announcement.DisplayAnnouncement("手応えがない", 1.2f);

        if (outcome.Counter == null) return;

        // 逆対話: 相手の問いかけに二択で答える
        await _scene.Dialogue.ShowTargetLineAsync(outcome.Counter.Prompt);
        var choice = await _scene.WaitCounterChoiceAsync(outcome.Counter, ct);
        _session.AnswerCounterDialogue(outcome.Target, choice);
        await _scene.Dialogue.ShowPlayerLineAsync(choice == 0 ? outcome.Counter.ChoiceA : outcome.Counter.ChoiceB);
    }

    private async UniTask RunBiddingAsync(CancellationToken ct)
    {
        _draft.Clear();
        _selectedEmotion = EmotionType.Joy;
        _scene.Auction.SetSelectedEmotion(_selectedEmotion);
        _scene.Auction.UpdateEmotionResources(Remaining());
        _scene.Auction.ShowBidWindow(_session.CurrentLot, _selectedEmotion, 0);
        _scene.Auction.SetEmotionInteractable(true);
        _scene.Auction.SetConfirmInteractable(true);
        UpdateBidInteractables();

        using var d = new CompositeDisposable();
        _scene.Auction.OnEmotionSelected.Subscribe(e =>
        {
            _selectedEmotion = e;
            _scene.Auction.SetBidEmotion(e);
            _scene.Auction.UpdateBidAmount(_draft.Get(e));
            UpdateBidInteractables();
        }).AddTo(d);

        _scene.Auction.OnIncrease.Subscribe(_ =>
        {
            if (_draft.Get(_selectedEmotion) >= _session.Player.Wallet.Get(_selectedEmotion)) return;
            _draft.Add(_selectedEmotion);
            SeManager.Instance.PlaySe(_selectedEmotion.ToResourceSeName(), pitch: 1f);
            AfterBidChanged();
        }).AddTo(d);

        _scene.Auction.OnDecrease.Subscribe(_ =>
        {
            if (_draft.Get(_selectedEmotion) <= 0) return;
            _draft.Add(_selectedEmotion, -1);
            AfterBidChanged();
        }).AddTo(d);

        await _scene.Auction.OnBiddingConfirmed.FirstAsync(ct);
        _scene.Auction.HideBidWindow();
        _scene.Auction.SetEmotionInteractable(false);
        _session.SubmitPlayerBid(_draft);
    }

    private async UniTask<AuctionParticipant> RunCompetitionAsync(CancellationToken ct)
    {
        _session.StartCompetition(Time.time);
        var competition = _session.Competition;
        var playerCompeting = competition.Competitors.Contains(_session.Player);
        var rivalTop = competition.Competitors.Where(c => !c.IsPlayer).Select(competition.TotalOf).DefaultIfEmpty(0).Max();

        _scene.Competition.Initialize(playerCompeting ? competition.TotalOf(_session.Player) : 0, rivalTop, Remaining());
        _scene.Competition.SetInstruction(playerCompeting ? "上乗せして競り勝て" : "競合を見守る");
        _scene.Competition.SetEmotionInteractable(playerCompeting);
        _scene.Competition.SetRaiseInteractable(playerCompeting);

        using var d = new CompositeDisposable();
        _scene.Competition.OnEmotionSelected.Subscribe(e => _selectedEmotion = e).AddTo(d);
        _scene.Competition.OnRaise.Subscribe(_ =>
        {
            if (!_session.TryPlayerRaise(_selectedEmotion, Time.time)) return;
            SeManager.Instance.PlaySe(_selectedEmotion.ToResourceSeName(), pitch: 1f);
            _scene.Competition.UpdateResources(Remaining());
            _scene.RefreshParticipants();
        }).AddTo(d);

        var npcNext = competition.Competitors.Where(c => !c.IsPlayer).ToDictionary(c => c, _ => Time.time + NextNpcInterval());
        while (!competition.IsTimedOut(Time.time) && !competition.IsDeadlocked())
        {
            foreach (var npc in npcNext.Keys.ToList())
            {
                if (Time.time < npcNext[npc]) continue;
                if (_session.TryNpcRaise(npc, Time.time)) SeManager.Instance.PlaySe("SE_RESOURCE_ANGER", pitch: 1f);
                npcNext[npc] = Time.time + NextNpcInterval();
            }
            var top = competition.Competitors.Where(c => !c.IsPlayer).Select(competition.TotalOf).DefaultIfEmpty(0).Max();
            _scene.Competition.UpdateBids(competition.TotalOf(_session.Player), top);
            _scene.Competition.UpdateTimer(competition.RemainingSeconds(Time.time), competition.TimeoutSeconds);
            _scene.ShowCompetitionTotals(competition);
            if (playerCompeting) _scene.Competition.SetRaiseInteractable(_session.Player.Wallet.Total > 0);
            await UniTask.Yield(ct);
        }

        _scene.Competition.Hide();
        var winner = _session.ResolveCompetition();
        await _scene.Auction.ShowResultAsync(winner != null && winner.IsPlayer, false, false, RivalColor());
        return winner;
    }

    private async UniTask RunBaptismAsync(CancellationToken ct)
    {
        _chosen = null;
        await _scene.Announcement.DisplayAnnouncement("洗礼", 1.6f);
        _scene.Baptism.Show(_session);

        using var d = new CompositeDisposable();
        _scene.Baptism.OnIntegrate.Subscribe(w =>
        {
            _chosen = w;
            _scene.Baptism.SetSelected(w);
        }).AddTo(d);

        await _scene.Baptism.OnFinish.Where(_ => _chosen != null).FirstAsync(ct);
        _session.Finish();
        var collapsed = _session.Rivals.Where(r => r.HasCollapsed).Select(r => r.Data.ParticipantId);
        _progress.RecordAuctionClearAndSave(_session.Player.Wallet, _chosen, _session.Player.WonLots, collapsed);
        await _sceneTransition.TransitionToSceneWithFade(SceneType.Home);
    }

    private async UniTask RunGameOverAsync(CancellationToken ct)
    {
        _scene.GameOver.Show(_session.Floor.FloorIndex, _session.MissedKey);
        var toRetry = await Observable.Merge(
            _scene.GameOver.OnRetry.Select(_ => true),
            _scene.GameOver.OnLobby.Select(_ => false)).FirstAsync(ct);
        // 進行度は変えず、同じ階層をやり直す
        await _sceneTransition.TransitionToSceneWithFade(toRetry ? SceneType.Auction : SceneType.Home);
    }

    private async UniTask SelectTargetAsync(AuctionParticipant target)
    {
        _scene.SetSelectedTarget(target);
        await _scene.Dialogue.SetTargetAsync(target.Data);
        await _scene.Rival.ChangePortraitAsync(target.Data.Portrait);
    }

    private AuctionParticipant CurrentTarget()
    {
        return _scene.SelectedTarget;
    }

    private void AfterBidChanged()
    {
        _scene.Auction.UpdateBidAmount(_draft.Get(_selectedEmotion));
        _scene.Auction.UpdateEmotionResources(Remaining());
        UpdateBidInteractables();
    }

    private void UpdateBidInteractables()
    {
        _scene.Auction.SetIncreaseInteractable(_draft.Get(_selectedEmotion) < _session.Player.Wallet.Get(_selectedEmotion));
    }

    private Dictionary<EmotionType, int> Remaining()
    {
        return EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, e => _session.Player.Wallet.Get(e) - _draft.Get(e));
    }

    private static Dictionary<EmotionType, int> BidBreakdown(EmotionBid bid)
    {
        return EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, bid.Get);
    }

    private Color RivalColor()
    {
        return _scene.SelectedTarget?.Data != null ? _scene.SelectedTarget.Data.ThemeColor : Color.red;
    }

    private static string PlayerLine(DialogueCommand command)
    {
        return command switch
        {
            DialogueCommand.Observe => "……あなたの手の内、見せてもらう。",
            DialogueCommand.Provoke => "その程度で、この記憶が欲しいの？",
            DialogueCommand.Empathize => "その気持ち、分かる気がする。",
            DialogueCommand.Persuade => "その記憶は、あなたには必要ないはず。",
            _ => "……。",
        };
    }

    private static float NextNpcInterval()
    {
        return UnityEngine.Random.Range(GameConstants.NPC_RAISE_INTERVAL_MIN, GameConstants.NPC_RAISE_INTERVAL_MAX);
    }

    public void Start()
    {
        _scene.Initialize(_session);
        BgmManager.Instance.PlayBGM("Battle");
        RunAsync(_cts.Token).Forget();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
