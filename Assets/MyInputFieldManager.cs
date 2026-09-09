using UnityEngine;
using TMPro; 
using UnityEngine.UI; // 1. 確保引入 UI 命名空間，解決 ScrollRect 報錯問題

public class MyInputFieldManager : MonoBehaviour
{
    [Header("UI 欄位")]
    public TMP_InputField inputField;  // 你的輸入框 InputField (TMP)
    
    [Header("對話紀錄設定")]
    public GameObject contentPrefabParent; // 拖入 Scroll View -> Viewport -> Content
    public GameObject textPrefab;         // 拖入對話文字 Prefab (DialogueItem)

    // ==========================================
    // 狀況 A：老師（玩家自己）在輸入框打字、按下按鈕送出
    // ==========================================
    public void OnButtonClick()
    {
        if (inputField == null)
        {
            Debug.LogError("【錯誤】inputField 欄位未拉入！");
            return;
        }

        if (contentPrefabParent == null || textPrefab == null)
        {
            Debug.LogError("【錯誤】Content 或 TextPrefab 欄位未拉入！");
            return;
        }

        // 如果玩家沒打字，就不送出
        if (string.IsNullOrEmpty(inputField.text)) return;

        // 呼叫新增對話的方法，身分帶入 "老師"
        AddDialogueLine("老師", inputField.text);

        // 送出後清空輸入框
        inputField.text = "";
    }

    // ==========================================
    // 狀況 B：隨機事件觸發時，供其他腳本呼叫（家長說話）
    // ==========================================
    public void AddDialogueLine(string speaker, string content)
    {
        if (contentPrefabParent == null || textPrefab == null) return;

        // 1. 生成新的對話框
        GameObject newDialogue = Instantiate(textPrefab, contentPrefabParent.transform);

        // 2. 取得文字元件
        TMP_Text tmpText = newDialogue.GetComponent<TMP_Text>();
        if (tmpText == null)
        {
            tmpText = newDialogue.GetComponentInChildren<TMP_Text>();
        }

        // 3. 組合文字格式與顏色
        if (tmpText != null)
        {
            if (speaker == "老師")
            {
                tmpText.text = $"<color=#0078D7>【老師】</color> {content}";
            }
            else
            {
                tmpText.text = $"<color=#D83B01>【家長】</color> {content}";
            }
        }

        // 4. 自動滾動到對話最底部
        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    // 自動滾動到底部
    private void ScrollToBottom()
    {
        ScrollRect scrollRect = contentPrefabParent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}