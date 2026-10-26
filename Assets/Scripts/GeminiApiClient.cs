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
        [SerializeField] private string apiBaseUrl = "https://xr-interview-backend-1019562597305.asia-east1.run.app";
        [SerializeField] private int timeoutSeconds = 30;
        [Tooltip("結算 (session/end) 需呼叫 Gemini 評分，耗時較長，另設較長逾時")]
        [SerializeField] private int endSessionTimeoutSeconds = 120;
        [Tooltip("教練指令 (暫停求救 / 盤點資訊) 需呼叫 Gemini，另設逾時")]
        [SerializeField] private int coachTimeoutSeconds = 60;

        [Tooltip("(選用) Supabase 登入 Token。有值時以 Authorization: Bearer 送出；留空則走後端的 VR 過渡通道 (需 ALLOW_VR_UNAUTHENTICATED=true)")]
        [SerializeField] private string authToken = string.Empty;

        /// <summary>
        /// 登入後由外部設定 Supabase access token
        /// </summary>
        public void SetAuthToken(string token)
        {
            authToken = token ?? string.Empty;
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
            StartCoroutine(SendPostRequestCoroutine(url, requestData, onSuccess, onError, endSessionTimeoutSeconds));
        }

        /// <summary>
        /// 教練「暫停求救」(POST /api/coach/help)：不寫入對話紀錄，NPC 凍結
        /// </summary>
        public void RequestCoachHelp(CoachRequest requestData,
            Action<CoachHelpResponse> onSuccess,
            Action<string> onError)
        {
            string url = $"{apiBaseUrl.TrimEnd('/')}/api/coach/help";
            StartCoroutine(SendPostRequestCoroutine(url, requestData, onSuccess, onError, coachTimeoutSeconds));
        }

        /// <summary>
        /// 教練「盤點資訊」(POST /api/coach/inventory)
        /// </summary>
        public void RequestInventory(CoachRequest requestData,
            Action<InventoryResponse> onSuccess,
            Action<string> onError)
        {
            string url = $"{apiBaseUrl.TrimEnd('/')}/api/coach/inventory";
            StartCoroutine(SendPostRequestCoroutine(url, requestData, onSuccess, onError, coachTimeoutSeconds));
        }

        /// <summary>
        /// 中止場次 (POST /api/session/abort)：閒置逾時或中途離開，不評分
        /// </summary>
        public void AbortSession(SessionAbortRequest requestData,
            Action<SessionAbortResponse> onSuccess,
            Action<string> onError)
        {
            string url = $"{apiBaseUrl.TrimEnd('/')}/api/session/abort";
            StartCoroutine(SendPostRequestCoroutine(url, requestData, onSuccess, onError));
        }

        /// <summary>
        /// 通用 HTTP POST 請求協程
        /// </summary>
        private IEnumerator SendPostRequestCoroutine<TReq, TRes>(string endpointUrl, 
            TReq requestData, 
            Action<TRes> onSuccess, 
            Action<string> onError,
            int timeoutOverrideSeconds = 0)
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
                webRequest.SetRequestHeader("x-client-platform", "unity-vr");
                if (!string.IsNullOrEmpty(authToken))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + authToken);
                }
                webRequest.timeout = timeoutOverrideSeconds > 0 ? timeoutOverrideSeconds : timeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errorMsg = $"[API 伺服器錯誤] URL: {endpointUrl}, 代碼: {webRequest.responseCode}, 訊息: {webRequest.error}\n回應: {webRequest.downloadHandler?.text}";
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
