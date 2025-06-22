using UnityEngine;
using TMPro;

// R3を使うには以下の名前空間をusingする
using R3;

namespace void2610.UnityTemplate.Tutorials
{
    public class R3TutorialUI : MonoBehaviour
    {
        [SerializeField] private R3Tutorial r3Tutorial;
        [SerializeField] private TMP_Text text;
        private void Start()
        {
            // 値が変更されたときに自動でUIが更新される！！！
            // 最低限これだけ覚えとけば多分大丈夫
            r3Tutorial.PlayerHp.Subscribe(newValue =>
            {
                text.text = $"Player HP: {newValue}";
            }).AddTo(this);
            // ↑ このAddTo(this)は、MonoBehaviourのライフサイクルに合わせて自動で購読を解除してくれる
            // AddTo(this)を使わないと、購読が解除されずにメモリリークの原因になることがあるので注意
        }
    }
}