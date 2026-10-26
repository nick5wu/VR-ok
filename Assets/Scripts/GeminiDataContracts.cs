using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRInterview.AI
{
    // =========================================================================
    // 步驟 ①：場次初始化 (Session Start) DTO
    // =========================================================================
    [Serializable]
    public class SessionStartRequest
    {
        public string user_id;            // 學員 ID (例如 "trainee_001")
        public string case_id;            // 個案 ID (例如 "persona_1")
        public string scene_id;           // 目前 Unity 場景名稱 (例如 "home1"、"coffee1")
        public string selected_duration;  // 預計訪談時間 (例如 "30" 分鐘)
    }

    [Serializable]
    public class SessionStartResponse
    {
        public bool success;                  // 成功旗標
        public string session_id;             // 後端建立的唯一 Session ID
        public string npc_reply;              // NPC 第一句開場/招呼語 (相容舊版欄位)
        public string initial_npc_reply;      // NPC 第一句開場/招呼語 (最新後端欄位)
        public string npc_emotion_tag;        // 初始情緒 ("neutral" | "defensive" | "relaxed")
        public int initial_rapport_score;     // 初始關係分數 (預設 50)
        public string message;                // 系統提示訊息
    }

    // =========================================================================
    // 步驟 ②：即時對話發送 (Chat Dialogue) DTO
    // =========================================================================
    [Serializable]
    public class DialogueRequest
    {
        public string session_id;         // 步驟一拿到的 Session ID
        public string player_message;     // 學員當前說的這句話
        public string input_mode;         // 輸入模式："text" 或 "voice"
    }

    [Serializable]
    public class GeminiResponseData
    {
        public string npc_reply;              // NPC 回應台詞
        public string npc_emotion_tag;        // "defensive" | "relaxed" | "neutral"
        public int rapport_score_change;      // 關係分數增減 (例如 +5, -10)
        public int current_rapport_score;     // 當前最新關係分數 (0-100)
        public int trust_score;               // 信任度 (0-100)
        public int defense_score;             // 防衛度 (0-100)
        public bool sensitive_triggered;      // 是否踩中隱藏痛點
        public string question_target;        // "child" | "family" | "other"
        public string ai_reasoning;           // AI 評估原因
        public string[] student_skill_tag;    // 偵測到的技巧標籤
    }

    // =========================================================================
    // 教練指令：暫停求救 / 盤點資訊 DTO
    // =========================================================================
    [Serializable]
    public class CoachRequest
    {
        public string session_id;
    }

    [Serializable]
    public class CoachHelpResponse
    {
        public bool success;
        public string session_id;
        public string coach_message;          // 目前觀察到的卡點
        public string[] suggested_questions;  // 1~2 句示範問法
    }

    [Serializable]
    public class InventoryItemData
    {
        public string field;                  // IFSP 前置資訊項目
        public string status;                 // "obtained" | "pending" | "missing"
        public string note;                   // 依據或缺少什麼
    }

    [Serializable]
    public class InventoryProgress
    {
        public int child_function;            // 兒童功能 (0-100)
        public int daily_routine;             // 日常作息 (0-100)
        public int family_needs;              // 家庭需求 (0-100)
        public int strengths_resources;       // 優勢與資源 (0-100)
    }

    [Serializable]
    public class InventoryResponse
    {
        public bool success;
        public string session_id;
        public InventoryItemData[] items;
        public string priority_hint;          // 下一步優先補問方向
        public InventoryProgress progress;    // 四大範疇達成率
    }

    // =========================================================================
    // 步驟 ③：結束訪談結算 (Session End) DTO
    // =========================================================================
    [Serializable]
    public class SessionEndRequest
    {
        public string session_id;         // 當前結算的 Session ID
        public int actual_duration;       // 實際訪談秒數 (供「時間掌握」面向評分)
    }

    // 中止場次 (閒置逾時或中途離開，不評分、不呼叫 Gemini)
    [Serializable]
    public class SessionAbortRequest
    {
        public string session_id;
        public string reason;             // "idle_timeout" | "user_aborted"
        public int actual_duration;       // 實際訪談秒數
    }

    [Serializable]
    public class SessionAbortResponse
    {
        public bool success;
        public string session_id;
        public string status;
    }

    [Serializable]
    public class SessionEndResponse
    {
        public string session_id;
        public int final_rapport_score;       // 最終關係分數
        public int total_dialogue_turns;      // 總對話輪數
        public InterviewEvaluationReport evaluation_report; // AI 自動產生的評估報告
    }

    [Serializable]
    public class InterviewEvaluationReport
    {
        // 與後端 evaluateSession 回傳欄位一致 (總分 100)
        public int dimension_1_rapport;           // 開場與關係建立 (滿分 15)
        public int dimension_2_questioning;       // 提問技巧 (滿分 25)
        public int dimension_3_empathy;           // 同理心 (滿分 20)
        public int dimension_4_family_centered;   // 家庭中心 (滿分 15)
        public int dimension_5_ifsp_info;         // IFSP 資訊完整度 (滿分 20)
        public int dimension_6_time_mgmt;         // 時間掌握 (滿分 5)
        public int total_score;                   // 總分 (0-100)
        public string feedback_comments;          // 整體評語
        public string[] strengths;                // 表現優異重點
        public string[] suggestions;              // 建議改進方向
        public string best_question;              // 最佳提問
        public string best_question_reason;
        public string suboptimal_question;        // 待改進提問
        public string suggested_alternative;      // 建議替代問法
    }
}
