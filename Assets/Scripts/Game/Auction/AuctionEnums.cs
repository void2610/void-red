/// <summary>
/// オークション 1 階層の進行フェーズ
/// </summary>
public enum AuctionPhase
{
    /// <summary> 記憶テーマ公開 </summary>
    ThemeAnnounce,
    /// <summary> ロット提示 </summary>
    LotReveal,
    /// <summary> 対話フェーズ (対話コマンド / 逆対話) </summary>
    Dialogue,
    /// <summary> 入札フェーズ (感情属性ホイールで枚数を決める) </summary>
    Bidding,
    /// <summary> 一斉開示 </summary>
    Reveal,
    /// <summary> 競合フェーズ (同数時のリアルタイム上乗せ) </summary>
    Competition,
    /// <summary> ロットの落札確定 </summary>
    LotResult,
    /// <summary> 洗礼 (落札内容の確認と人格統合) </summary>
    Baptism,
    /// <summary> 主人公が 1 つも落札できずゲームオーバー </summary>
    GameOver,
    /// <summary> 階層を突破して終了 </summary>
    Finished,
}

/// <summary>
/// プレイヤーが仕掛ける対話コマンド
/// </summary>
public enum DialogueCommand
{
    /// <summary> 観察する: 相手の入札予定枚数を知る。逆対話を仕掛けられるリスクがある </summary>
    Observe,
    /// <summary> 挑発する: 入札額を増減させる </summary>
    Provoke,
    /// <summary> 共感する: 入札額を上げる (相手によっては逆効果) </summary>
    Empathize,
    /// <summary> 説得する: 入札額を下げる (相手によっては逆効果) </summary>
    Persuade,
}

/// <summary>
/// 対話コマンドが成功したときにキャラの入札予定へ起きる変化
/// </summary>
public enum BidReaction
{
    /// <summary> 変動なし </summary>
    None,
    /// <summary> 入札増加 </summary>
    Increase,
    /// <summary> 入札大幅増加 </summary>
    BigIncrease,
    /// <summary> 入札減少 </summary>
    Decrease,
    /// <summary> 入札大幅減少 </summary>
    BigDecrease,
    /// <summary> ランダムに変動 </summary>
    Random,
    /// <summary> 確率で入札を取りやめる </summary>
    Withdraw,
    /// <summary> このロットを減らし、次のロットへ振り分ける </summary>
    ShiftToNext,
    /// <summary> 次のロットから引き寄せてこのロットを増やす </summary>
    PullFromNext,
}

/// <summary>
/// 競合フェーズでの追加入札方針
/// </summary>
public enum CompetitionPolicy
{
    /// <summary> 競合する意思なし </summary>
    Never,
    /// <summary> そこまで追わない </summary>
    Rarely,
    /// <summary> 普通に追う </summary>
    Normal,
    /// <summary> 司る感情属性のロット以外は追わない </summary>
    FavoriteOnly,
    /// <summary> 司る感情属性のロットは特に追い、確実に取りに行く </summary>
    FavoriteAggressive,
    /// <summary> 追うかどうかは完全にランダム </summary>
    Random,
    /// <summary> 入札していたリソース数に応じて追う </summary>
    ByBidSize,
    /// <summary> 一度の競合に全てつぎ込む </summary>
    AllIn,
}
