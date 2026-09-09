using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 10f;

    [Header("旋轉設定")]
    public float mouseSensitivity = 100f; // 滑鼠旋轉靈敏度
    
    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        // 初始化旋轉角度為攝影機當前的角度
        Vector3 currentRotation = transform.localRotation.eulerAngles;
        xRotation = currentRotation.x;
        yRotation = currentRotation.y;
    }

    void Update()
    {
        // 1. 🛠️ 改成：按住滑鼠「右鍵」（1 代表右鍵）時，才處理滑鼠旋轉
        // 這樣你的右手握滑鼠控制視角，左手就可以伸過去按「方向鍵」移動，兩手不會打結！
        if (Input.GetMouseButton(1))
        {
            // 隱藏滑鼠游標，防止旋轉時滑鼠滑出遊戲視窗
            Cursor.lockState = CursorLockMode.Locked;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;        // 左右轉
            xRotation -= mouseY;        // 上下看
            xRotation = Mathf.Clamp(xRotation, -85f, 85f); // 限制上下看角度

            // 套用旋轉
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
        else
        {
            // 放開右鍵時，恢復滑鼠游標，讓你自由點擊畫面（例如點擊人物）
            Cursor.lockState = CursorLockMode.None;
        }

        // 2. 🛠️ 處理鍵盤移動：直接綁定鍵盤的「上下左右鍵」
        float horizontal = 0f;
        float vertical = 0f;

        // 偵測左右方向鍵
        if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;  // 左方向鍵
        if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;  // 右方向鍵

        // 偵測上下方向鍵
        if (Input.GetKey(KeyCode.UpArrow)) vertical = 1f;       // 上方向鍵
        if (Input.GetKey(KeyCode.DownArrow)) vertical = -1f;    // 下方向鍵

        // 讓移動方向隨著攝影機面對的方向改變
        Vector3 moveDirection = transform.forward * vertical + transform.right * horizontal;
        
        // 讓攝影機移動
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
}