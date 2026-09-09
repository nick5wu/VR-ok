using UnityEngine;

[CreateAssetMenu(fileName = "NewVRPersona", menuName = "Interview VR/Persona Data")]
public class VRPersonaData : ScriptableObject
{
    public string personaId;                  // 例如: "persona_1"
    public string personaName;                // 例如: "小A家 (單親媽)"
    [TextArea(3, 5)] 
    public string hiddenPainPoint;            // 隱藏痛點描述
    public Sprite avatarPortrait;             // 選單按鈕上的頭像/立繪
    public GameObject avatar3DPrefab;         // 3D 角色 Prefab (帶 Animator/BlendShapes)
}