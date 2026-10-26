using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace XRInterview.AI
{
    public class GeminiApiClient : MonoBehaviour
    {
        public static GeminiApiClient Instance { get; private set; }

        [Header("API 伺服器設定")]
        [Tooltip("Web Express 中介伺服器基礎網址 (建議使用 http://127.0.0.1:3001 防止 Windows IPv6 解析逾時)")]
        [SerializeField] private string apiBaseUrl = "http://127.0.0.1:3001";
        [SerializeField] private int timeoutSeconds = 30;

        // 後端 requireAuth 以此標頭辨識 Unity VR 實機連線 (過渡期，需後端 ALLOW_VR_UNAUTHENTICATED=true)
        private const string ClientPlatformHeader = "X-Client-Platform";
        private const string ClientPlatformValue = "unity-vr";

        // Supabase access token (JWT)。取得後由外部呼叫 SetAccessToken 設定，之後每個請求都會帶上 Authorization: Bearer
        private string accessToken = string.Empty;

        /// <summary>
        /// 設定 Supabase 登入後取得的 JWT；傳入空字串則清除，回到過渡期的 VR 標頭模式
        /// </summary>
        public void SetAccessToken(string token)
        {
            accessToken = token ?? string.Empty;
        }

        private void Awake()
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

        /// <summary>
        /// 步驟 ①：場次初始化 (POST /api/session/start)
        /// </summary>
        public void StartSession(SessionStartRequest requestData, 
            Action<SessionStartResponse> onSuccess, 
            Action<string> onError)
        {
            string url = $"{apiBaseUrl.TrimEnd('/')}/api/session/start";
            StartCoroutine(SendPostRequestCoroutine(url, requestData, onSuccess, onError));
        }

        /// <summary>
        /// 步驟 ②：即時對話發送 (POST /api/chat)
        /// </summary>
        public void PostDialogue(DialogueRequest requestData, 
            Action<GeminiResponseData> onSuccess, 
            Action<string> onError)
        {
            string url = $"{apiBaseUrl.TrimEnd('/')}/api/chat";
            StartCoroutine(SendPostRequestCoroutine(url, requestData, onSuccess, onError));
        }

        /// <summary>
        /// 步驟 ③：結束訪談結算 (POST /api/session/end)
        /// </summary>
        public void EndSession(SessionEndRequest requestData, 
            Action<SessionEndResponse> onSuccess, 
            Action<string> onError)
        {
            string url = $"{apiBaseUrl.TrimEnd('/')}/api/session/end";
            StartCoroutine(SendPostRequestCoroutine(url, requestData, onSuccess, onError));
        }

        /// <summary>
        /// 通用 HTTP POST 請求協程
        /// </summary>
        private IEnumerator SendPostRequestCoroutine<TReq, TRes>(string endpointUrl, 
            TReq requestData, 
            Action<TRes> onSuccess, 
            Action<string> onError)
        {
            // 防呆處理：若網址含有 localhost，自動替換為 127.0.0.1 避免 Windows 下 IPv6 (::1) 解析延遲導致逾時
            if (endpointUrl.Contains("://localhost"))
            {
                endpointUrl = endpointUrl.Replace("://localhost", "://127.0.0.1");
            }

            string jsonBody = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest webRequest = new UnityWebRequest(endpointUrl, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader(ClientPlatformHeader, ClientPlatformValue);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                }
                webRequest.timeout = timeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errorMsg = $"[API 伺服器錯誤] URL: {endpointUrl}, 代碼: {webRequest.responseCode}, 訊息: {webRequest.error}\n回應: {webRequest.downloadHandler?.text}";
                    if (webRequest.responseCode == 401)
                    {
                        errorMsg += "\n[提示] 後端要求身分驗證：請設定 Supabase JWT (SetAccessToken)，或在後端測試環境設定 ALLOW_VR_UNAUTHENTICATED=true。";
                    }
                    Debug.LogError(errorMsg);
                    onError?.Invoke(errorMsg);
                }
                else
                {
                    string responseJson = webRequest.downloadHandler.text;
                    Debug.Log($"[API 回應成功 - {endpointUrl}]: {responseJson}");

                    try
                    {
                        TRes responseData = JsonUtility.FromJson<TRes>(responseJson);
                        onSuccess?.Invoke(responseData);
                    }
                    catch (Exception ex)
                    {
                        string parseError = $"[JSON 解析失敗]: {ex.Message}\n原文: {responseJson}";
                        Debug.LogError(parseError);
                        onError?.Invoke(parseError);
                    }
                }
            }
        }
    }
}
