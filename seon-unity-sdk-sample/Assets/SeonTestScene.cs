using System;
using Seon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SEON SDK test scene controller.
/// All UI elements are assigned via the Inspector — no runtime construction.
/// </summary>
public class SeonTestScene : MonoBehaviour
{
    // ── Inspector References ────────────────────────────────────
    [Header("Status")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button getFingerprintButton;
    [SerializeField] private Button startMonitorButton;
    [SerializeField] private Button stopMonitorButton;
    [SerializeField] private Button toggleLoggingButton;
    [SerializeField] private Button toggleGeoButton;
    [SerializeField] private Button copyButton;
    [SerializeField] private Button clearLogButton;

    [Header("Output Log")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private ScrollRect scrollRect;

    // ── Colors (editable in inspector) ──────────────────────────
    [Header("Theme Colors")]
    [SerializeField] private Color accentColor = new Color(0.30f, 0.65f, 1.0f);
    [SerializeField] private Color successColor = new Color(0.30f, 0.85f, 0.55f);
    [SerializeField] private Color errorColor = new Color(1.0f, 0.40f, 0.40f);
    [SerializeField] private Color warningColor = new Color(1.0f, 0.75f, 0.30f);
    [SerializeField] private Color disabledColor = new Color(0.35f, 0.35f, 0.40f);
    [SerializeField] private Color textColor = new Color(0.93f, 0.93f, 0.95f);
    [SerializeField] private Color textDimColor = new Color(0.60f, 0.60f, 0.65f);

    // ── State ───────────────────────────────────────────────────
    private bool _monitoringActive;
    private bool _loggingEnabled;
    private bool _geoEnabled;
    private string _sessionId;
    private string _fingerprint;

    void Start()
    {
        // Wire up button listeners
        if (getFingerprintButton) getFingerprintButton.onClick.AddListener(OnGetFingerprint);
        if (startMonitorButton) startMonitorButton.onClick.AddListener(OnStartMonitoring);
        if (stopMonitorButton) stopMonitorButton.onClick.AddListener(OnStopMonitoring);
        if (toggleLoggingButton) toggleLoggingButton.onClick.AddListener(OnToggleLogging);
        if (toggleGeoButton) toggleGeoButton.onClick.AddListener(OnToggleGeo);
        if (copyButton) copyButton.onClick.AddListener(OnCopy);
        if (clearLogButton) clearLogButton.onClick.AddListener(() => { if (outputText) outputText.text = ""; });

        // Generate a session ID and set it
        _sessionId = "test-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        SeonSDK.SetSessionId(_sessionId);

        UpdateButtonStates();
        Log($"SEON SDK Test Scene ready.\nSession: <color=#{ColorUtility.ToHtmlStringRGB(accentColor)}>{_sessionId}</color>", textDimColor);
        SetStatus($"Session: {_sessionId}");
    }

    // ═══════════════════════════════════════════════════════════
    // SDK Actions
    // ═══════════════════════════════════════════════════════════

    private void OnGetFingerprint()
    {
        Log("<b>GetFingerprint</b>...", textDimColor);
        SetStatus("Getting fingerprint...");
        var startTime = Time.realtimeSinceStartup;

        SeonSDK.GetFingerprint(
            onSuccess: fp =>
            {
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                Log($"✅ <b>Fingerprint</b> ({fp.Length} chars, {elapsed:F0}ms):\n<color=#{ColorUtility.ToHtmlStringRGB(accentColor)}>{fp}</color>", successColor);
                SetStatus($"Fingerprint OK — {fp.Length} chars");
                _fingerprint = fp;
                Debug.Log($"<b>Fingerprint</b> ({fp.Length} chars, {elapsed:F0}ms):\n{fp}");
            },
            onError: err =>
            {
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                Log($"❌ <b>Fingerprint</b> error [{err.Code}] ({elapsed:F0}ms):\n{err.Message}" +
                    (err.GeolocationStatusCode.HasValue ? $"\n   GeoStatus: {err.GeolocationStatusCode}" : ""),
                    errorColor);
                SetStatus($"Fingerprint error [{err.Code}]");
            });
    }

    private void OnStartMonitoring()
    {
        try
        {
            SeonSDK.StartBehaviourMonitoring();
            _monitoringActive = true;
            Log("🟢 <b>Behaviour monitoring started</b>\n   Interact with the device, then press Stop.", successColor);
            SetStatus("Monitoring active...");
        }
        catch (Exception ex)
        {
            Log($"❌ <b>StartMonitoring</b> failed: {ex.Message}", errorColor);
        }
        UpdateButtonStates();
    }

    private void OnStopMonitoring()
    {
        Log("⏳ <b>StopBehaviourMonitoring</b>...", textDimColor);
        var startTime = Time.realtimeSinceStartup;

        SeonSDK.StopBehaviourMonitoring(
            onSuccess: fp =>
            {
                _monitoringActive = false;
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                Log($"✅ <b>Behaviour fingerprint</b> ({fp.Length} chars, {elapsed:F0}ms):\n<color=#{ColorUtility.ToHtmlStringRGB(accentColor)}>{fp}</color>", successColor);
                SetStatus($"Behaviour FP OK — {fp.Length} chars");
                _fingerprint = fp;
                Debug.Log($"<b>Behaviour fingerprint</b> ({fp.Length} chars, {elapsed:F0}ms):\n{fp}");
                UpdateButtonStates();
            },
            onError: err =>
            {
                _monitoringActive = false;
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                Log($"❌ <b>Behaviour</b> error [{err.Code}] ({elapsed:F0}ms):\n{err.Message}", errorColor);
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
            Log($"📝 <b>Logging</b>: {(_loggingEnabled ? "ON" : "OFF")}", _loggingEnabled ? successColor : warningColor);
        }
        catch (Exception ex)
        {
            Log($"❌ <b>SetLogging</b> failed: {ex.Message}", errorColor);
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
            Log($"📍 <b>Geolocation</b>: {(_geoEnabled ? "ON (prefetch, 5s timeout)" : "OFF")}", _geoEnabled ? successColor : warningColor);
        }
        catch (Exception ex)
        {
            Log($"❌ <b>SetGeoConfig</b> failed: {ex.Message}", errorColor);
        }
        UpdateButtonStates();
    }

    private void OnCopy()
    {
        if (!string.IsNullOrEmpty(_fingerprint))
        {
            GUIUtility.systemCopyBuffer = _fingerprint;
            Log("📋 Fingerprint copied to clipboard.", textDimColor);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private void Log(string message, Color color)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string dimHex = ColorUtility.ToHtmlStringRGB(textDimColor);
        string entry = $"<color=#{dimHex}>[{timestamp}]</color> <color=#{colorHex}>{message}</color>";

        if (outputText != null)
        {
            if (!string.IsNullOrEmpty(outputText.text))
                outputText.text += "\n\n" + entry;
            else
                outputText.text = entry;

            // Auto-scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        Debug.Log($"[SeonTest] {message}");
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    private void UpdateButtonStates()
    {
        SetButtonInteractable(startMonitorButton, !_monitoringActive);
        SetButtonInteractable(stopMonitorButton, _monitoringActive);

        SetButtonLabel(toggleLoggingButton, _loggingEnabled ? "Logging: ON" : "Logging: OFF");
        SetButtonLabel(toggleGeoButton, _geoEnabled ? "Geo: ON" : "Geo: OFF");
    }

    private void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn == null) return;
        btn.interactable = interactable;
        var colors = btn.colors;
        colors.disabledColor = disabledColor;
        btn.colors = colors;
    }

    private void SetButtonLabel(Button btn, string label)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = label;
    }
}
