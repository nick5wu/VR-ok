using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewVRSceneConfig", menuName = "Interview VR/Scene Config")]
public class VRSceneConfigData : ScriptableObject
{
    public string sceneId;
    public string sceneDisplayName;
    public Sprite previewSprite;
    public string unitySceneName;

    [System.Serializable]
    public struct PersonaTransformSetting
    {
        public VRPersonaData personaData;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        public Vector3 customScale;
    }

    [Header("角色在此場景的位置與大小微調")]
    public List<PersonaTransformSetting> personaSettings = new List<PersonaTransformSetting>();

    public bool GetSettingForPersona(VRPersonaData targetPersona, out Vector3 posOffset, out Vector3 rotOffset, out Vector3 scale)
    {
        posOffset = Vector3.zero;
        rotOffset = Vector3.zero;
        scale = Vector3.one;

        if (targetPersona == null) return false;

        foreach (var setting in personaSettings)
        {
            if (setting.personaData != null && 
               (setting.personaData == targetPersona || setting.personaData.personaId == targetPersona.personaId))
            {
                posOffset = setting.positionOffset;
                rotOffset = setting.rotationOffset;
                scale = (setting.customScale == Vector3.zero) ? Vector3.one : setting.customScale;
                return true;
            }
        }

        // 如果 Config 清單裡沒設定這個角色，預設給 (1,1,1) 防呆，避免縮放變成 0 導致消失
        scale = Vector3.one;
        return false;
    }
}