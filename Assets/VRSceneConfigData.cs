using UnityEngine;

[CreateAssetMenu(fileName = "NewVRSceneConfig", menuName = "Interview VR/Scene Config")]
public class VRSceneConfigData : ScriptableObject
{
    public string sceneId;              // 例如: "coffee_1"
    public string sceneDisplayName;     // 例如: "咖啡廳 1"
    public Sprite previewSprite;        // 選單按鈕預覽圖
    public string unitySceneName;       // 對應 Scenes 資料夾裡的場景名稱 (如 "coffee1", "coffee2", "home2")
}