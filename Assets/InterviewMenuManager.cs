using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class InterviewMenuManager : MonoBehaviour
{
    public static InterviewMenuManager Instance;

    [System.Serializable]
    public struct UserAccount
    {
        public string account;
        public string password;
    }

    // 用於 PlayerPrefs 序列化的包裝類別
    [System.Serializable]
    public class UserDataWrapper
    {
        public List<UserAccount> accounts = new List<UserAccount>();
    }

    [Header("頁面設定")]
    public GameObject page0Login;      // 登入頁
    public GameObject registerPanel;  // 註冊彈窗/頁面
    public GameObject page1Family;     // 家庭選擇頁
    public GameObject page2Scene;      // 場景選擇頁

    [Header("登入 UI 元素")]
    public TMP_InputField accountInput;
    public TMP_InputField passwordInput;
    public TMP_Text errorMsgText;      // 登入錯誤訊息

    [Header("註冊 UI 元素")]
    public TMP_InputField regAccountInput;
    public TMP_InputField regPasswordInput;
    public TMP_Text regErrorMsgText;   // 註冊提示/錯誤訊息

    [Header("帳號密碼清單")]
    public List<UserAccount> userAccountList = new List<UserAccount>();

    [Header("暫存選取的資料")]
    public ScriptableObject selectedVRPersona;

    private Dictionary<string, string> accountDict = new Dictionary<string, string>();
    private const string SAVE_KEY = "SavedUserAccounts"; // PlayerPrefs 儲存鍵名
    public const string CURRENT_USER_KEY = "CurrentUserId";
    public const string DURATION_KEY = "SelectedDuration"; // "30" | "60" | "90"

    private static readonly int[] DurationOptions = { 30, 60, 90 };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ShowPage(page0Login);
        LoadAccounts(); // 遊戲啟動時自動讀取硬碟中的帳密紀錄
        
        if (errorMsgText != null) errorMsgText.text = "";
        if (regErrorMsgText != null) regErrorMsgText.text = "";
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    // 從 PlayerPrefs 載入帳密紀錄
    private void LoadAccounts()
    {
        accountDict.Clear();

        // 1. 先將 Inspector 預設的帳密放入
        foreach (var user in userAccountList)
        {
            if (!string.IsNullOrEmpty(user.account) && !accountDict.ContainsKey(user.account))
            {
                accountDict.Add(user.account, user.password);
            }
        }

        // 2. 讀取先前儲存在本機的註冊帳密
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            UserDataWrapper wrapper = JsonUtility.FromJson<UserDataWrapper>(json);
            if (wrapper != null && wrapper.accounts != null)
            {
                foreach (var user in wrapper.accounts)
                {
                    if (!accountDict.ContainsKey(user.account))
                    {
                        accountDict.Add(user.account, user.password);
                        userAccountList.Add(user); // 同步顯示於 Inspector 清單
                    }
                }
            }
        }
    }

    // 儲存帳密至 PlayerPrefs
    private void SaveAccounts()
    {
        UserDataWrapper wrapper = new UserDataWrapper();
        wrapper.accounts = userAccountList;
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    // 點擊「登入」按鈕
    public void OnLoginButtonClicked()
    {
        string inputAccount = accountInput != null ? accountInput.text : "";
        string inputPassword = passwordInput != null ? passwordInput.text : "";

        if (accountDict.ContainsKey(inputAccount) && accountDict[inputAccount] == inputPassword)
        {
            Debug.Log("【登入成功】使用者：" + inputAccount);
            PlayerPrefs.SetString(CURRENT_USER_KEY, inputAccount); // 送給後台的 user_id
            if (errorMsgText != null) errorMsgText.text = "";
            ShowPage(page1Family);
        }
        else
        {
            Debug.LogWarning("【登入失敗】帳號或密碼不正確！");
            if (errorMsgText != null)
            {
                errorMsgText.text = "帳號或密碼錯誤，請重新輸入！";
            }
        }
    }

    public void OpenRegisterPanel()
    {
        if (page0Login != null) page0Login.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (regErrorMsgText != null) regErrorMsgText.text = "";
    }

    public void CloseRegisterPanel()
    {
        if (registerPanel != null) registerPanel.SetActive(false);
        if (page0Login != null) page0Login.SetActive(true);
    }

    // 點擊「確認註冊」按鈕
    public void OnRegisterButtonClicked()
    {
        string newAccount = regAccountInput != null ? regAccountInput.text : "";
        string newPassword = regPasswordInput != null ? regPasswordInput.text : "";

        if (string.IsNullOrEmpty(newAccount) || string.IsNullOrEmpty(newPassword))
        {
            if (regErrorMsgText != null) regErrorMsgText.text = "帳號與密碼不可空白！";
            return;
        }

        if (accountDict.ContainsKey(newAccount))
        {
            if (regErrorMsgText != null) regErrorMsgText.text = "該帳號已存在！";
            return;
        }

        // 新增至字典與清單
        UserAccount newUser = new UserAccount { account = newAccount, password = newPassword };
        accountDict.Add(newAccount, newPassword);
        userAccountList.Add(newUser);

        // 永久寫入硬碟儲存
        SaveAccounts();

        Debug.Log($"【註冊成功並已寫入儲存】新帳號：{newAccount}");

        if (accountInput != null) accountInput.text = newAccount;
        if (passwordInput != null) passwordInput.text = newPassword;
        if (errorMsgText != null) errorMsgText.text = "註冊成功，請點擊登入！";

        CloseRegisterPanel();
    }

    public void SelectFamily(ScriptableObject personaData)
    {
        selectedVRPersona = personaData;
        if (personaData != null)
        {
            PlayerPrefs.SetString("SelectedFamily", personaData.name);
        }
        ShowPage(page2Scene);
    }

    public void SelectScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // ==========================================
    // 返回功能（供按鈕 On Click 使用）
    // ==========================================

    /// <summary>
    /// 【通用返回上一步】綁定給通用返回按鈕使用
    /// </summary>
    public void GoBack()
    {
        // 1. 若在註冊彈窗，先關閉彈窗回到登入頁
        if (registerPanel != null && registerPanel.activeSelf)
        {
            CloseRegisterPanel();
        }
        // 2. 若在場景選擇頁 (Page2)，返回家庭選擇頁 (Page1)
        else if (page2Scene != null && page2Scene.activeSelf)
        {
            GoBackToFamily();
        }
        // 3. 若在家庭選擇頁 (Page1)，返回登入頁 (Page0)
        else if (page1Family != null && page1Family.activeSelf)
        {
            GoBackToLogin();
        }
    }

    /// <summary>
    /// 返回家庭選擇頁面
    /// </summary>
    public void GoBackToFamily()
    {
        ShowPage(page1Family);
    }

    /// <summary>
    /// 返回登入頁面
    /// </summary>
    public void GoBackToLogin()
    {
        ShowPage(page0Login);
    }

    // ==========================================
    // 頁面切換控制與 UI 安全防呆
    // ==========================================

    private void ShowPage(GameObject targetPage)
    {
        // 加上完整空值防呆，避免換場景時 UI 被 Destroy 造成 MissingReferenceException 報錯
        if (page0Login != null) page0Login.SetActive(page0Login == targetPage);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (page1Family != null) page1Family.SetActive(page1Family == targetPage);
        if (page2Scene != null) page2Scene.SetActive(page2Scene == targetPage);

        if (targetPage != null && targetPage == page2Scene) RefreshDurationButtons();
    }

    // ==========================================
    // 訪談時長選擇 (30 / 60 / 90 分鐘)
    // 場景選擇頁 (Page2_Scene) 內的三顆按鈕，onClick 綁定 SetDuration(30/60/90)；
    // 這裡只負責記錄選擇並標示目前選中的按鈕。
    // ==========================================

    [Header("訪談時長選擇 (依序放入 30 / 60 / 90 分鐘按鈕的 Image，用於標示選中)")]
    public UnityEngine.UI.Image[] durationButtonImages = new UnityEngine.UI.Image[3];

    [Tooltip("目前選中的按鈕底色")]
    public Color durationSelectedColor = new Color(0.55f, 0.78f, 1.00f, 1f);
    [Tooltip("未選中的按鈕底色")]
    public Color durationNormalColor = Color.white;

    /// <summary>
    /// 設定訪談時長 (分鐘)，供按鈕 OnClick 呼叫。僅接受 30 / 60 / 90。
    /// </summary>
    public void SetDuration(int minutes)
    {
        if (System.Array.IndexOf(DurationOptions, minutes) < 0) minutes = 30;
        PlayerPrefs.SetString(DURATION_KEY, minutes.ToString());
        PlayerPrefs.Save();
        RefreshDurationButtons();
    }

    private void RefreshDurationButtons()
    {
        if (durationButtonImages == null) return;
        int current = int.TryParse(PlayerPrefs.GetString(DURATION_KEY, "30"), out int v) ? v : 30;
        for (int i = 0; i < durationButtonImages.Length && i < DurationOptions.Length; i++)
        {
            if (durationButtonImages[i] == null) continue;
            durationButtonImages[i].color = DurationOptions[i] == current ? durationSelectedColor : durationNormalColor;
        }
    }
}