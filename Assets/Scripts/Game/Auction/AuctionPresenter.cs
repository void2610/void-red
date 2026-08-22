using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;
using Void2610.UnityTemplate;

/// <summary>
/// 1 階層分のオークションを View と結び付けて進行させる
/// 5 ロットを回し、洗礼で統合を選ばせ、進行度を記録してロビーへ戻る
/// </summary>
public class AuctionPresenter : IStartable, IDisposable
{
    private readonly AuctionView _view;
    private readonly AuctionSession _session;
    private readonly GameProgressService _progress;
    private readonly SceneTransitionManager _sceneTransition;
    private readonly CompositeDisposable _disposables = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly EmotionBid _draft = new();
    private AuctionParticipant _dialogueTarget;
    private WonLot _chosen;

    public AuctionPresenter(AuctionView view, AuctionSession session, GameProgressService progress, SceneTransitionManager sceneTransition)
    {
        _view = view;
        _session = session;
        _progress = progress;
        _sceneTransition = sceneTransition;
    }

    private static float NextNpcInterval()
    {
        return UnityEngine.Random.Range(GameConstants.NPC_RAISE_INTERVAL_MIN, GameConstants.NPC_RAISE_INTERVAL_MAX);
    }

    private async UniTask RunAsync(CancellationToken ct)
    {
        await _view.WaitNextAsync($"記憶テーマ「{_session.Floor.ThemeTitle}」\n第 {_session.Floor.FloorIndex} 階層の記憶オークションを始める");
        while (!_session.IsLastLot)
        {
            await RunLotAsync(ct);
        }
        _session.FinishLots();
        if (_session.IsPlayerGameOver)
        {
            await RunGameOverAsync(ct);
            return;
        }
        await RunBaptismAsync(ct);
    }

    private async UniTask RunLotAsync(CancellationToken ct)
    {
        var lot = _session.BeginNextLot();
        _view.HideBids();
        _view.RefreshSlots();
        _view.LotViewShow(lot, _session.CurrentLotIndex + 1);
        await _view.WaitNextAsync($"ロット {_session.CurrentLotIndex + 1}『{lot.Title}』");

        await RunDialogueAsync(ct);
        await RunBiddingAsync(ct);
        var reveal = _session.LastReveal;
        foreach (var b in reveal.Bidders) _view.SlotOf(b).ShowBid(b.SubmittedBid.Total);
        _view.RefreshSlots();

        AuctionParticipant winner;
        if (reveal.IsTie)
        {
            await _view.WaitNextAsync($"{reveal.TiedParticipants.Count} 人が同額。競合に入る");
            winner = await RunCompetitionAsync(ct);
        }
        else
        {
            winner = _session.ResolveReveal();
        }
        _view.RefreshSlots();
        if (winner != null) _view.SlotOf(winner).SetWinner(true);
        await _view.WaitNextAsync(winner != null ? $"{winner.DisplayName} が『{lot.Title}』を落札" : $"『{lot.Title}』は流札");
    }

    private async UniTask RunDialogueAsync(CancellationToken ct)
    {
        _dialogueTarget = null;
        _view.DialoguePanel.Show();
        _view.DialoguePanel.SetInteractable(true);
        _view.SetSlotsSelectable(true);
        _view.SetMessage("対話コマンドで相手の出方を探る");

        using var d = new CompositeDisposable();
        _view.OnSlotSelected.Subscribe(p =>
        {
            _dialogueTarget = p;
            foreach (var s in _view.Slots) s.SetHighlighted(s.Participant == p);
            _view.DialoguePanel.SetTarget(p, _session);
        }).AddTo(d);

        var counterPending = false;
        _view.DialoguePanel.OnCommand.Subscribe(cmd =>
        {
            if (_dialogueTarget == null || !_session.CanUseDialogue(_dialogueTarget, cmd)) return;
            var outcome = _session.UseDialogue(_dialogueTarget, cmd);
            _view.DialoguePanel.ShowOutcome(outcome);
            _view.DialoguePanel.SetTarget(_dialogueTarget, _session);
            if (outcome.Counter != null)
            {
                counterPending = true;
                _view.DialoguePanel.SetInteractable(false);
                _view.SetSlotsSelectable(false);
                _view.CounterDialogue.Show(outcome.Target, outcome.Counter);
            }
        }).AddTo(d);

        _view.CounterDialogue.OnChoice.Subscribe(choice =>
        {
            if (!counterPending) return;
            counterPending = false;
            _session.AnswerCounterDialogue(_dialogueTarget, choice);
            _view.CounterDialogue.Hide();
            _view.DialoguePanel.SetInteractable(true);
            _view.SetSlotsSelectable(true);
            _view.DialoguePanel.SetTarget(_dialogueTarget, _session);
        }).AddTo(d);

        await _view.DialoguePanel.OnToBidding.Where(_ => !counterPending).FirstAsync(ct);
        _view.DialoguePanel.Hide();
        _view.SetSlotsSelectable(false);
        foreach (var s in _view.Slots) s.SetHighlighted(false);
        _session.EnterBidding();
    }

    private async UniTask RunBiddingAsync(CancellationToken ct)
    {
        _draft.Clear();
        _view.BidPanel.ResetMode();
        _view.BidPanel.Show();
        _view.BidPanel.Refresh(_session.Player.Wallet, _draft);
        _view.SetMessage("感情リソースを入札する。落札できなければ返ってくる");

        using var d = new CompositeDisposable();
        _view.BidPanel.OnPlus.Subscribe(e =>
        {
            if (_draft.Get(e) >= _session.Player.Wallet.Get(e)) return;
            _draft.Add(e);
            SeManager.Instance.PlaySe(e.ToResourceSeName(), pitch: 1f);
            _view.BidPanel.Refresh(_session.Player.Wallet, _draft);
        }).AddTo(d);
        _view.BidPanel.OnMinus.Subscribe(e =>
        {
            if (_draft.Get(e) <= 0) return;
            _draft.Add(e, -1);
            _view.BidPanel.Refresh(_session.Player.Wallet, _draft);
        }).AddTo(d);

        await _view.BidPanel.OnConfirm.FirstAsync(ct);
        _view.BidPanel.Hide();
        _session.SubmitPlayerBid(_draft);
    }

    private async UniTask<AuctionParticipant> RunCompetitionAsync(CancellationToken ct)
    {
        _session.StartCompetition(Time.time);
        var competition = _session.Competition;
        _view.CompetitionPanel.Show(competition);
        _view.RefreshSlots();
        var playerCompeting = competition.Competitors.Contains(_session.Player);
        if (playerCompeting)
        {
            _view.BidPanel.RefreshAsRaise(_session.Player.Wallet);
            _view.BidPanel.Show();
        }
        _view.SetMessage(playerCompeting ? "1 枚ずつ上乗せできる。競合に入った分は返ってこない" : "他の参加者同士の競合を見守る");

        using var d = new CompositeDisposable();
        _view.BidPanel.OnPlus.Subscribe(e =>
        {
            if (!_session.TryPlayerRaise(e, Time.time)) return;
            SeManager.Instance.PlaySe(e.ToResourceSeName(), pitch: 1f);
            _view.BidPanel.RefreshAsRaise(_session.Player.Wallet);
            _view.RefreshSlots();
        }).AddTo(d);

        var npcNext = competition.Competitors.Where(c => !c.IsPlayer).ToDictionary(c => c, _ => Time.time + NextNpcInterval());
        while (!competition.IsTimedOut(Time.time) && !competition.IsDeadlocked())
        {
            foreach (var npc in npcNext.Keys.ToList())
            {
                if (Time.time < npcNext[npc]) continue;
                if (_session.TryNpcRaise(npc, Time.time)) _view.RefreshSlots();
                npcNext[npc] = Time.time + NextNpcInterval();
            }
            _view.CompetitionPanel.Refresh(competition, Time.time);
            foreach (var c in competition.Competitors) _view.SlotOf(c).ShowBid(competition.TotalOf(c));
            await UniTask.Yield(ct);
        }

        _view.BidPanel.Hide();
        _view.CompetitionPanel.Hide();
        return _session.ResolveCompetition();
    }

    private async UniTask RunBaptismAsync(CancellationToken ct)
    {
        _chosen = null;
        _view.HideAllPanels();
        _view.Baptism.Show(_session);
        _view.SetMessage("洗礼: 落札した記憶から 1 つを人格に統合する");

        using var d = new CompositeDisposable();
        _view.Baptism.OnIntegrate.Subscribe(w =>
        {
            _chosen = w;
            _view.Baptism.SetSelected(w);
        }).AddTo(d);

        await _view.Baptism.OnFinish.Where(_ => _chosen != null).FirstAsync(ct);
        _session.Finish();
        var collapsed = _session.Rivals.Where(r => r.HasCollapsed).Select(r => r.Data.ParticipantId);
        _progress.RecordAuctionClearAndSave(_session.Player.Wallet, _chosen, _session.Player.WonLots, collapsed);
        await _sceneTransition.TransitionToSceneWithFade(SceneType.Home);
    }

    private async UniTask RunGameOverAsync(CancellationToken ct)
    {
        _view.HideAllPanels();
        _view.GameOver.Show(_session.Floor.FloorIndex);
        _view.SetMessage("ゲームオーバー");
        var retry = _view.GameOver.OnRetry.Select(_ => true);
        var lobby = _view.GameOver.OnLobby.Select(_ => false);
        var toRetry = await Observable.Merge(retry, lobby).FirstAsync(ct);
        // 進行度は変えず、同じ階層をやり直す
        await _sceneTransition.TransitionToSceneWithFade(toRetry ? SceneType.Auction : SceneType.Home);
    }

    public void Start()
    {
        _view.Initialize(_session);
        BgmManager.Instance.PlayBGM("Battle");
        RunAsync(_cts.Token).Forget();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _disposables.Dispose();
    }
}
