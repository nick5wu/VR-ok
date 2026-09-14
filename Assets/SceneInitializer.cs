using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    [Header("角色生成點")]
    public Transform spawnPoint;

    void Start()
    {
        // 1. 檢查是否有 SpawnPoint
        if (spawnPoint == null)
        {
            Debug.LogError("【生成失敗】Inspector 中的 Spawn Point 欄位未設定 (None)！");
            return;
        }

        // 2. 讀取選取的人物資料
        if (InterviewMenuManager.Instance != null && InterviewMenuManager.Instance.selectedVRPersona != null)
        {
            // 將 ScriptableObject 轉型為你的 VRPersonaData 類別
            VRPersonaData persona = InterviewMenuManager.Instance.selectedVRPersona as VRPersonaData;

            if (persona != null && persona.avatar3DPrefab != null)
            {
                // 成功生成 Prefab
                GameObject npc = Instantiate(persona.avatar3DPrefab, spawnPoint.position, spawnPoint.rotation);
                Debug.Log("【NPC 生成成功】：" + persona.name);
                BindNPCToAI(npc);
                return;
            }
        }

        // 備用方案：如果沒拿到 ScriptableObject，才嘗試從 Resources 載入
        string familyName = PlayerPrefs.GetString("SelectedFamily", "persona_1");
        GameObject fallbackPrefab = Resources.Load<GameObject>("Avatars/" + familyName);
        
        if (fallbackPrefab != null)
        {
            GameObject npc = Instantiate(fallbackPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log("【NPC 備用生成成功】：" + familyName);
            BindNPCToAI(npc);
        }
        else
        {
            Debug.LogError("【生成失敗】請檢查 ScriptableObject 裡是否有拉入 avatar3DPrefab，或 Resources/Avatars/ 路徑是否正確！");
        }
    }

    /// <summary>
    /// 自動為動態生成的 NPC 確保掛載 NPCAnimationController 並綁定至 AI 對話管理器
    /// </summary>
    private void BindNPCToAI(GameObject npc)
    {
        if (npc == null) return;

        var npcAnim = npc.GetComponentInChildren<XRInterview.AI.NPCAnimationController>();
        if (npcAnim == null)
        {
            // 若為原始 FBX 或尚未預先掛載腳本之 Prefab，自動動態附加
            npcAnim = npc.AddComponent<XRInterview.AI.NPCAnimationController>();
        }

        if (XRInterview.AI.DialogueManager_AI.Instance != null)
        {
            XRInterview.AI.DialogueManager_AI.Instance.BindNPC(npcAnim);
            Debug.Log("【AI 綁定成功】已將動態生成之 NPC 綁定至 DialogueManager_AI");
        }
    }
}