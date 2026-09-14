# VR 家庭訪談系統 - 前端與 AI 核心整合專案 (Unity)

本專案為「VR 家庭訪談 XR 培訓系統」之 Unity 前端專案。本次更新已將後端 AI 連線、即時對話管理器、動態 NPC 生成綁定與動作表情驅動系統完整整合。

---

## 📌 本次改版更新日誌 (Changelog v1.1.0 - 2026/09/15)

### 1. 新增 AI 核心模組 (`Assets/Scripts/`)
* **`DialogueManager_AI.cs`**：
  * **自動相容前端 UI (Auto-Binding)**：啟動時自動掃描子物件尋找 `InputField (TMP)`、`Button`、`ScrollRect` 與 `Content`，即便在 Inspector 未手動拉線也能正常運行。
  * **New Input System 相容**：移除舊版 `UnityEngine.Input.GetKeyDown`，改由 TextMesh Pro 原生 `inputField.onSubmit` 處理 Enter 鍵送出，完全相容 Unity 6 New Input System。
  * **後端開場白解析**：相容後端回傳之 `initial_npc_reply` 欄位，場次初始化成功後自動於對話視窗冒出家長第一句開場招呼語（橘色氣泡）。
  * **單例與按鈕介面**：提供 `DialogueManager_AI.Instance` 以及無參數方法 `SubmitCurrentInput()`，方便 Inspector 按鈕直接選取綁定。
* **`GeminiApiClient.cs`**：
  * **Windows IPv6 延遲修復**：底層發送請求前自動將 `localhost` 轉為 IPv4 `127.0.0.1`，徹底杜絕 Windows 本機 IPv6 (`::1`) 解析逾時（Request timeout）問題。
  * **超時門檻放寬**：將請求逾時時間延長至 `30` 秒，以配合大語言模型生成反應時間。
* **`GeminiDataContracts.cs`**：
  * 更新前後端傳輸 DTO，增加 `initial_npc_reply` 與 `success` 旗標欄位。
* **`NPCAnimationController.cs`**：
  * 提供 3D 角色表情與動作控制器，支援依情緒標籤切換 `Neutral`、`Relaxed`、`Defensive` 狀態機，並依說話字數動態控制 `IsTalking` 動畫持續時間。

### 2. 優化前端既有邏輯 (`Assets/`)
* **`SceneInitializer.cs`**：
  * 新增 **`BindNPCToAI()` 動態綁定機制**：在動態實例化（Instantiate）NPC 後，自動檢查並附加 `NPCAnimationController`，並主動呼叫 `DialogueManager_AI.Instance.BindNPC()`，徹底解決動態生成角色無法在 Inspector 預先拉線的難題。

---

## 🛠️ 前端工程師接線與配置指引 (Setup Guide)

若您開啟新的場景或需要重新確認配置，只需注意以下兩處：

### 1. 主選單起始場景 (`Assets/Scenes/SampleScene 1.unity`)
* 場景中需有常駐通訊物件 **`[NetworkManager]`**（掛載 `GeminiApiClient.cs`）。
* **Inspector 設定**：
  * `Api Base Url`：`http://127.0.0.1:3001`（或後端伺服器對應 Port）。
  * `Timeout Seconds`：`30`。

### 2. 各訪談場景 (`home1`, `home2`, `coffee1`, `coffee2`, `Supermarket`)
* **Canvas 物件**：
  * 移除舊版陽春的 `DialogueManager`。
  * 改掛新版 **`DialogueManager_AI`**。
  * 將 `Assets/Prefabs/DialogueItem.prefab` 拖入 **Dialogue Item Prefab** 欄位。
  * *(其餘 UI 欄位與 NPC 欄位已由 Auto-Binding 機制接管，留在 None 亦可正常運作)*。
* **送出按鈕 (選用)**：
  * 點擊按鈕送出已自動於程式碼 `Start()` 中以 `AddListener` 註冊，按鈕上的 `On Click ()` 列表留空即可；若想手動拉線，可綁定 `DialogueManager_AI -> SubmitCurrentInput ()`。

---

## 🚀 測試驗證步驟
1. 啟動後端伺服器（預設 Port `3001`）。
2. 在 Unity 開啟 `SampleScene 1.unity` 並點擊 **Play ▶**。
3. 登入 ➔ 選擇家庭（小A家）➔ 進入訪談場景。
4. Console 視窗應顯示：
   * `【NPC 生成成功】：persona_1`
   * `【AI 綁定成功】已將動態生成之 NPC 綁定至 DialogueManager_AI`
   * `<color=green>[DialogueManager_AI] 場次初始化成功！</color>`
5. 對話框自動顯示家長問候語，打字後按 Enter 或點擊按鈕即可與 AI 進行即時訪談互動！
