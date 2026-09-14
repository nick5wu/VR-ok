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
    // 步驟 ③：結束訪談結算 (Session End) DTO
    // =========================================================================
    [Serializable]
    public class SessionEndRequest
    {
        public string session_id;         // 當前結算的 Session ID
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
        public string overall_summary;        // 整體評語
        public int empathy_score;             // 同理心與支持技巧 (0-100)
        public int questioning_score;         // 開放式提問技巧 (0-100)
        public int active_listening_score;    // 傾聽與回應能力 (0-100)
        public int problem_solving_score;     // 問題探討與引導能力 (0-100)
        public int rapport_building_score;    // 信任關係建立 (0-100)
        public int professional_attitude_score;// 專業倫理與態度 (0-100)
        public string[] key_strengths;        // 表現優異重點
        public string[] improvement_tips;     // 建議改進方向
    }
}
