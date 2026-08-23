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
}
