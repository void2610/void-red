/// <summary>
/// 感情リソースの8属性（プルチックの感情の輪に基づく）
/// </summary>
public enum EmotionType
{
    /// <summary> 喜び </summary>
    Joy,
    /// <summary> 信頼 </summary>
    Trust,
    /// <summary> 恐れ </summary>
    Fear,
    /// <summary> 驚き </summary>
    Surprise,
    /// <summary> 悲しみ </summary>
    Sadness,
    /// <summary> 嫌悪 </summary>
    Disgust,
    /// <summary> 怒り </summary>
    Anger,
    /// <summary> 期待 </summary>
    Anticipation
}

/// <summary>
/// 複合感情タイプ（プルチックの感情の輪に基づく）
/// 隣接する2つの基本感情の組み合わせ
/// </summary>
public enum CompoundEmotionType
{
    /// <summary> 愛 = 喜び + 信頼 </summary>
    Love,
    /// <summary> 服従 = 信頼 + 恐れ </summary>
    Submission,
    /// <summary> 畏敬 = 恐れ + 驚き </summary>
    Awe,
    /// <summary> 失望 = 驚き + 悲しみ </summary>
    Disapproval,
    /// <summary> 自責（後悔） = 悲しみ + 嫌悪 </summary>
    Remorse,
    /// <summary> 軽蔑 = 嫌悪 + 怒り </summary>
    Contempt,
    /// <summary> 積極性 = 怒り + 期待 </summary>
    Aggressiveness,
    /// <summary> 楽観 = 期待 + 喜び </summary>
    Optimism
}

// EmotionTypeの拡張メソッド
public static class EmotionTypeExtensions
{
    // 感情タイプに対応する色を取得
    public static UnityEngine.Color GetColor(this EmotionType emotion) => emotion switch
    {
        EmotionType.Joy => new UnityEngine.Color(1f, 0.85f, 0.2f),           // 黄色（喜び）
        EmotionType.Trust => new UnityEngine.Color(0.3f, 0.75f, 0.4f),       // 緑（信頼）
        EmotionType.Fear => new UnityEngine.Color(0.2f, 0.4f, 0.2f),         // 暗緑（恐れ）
        EmotionType.Surprise => new UnityEngine.Color(0.4f, 0.8f, 0.9f),     // シアン（驚き）
        EmotionType.Sadness => new UnityEngine.Color(0.3f, 0.45f, 0.75f),    // 青（悲しみ）
        EmotionType.Disgust => new UnityEngine.Color(0.5f, 0.3f, 0.6f),      // 紫（嫌悪）
        EmotionType.Anger => new UnityEngine.Color(0.9f, 0.25f, 0.25f),      // 赤（怒り）
        EmotionType.Anticipation => new UnityEngine.Color(0.95f, 0.5f, 0.2f),// オレンジ（期待）
        _ => UnityEngine.Color.white
    };

    // 感情タイプの日本語名を取得
    public static string ToJapaneseName(this EmotionType emotion) => emotion switch
    {
        EmotionType.Joy => "喜び",
        EmotionType.Trust => "信頼",
        EmotionType.Fear => "恐れ",
        EmotionType.Surprise => "驚き",
        EmotionType.Sadness => "悲しみ",
        EmotionType.Disgust => "嫌悪",
        EmotionType.Anger => "怒り",
        EmotionType.Anticipation => "期待",
        _ => "不明"
    };

    // 感情タイプに対応する薄い色調を取得（キャラクター染色用）
    public static UnityEngine.Color GetTintColor(this EmotionType emotion) => emotion switch
    {
        EmotionType.Joy => new UnityEngine.Color(1f, 1f, 0.8f),
        EmotionType.Trust => new UnityEngine.Color(0.8f, 1f, 0.8f),
        EmotionType.Fear => new UnityEngine.Color(0.7f, 0.8f, 0.7f),
        EmotionType.Surprise => new UnityEngine.Color(0.8f, 1f, 1f),
        EmotionType.Sadness => new UnityEngine.Color(0.8f, 0.8f, 1f),
        EmotionType.Disgust => new UnityEngine.Color(0.9f, 0.8f, 1f),
        EmotionType.Anger => new UnityEngine.Color(1f, 0.8f, 0.8f),
        EmotionType.Anticipation => new UnityEngine.Color(1f, 0.9f, 0.8f),
        _ => UnityEngine.Color.white
    };

    // 感情タイプからリアクションSE名を取得
    public static string ToReactSeName(this EmotionType emotion) => emotion switch
    {
        EmotionType.Joy => "SE_REACT_JOY",
        EmotionType.Trust => "SE_REACT_TRUST",
        EmotionType.Fear => "SE_REACT_FEAR",
        EmotionType.Surprise => "SE_REACT_WONDER",
        EmotionType.Sadness => "SE_REACT_GRIEF",
        EmotionType.Disgust => "SE_REACT_HATE",
        EmotionType.Anger => "SE_REACT_ANGER",
        EmotionType.Anticipation => "SE_REACT_EXPECT",
        _ => "SE_REACT_JOY"
    };

    // 感情タイプから記憶育成フェーズのリアクションSE名を取得
    public static string ToFaceReactSeName(this EmotionType emotion) => emotion switch
    {
        EmotionType.Joy => "SE_FACE_REACT_JOY",
        EmotionType.Trust => "SE_FACE_REACT_TRUST",
        EmotionType.Fear => "SE_FACE_REACT_FEAR",
        EmotionType.Surprise => "SE_FACE_REACT_WONDER",
        EmotionType.Sadness => "SE_FACE_REACT_GRIEF",
        EmotionType.Disgust => "SE_FACE_REACT_HATE",
        EmotionType.Anger => "SE_FACE_REACT_ANGER",
        EmotionType.Anticipation => "SE_FACE_REACT_EXPECT",
        _ => "SE_FACE_REACT_JOY"
    };

    // 感情タイプからリソース配置SE名を取得
    public static string ToResourceSeName(this EmotionType emotion) => emotion switch
    {
        EmotionType.Joy => "SE_RESOURCE_JOY",
        EmotionType.Trust => "SE_RESOURCE_TRUST",
        EmotionType.Fear => "SE_RESOURCE_FEAR",
        EmotionType.Surprise => "SE_RESOURCE_WONDER",
        EmotionType.Sadness => "SE_RESOURCE_GRIEF",
        EmotionType.Disgust => "SE_RESOURCE_HATE",
        EmotionType.Anger => "SE_RESOURCE_ANGER",
        EmotionType.Anticipation => "SE_RESOURCE_EXPECT",
        _ => "SE_RESOURCE_JOY"
    };

    /// <summary>
    /// 隣接する感情タイプを取得（感情の輪で時計回りに次の感情）
    /// </summary>
    public static EmotionType GetNextEmotion(this EmotionType emotion) => emotion switch
    {
        EmotionType.Joy => EmotionType.Trust,
        EmotionType.Trust => EmotionType.Fear,
        EmotionType.Fear => EmotionType.Surprise,
        EmotionType.Surprise => EmotionType.Sadness,
        EmotionType.Sadness => EmotionType.Disgust,
        EmotionType.Disgust => EmotionType.Anger,
        EmotionType.Anger => EmotionType.Anticipation,
        EmotionType.Anticipation => EmotionType.Joy,
        _ => emotion
    };

    /// <summary>
    /// 隣接する感情タイプを取得（感情の輪で反時計回りに前の感情）
    /// </summary>
    public static EmotionType GetPreviousEmotion(this EmotionType emotion) => emotion switch
    {
        EmotionType.Joy => EmotionType.Anticipation,
        EmotionType.Trust => EmotionType.Joy,
        EmotionType.Fear => EmotionType.Trust,
        EmotionType.Surprise => EmotionType.Fear,
        EmotionType.Sadness => EmotionType.Surprise,
        EmotionType.Disgust => EmotionType.Sadness,
        EmotionType.Anger => EmotionType.Disgust,
        EmotionType.Anticipation => EmotionType.Anger,
        _ => emotion
    };
}

/// <summary>
/// 複合感情タイプの拡張メソッド
/// </summary>
public static class CompoundEmotionTypeExtensions
{
    /// <summary>
    /// 複合感情を構成する2つの基本感情を取得
    /// </summary>
    public static (EmotionType first, EmotionType second) GetComponentEmotions(this CompoundEmotionType compound) => compound switch
    {
        CompoundEmotionType.Love => (EmotionType.Joy, EmotionType.Trust),
        CompoundEmotionType.Submission => (EmotionType.Trust, EmotionType.Fear),
        CompoundEmotionType.Awe => (EmotionType.Fear, EmotionType.Surprise),
        CompoundEmotionType.Disapproval => (EmotionType.Surprise, EmotionType.Sadness),
        CompoundEmotionType.Remorse => (EmotionType.Sadness, EmotionType.Disgust),
        CompoundEmotionType.Contempt => (EmotionType.Disgust, EmotionType.Anger),
        CompoundEmotionType.Aggressiveness => (EmotionType.Anger, EmotionType.Anticipation),
        CompoundEmotionType.Optimism => (EmotionType.Anticipation, EmotionType.Joy),
        _ => (EmotionType.Joy, EmotionType.Joy)
    };

    /// <summary>
    /// 複合感情の日本語名を取得
    /// </summary>
    public static string ToJapaneseName(this CompoundEmotionType compound) => compound switch
    {
        CompoundEmotionType.Love => "愛",
        CompoundEmotionType.Submission => "服従",
        CompoundEmotionType.Awe => "畏敬",
        CompoundEmotionType.Disapproval => "失望",
        CompoundEmotionType.Remorse => "自責",
        CompoundEmotionType.Contempt => "軽蔑",
        CompoundEmotionType.Aggressiveness => "積極性",
        CompoundEmotionType.Optimism => "楽観",
        _ => "不明"
    };

    /// <summary>
    /// 2つの基本感情から複合感情を取得（隣接していない場合はnull）
    /// </summary>
    public static CompoundEmotionType? GetCompoundEmotion(EmotionType first, EmotionType second)
    {
        // 順序を正規化（感情の輪の順序で先に来る方をfirstに）
        var (a, b) = NormalizeEmotionPair(first, second);

        return (a, b) switch
        {
            (EmotionType.Joy, EmotionType.Trust) => CompoundEmotionType.Love,
            (EmotionType.Trust, EmotionType.Fear) => CompoundEmotionType.Submission,
            (EmotionType.Fear, EmotionType.Surprise) => CompoundEmotionType.Awe,
            (EmotionType.Surprise, EmotionType.Sadness) => CompoundEmotionType.Disapproval,
            (EmotionType.Sadness, EmotionType.Disgust) => CompoundEmotionType.Remorse,
            (EmotionType.Disgust, EmotionType.Anger) => CompoundEmotionType.Contempt,
            (EmotionType.Anger, EmotionType.Anticipation) => CompoundEmotionType.Aggressiveness,
            (EmotionType.Anticipation, EmotionType.Joy) => CompoundEmotionType.Optimism,
            _ => null // 隣接していない組み合わせ
        };
    }

    /// <summary>
    /// 複合感情に対応する色を取得（構成感情の中間色）
    /// </summary>
    public static UnityEngine.Color GetColor(this CompoundEmotionType compound)
    {
        var (first, second) = compound.GetComponentEmotions();
        var color1 = first.GetColor();
        var color2 = second.GetColor();
        return UnityEngine.Color.Lerp(color1, color2, 0.5f);
    }

    /// <summary>
    /// 複合感情に対応する薄い色調を取得（キャラクター染色用）
    /// </summary>
    public static UnityEngine.Color GetTintColor(this CompoundEmotionType compound)
    {
        var (first, second) = compound.GetComponentEmotions();
        var color1 = first.GetTintColor();
        var color2 = second.GetTintColor();
        return UnityEngine.Color.Lerp(color1, color2, 0.5f);
    }

    /// <summary>
    /// 感情ペアを感情の輪の順序で正規化
    /// </summary>
    private static (EmotionType, EmotionType) NormalizeEmotionPair(EmotionType a, EmotionType b)
    {
        // 同じ感情の場合はそのまま返す
        if (a == b) return (a, b);

        // aの次がbならそのまま、bの次がaなら入れ替え
        if (a.GetNextEmotion() == b) return (a, b);
        if (b.GetNextEmotion() == a) return (b, a);

        // 隣接していない場合は元の順序を維持
        return (a, b);
    }
}
