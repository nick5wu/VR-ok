using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatManager : MonoBehaviour
{
    [Header("UI 元件參考")]
    public TMP_InputField inputField;    // 玩家輸入框
    public Transform messageContent;     // Scroll View 裡面的 Content 物件
    public GameObject messagePrefab;    // 訊息預製物 (文字 Prefab)
    public ScrollRect scrollRect;        // Scroll View 本身

    void Update()
    {
        // 按下 Enter 鍵（主鍵盤或小鍵盤）發送訊息
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMessageToChat();
        }
    }

    public void SendMessageToChat()
    {
        // 如果輸入框是空的或只有空白字元，就不處理
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        // 1. 生成新訊息物件到 Content 下面
        GameObject newMessage = Instantiate(messagePrefab, messageContent);

        // 2. 抓取 TMP_Text 組件（這裡加上了角括號 <TMP_Text> 就不會報錯了）
        TMP_Text textComponent = newMessage.GetComponent<TMP_Text>();

        if (textComponent != null)
        {
            // 設定文字內容
            textComponent.text = $"<b>[玩家]:</b> {inputField.text}";
        }
        else
        {
            Debug.LogError("Message Prefab 上找不到 TMP_Text 組件，請檢查 Prefab！");
        }

        // 3. 清空輸入框並重新聚焦（讓玩家可以繼續打字）
        inputField.text = "";
        inputField.ActivateInputField();

        // 4. 強制更新 UI 並捲動到最底部
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    // 切換聊天室開關（如果開著就關閉，關著就開啟）

}