using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI 連結")]
    public TMP_InputField inputField;       // 拖入你的 InputField (TMP)
    public Transform historyContent;        // 拖入 Scroll View -> Viewport -> Content
    public Button submitButton;             // 拖入你的「輸入」按鈕

    [Header("Prefab")]
    public GameObject dialogueItemPrefab;   // 拖入你剛剛製作的 DialogueItem Prefab

    void Start()
    {
        // 綁定按鈕點擊事件
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitDialogue);
        }
    }

    public void SubmitDialogue()
    {
        // 如果輸入欄位是空的，就不處理
        if (string.IsNullOrEmpty(inputField.text)) return;

        // 1. 生成新的對話文字物件，並設為 Content 的子物件
        GameObject newTextObj = Instantiate(dialogueItemPrefab, historyContent);
        
        // 2. 設定文字內容
        TMP_Text tmpText = newTextObj.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            // 這裡可以加上稱謂，例如 "我: 內容"
            tmpText.text = $"玩家: {inputField.text}";
        }

        // 3. 清空輸入框並重新聚焦
        inputField.text = "";
        inputField.ActivateInputField();

        // 4. (選填) 強制滾動到最底部
        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    void ScrollToBottom()
    {
        ScrollRect scrollRect = historyContent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}