using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyChatManager : MonoBehaviour
{
    [Header("UI 元件綁定")]
    [Tooltip("玩家輸入文字的 Input Field (TMP)")]
    public TMP_InputField inputField;

    [Tooltip("Scroll View 內部的 Content 物件（負責擺放訊息）")]
    public Transform messageContent;

    [Tooltip("單條訊息的 Prefab 預製物")]
    public GameObject messagePrefab;

    [Tooltip("Scroll View 本身（用來控制捲動軸置底）")]
    public ScrollRect scrollRect;

    void Update()
    {
        // 當按下 Enter 鍵（主鍵盤 Enter 或小鍵盤 Enter）時發送訊息
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMessageToChat();
        }
    }

    /// <summary>
    /// 發送聊天訊息的主邏輯
    /// </summary>
    public void SendMessageToChat()
    {
        // 如果輸入框是空的或只有空白字元，直接跳過不發送
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        // 1. 在 Content 物件底下動態生成新的訊息 Prefab
        GameObject newMessage = Instantiate(messagePrefab, messageContent);

        // 2. 取得 Prefab 上的 TMP_Text 組件並填入文字
        TMP_Text textComponent = newMessage.GetComponent<TMP_Text>();

        if (textComponent != null)
        {
            textComponent.text = $"<b>[玩家]:</b> {inputField.text}";
        }
        else
        {
            Debug.LogError("［錯誤］Message Prefab 上找不到 TMP_Text 組件，請檢查 Prefab 物件！");
        }

        // 3. 清空輸入框內容並重新聚焦（讓玩家可連續打字）
        inputField.text = "";
        inputField.ActivateInputField();

        // 4. 強制刷新 Canvas UI 並將 ScrollBar 自動拉到最底部
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 切換聊天室視窗顯示 / 隱藏（可綁定給開關按鈕使用）
    /// </summary>
    public void ToggleChatWindow()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}