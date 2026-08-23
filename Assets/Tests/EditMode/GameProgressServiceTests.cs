using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 階層のやり直しで感情リソースが増え続けないことを確かめる
/// </summary>
public class GameProgressServiceTests
{
    private GameProgressService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new GameProgressService(new SaveDataManager());
        _service.ResetToDefaultData();
    }

    [Test]
    public void 同じ階層をやり直しても補充は一度きり()
    {
        var first = _service.PrepareWalletForFloor(1).Total;
        var retry = _service.PrepareWalletForFloor(1).Total;

        Assert.AreEqual(first, retry);
    }

    [Test]
    public void やり直し用の控えを持ち出しても元の手持ちは変わらない()
    {
        var wallet = _service.PrepareWalletForFloor(1);
        wallet.TryConsume(EmotionWallet.ALL_EMOTIONS[0], GameConstants.EMOTION_REFILL_PER_FLOOR);

        Assert.AreEqual(GameConstants.EMOTION_REFILL_PER_FLOOR, _service.PrepareWalletForFloor(1).Get(EmotionWallet.ALL_EMOTIONS[0]));
    }

    [Test]
    public void 初期化すると控えも捨てられる()
    {
        _service.PrepareWalletForFloor(1);
        _service.ResetToDefaultData();

        Assert.AreEqual(GameConstants.EMOTION_REFILL_PER_FLOOR * EmotionWallet.ALL_EMOTIONS.Length, _service.PrepareWalletForFloor(1).Total);
    }

    [Test]
    public void 洗礼の結果はセーブされ次の起動で戻る()
    {
        var lot = AuctionTestFactory.CreateLot(0);
        var bid = new EmotionBid();
        bid.Set(EmotionType.Sadness, 2);
        var won = new WonLot(lot, 0, bid, false, new Dictionary<string, int>());
        var remaining = new EmotionWallet();
        remaining.Add(EmotionType.Joy, 7);

        _service.RecordAuctionClearAndSave(remaining, won, new[] { won }, new[] { "rival0" });
        var reloaded = new GameProgressService(new SaveDataManager());

        Assert.AreEqual(7, reloaded.PlayerWallet.Get(EmotionType.Joy), "持ち越しリソース");
        CollectionAssert.Contains(reloaded.Persona.IntegratedLotIds, lot.LotId, "統合した記憶");
        CollectionAssert.Contains(reloaded.Persona.CollectionLotIds, lot.LotId, "コレクション");
        CollectionAssert.Contains(reloaded.CollapsedParticipantIds, "rival0", "人格崩壊した参加者");
    }

    [Test]
    public void 洗礼を終えると次の階層では改めて補充される()
    {
        var before = _service.PrepareWalletForFloor(1).Total;
        var lot = AuctionTestFactory.CreateLot(0);
        var won = new WonLot(lot, 0, new EmotionBid(), false, new Dictionary<string, int>());
        _service.RecordAuctionClearAndSave(_service.PrepareWalletForFloor(1), won, new[] { won }, new string[0]);

        Assert.Greater(_service.PrepareWalletForFloor(2).Total, before, "階層を抜けたら控えを捨てて補充し直す");
    }
}
