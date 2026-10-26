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
    private readonly List<UnityEngine.UI.Image> durationButtonImages = new List<UnityEngine.UI.Image>();
    private GameObject durationPickerRoot;

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

        if (targetPage != null && targetPage == page2Scene) EnsureDurationPicker();
    }

    // ==========================================
    // 訪談時長選擇 (30 / 60 / 90 分鐘)
    // ==========================================

    /// <summary>
    /// 設定訪談時長 (分鐘)，供按鈕或外部呼叫。僅接受 30 / 60 / 90。
    /// </summary>
    public void SetDuration(int minutes)
    {
        if (System.Array.IndexOf(DurationOptions, minutes) < 0) minutes = 30;
        PlayerPrefs.SetString(DURATION_KEY, minutes.ToString());
        RefreshDurationButtons();
    }

    private void RefreshDurationButtons()
    {
        int current = int.TryParse(PlayerPrefs.GetString(DURATION_KEY, "30"), out int v) ? v : 30;
        for (int i = 0; i < durationButtonImages.Count; i++)
        {
            if (durationButtonImages[i] == null) continue;
            durationButtonImages[i].color = DurationOptions[i] == current
                ? new Color(0.20f, 0.55f, 1.00f, 1f)
                : new Color(0.25f, 0.25f, 0.30f, 0.9f);
        }
    }

    /// <summary>
    /// 在場景選擇頁下方以程式建立 30/60/90 時長按鈕 (沿用頁面既有字型，無須手動拉 UI)。
    /// </summary>
    private void EnsureDurationPicker()
    {
        if (page2Scene == null) return;
        if (durationPickerRoot != null) { RefreshDurationButtons(); return; }

        TMP_Text sample = page2Scene.GetComponentInChildren<TMP_Text>(true);
        TMP_FontAsset font = sample != null ? sample.font : null;

        durationPickerRoot = new GameObject("DurationPicker", typeof(RectTransform));
        var rootRect = durationPickerRoot.GetComponent<RectTransform>();
        rootRect.SetParent(page2Scene.transform, false);
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.sizeDelta = new Vector2(620f, 150f);
        rootRect.anchoredPosition = new Vector2(0f, 20f);

        CreateLabel(rootRect, "訪談時長", font, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -4f), new Vector2(0f, 46f), 32);

        durationButtonImages.Clear();
        for (int i = 0; i < DurationOptions.Length; i++)
        {
            int minutes = DurationOptions[i];
            var btnGo = new GameObject($"Duration_{minutes}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var rect = btnGo.GetComponent<RectTransform>();
            rect.SetParent(rootRect, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(180f, 80f);
            rect.anchoredPosition = new Vector2((i - 1) * 200f, 0f);

            var img = btnGo.GetComponent<UnityEngine.UI.Image>();
            durationButtonImages.Add(img);
            btnGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SetDuration(minutes));

            CreateLabel(rect, $"{minutes} 分鐘", font, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 32);
        }
        RefreshDurationButtons();
    }

    private static TMP_Text CreateLabel(RectTransform parent, string text, TMP_FontAsset font,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, float fontSize)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        if (anchorMin == Vector2.zero && anchorMax == Vector2.one) rect.pivot = new Vector2(0.5f, 0.5f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }
}