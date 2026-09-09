using UnityEngine;
using TMPro; // 如果使用 TextMeshPro 必須引入此命名空間

public class ClickToInput : MonoBehaviour
{
    // 拖入畫面的 InputField 物件
    public GameObject inputFieldObject; 
    // 拖入 InputField 的組件，用來自動聚焦
    public TMP_InputField mainInputField; 

    void Start()
    {
        // 遊戲開始時確保輸入框是隱藏的
        if (inputFieldObject != null)
        {
            inputFieldObject.SetActive(false);
        }
    }

    void Update()
    {
        // 偵測玩家按下滑鼠左鍵
        if (Input.GetMouseButtonDown(0))
        {
            // 從攝影機發射一條射線到滑鼠點擊的位置
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 檢查射線是否有打到任何有 Collider 的物件
            if (Physics.Raycast(ray, out hit))
            {
                // 檢查被打到的物件是不是「這一個」裝有腳本的物件
                if (hit.transform == this.transform)
                {
                    ShowInputField();
                }
            }
        }
    }

    void ShowInputField()
    {
        if (inputFieldObject != null)
        {
            // 顯示輸入框
            inputFieldObject.SetActive(true);
            
            // 自動幫玩家點進輸入框（直接可以開始打字）
            if (mainInputField != null)
            {
                mainInputField.ActivateInputField();
            }
        }
    }

    // (選填) 當輸入完成（按 Enter 鍵）後要執行的功能
    public void OnInputComplete(string text)
    {
        Debug.Log("玩家輸入了: " + text);
        
        // 可以在這裡處理文字（例如傳給 NPC、當作密碼等）
        
        // 處理完後把輸入框隱藏，並清空文字
        mainInputField.text = "";
        inputFieldObject.SetActive(false);
    }
}