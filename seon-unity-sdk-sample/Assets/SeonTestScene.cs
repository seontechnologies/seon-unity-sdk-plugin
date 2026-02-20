using System;
using Seon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-contained SEON SDK test scene.
/// Add this script to an empty GameObject in an empty scene — it builds the entire UI at runtime.
/// No manual wiring needed.
/// </summary>
public class SeonTestScene : MonoBehaviour
{
    // ── State ───────────────────────────────────────────────────
    private bool _initialized;
    private bool _monitoringActive;
    private bool _loggingEnabled;
    private bool _geoEnabled;
    private string _sessionId;
    private string _fingerprint;

    // ── UI References ───────────────────────────────────────────
    private TMP_Text _outputText;
    private ScrollRect _scrollRect;
    private TMP_Text _statusText;
    private Button _initButton;
    private Button _newSessionButton;
    private Button _fingerprintButton;
    private Button _startMonitorButton;
    private Button _stopMonitorButton;
    private Button _toggleLoggingButton;
    private Button _toggleGeoButton;
    private Button _cleanupButton;
    private Button _clearLogButton;
    private Button _copyButton;

    // ── Colors ──────────────────────────────────────────────────
    private static readonly Color BgColor = new Color(0.11f, 0.11f, 0.14f);
    private static readonly Color CardColor = new Color(0.16f, 0.16f, 0.20f);
    private static readonly Color AccentColor = new Color(0.30f, 0.65f, 1.0f);
    private static readonly Color SuccessColor = new Color(0.30f, 0.85f, 0.55f);
    private static readonly Color ErrorColor = new Color(1.0f, 0.40f, 0.40f);
    private static readonly Color WarningColor = new Color(1.0f, 0.75f, 0.30f);
    private static readonly Color DisabledColor = new Color(0.35f, 0.35f, 0.40f);
    private static readonly Color TextColor = new Color(0.93f, 0.93f, 0.95f);
    private static readonly Color TextDimColor = new Color(0.60f, 0.60f, 0.65f);

    void Start()
    {
        BuildUI();
        Log("SEON SDK Test Scene ready.\nPress <b>Init</b> to start.", TextDimColor);
        UpdateButtonStates();
    }

    // ═══════════════════════════════════════════════════════════
    // SDK Actions
    // ═══════════════════════════════════════════════════════════

    private void OnInit()
    {
        _sessionId = "test-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        try
        {
            SeonSDK.Init(_sessionId);
            _initialized = true;
            Log($"✅ <b>Init</b> OK\n   Session: <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{_sessionId}</color>", SuccessColor);
            SetStatus($"Initialized — {_sessionId}");
        }
        catch (Exception ex)
        {
            Log($"❌ <b>Init</b> failed: {ex.Message}", ErrorColor);
            SetStatus("Init failed");
        }
        UpdateButtonStates();
    }

    private void OnNewSession()
    {
        _sessionId = "test-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        try
        {
            SeonSDK.SetSessionId(_sessionId);
            Log($"🔄 <b>New Session</b>: <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{_sessionId}</color>", SuccessColor);
            SetStatus($"Session: {_sessionId}");
        }
        catch (Exception ex)
        {
            Log($"❌ <b>SetSessionId</b> failed: {ex.Message}", ErrorColor);
        }
    }

    private void OnGetFingerprint()
    {
        Log("<b>GetFingerprint</b>...", TextDimColor);
        SetStatus("Getting fingerprint...");
        var startTime = Time.realtimeSinceStartup;

        SeonSDK.GetFingerprint(
            onSuccess: fp =>
            {
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                Log($"✅ <b>Fingerprint</b> ({fp.Length} chars, {elapsed:F0}ms):\n<color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{fp}</color>", SuccessColor);
                SetStatus($"Fingerprint OK — {fp.Length} chars");
                _fingerprint = fp;
                Debug.Log($"<b>Fingerprint</b> ({fp.Length} chars, {elapsed:F0}ms):\n{fp}");
            },
            onError: err =>
            {
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                Log($"❌ <b>Fingerprint</b> error [{err.Code}] ({elapsed:F0}ms):\n{err.Message}" +
                    (err.GeolocationStatusCode.HasValue ? $"\n   GeoStatus: {err.GeolocationStatusCode}" : ""),
                    ErrorColor);
                SetStatus($"Fingerprint error [{err.Code}]");
            });
    }

    private void OnStartMonitoring()
    {
        try
        {
            SeonSDK.StartBehaviourMonitoring();
            _monitoringActive = true;
            Log("🟢 <b>Behaviour monitoring started</b>\n   Interact with the device, then press Stop.", SuccessColor);
            SetStatus("Monitoring active...");
        }
        catch (Exception ex)
        {
            Log($"❌ <b>StartMonitoring</b> failed: {ex.Message}", ErrorColor);
        }
        UpdateButtonStates();
    }

    private void OnStopMonitoring()
    {
        Log("⏳ <b>StopBehaviourMonitoring</b>...", TextDimColor);
        var startTime = Time.realtimeSinceStartup;

        SeonSDK.StopBehaviourMonitoring(
            onSuccess: fp =>
            {
                _monitoringActive = false;
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                Log($"✅ <b>Behaviour fingerprint</b> ({fp.Length} chars, {elapsed:F0}ms):\n<color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{fp}</color>", SuccessColor);
                SetStatus($"Behaviour FP OK — {fp.Length} chars");
                UpdateButtonStates();
                Debug.Log($"<b>Behaviour fingerprint</b> ({fp.Length} chars, {elapsed:F0}ms):\n{fp}");
                _fingerprint = fp;
            },
            onError: err =>
            {
                _monitoringActive = false;
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                Log($"❌ <b>Behaviour</b> error [{err.Code}] ({elapsed:F0}ms):\n{err.Message}", ErrorColor);
                SetStatus($"Behaviour error [{err.Code}]");
                UpdateButtonStates();
            });
    }

    private void OnToggleLogging()
    {
        _loggingEnabled = !_loggingEnabled;
        try
        {
            SeonSDK.SetLoggingEnabled(_loggingEnabled);
            Log($"📝 <b>Logging</b>: {(_loggingEnabled ? "ON" : "OFF")}", _loggingEnabled ? SuccessColor : WarningColor);
        }
        catch (Exception ex)
        {
            Log($"❌ <b>SetLogging</b> failed: {ex.Message}", ErrorColor);
        }
        UpdateButtonStates();
    }

    private void OnToggleGeo()
    {
        _geoEnabled = !_geoEnabled;
        try
        {
            SeonSDK.SetGeolocationConfig(new SeonGeolocationConfig
            {
                Enabled = _geoEnabled,
                PrefetchEnabled = _geoEnabled,
                MaxCacheAgeSec = 300,
                ServiceTimeoutMs = 5000
            });
            Log($"📍 <b>Geolocation</b>: {(_geoEnabled ? "ON (prefetch, 5s timeout)" : "OFF")}", _geoEnabled ? SuccessColor : WarningColor);
        }
        catch (Exception ex)
        {
            Log($"❌ <b>SetGeoConfig</b> failed: {ex.Message}", ErrorColor);
        }
        UpdateButtonStates();
    }

    private void OnCleanup()
    {
        SeonSDK.Cleanup();
        _initialized = false;
        _monitoringActive = false;
        _loggingEnabled = false;
        _geoEnabled = false;
        Log("🧹 <b>Cleanup</b> — SDK reset. Press Init to start again.", WarningColor);
        SetStatus("Not initialized");
        UpdateButtonStates();
    }

    private void OnCopy()
    {
        if (_outputText != null)
        {
            GUIUtility.systemCopyBuffer = _fingerprint;
            Log("Fingerprint Copied to clipboard.", TextDimColor);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private void Log(string message, Color color)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string entry = $"<color=#{ColorUtility.ToHtmlStringRGB(TextDimColor)}>[{timestamp}]</color> <color=#{colorHex}>{message}</color>";

        if (_outputText != null)
        {
            if (!string.IsNullOrEmpty(_outputText.text))
                _outputText.text += "\n\n" + entry;
            else
                _outputText.text = entry;

            // Auto-scroll to bottom
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        Debug.Log($"[SeonTest] {message}");
    }

    private void SetStatus(string text)
    {
        if (_statusText != null)
            _statusText.text = text;
    }

    private void UpdateButtonStates()
    {
        SetButtonInteractable(_initButton, !_initialized);
        SetButtonInteractable(_newSessionButton, _initialized);
        SetButtonInteractable(_fingerprintButton, _initialized);
        SetButtonInteractable(_startMonitorButton, _initialized && !_monitoringActive);
        SetButtonInteractable(_stopMonitorButton, _initialized && _monitoringActive);
        SetButtonInteractable(_toggleLoggingButton, _initialized);
        SetButtonInteractable(_toggleGeoButton, _initialized);
        SetButtonInteractable(_cleanupButton, _initialized);

        // Update toggle button labels
        SetButtonLabel(_toggleLoggingButton, _loggingEnabled ? "Logging: ON" : "Logging: OFF");
        SetButtonLabel(_toggleGeoButton, _geoEnabled ? "Geo: ON" : "Geo: OFF");
    }

    private void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn == null) return;
        btn.interactable = interactable;
        var colors = btn.colors;
        colors.disabledColor = DisabledColor;
        btn.colors = colors;
    }

    private void SetButtonLabel(Button btn, string label)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = label;
    }

    // ═══════════════════════════════════════════════════════════
    // Programmatic UI Construction
    // ═══════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("TestCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        canvasGo.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Full-screen background
        var bg = CreateImage(canvasGo.transform, "Background", BgColor);
        Stretch(bg.rectTransform);

        // ── Header ──────────────────────────────────────────────
        var header = CreatePanel(canvasGo.transform, "Header", new Color(0.14f, 0.14f, 0.18f));
        SetAnchors(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
        header.sizeDelta = new Vector2(0, 120);

        var title = CreateTMPText(header, "Title", "SEON SDK Test", 36, TextColor, FontStyles.Bold);
        title.anchoredPosition = new Vector2(0, -30);

        _statusText = CreateTMPText(header, "Status", "Not initialized", 22, TextDimColor, FontStyles.Italic).GetComponent<TMP_Text>();
        ((RectTransform)_statusText.transform).anchoredPosition = new Vector2(0, -75);

        // ── Button Grid ─────────────────────────────────────────
        float buttonAreaTop = -130;
        float buttonH = 80;
        float gap = 12;
        float pad = 24;
        float halfW = 0.5f;

        // Row 1: Init | New Session
        _initButton = CreateButton(canvasGo.transform, "Init with sessionId", AccentColor, OnInit,
            0, pad, halfW, -pad / 2, buttonAreaTop, buttonH);
        // _newSessionButton = CreateButton(canvasGo.transform, "New Session", AccentColor, OnNewSession,
        //     halfW, pad / 2, 1, -pad, buttonAreaTop, buttonH);

        buttonAreaTop -= buttonH + gap;

        // Row 2: Get Fingerprint (full width)
        _fingerprintButton = CreateButton(canvasGo.transform, "Get Fingerprint", SuccessColor, OnGetFingerprint,
            0, pad, 1, -pad, buttonAreaTop, buttonH);

        buttonAreaTop -= buttonH + gap;

        // Row 3: Start Monitor | Stop Monitor
        _startMonitorButton = CreateButton(canvasGo.transform, "Start Monitor", new Color(0.55f, 0.35f, 0.90f), OnStartMonitoring,
            0, pad, halfW, -pad / 2, buttonAreaTop, buttonH);
        _stopMonitorButton = CreateButton(canvasGo.transform, "Stop Monitor", new Color(0.55f, 0.35f, 0.90f), OnStopMonitoring,
            halfW, pad / 2, 1, -pad, buttonAreaTop, buttonH);

        buttonAreaTop -= buttonH + gap;

        // Row 4: Logging | Geolocation
        _toggleLoggingButton = CreateButton(canvasGo.transform, "Logging: OFF", WarningColor, OnToggleLogging,
            0, pad, halfW, -pad / 2, buttonAreaTop, buttonH);
        _toggleGeoButton = CreateButton(canvasGo.transform, "Geo: OFF", WarningColor, OnToggleGeo,
            halfW, pad / 2, 1, -pad, buttonAreaTop, buttonH);

        buttonAreaTop -= buttonH + gap;

        // Row 5: Cleanup | Copy | Clear
        float thirdW = 1f / 3f;
        // _cleanupButton = CreateButton(canvasGo.transform, "🧹 Cleanup", ErrorColor, OnCleanup,
        //     0, pad, thirdW, -pad / 3, buttonAreaTop, buttonH);
        _copyButton = CreateButton(canvasGo.transform, "Copy", TextDimColor, OnCopy,
            thirdW, pad / 3, thirdW * 2, -pad / 3, buttonAreaTop, buttonH);
        _clearLogButton = CreateButton(canvasGo.transform, "Clear", TextDimColor, () =>
        {
            if (_outputText != null) _outputText.text = "";
        }, thirdW * 2, pad / 3, 1, -pad, buttonAreaTop, buttonH);

        buttonAreaTop -= buttonH + gap + 8;

        // ── Output Scroll View ──────────────────────────────────
        BuildScrollView(canvasGo.transform, buttonAreaTop, pad);
    }

    private void BuildScrollView(Transform parent, float top, float pad)
    {
        // Scroll view container
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(parent, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(pad, pad); // left, bottom
        scrollRT.offsetMax = new Vector2(-pad, top); // right, top

        var scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = CardColor;

        // Rounded feel via slight transparency
        scrollBg.type = Image.Type.Sliced;

        _scrollRect = scrollGo.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 40f;

        // Mask
        var maskGo = new GameObject("Mask");
        maskGo.transform.SetParent(scrollGo.transform, false);
        var maskRT = maskGo.AddComponent<RectTransform>();
        Stretch(maskRT);
        maskRT.offsetMin = new Vector2(16, 8);
        maskRT.offsetMax = new Vector2(-16, -8);
        var maskImg = maskGo.AddComponent<Image>();
        maskImg.color = Color.white;
        maskGo.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(maskGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.content = contentRT;
        _scrollRect.viewport = maskRT;

        // Output text
        var textGo = new GameObject("OutputText");
        textGo.transform.SetParent(contentGo.transform, false);
        var textRT = textGo.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0.5f, 1);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        _outputText = textGo.AddComponent<TextMeshProUGUI>();
        _outputText.fontSize = 20;
        _outputText.color = TextColor;
        _outputText.richText = true;
        _outputText.enableWordWrapping = true;
        _outputText.overflowMode = TextOverflowModes.Overflow;
        _outputText.text = "";

        // Make selectable but not editable via an invisible InputField overlay
        var inputGo = new GameObject("SelectableOverlay");
        inputGo.transform.SetParent(contentGo.transform, false);
        var inputRT = inputGo.AddComponent<RectTransform>();
        inputRT.anchorMin = Vector2.zero;
        inputRT.anchorMax = new Vector2(1, 1);
        inputRT.offsetMin = Vector2.zero;
        inputRT.offsetMax = Vector2.zero;
    }

    // ── UI Factory Methods ──────────────────────────────────────

    private Button CreateButton(Transform parent, string label, Color color, Action onClick,
        float anchorMinX, float leftPad, float anchorMaxX, float rightPad, float top, float height)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorMinX, 1);
        rt.anchorMax = new Vector2(anchorMaxX, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(leftPad, 0);
        rt.offsetMax = new Vector2(rightPad, top);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1, 1, 1, 0.85f);
        colors.pressedColor = new Color(1, 1, 1, 0.65f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRT = textGo.AddComponent<RectTransform>();
        Stretch(textRT);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        return btn;
    }

    private Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    private RectTransform CreateTMPText(Transform parent, string name, string text,
        float size, Color color, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        Stretch(rt);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
