using UnityEngine;

namespace XRInterview.AI
{
    public class NPCAnimationController : MonoBehaviour
    {
        [Header("元件綁定")]
        [SerializeField] private Animator npcAnimator;

        [Header("Animator 參數名稱 (可於 Inspector 自訂)")]
        [SerializeField] private string isTalkingParam = "IsTalking";
        [SerializeField] private string emotionIntParam = "EmotionState"; // 0: Neutral, 1: Relaxed, 2: Defensive
        [SerializeField] private string triggerTalkParam = "TriggerTalk";
        [SerializeField] private string triggerDefensiveParam = "TriggerDefensive";
        [SerializeField] private string triggerRelaxedParam = "TriggerRelaxed";

        private void Reset()
        {
            npcAnimator = GetComponent<Animator>();
        }

        /// <summary>
        /// 根據 AI 回傳的 Emotion_Tag 驅動角色動作與表情
        /// </summary>
        public void ApplyEmotionAndSpeak(string emotionTag, string speechText)
        {
            if (npcAnimator == null) return;

            // 1. 設置說話狀態
            npcAnimator.SetBool(isTalkingParam, true);
            npcAnimator.SetTrigger(triggerTalkParam);

            // 2. 依情緒標籤切換狀態機 (Defensive / Relaxed / Neutral)
            switch (emotionTag?.ToLower())
            {
                case "defensive":
                    npcAnimator.SetInteger(emotionIntParam, 2);
                    npcAnimator.SetTrigger(triggerDefensiveParam);
                    Debug.Log("<color=red>[NPC 動畫] 切換至防衛/抗拒狀態 (Defensive)</color>");
                    break;

                case "relaxed":
                    npcAnimator.SetInteger(emotionIntParam, 1);
                    npcAnimator.SetTrigger(triggerRelaxedParam);
                    Debug.Log("<color=green>[NPC 動畫] 切換至放鬆/信任狀態 (Relaxed)</color>");
                    break;

                case "neutral":
                default:
                    npcAnimator.SetInteger(emotionIntParam, 0);
                    Debug.Log("<color=cyan>[NPC 動畫] 維持一般/平靜狀態 (Neutral)</color>");
                    break;
            }

            // 3. 預計說話完畢後關閉說話動作 (依字數概算持續時間，每字約 0.2 秒)
            float speechDuration = Mathf.Clamp(speechText.Length * 0.2f, 1.5f, 6.0f);
            CancelInvoke(nameof(StopTalking));
            Invoke(nameof(StopTalking), speechDuration);
        }

        private void StopTalking()
        {
            if (npcAnimator != null)
            {
                npcAnimator.SetBool(isTalkingParam, false);
            }
        }
    }
}
