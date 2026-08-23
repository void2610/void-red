using System;
using UnityEngine;

/// <summary>
/// オークション参加者 (ライバル / モブ) の定義と入札 AI のパラメータ
/// 数値はすべて仮値。実装後に Inspector で調整する
/// </summary>
[CreateAssetMenu(fileName = "Participant", menuName = "VoidRed/Participant")]
public class ParticipantData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private EmotionType emotion;
    [Header("見た目")]
    [SerializeField] private Sprite portrait;
    [SerializeField] private Sprite cutInSprite;
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Color themeColor = Color.red;
    [SerializeField] private bool isMob;
    [SerializeField] private BiddingProfile profile = new();

    public string ParticipantId => name;
    public string DisplayName => displayName;
    public EmotionType Emotion => emotion;
    public Sprite Portrait => portrait;
    public Sprite CutInSprite => cutInSprite;
    public Sprite IconSprite => iconSprite;
    public Color ThemeColor => themeColor;
    public bool IsMob => isMob;
    public BiddingProfile Profile => profile;
}

/// <summary>
/// 入札傾向と対話コマンドへの反応
/// </summary>
[Serializable]
public class BiddingProfile
{
    [Header("基本入札")]
    [Tooltip("司る感情属性以外のロットへの基準枚数")]
    [SerializeField] private int baseBid = 2;
    [Tooltip("司る感情属性のロットへの基準枚数")]
    [SerializeField] private int favoriteBid = 5;
    [Tooltip("基準枚数に足す乱数の幅 (±)")]
    [SerializeField] private int spread = 1;

    [Header("対話への反応")]
    [SerializeField] private BidReaction provokeReaction = BidReaction.Increase;
    [SerializeField] private BidReaction empathizeReaction = BidReaction.Increase;
    [SerializeField] private BidReaction persuadeReaction = BidReaction.Decrease;
    [Tooltip("司る感情属性のロットでは説得が逆に大幅増加になる (レイナ / モルガナ)")]
    [SerializeField] private bool persuadeBoostsFavorite;
    [Tooltip("反応の効き具合 (%)。セリカのように対話がほとんど効かないキャラは小さく")]
    [SerializeField, Range(0, 200)] private int reactionScale = 100;

    [Header("競合")]
    [SerializeField] private CompetitionPolicy competitionPolicy = CompetitionPolicy.Normal;

    [Header("逆対話")]
    [Tooltip("観察されたときに逆対話を仕掛ける確率 (%)")]
    [SerializeField, Range(0, 100)] private int counterDialogueChance = 30;
    [SerializeField] private CounterDialogue counterDialogue = new();

    [Header("セリフ")]
    [SerializeField] private string observeLine = "……。";
    [SerializeField] private string provokeLine = "……。";
    [SerializeField] private string empathizeLine = "……。";
    [SerializeField] private string persuadeLine = "……。";
    [SerializeField] private string failLine = "……。";

    public int BaseBid => baseBid;
    public int FavoriteBid => favoriteBid;
    public int Spread => spread;
    public BidReaction ProvokeReaction => provokeReaction;
    public BidReaction EmpathizeReaction => empathizeReaction;
    public BidReaction PersuadeReaction => persuadeReaction;
    public bool PersuadeBoostsFavorite => persuadeBoostsFavorite;
    public int ReactionScale => reactionScale;
    public CompetitionPolicy CompetitionPolicy => competitionPolicy;
    public int CounterDialogueChance => counterDialogueChance;
    public CounterDialogue CounterDialogue => counterDialogue;
    public string ObserveLine => observeLine;
    public string ProvokeLine => provokeLine;
    public string EmpathizeLine => empathizeLine;
    public string PersuadeLine => persuadeLine;
    public string FailLine => failLine;

    public BidReaction ReactionFor(DialogueCommand command) => command switch
    {
        DialogueCommand.Provoke => provokeReaction,
        DialogueCommand.Empathize => empathizeReaction,
        DialogueCommand.Persuade => persuadeReaction,
        _ => BidReaction.None,
    };

    public string LineFor(DialogueCommand command) => command switch
    {
        DialogueCommand.Observe => observeLine,
        DialogueCommand.Provoke => provokeLine,
        DialogueCommand.Empathize => empathizeLine,
        DialogueCommand.Persuade => persuadeLine,
        _ => failLine,
    };
}

/// <summary>
/// キャラ側から仕掛けてくる二択の問いかけ
/// </summary>
[Serializable]
public class CounterDialogue
{
    [SerializeField, TextArea] private string prompt = "……ねえ、あなたはどう思う？";
    [SerializeField] private string choiceA = "そうだね";
    [SerializeField] private string choiceB = "違うと思う";
    [SerializeField] private BidReaction reactionA = BidReaction.BigIncrease;
    [SerializeField] private BidReaction reactionB = BidReaction.BigDecrease;

    public string Prompt => prompt;
    public string ChoiceA => choiceA;
    public string ChoiceB => choiceB;

    public BidReaction ReactionFor(int choiceIndex) => choiceIndex == 0 ? reactionA : reactionB;
}
