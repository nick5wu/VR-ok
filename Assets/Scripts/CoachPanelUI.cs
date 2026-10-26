using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRInterview.AI
{
    /// <summary>
    /// 以程式建立的置中面板 (含半透明遮罩、標題、可捲動內容與關閉鈕)。
    /// 供「暫停求救」「盤點資訊」與「結算報告」共用，免手動拉 UI；尺寸依螢幕高度自動縮放。
    /// </summary>
    public class CoachPanelUI : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.09f, 0.11f, 0.15f, 0.98f);
        private static readonly Color HeadingColor = new Color(0.55f, 0.80f, 1.00f);
        private static readonly Color BodyColor = new Color(0.93f, 0.94f, 0.96f);

        private TMP_FontAsset font;
        private float scale = 1f;
        private GameObject dim;
        private TMP_Text titleText;
        private RectTransform content;
        private ScrollRect scrollRect;
        private Action onClosed;

        public bool IsOpen => dim != null && dim.activeSelf;

        /// <summary>畫布為 ConstantPixelSize，故以螢幕高度換算縮放 (1080p = 1.0)。</summary>
        public static float ComputeScale()
        {
            return Mathf.Max(0.75f, Screen.height / 1080f);
        }

        public static CoachPanelUI Create(RectTransform parent, TMP_FontAsset font)
        {
            var go = new GameObject("CoachPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<CoachPanelUI>();
            ui.font = font;
            ui.scale = ComputeScale();
            ui.Build();
            return ui;
        }

        private void Build()
        {
            float s = scale;
            var rootRect = (RectTransform)transform;
            Stretch(rootRect);

            // 半透明遮罩 (同時擋住背後 UI 的點擊)
            dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            var dimRect = (RectTransform)dim.transform;
            dimRect.SetParent(rootRect, false);
            Stretch(dimRect);
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            // 面板
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(dimRect, false);
            panelRect.anchorMin = new Vector2(0.15f, 0.05f);
            panelRect.anchorMax = new Vector2(0.85f, 0.95f);
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = PanelColor;

            // 標題
            titleText = MakeText(panelRect, "Title", "", 40f * s, Color.white, TextAlignmentOptions.Midline, true);
            var titleRect = (RectTransform)titleText.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -14f * s);
            titleRect.sizeDelta = new Vector2(-60f * s, 64f * s);

            // 捲動區
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            var scrollRectTf = (RectTransform)scrollGo.transform;
            scrollRectTf.SetParent(panelRect, false);
            scrollRectTf.anchorMin = Vector2.zero;
            scrollRectTf.anchorMax = Vector2.one;
            scrollRectTf.offsetMin = new Vector2(28f * s, 104f * s);
            scrollRectTf.offsetMax = new Vector2(-28f * s, -84f * s);
            scrollGo.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.65f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(scrollRectTf, false);
            Stretch(viewportRect);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content = (RectTransform)contentGo.transform;
            content.SetParent(viewportRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            int pad = Mathf.RoundToInt(16f * s);
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = 8f * s;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f * s;

            // 垂直捲軸 (提示內容可捲動)
            var sbGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var sbRect = (RectTransform)sbGo.transform;
            sbRect.SetParent(scrollRectTf, false);
            sbRect.anchorMin = new Vector2(1f, 0f);
            sbRect.anchorMax = new Vector2(1f, 1f);
            sbRect.pivot = new Vector2(1f, 0.5f);
            sbRect.sizeDelta = new Vector2(18f * s, 0f);
            sbRect.anchoredPosition = Vector2.zero;
            sbGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleRect = (RectTransform)handleGo.transform;
            handleRect.SetParent(sbRect, false);
            Stretch(handleRect);
            handleGo.GetComponent<Image>().color = new Color(0.60f, 0.72f, 0.95f, 0.75f);

            var scrollbar = sbGo.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleGo.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // 關閉鈕
            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var closeRect = (RectTransform)closeGo.transform;
            closeRect.SetParent(panelRect, false);
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 18f * s);
            closeRect.sizeDelta = new Vector2(260f * s, 68f * s);
            closeGo.GetComponent<Image>().color = new Color(0.20f, 0.45f, 0.85f, 1f);
            closeGo.GetComponent<Button>().onClick.AddListener(Close);
            var closeLabel = MakeText(closeRect, "Label", "關閉", 32f * s, Color.white, TextAlignmentOptions.Midline, false);
            Stretch((RectTransform)closeLabel.transform);

            dim.SetActive(false);
        }

        // =====================================================================
        // 開關
        // =====================================================================
        public void Open(string title, Action closedCallback = null)
        {
            onClosed = closedCallback;
            titleText.text = title;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            dim.SetActive(true);
            dim.transform.SetAsLastSibling();
            transform.SetAsLastSibling();
            ScrollToTop();
        }

        public void Close()
        {
            if (dim != null) dim.SetActive(false);
            var cb = onClosed;
            onClosed = null;
            cb?.Invoke();
        }

        public void ScrollToTop()
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        }

        // =====================================================================
        // 內容元件
        // =====================================================================
        public void AddHeading(string text)
        {
            MakeText(content, "Heading", text, 34f * scale, HeadingColor, TextAlignmentOptions.Left, true);
        }

        public void AddParagraph(string text, Color? color = null)
        {
            MakeText(content, "Paragraph", text, 28f * scale, color ?? BodyColor, TextAlignmentOptions.Left, false);
        }

        public void AddBigScore(string bigText, string subText)
        {
            MakeText(content, "BigScore", bigText, 64f * scale, Color.white, TextAlignmentOptions.Center, true);
            if (!string.IsNullOrEmpty(subText))
            {
                MakeText(content, "SubScore", subText, 28f * scale, new Color(0.7f, 0.74f, 0.8f), TextAlignmentOptions.Center, false);
            }
        }

        /// <summary>水平進度條：label ─ [▬▬▬　] ─ valueText</summary>
        public void AddBar(string label, float value01, string valueText, Color fillColor)
        {
            float s = scale;
            var row = new GameObject("BarRow", typeof(RectTransform), typeof(LayoutElement));
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(content, false);
            row.GetComponent<LayoutElement>().preferredHeight = 56f * s;

            var labelText = MakeText(rowRect, "Label", label, 28f * s, BodyColor, TextAlignmentOptions.MidlineLeft, false);
            SetAnchors((RectTransform)labelText.transform, 0f, 0f, 0.42f, 1f);

            var bg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            var bgRect = (RectTransform)bg.transform;
            bgRect.SetParent(rowRect, false);
            SetAnchors(bgRect, 0.44f, 0.28f, 0.84f, 0.72f);
            bg.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.28f, 1f);

            var fill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            var fillRect = (RectTransform)fill.transform;
            fillRect.SetParent(bgRect, false);
            SetAnchors(fillRect, 0f, 0f, Mathf.Clamp01(value01), 1f);
            fill.GetComponent<Image>().color = fillColor;

            var valText = MakeText(rowRect, "Value", valueText, 28f * s, BodyColor, TextAlignmentOptions.MidlineRight, false);
            SetAnchors((RectTransform)valText.transform, 0.86f, 0f, 1f, 1f);
        }

        public void AddSpacer(float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(content, false);
            go.GetComponent<LayoutElement>().preferredHeight = height * scale;
        }

        // =====================================================================
        // 工具
        // =====================================================================
        private TMP_Text MakeText(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
