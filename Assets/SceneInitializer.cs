using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    [Header("角色生成點")]
    public Transform spawnPoint;

    [Header("當前場景設定檔")]
    public VRSceneConfigData currentSceneConfig;

    void Start()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("【生成失敗】spawnPoint 未指定！");
            return;
        }

        if (InterviewMenuManager.Instance == null || InterviewMenuManager.Instance.selectedVRPersona == null)
        {
            Debug.LogError("【生成失敗】未取得 InterviewMenuManager 或 selectedVRPersona！");
            return;
        }

        VRPersonaData persona = InterviewMenuManager.Instance.selectedVRPersona as VRPersonaData;

        if (persona != null && persona.avatar3DPrefab != null)
        {
            // 1. 先讀取 Config 設定（偏置與縮放）
            Vector3 posOffset = Vector3.zero;
            Vector3 rotOffset = Vector3.zero;
            Vector3 scale = Vector3.one;

            if (currentSceneConfig != null)
            {
                currentSceneConfig.GetSettingForPersona(persona, out posOffset, out rotOffset, out scale);
            }

            // 防呆：防止 Scale 被設為 0
            if (scale == Vector3.zero) scale = Vector3.one;

            // 2. 計算精準的目標位置與角度
            Vector3 finalPosition = spawnPoint.position + posOffset;
            Quaternion finalRotation = spawnPoint.rotation * Quaternion.Euler(rotOffset);

            // 3. 生成角色 Prefab
            GameObject npc = Instantiate(persona.avatar3DPrefab, finalPosition, finalRotation);

            // 4. 套用 Scale
            npc.transform.localScale = scale;

            Debug.Log($"✅【生成成功】{persona.personaName} | 位置已寫入：{finalPosition} | Scale：{scale}");

            // 5. 綁定 AI 與動畫控制器
            BindNPCToAI(npc);
        }
        else
        {
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