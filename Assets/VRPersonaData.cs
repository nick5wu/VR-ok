using UnityEngine;

[CreateAssetMenu(fileName = "NewVRPersona", menuName = "VR Interview/Persona Data")]
public class VRPersonaData : ScriptableObject
{
    public string personaId;
    public string personaName;
    [TextArea] public string hiddenPainPoint;
    public Sprite avatarPortrait;
    public GameObject avatar3DPrefab;

    [Header("角色專屬微調參數 (獨立設定)")]
    public Vector3 customScale = Vector3.one;             // 專屬縮放比例
    public Vector3 positionOffset = Vector3.zero;        // 專屬座標偏移
    public Vector3 rotationOffset = Vector3.zero;        // 專屬角度偏移 (例如 X = -90)
}