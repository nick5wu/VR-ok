using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

public class SceneController : MonoBehaviour
{
    public void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("收到空白鍵指令，強行前往 coffee1！");
            SceneManager.LoadScene("coffee1");
        }
    }
}