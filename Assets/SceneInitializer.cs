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
                return;
            }
        }

        // 備用方案：如果沒拿到 ScriptableObject，才嘗試從 Resources 載入
        string familyName = PlayerPrefs.GetString("SelectedFamily", "persona_1");
        GameObject fallbackPrefab = Resources.Load<GameObject>("Avatars/" + familyName);
        
        if (fallbackPrefab != null)
        {
            Instantiate(fallbackPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log("【NPC 備用生成成功】：" + familyName);
        }
        else
        {
            Debug.LogError("【生成失敗】請檢查 ScriptableObject 裡是否有拉入 avatar3DPrefab，或 Resources/Avatars/ 路徑是否正確！");
        }
    }
}