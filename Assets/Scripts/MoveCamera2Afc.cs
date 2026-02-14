using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public partial class MoveCamera : MonoBehaviour
{
    // ------ 2AFC fields (moved) ------
    // CN: 表示当前是否处于 2AFC 任务流程中（用于暂停其它交互）
    // EN: Whether currently running a 2AFC sequence (used to gate other interactions)
    // JP: 現在 2AFC シーケンス中かどうか（他の操作を抑制するために使用）
    private bool in2AfcMode = false;

    // CN: 动态创建的 2AFC UI 容器
    // EN: Dynamically created 2AFC UI panel GameObject
    // JP: 動的に作成される 2AFC UI パネルの GameObject
    private GameObject _2afcPanel;

    // CN: 上/下 2AFC 选择按钮引用
    // EN: References to upper and lower choice buttons
    // JP: 上／下の選択ボタン参照
    private Button _2afcUpperButton;
    private Button _2afcLowerButton;

    // CN: 试次索引与随机顺序
    // EN: Trial index and randomized order list (true => top shows Linear)
    // JP: トライアルインデックスとランダム順序リスト（true = 上が Linear を表示）
    private int _2afcTrialIndex = 0;
    private List<bool> _2afcOrder = new List<bool>(); // true => top shows Linear

    // CN: 是否等待被试响应（UI 阶段）
    // EN: Whether waiting for subject response (UI phase)
    // JP: 被験者の応答待ちかどうか（UI フェーズ）
    private bool _2afcWaitingForResponse = false;

    // CN: 2AFC 时长与入口相关 UI
    // EN: 2AFC duration and intro UI references
    // JP: 2AFC の継続時間とイントロ UI 参照
    public float twoAfcDurationSec = 10f; // default (can be overridden in Inspector)
    private GameObject _2afcIntroPanel;
    private Button _2afcIntroStartButton;

    // CN: 仅运行 2AFC 模式开关 与试次数（可由 Inspector 配置）
    // EN: only-2AFC mode toggle and number of trials (configurable in Inspector)
    // JP: 2AFC のみモード切替と試行回数（Inspector で設定可）
    public bool only2AfcMode = false;
    public int twoAfcTrials = 20;
    // ------------------------

    /// <summary>
    /// Start2AfcTrials
    /// CN: 从当前状态进入 2AFC 序列：保存当前调参数据（非 only2Afc），生成随机试次顺序并启动协程。
    /// EN: Enter 2AFC sequence from current state: optionally save current data, randomize trial order and start coroutine.
    /// JP: 現在の状態から 2AFC シーケンスを開始：必要ならデータ保存、試行順をランダム化してコルーチンを開始。
    /// </summary>
    public void Start2AfcTrials()
    {
        Time.timeScale = 1f;
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        mouseClicked = true;

        if (!only2AfcMode) SaveCurrentDataToCsv();

        if (frames == null || frames.Length == 0) { Debug.LogError("Cannot start 2AFC: frames not loaded."); return; }

        Set2AfcLayout(true);

        int trials = Mathf.Max(1, twoAfcTrials);
        _2afcOrder.Clear();
        int half = trials / 2;
        for (int i = 0; i < half; i++) _2afcOrder.Add(true);
        for (int i = 0; i < trials - half; i++) _2afcOrder.Add(false);

        var rnd = new System.Random(subjectSeed == 0 ? DateTime.Now.GetHashCode() : subjectSeed);
        _2afcOrder = _2afcOrder.OrderBy(x => rnd.Next()).ToList();

        _2afcTrialIndex = 0;
        StartCoroutine(Run2AfcSequence());
    }

    /// <summary>
    /// Run2AfcSequence 协程
    /// CN: 逐试次执行播放两段刺激（top/bottom），随后显示 UI 等待响应并保存结果。
    /// EN: Coroutine that runs trials: present two intervals (top/bottom), wait for response and log result.
    /// JP: 各トライアルを実行するコルーチン：2区間提示、応答待ち、結果記録。
    /// </summary>
    private IEnumerator Run2AfcSequence()
    {
        in2AfcMode = true;
        Material matLinearInstance = new Material(Mat_GrayscaleOverBlend);
        Material matGaussInstance = new Material(GaussBlendMat);

        if (CaptureCameraLinearBlendRawImage != null) CaptureCameraLinearBlendRawImage.gameObject.SetActive(true);
        if (CaptureCameraLinearBlendTopRawImage != null) CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(true);

        while (_2afcTrialIndex < _2afcOrder.Count)
        {
            bool topIsLinear = _2afcOrder[_2afcTrialIndex];
            Debug.Log($"2AFC trial {_2afcTrialIndex + 1}/{_2afcOrder.Count} topIsLinear={topIsLinear}");

            Set2AfcLayout(true);
            PositionForTopIsLinear(topIsLinear);

            if (matLinearInstance != null && CaptureCameraLinearBlendRawImage != null)
                CaptureCameraLinearBlendRawImage.material = matLinearInstance;
            if (matGaussInstance != null && CaptureCameraLinearBlendTopRawImage != null)
                CaptureCameraLinearBlendTopRawImage.material = matGaussInstance;

            if (topIsLinear)
            {
                CaptureCameraLinearBlendRawImage.transform.SetAsLastSibling();
                CaptureCameraLinearBlendTopRawImage.transform.SetSiblingIndex(0);
            }
            else
            {
                CaptureCameraLinearBlendTopRawImage.transform.SetAsLastSibling();
                CaptureCameraLinearBlendRawImage.transform.SetSiblingIndex(0);
            }

            float elapsed = 0f;
            int localFrameNum = 1;
            float frameMs = updateInterval * 1000f;
            Debug.Log($"Run2AfcSequence: twoAfcDurationSec={twoAfcDurationSec} Time.timeScale={Time.timeScale}");
            ResetGaussWarmup();

            // CN: 播放刺激段（在此循环内按 unscaled 时间推进并更新材质参数）
            // EN: Present stimuli segment, advance with unscaled time and update material parameters
            // JP: 刺激提示を行うループ（unscaled 時間で進行しマテリアルを更新）
            while (elapsed < twoAfcDurationSec)
            {
                elapsed += Time.unscaledDeltaTime;
                float tGlobalMs = elapsed * 1000f;
                while (tGlobalMs >= localFrameNum * frameMs) localFrameNum++;

                int n = frames.Length;
                int prevIdx = Mathf.Clamp(localFrameNum - 1, 0, n - 1);
                int nextIdx = Mathf.Clamp(localFrameNum, 0, n - 1);
                Texture linearBotTex = frames[prevIdx];
                Texture linearTopTex = frames[nextIdx];
                float Image1ToNowDeltaTime = tGlobalMs - (localFrameNum - 1) * updateInterval * 1000f;
                float linearAlpha = Mathf.Clamp01(Image1ToNowDeltaTime / (updateInterval * 1000f));

                float step = Mathf.Max(1e-6f, secondsPerStep);
                float sigmaStep = Mathf.Max(1e-4f, sigmaSec);
                float halfFrame = 0.5f / 60f;
                float u = ((tGlobalMs / 1000f) + halfFrame) / step;
                int c = Mathf.RoundToInt(u);
                int i0 = Mathf.Clamp(c - 1, 0, n - 1);
                int i1 = Mathf.Clamp(c, 0, n - 1);
                int i2 = Mathf.Clamp(c + 1, 0, n - 1);
                float d0 = (i0 - u) / sigmaStep;
                float d1 = (i1 - u) / sigmaStep;
                float d2 = (i2 - u) / sigmaStep;
                float w0 = Mathf.Exp(-0.5f * d0 * d0);
                float w1 = Mathf.Exp(-0.5f * d1 * d1);
                float w2 = Mathf.Exp(-0.5f * d2 * d2);
                float s = w0 + w1 + w2;
                if (s > 1e-12f) { w0 /= s; w1 /= s; w2 /= s; }
                else { w0 = 0f; w1 = 1f; w2 = 0f; }

                if (matLinearInstance != null)
                {
                    matLinearInstance.SetTexture("_TopTex", linearTopTex);
                    matLinearInstance.SetTexture("_BottomTex", linearBotTex);
                    matLinearInstance.SetColor("_TopColor", new Color(1f, 1f, 1f, linearAlpha));
                    matLinearInstance.SetColor("_BottomColor", new Color(1f, 1f, 1f, 1f));
                }
                if (matGaussInstance != null)
                {
                    matGaussInstance.SetTexture("_Tex0", frames[i0]);
                    matGaussInstance.SetTexture("_Tex1", frames[i1]);
                    matGaussInstance.SetTexture("_Tex2", frames[i2]);
                    matGaussInstance.SetFloat("_W0", w0);
                    matGaussInstance.SetFloat("_W1", w1);
                    matGaussInstance.SetFloat("_W2", w2);
                }

                yield return null;
            }

            // CN: 播放结束后显示选择 UI 并等待响应（使用局部 handler 来记录结果）
            // EN: After presentation, show choice UI and wait for response; local handler logs the result
            // JP: 提示後に選択 UI を表示して応答を待ち、ローカルハンドラで結果を記録
            bool _waitingFor2AfcResponse = true;
            bool uiShown = false;
            try
            {
                _2afcWaitingForResponse = true;
                Show2AfcButtons();
                uiShown = true;

                void LocalHandler(bool upperSelected)
                {
                    string setting = topIsLinear ? "TopLinear_BottomGauss" : "TopGauss_BottomLinear";
                    string choice = upperSelected ? "Upper" : "Lower";
                    data.Add($"2AFC,Trial{_2afcTrialIndex + 1},{setting},Choice,{choice},duration,{twoAfcDurationSec:F1}");

                    string csvDir = Path.Combine(@"D:\vectionProject\public", folderName);
                    string safeParticipant = string.IsNullOrEmpty(participantName)
                        ? "Unknown"
                        : new string(participantName.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)).ToArray());
                    string fileName = $"ParticipantName_{safeParticipant}_2AFC_results.csv";
                    string csvPath = Path.Combine(csvDir, fileName);
                    string header = "Trial,Setting,Choice,Duration,Timestamp";
                    string line = $"{_2afcTrialIndex + 1},{setting},{choice},{twoAfcDurationSec:F1},{DateTime.Now:O}";
                    AppendCsvLine(csvPath, header, line);

                    _waitingFor2AfcResponse = false;
                }

                if (_2afcUpperButton != null) _2afcUpperButton.onClick.AddListener(() => LocalHandler(true));
                if (_2afcLowerButton != null) _2afcLowerButton.onClick.AddListener(() => LocalHandler(false));

                while (_waitingFor2AfcResponse)
                    yield return null;
            }
            finally
            {
                _2afcWaitingForResponse = false;
                if (uiShown)
                {
                    if (_2afcUpperButton != null) _2afcUpperButton.onClick.RemoveAllListeners();
                    if (_2afcLowerButton != null) _2afcLowerButton.onClick.RemoveAllListeners();
                }
                Cleanup2AfcUI();
            }

            _2afcTrialIndex++;
        }

        // CN: 全部试次完成后的清理与退出
        // EN: Cleanup after all trials and exit experiment
        // JP: 全トライアル終了後のクリーンアップと終了
        in2AfcMode = false;
        Set2AfcLayout(false);

        CaptureCameraLinearBlendRawImage.material = new Material(Mat_GrayscaleOverBlend);
        CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);
        CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);
        _Gaussmat = new Material(GaussBlendMat);
        CaptureCameraLinearBlendTopRawImage.material = _Gaussmat;

        Debug.Log("2AFC sequence finished.");
        QuitGame();
    }

    /// <summary>
    /// GetSafeUiFont
    /// CN: 尝试获取安全的 UI 字体（优先系统 Arial，其次内置 Arial）。
    /// EN: Try to get a safe UI font (prefer OS Arial, fallback to builtin).
    /// JP: UI 用の安全なフォントを取得（OS の Arial を優先、組込の Arial をフォールバック）。
    /// </summary>
    private Font GetSafeUiFont()
    {
        try { var f = Font.CreateDynamicFontFromOSFont("Arial", 14); if (f != null) return f; } catch { }
        try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { Debug.LogWarning("GetSafeUiFont fail"); return null; }
    }

    /// <summary>
    /// Show2AfcButtons
    /// CN: 动态创建并显示上/下选择按钮与提示文本（在 _2afcPanel 下）。
    /// EN: Dynamically create and show upper/lower choice buttons and question text under _2afcPanel.
    /// JP: 上／下の選択ボタンと質問文を _2afcPanel 下に動的に作成して表示。
    /// </summary>
    private void Show2AfcButtons()
    {
        if (_2afcPanel == null)
        {
            _2afcPanel = new GameObject("2AFC_Panel");
            var rt = _2afcPanel.AddComponent<RectTransform>();
            _2afcPanel.transform.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _2afcPanel.transform.SetAsLastSibling();
        }

        Font uiFont = GetSafeUiFont();

        var bgGO = new GameObject("2AFC_Background");
        bgGO.transform.SetParent(_2afcPanel.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        bgGO.transform.SetSiblingIndex(0);

        if (uiFont != null)
        {
            var qGO = new GameObject("2AFC_Question");
            qGO.transform.SetParent(_2afcPanel.transform, false);
            var qRT = qGO.AddComponent<RectTransform>();
            qRT.anchorMin = new Vector2(0.1f, 0.92f); qRT.anchorMax = new Vector2(0.9f, 0.99f);
            var qText = qGO.AddComponent<Text>();
            qText.alignment = TextAnchor.MiddleCenter; qText.font = uiFont;
            qText.text = "どちらが等速に近いでしょうか?"; qText.fontSize = 48; qText.color = Color.white;
            qGO.transform.SetAsLastSibling();
        }

        var upperGO = new GameObject("2AFC_UpperButton"); upperGO.transform.SetParent(_2afcPanel.transform, false);
        var upperRT = upperGO.AddComponent<RectTransform>(); upperRT.anchorMin = new Vector2(0.25f, 0.55f); upperRT.anchorMax = new Vector2(0.75f, 0.9f);
        var upperImg = upperGO.AddComponent<Image>(); upperImg.color = new Color(0f, 0f, 0f, 0.15f);
        _2afcUpperButton = upperGO.AddComponent<Button>();
        if (uiFont != null) { var ut = new GameObject("Text"); ut.transform.SetParent(upperGO.transform, false); var utText = ut.AddComponent<Text>(); utText.font = uiFont; utText.text = "上"; utText.alignment = TextAnchor.MiddleCenter; utText.fontSize = 64; utText.color = Color.white; }

        var lowerGO = new GameObject("2AFC_LowerButton"); lowerGO.transform.SetParent(_2afcPanel.transform, false);
        var lowerRT = lowerGO.AddComponent<RectTransform>(); lowerRT.anchorMin = new Vector2(0.25f, 0.1f); lowerRT.anchorMax = new Vector2(0.75f, 0.45f);
        var lowerImg = lowerGO.AddComponent<Image>(); lowerImg.color = new Color(0f, 0f, 0f, 0.15f);
        _2afcLowerButton = lowerGO.AddComponent<Button>();
        if (uiFont != null) { var lt = new GameObject("Text"); lt.transform.SetParent(lowerGO.transform, false); var ltText = lt.AddComponent<Text>(); ltText.font = uiFont; ltText.text = "下"; ltText.alignment = TextAnchor.MiddleCenter; ltText.fontSize = 64; ltText.color = Color.white; }
    }

    /// <summary>
    /// Cleanup2AfcUI
    /// CN: 销毁动态创建的 2AFC UI 元素并清理引用。
    /// EN: Destroy dynamically created 2AFC UI elements and clear references.
    /// JP: 動的に生成した 2AFC UI 要素を破棄し参照をクリア。
    /// </summary>
    private void Cleanup2AfcUI()
    {
        if (_2afcPanel != null) { Destroy(_2afcPanel); _2afcPanel = null; _2afcUpperButton = null; _2afcLowerButton = null; }
    }

    /// <summary>
    /// Show2AfcIntroPanel
    /// CN: 在试验结束/过渡点显示 2AFC 说明面板，用户点击“开始”后进入 Start2AfcTrials。
    /// EN: Show intro panel before 2AFC; start trials after participant presses Start.
    /// JP: 2AFC 実験前のイントロパネルを表示し、開始ボタンで試行を開始。
    /// </summary>
    private void Show2AfcIntroPanel()
    {
        if (_2afcIntroPanel != null) return;
        if (canvas == null) canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogWarning("Show2AfcIntroPanel: Canvas not found."); return; }

        _2afcIntroPanel = new GameObject("2AFC_IntroPanel");
        var rt = _2afcIntroPanel.AddComponent<RectTransform>();
        _2afcIntroPanel.transform.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var bg = _2afcIntroPanel.AddComponent<Image>(); bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        _2afcIntroPanel.transform.SetAsLastSibling();
        Font uiFont = GetSafeUiFont();

        var textGO = new GameObject("2AFC_IntroText"); textGO.transform.SetParent(_2afcIntroPanel.transform, false);
        var textRT = textGO.AddComponent<RectTransform>(); textRT.anchorMin = new Vector2(0.1f, 0.55f); textRT.anchorMax = new Vector2(0.9f, 0.85f);
        var txt = textGO.AddComponent<Text>(); txt.alignment = TextAnchor.MiddleCenter; txt.font = uiFont;
        txt.text = "接下来将进行 2AFC 任务：每次会播放两段刺激，请判断哪个更接近匀速。\n当您准备好时请按下面的“开始”按钮。"; txt.fontSize = 30; txt.color = Color.white;

        var btnGO = new GameObject("2AFC_StartButton"); btnGO.transform.SetParent(_2afcIntroPanel.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>(); btnRT.anchorMin = new Vector2(0.35f, 0.2f); btnRT.anchorMax = new Vector2(0.65f, 0.32f);
        var img = btnGO.AddComponent<Image>(); img.color = new Color(0.1f, 0.6f, 0.9f, 1f);
        _2afcIntroStartButton = btnGO.AddComponent<Button>();

        if (uiFont != null)
        {
            var btTextGO = new GameObject("Text"); btTextGO.transform.SetParent(btnGO.transform, false);
            var btText = btTextGO.AddComponent<Text>(); btText.font = uiFont; btText.text = "开始"; btText.alignment = TextAnchor.MiddleCenter; btText.fontSize = 28; btText.color = Color.white;
            var btRT = btTextGO.GetComponent<RectTransform>(); btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
        }

        _2afcIntroStartButton.onClick.AddListener(() =>
        {
            mouseClicked = true;
            if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
            Time.timeScale = 1f;
            Cleanup2AfcIntroPanel();
            Start2AfcTrials();
        });
    }

    /// <summary>
    /// Cleanup2AfcIntroPanel
    /// CN: 销毁 2AFC intro 面板并移除事件监听器。
    /// EN: Destroy intro panel and remove listeners.
    /// JP: イントロパネルを破棄しリスナを削除。
    /// </summary>
    private void Cleanup2AfcIntroPanel()
    {
        if (_2afcIntroStartButton != null) _2afcIntroStartButton.onClick.RemoveAllListeners();
        if (_2afcIntroPanel != null) { Destroy(_2afcIntroPanel); _2afcIntroPanel = null; _2afcIntroStartButton = null; }
    }

    // Position helpers used by 2AFC UI layout
    // CN: 根据 topIsLinear 布局调整 top/bottom RawImage 的 anchoredPosition 与层级
    // EN: Position top/bottom RawImages and sibling order according to topIsLinear
    // JP: topIsLinear に従って RawImage の位置と兄弟順を設定
    void PositionForTopIsLinear(bool topIsLinear)
    {
        if (CaptureCameraLinearBlendTopRawImage == null || CaptureCameraLinearBlendRawImage == null) return;
        RectTransform rtLinear = CaptureCameraLinearBlendRawImage.GetComponent<RectTransform>();
        RectTransform rtGauss = CaptureCameraLinearBlendTopRawImage.GetComponent<RectTransform>();
        float topY = 353f; float botY = -356f;
        if (topIsLinear) { rtLinear.anchoredPosition = new Vector2(rtLinear.anchoredPosition.x, topY); rtGauss.anchoredPosition = new Vector2(rtGauss.anchoredPosition.x, botY); CaptureCameraLinearBlendRawImage.transform.SetAsLastSibling(); CaptureCameraLinearBlendTopRawImage.transform.SetSiblingIndex(0); }
        else { rtGauss.anchoredPosition = new Vector2(rtGauss.anchoredPosition.x, topY); rtLinear.anchoredPosition = new Vector2(rtLinear.anchoredPosition.x, botY); CaptureCameraLinearBlendTopRawImage.transform.SetAsLastSibling(); CaptureCameraLinearBlendRawImage.transform.SetSiblingIndex(0); }
    }

    /// <summary>
    /// Set2AfcLayout
    /// CN: 启用/禁用 2AFC 布局（隐藏或显示 continuous UI，调整位置）。
    /// EN: Enable/disable 2AFC layout (hide/show continuous UI and adjust positions).
    /// JP: 2AFC レイアウトを有効/無効にし、continuous UI の表示と位置を調整。
    /// </summary>
    void Set2AfcLayout(bool enable2Afc)
    {
        if (continuousImageRawImage != null) continuousImageRawImage.gameObject.SetActive(!enable2Afc);
        RectTransform rtTop = CaptureCameraLinearBlendTopRawImage != null ? CaptureCameraLinearBlendTopRawImage.GetComponent<RectTransform>() : null;
        RectTransform rtBot = CaptureCameraLinearBlendRawImage != null ? CaptureCameraLinearBlendRawImage.GetComponent<RectTransform>() : null;

        if (enable2Afc)
        {
            if (rtTop != null) rtTop.anchoredPosition = new Vector2(rtTop.anchoredPosition.x, 353f);
            if (rtBot != null) rtBot.anchoredPosition = new Vector2(rtBot.anchoredPosition.x, -356f);
            if (CaptureCameraLinearBlendRawImage != null) { CaptureCameraLinearBlendRawImage.gameObject.SetActive(true); CaptureCameraLinearBlendRawImage.enabled = true; CaptureCameraLinearBlendRawImage.transform.SetSiblingIndex(0); }
            if (CaptureCameraLinearBlendTopRawImage != null) { CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(true); CaptureCameraLinearBlendTopRawImage.enabled = true; CaptureCameraLinearBlendTopRawImage.transform.SetAsLastSibling(); }
        }
        else
        {
            if (rtTop != null) rtTop.anchoredPosition = new Vector2(rtTop.anchoredPosition.x, -356f);
            if (rtBot != null) rtBot.anchoredPosition = new Vector2(rtBot.anchoredPosition.x, -356f);
            if (CaptureCameraLinearBlendRawImage != null) { CaptureCameraLinearBlendRawImage.gameObject.SetActive(true); CaptureCameraLinearBlendRawImage.enabled = true; CaptureCameraLinearBlendRawImage.transform.SetAsLastSibling(); }
            if (CaptureCameraLinearBlendTopRawImage != null) { CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(true); CaptureCameraLinearBlendTopRawImage.enabled = true; CaptureCameraLinearBlendTopRawImage.transform.SetSiblingIndex(0); }
            if (continuousImageRawImage != null) { var crt = continuousImageRawImage.GetComponent<RectTransform>(); if (crt != null) crt.anchoredPosition = new Vector2(crt.anchoredPosition.x, 353f); continuousImageRawImage.gameObject.SetActive(true); continuousImageRawImage.transform.SetAsLastSibling(); }
        }
    }
}