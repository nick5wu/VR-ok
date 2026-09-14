export interface ChangelogItem {
  version: string;
  date: string;
  changes: string[];
}

export const changelog: ChangelogItem[] = [
  {
    version: 'v1.1.0',
    date: '2026-09-15',
    changes: [
      '新增 AI 核心模組 (DialogueManager_AI, GeminiApiClient, GeminiDataContracts, NPCAnimationController)',
      '實作 UI 自動綁定 (Auto-Binding) 與單例機制，相容前端 DialogueSystem_UI 最新兄弟節點階層',
      '重構輸入監聽邏輯以完整適配 Unity New Input System，改由 TextMesh Pro 原生 onSubmit 處理鍵盤 Enter 送出',
      '修復 Windows IPv6 (::1) 本機解析逾時問題，加入 localhost 自動轉換為 127.0.0.1 之連線保護',
      '相容後端 session start 之 initial_npc_reply 欄位，確保家長首句開場問候正常顯示',
      '於 SceneInitializer 加入動態實例化 NPC 自動補掛 NPCAnimationController 與 BindNPC 雙向綁定機制',
      '建立根目錄 README.md 提供前端工程師完整接線與部署指引'
    ]
  }
];
