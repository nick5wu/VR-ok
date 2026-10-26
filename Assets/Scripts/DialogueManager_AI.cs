using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace XRInterview.AI
{
    public class DialogueManager_AI : MonoBehaviour
    {
        [Header("=== UI 元件綁定 (DialogueSystem_UI.prefab) ===")]
        [Tooltip("輸入框 (TMP)")]
        [SerializeField] private TMP_InputField inputField;
        
        [Tooltip("送出按鈕 (Button)")]
        [SerializeField] private Button submitButton;

        [Tooltip("結束訪談按鈕 (Button，選用)")]
        [SerializeField] private Button endSessionButton;
        
        [Tooltip("歷史對話清單容器 (Scroll View -> Viewport -> Content)")]
        [SerializeField] private Transform historyContent;
        
        [Tooltip("滾動視圖 (ScrollRect)")]
        [SerializeField] private ScrollRect scrollRect;
        
        [Tooltip("單筆對話文字預製體 (DialogueItem.prefab)")]
        [SerializeField] private GameObject dialogueItemPrefab;

        [Header("=== 即時儀表板 UI (選用) ===")]
        [SerializeField] private Slider rapportSlider;          // 關係分數條 (0-1)
        [SerializeField] private TMP_Text rapportScoreText;     // 分數文字顯示
        [SerializeField] private TMP_Text statusHintText;       // 狀態提示 (例如: NPC 思考中...)

        [Header("=== 訪談結算 UI 面板 (選用) ===")]
        [SerializeField] private GameObject summaryPanel;       // 結算報告面板
        [SerializeField] private TMP_Text summaryScoreText;     // 結算總分顯示
        [SerializeField] private TMP_Text summaryCommentText;   // 結算綜合評語與面向分析

        [Header("=== 場景 NPC 角色綁定 ===")]
        [SerializeField] private NPCAnimationController targetNPCAnim;

        [Header("=== 訪談場次狀態 (Stateful Mode) ===")]
        [SerializeField] private string currentUserId = "trainee_001";
        [SerializeField] private string currentCaseId = "persona_1";
        [SerializeField] private string selectedDuration = "30";
        [SerializeField] private string currentSessionId = string.Empty;

        [Header("=== 時間與閒置控制 ===")]
        [Tooltip("閒置超過此分鐘數未操作，場次自動中止 (不評分)")]
        [SerializeField] private float idleTimeoutMinutes = 10f;
        [Tooltip("倒數計時文字 (選用，留空會自動建立於右上角)")]
        [SerializeField] private TMP_Text timerText;

        private float sessionStartRealtime;
        private float lastActivityRealtime;
        private float endConfirmUntil;
        private bool endRequested;
        private bool isQuitting;
        private TMP_Text endButtonLabel;
        private const string EndLabelNormal = "結束訪談";
        private const string EndLabelConfirm = "再按一次確認";

        // 當前訪談狀態數值
        private int currentRapportScore = 50;
        private int currentDefenseScore = 50;
        private int currentTrustScore = 50;
        private bool isSessionActive = false;

        public static DialogueManager_AI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // 自動相容前端最新 UI 結構：若 Inspector 欄位為空，自動從自身或子物件搜尋對應組件
            AutoBindUIComponents();
        }

        /// <summary>
        /// 自動搜尋並綁定 UI 元件，避免 Inspector 漏拉或前端修改 UI 結構導致 NullReferenceException
        /// </summary>
        private void AutoBindUIComponents()
        {
            if (inputField == null)
            {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }

            if (submitButton == null)
            {
                // 優先尋找 ChatInputGroup 底下的送出 Button
                Button[] buttons = GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn.name.Contains("Button") || btn.name.Contains("Submit") || btn.name.Contains("Send"))
                    {
                        submitButton = btn;
                        break;
                    }
                }
                if (submitButton == null && buttons.Length > 0)
                {
                    submitButton = buttons[0];
                }
            }

            if (scrollRect == null)
            {
                scrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (historyContent == null)
            {
                if (scrollRect != null && scrollRect.content != null)
                {
                    historyContent = scrollRect.content;
                }
                else
                {
                    Transform viewport = transform.Find("HistoryScrollView/Viewport/Content");
                    if (viewport != null) historyContent = viewport;
                }
            }

            // 自動尋找場景中的 NPC
            if (targetNPCAnim == null)
            {
                targetNPCAnim = FindFirstObjectByType<NPCAnimationController>();
            }
        }

        private void Start()
        {
            // 再次確認 NPC 綁定（若 NPC 是在其他腳本的 Awake/Start 動態生成的）
            if (targetNPCAnim == null)
            {
                targetNPCAnim = FindFirstObjectByType<NPCAnimationController>();
            }

            // 嘗試從 InterviewMenuManager 或 PlayerPrefs 讀取選定的個案資訊
            LoadSelectedCaseInfo();

            // 讀取登入帳號 (user_id) 與選單選定的訪談時長
            currentUserId = PlayerPrefs.GetString(InterviewMenuManager.CURRENT_USER_KEY, currentUserId);
            string savedDuration = PlayerPrefs.GetString(InterviewMenuManager.DURATION_KEY, selectedDuration);
            selectedDuration = (savedDuration == "30" || savedDuration == "60" || savedDuration == "90") ? savedDuration : "30";

            BuildRuntimeUI();

            // 綁定 UI 按鈕事件
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(SubmitCurrentInput);
            }

            if (inputField != null)
            {
                inputField.onSubmit.AddListener((val) => SubmitCurrentInput());
            }

            if (endSessionButton != null)
            {
                endSessionButton.onClick.AddListener(OnEndButtonClicked);
            }

            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(_ => MarkActivity());
            }

            if (summaryPanel != null)
            {
                summaryPanel.SetActive(false);
            }

            UpdateUIState();

            // 執行【步驟 ①：場次初始化】
            InitSessionStart();
        }

        /// <summary>
        /// 讀取先前選單選取的個案 ID
        /// </summary>
        private void LoadSelectedCaseInfo()
        {
            string savedFamily = PlayerPrefs.GetString("SelectedFamily", string.Empty);
            if (!string.IsNullOrEmpty(savedFamily))
            {
                currentCaseId = savedFamily;
            }
        }

        // =========================================================================
        // 步驟 ①：場次初始化 (Session Start)
        // =========================================================================
        private void InitSessionStart()
        {
            SetInputInteractable(false);
            if (statusHintText != null) statusHintText.text = "正在連線建立訪談場次...";

            SessionStartRequest startReq = new SessionStartRequest
            {
                user_id = currentUserId,
                case_id = currentCaseId,
                scene_id = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                selected_duration = selectedDuration
            };

            Debug.Log($"[DialogueManager_AI] 發送場次初始化請求 -> User: {startReq.user_id}, Case: {startReq.case_id}, Scene: {startReq.scene_id}");
            GeminiApiClient.Instance.StartSession(startReq, OnSessionStartSuccess, OnSessionStartError);
        }

        private void OnSessionStartSuccess(SessionStartResponse response)
        {
            currentSessionId = response.session_id;
            isSessionActive = true;
            endRequested = false;
            sessionStartRealtime = Time.realtimeSinceStartup;
            lastActivityRealtime = sessionStartRealtime;
            SetInputInteractable(true);
            if (statusHintText != null) statusHintText.text = string.Empty;

            Debug.Log($"<color=green>[DialogueManager_AI] 場次初始化成功！Session ID: {currentSessionId}</color>");

            if (response.initial_rapport_score > 0)
            {
                currentRapportScore = response.initial_rapport_score;
                UpdateUIState();
            }

            // 生成家長第一句開場/招呼語氣泡 (相容 initial_npc_reply 與 npc_reply)
            string openingReply = !string.IsNullOrEmpty(response.initial_npc_reply) ? response.initial_npc_reply : response.npc_reply;
            if (!string.IsNullOrEmpty(openingReply))
            {
                AppendDialogueBubble("【家長】", openingReply, new Color(1.0f, 0.5f, 0.1f)); // 橘色字體
            }

            // 驅動 3D NPC 初始表情動作（安全檢查）
            PlayNPCAnimation(response.npc_emotion_tag, openingReply);
        }

        private void OnSessionStartError(string errorMsg)
        {
            SetInputInteractable(false);
            if (statusHintText != null) statusHintText.text = "場次建立失敗，請確認 API 伺服器是否啟動。";

            AppendDialogueBubble("【系統】", "（無法連線至 Web 中介伺服器 /api/session/start，請檢查網路連線）", Color.red);
        }

        // =========================================================================
        // 步驟 ②：即時對話發送 (Chat Dialogue)
        // =========================================================================
        /// <summary>
        /// 提供給 Inspector Button OnClick 零參數綁定呼叫
        /// </summary>
        public void SubmitCurrentInput()
        {
            OnSubmitMessage("text");
        }

        public void OnSubmitMessage(string inputMode = "text")
        {
            if (string.IsNullOrEmpty(currentSessionId) || !isSessionActive)
            {
                Debug.LogWarning("[DialogueManager_AI] Session 尚未建立或已結束，無法發送訊息。");
                return;
            }

            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
                return;

            string userMessage = inputField.text.Trim();
            inputField.text = string.Empty;
            MarkActivity();

            // 1. 生成學員端發話氣泡
            AppendDialogueBubble("【學員】", userMessage, new Color(0.2f, 0.6f, 1.0f)); // 藍色字體

            // 2. 進入發送狀態
            SetInputInteractable(false);
            if (statusHintText != null) statusHintText.text = "對方正在思考回覆...";

            // 3. 打包狀態同步模式的 DialogueRequest
            DialogueRequest request = new DialogueRequest
            {
                session_id = currentSessionId,
                player_message = userMessage,
                input_mode = inputMode
            };

            // 4. 發送至 /api/chat
            GeminiApiClient.Instance.PostDialogue(request, OnDialogueSuccess, OnDialogueError);
        }

        private void OnDialogueSuccess(GeminiResponseData response)
        {
            MarkActivity();
            SetInputInteractable(true);
            if (statusHintText != null) statusHintText.text = string.Empty;

            // 1. 產生 NPC（家長）回覆氣泡
            AppendDialogueBubble("【家長】", response.npc_reply, new Color(1.0f, 0.5f, 0.1f));

            // 2. 更新即時關係分數與防衛/信任度
            if (response.current_rapport_score > 0)
            {
                currentRapportScore = response.current_rapport_score;
            }
            else
            {
                currentRapportScore = Mathf.Clamp(currentRapportScore + response.rapport_score_change, 0, 100);
            }
            currentDefenseScore = response.defense_score;
            currentTrustScore = response.trust_score;
            UpdateUIState();

            // 3. 驅動 3D NPC 動畫與表情（加入安全檢查與容錯）
            PlayNPCAnimation(response.npc_emotion_tag, response.npc_reply);
        }

        private void OnDialogueError(string errorMsg)
        {
            SetInputInteractable(true);
            if (statusHintText != null) statusHintText.text = "對話發送逾時或伺服器異常。";

            AppendDialogueBubble("【系統】", "（回應逾時或連線失敗，請檢查網路設定）", Color.gray);
        }

        // =========================================================================
        // 步驟 ③：結束訪談結算 (Session End)
        // =========================================================================
        public void EndInterviewSession()
        {
            if (string.IsNullOrEmpty(currentSessionId))
            {
                Debug.LogWarning("[DialogueManager_AI] 無有效 Session ID，無法進行結算。");
                return;
            }

            if (endRequested) return;
            endRequested = true;

            SetInputInteractable(false);
            isSessionActive = false;
            SetTimerVisible(false);
            if (statusHintText != null) statusHintText.text = "正在進行訪談結算，生成 AI 評估報告...";

            SessionEndRequest endReq = new SessionEndRequest
            {
                session_id = currentSessionId,
                actual_duration = Mathf.RoundToInt(Time.realtimeSinceStartup - sessionStartRealtime)
            };

            GeminiApiClient.Instance.EndSession(endReq, OnSessionEndSuccess, OnSessionEndError);
        }

        private void OnSessionEndSuccess(SessionEndResponse response)
        {
            if (statusHintText != null) statusHintText.text = "訪談已順利結束！";
            AppendDialogueBubble("【系統】", $"🎉 訪談結束！最終關係分數：{response.final_rapport_score}，總對話輪數：{response.total_dialogue_turns}", Color.green);

            // 顯示結算報告面板
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(true);

                if (summaryScoreText != null)
                {
                    summaryScoreText.text = $"最終關係建立指數: {response.final_rapport_score}";
                }

                if (summaryCommentText != null && response.evaluation_report != null)
                {
                    var rep = response.evaluation_report;
                    summaryCommentText.text = $"<b>【整體表現評語】</b>\n{rep.feedback_comments}\n\n" +
                                              $"<b>【六大面向評分（總分 {rep.total_score}/100）】</b>\n" +
                                              $"• 開場與關係建立: {rep.dimension_1_rapport}/15\n" +
                                              $"• 提問技巧: {rep.dimension_2_questioning}/25\n" +
                                              $"• 同理心: {rep.dimension_3_empathy}/20\n" +
                                              $"• 家庭中心: {rep.dimension_4_family_centered}/15\n" +
                                              $"• IFSP 資訊完整度: {rep.dimension_5_ifsp_info}/20\n" +
                                              $"• 時間掌握: {rep.dimension_6_time_mgmt}/5";
                }
            }

            Debug.Log($"<color=green>[DialogueManager_AI] 結算完成！總分: {response.evaluation_report?.total_score}，評語: {response.evaluation_report?.feedback_comments}</color>");
        }

        private void OnSessionEndError(string errorMsg)
        {
            if (statusHintText != null) statusHintText.text = "結算請求失敗。";
            AppendDialogueBubble("【系統】", "（無法取得 AI 結算報告，請檢查伺服器狀態）", Color.red);
        }

        // =========================================================================
        // 計時、結束按鈕與閒置處理
        // =========================================================================
        private void MarkActivity()
        {
            lastActivityRealtime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (!isSessionActive || endRequested) return;

            float now = Time.realtimeSinceStartup;
            float remaining = int.Parse(selectedDuration) * 60f - (now - sessionStartRealtime);
            UpdateTimerLabel(remaining);

            if (endConfirmUntil > 0f && now > endConfirmUntil)
            {
                endConfirmUntil = 0f;
                if (endButtonLabel != null) endButtonLabel.text = EndLabelNormal;
            }

            // 時間到：自動結算並評分
            if (remaining <= 0f)
            {
                AppendDialogueBubble("【系統】", "（訪談時間已到，正在結算…）", Color.yellow);
                EndInterviewSession();
                return;
            }

            // 閒置過久：中止場次 (不評分、不呼叫 Gemini)
            if (idleTimeoutMinutes > 0f && now - lastActivityRealtime > idleTimeoutMinutes * 60f)
            {
                AbortInterviewSession("idle_timeout");
            }
        }

        /// <summary>
        /// 結束訪談按鈕：第一次按下進入確認狀態，4 秒內再按一次才會結算，避免誤觸。
        /// </summary>
        public void OnEndButtonClicked()
        {
            if (!isSessionActive) return;
            float now = Time.realtimeSinceStartup;
            if (endConfirmUntil > 0f && now <= endConfirmUntil)
            {
                endConfirmUntil = 0f;
                EndInterviewSession();
                return;
            }
            endConfirmUntil = now + 4f;
            if (endButtonLabel != null) endButtonLabel.text = EndLabelConfirm;
        }

        private void AbortInterviewSession(string reason)
        {
            if (string.IsNullOrEmpty(currentSessionId) || !isSessionActive || endRequested) return;
            endRequested = true;
            isSessionActive = false;
            SetInputInteractable(false);
            SetTimerVisible(false);
            if (statusHintText != null) statusHintText.text = "因閒置過久，訪談已中止。";
            AppendDialogueBubble("【系統】", "（閒置過久，本次訪談已中止，未進行評分）", Color.gray);
            SendAbort(reason);
        }

        private void SendAbort(string reason)
        {
            if (GeminiApiClient.Instance == null || string.IsNullOrEmpty(currentSessionId)) return;
            GeminiApiClient.Instance.AbortSession(new SessionAbortRequest
            {
                session_id = currentSessionId,
                reason = reason,
                actual_duration = Mathf.RoundToInt(Time.realtimeSinceStartup - sessionStartRealtime)
            }, _ => { }, _ => { });
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            // 離開場景 (例如回到選單) 而尚未結算：通知後台中止。直接關閉程式則由後台過期清理處理。
            if (!isQuitting && isSessionActive && !endRequested)
            {
                endRequested = true;
                isSessionActive = false;
                SendAbort("user_aborted");
            }
        }

        private void UpdateTimerLabel(float remainingSeconds)
        {
            if (timerText == null) return;
            int sec = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            timerText.text = $"剩餘 {sec / 60:00}:{sec % 60:00}";
            timerText.color = sec <= 60 ? new Color(1f, 0.35f, 0.3f) : Color.white;
        }

        private void SetTimerVisible(bool visible)
        {
            if (timerText != null) timerText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 若 Inspector 未綁定，於程式中建立倒數計時文字與結束按鈕 (沿用輸入框字型)。
        /// </summary>
        private void BuildRuntimeUI()
        {
            RectTransform parent = transform as RectTransform;
            if (parent == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                parent = canvas != null ? canvas.transform as RectTransform : null;
            }
            if (parent == null) return;

            TMP_FontAsset font = inputField != null && inputField.textComponent != null
                ? inputField.textComponent.font : null;

            if (timerText == null)
            {
                var go = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-20f, -20f);
                rect.sizeDelta = new Vector2(320f, 60f);
                timerText = go.GetComponent<TextMeshProUGUI>();
                if (font != null) timerText.font = font;
                timerText.fontSize = 36f;
                timerText.alignment = TextAlignmentOptions.Right;
                timerText.raycastTarget = false;
                timerText.text = $"剩餘 {int.Parse(selectedDuration):00}:00";
            }

            if (endSessionButton == null)
            {
                var go = new GameObject("EndSessionButton", typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-20f, 20f);
                rect.sizeDelta = new Vector2(240f, 72f);
                go.GetComponent<Image>().color = new Color(0.75f, 0.2f, 0.2f, 0.95f);
                endSessionButton = go.GetComponent<Button>();

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                endButtonLabel = labelGo.GetComponent<TextMeshProUGUI>();
                if (font != null) endButtonLabel.font = font;
                endButtonLabel.fontSize = 30f;
                endButtonLabel.alignment = TextAlignmentOptions.Center;
                endButtonLabel.color = Color.white;
                endButtonLabel.raycastTarget = false;
                endButtonLabel.text = EndLabelNormal;
            }
            else if (endButtonLabel == null)
            {
                endButtonLabel = endSessionButton.GetComponentInChildren<TMP_Text>(true);
            }
        }

        // =========================================================================
        // 輔助函式：UI 與 NPC 動畫安全呼叫
        // =========================================================================
        private void PlayNPCAnimation(string emotionTag, string speechText)
        {
            if (targetNPCAnim != null)
            {
                try
                {
                    targetNPCAnim.ApplyEmotionAndSpeak(emotionTag, speechText ?? string.Empty);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DialogueManager_AI] 播放 NPC 動畫時發生例外（已安全略過）: {ex.Message} | Emotion_Tag: {emotionTag}");
                }
            }
            else
            {
                Debug.Log($"[DialogueManager_AI] 目前未綁定 NPCAnimationController。收到 Emotion_Tag: '{emotionTag}'，已安全略過動作播放。");
            }
        }

        private void AppendDialogueBubble(string speaker, string content, Color textColor)
        {
            if (dialogueItemPrefab == null || historyContent == null) return;

            GameObject newBubble = Instantiate(dialogueItemPrefab, historyContent);
            TMP_Text tmpText = newBubble.GetComponent<TMP_Text>();
            if (tmpText == null) tmpText = newBubble.GetComponentInChildren<TMP_Text>();

            if (tmpText != null)
            {
                tmpText.text = $"<b>{speaker}</b>：{content}";
                tmpText.color = textColor;
            }

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void SetInputInteractable(bool interactable)
        {
            if (inputField != null) inputField.interactable = interactable;
            if (submitButton != null) submitButton.interactable = interactable;
            if (interactable && inputField != null)
            {
                inputField.ActivateInputField();
            }
        }

        private void UpdateUIState()
        {
            if (rapportSlider != null) rapportSlider.value = currentRapportScore / 100f;
            if (rapportScoreText != null) rapportScoreText.text = $"關係指數: {currentRapportScore}";
        }

        /// <summary>
        /// 供外部動態指定場景中生成的 NPC
        /// </summary>
        public void BindNPC(NPCAnimationController npc)
        {
            this.targetNPCAnim = npc;
        }
    }
}
