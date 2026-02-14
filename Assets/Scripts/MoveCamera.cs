using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text;
using System.Linq;
using System.Reflection; // ensure at top of file if not already

#if UNITY_EDITOR
using UnityEditor;
#endif


public partial class MoveCamera : MonoBehaviour
{
    // ----------------------------------------------------------------
    // 文件说明 / File overview / ファイル概要
    // CN: 本文件包含主运行流程与核心方法实现（不重复定义字段，字段应在 MoveCamera.Fields.cs 中声明）
    // EN: Contains main runtime flow and core method implementations (fields expected in MoveCamera.Fields.cs)
    // JP: 主要な実行フローとコアメソッドの実装（フィールドは MoveCamera.Fields.cs に定義）
    // ----------------------------------------------------------------

    // =========================
    // 生命周期 / Lifecycle / ライフサイクル
    // =========================

    // Start: 初始化设置
    // CN: 对数刻度与启动初始化（包含中/英/日注释）
    // EN: Initial setup and initialization of runtime parameters
    // JP: 初期設定とランタイムパラメータの初期化
    void Start()
    {
        // 禁用垂直同步以便控制帧 / Disable v-sync to control framerate / 垂直同期を無効化
        QualitySettings.vSyncCount = 0;
        // 固定帧时长 / Ensure fixed delta time (approx 60Hz) / 固定フレーム間隔を設定 (約60Hz)
        Time.fixedDeltaTime = 1.0f / 60.0f;
        if (fps <= 0f) fps = 60f;
        updateInterval = 1f / fps; // 各フレーム的显示间隔时间 // per-frame display interval
        captureIntervalDistance = cameraSpeed / Mathf.Max(1f, fps); // 每帧移动距离 // per-frame displacement

        GetRawImage();
        InitialSetup();

        continuousImageRawImage.enabled = true;

        // 把 camera2 / camera3 提前一/二帧的位置（用于三相机流水）
        captureCamera2.transform.position += direction * captureIntervalDistance;
        SerialReader = GetComponent<SerialReader>();

        TrailSettings();

        if (nextStepButton != null)
        {
            nextStepButtonTextComponent = nextStepButton.GetComponentInChildren<TextMeshProUGUI>();
            nextStepButton.onClick.RemoveAllListeners();
            nextStepButton.onClick.AddListener(OnNextStep); // 绑定下一步按钮 // bind Next button
        }

        // data 表头初始化（用于记录）
        data.Add(
          "Mode," +
          "BackFrameNum,BackWeight," +
          "MidFrameNum,MidWeight," +
          "FrontFrameNum,FrontWeight," +
          "TimeMs,Knob,ResponsePattern,StepNumber,Amplitude,Velocity,CameraSpeed"
        );

        // 设定资源文件夹与前缀并加载帧资源
        resourcesFolder = string.IsNullOrEmpty(resourcesFolder) ? "CamFrames" : resourcesFolder;
        namePrefix = string.IsNullOrEmpty(namePrefix) ? "cam1_" : namePrefix;
        Debug.Log($"[ForceConfig] folder='{resourcesFolder}' prefix='{namePrefix}'");
        EnsureFramesLoaded();
    }

    // Show intro next frame (ensures Canvas exists)
    private IEnumerator Show2AfcIntroNextFrame()
    {
        // 等一帧以确保 Canvas / UI 已初始化 / wait one frame to ensure UI ready
        yield return null;
        Debug.Log($"Show2AfcIntroNextFrame: only2AfcMode={only2AfcMode}");
        Show2AfcIntroPanel();
    }

    // Awake: 启动前准备（文件路径、only2Afc 跳过逻辑等）
    // EN: Pre-start checks and only2Afc mode handling
    // JP: 起動前のチェックと only2Afc モードの処理
    void Awake()
    {
        // 保证 stepNumber 有默认值 / ensure sane default
        if ((int)stepNumber < 1) stepNumber = StepNumber.Option1;
        Debug.Log($"[MoveCamera] stepNumber = {(int)stepNumber} ({stepNumber}) in Awake");

        savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");
        Debug.Log($"[MoveCamera] trial savePath = {savePath}");

        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Trial file not found. Please run: Tools → Generate initial trial file");
            isEnd = true;
            return;
        }

        // 如果 only2AfcMode 打开，则直接跳到结束状态以跳过调参流程
        if (only2AfcMode)
        {
            Debug.Log("Awake: only2AfcMode enabled -> forcing currentStep=5 and isEnd=true to skip calibration.");
            currentStep = 5;
            isEnd = true;
        }

        // 默认为 10s（可以在 Inspector 覆盖或在 Awake 中强制赋值）
        twoAfcDurationSec = twoAfcDurationSec <= 0f ? 10f : twoAfcDurationSec;
    }

    // Update: 处理 UI 点击与交互（使用非物理输入检测）
    // EN: Handles mouse clicks & UI activation
    // JP: マウスクリックとUIの処理
    void Update()
    {
        // 在 2AFC 序列中不要响应此处的全局点击（以免干扰按键 UI）
        if (in2AfcMode) return;

        // 鼠标左键在一帧内检测 / detect single-frame mouse press
        if (!mouseClicked && Input.GetMouseButtonDown(0))
        {
            // 将 captureCamera0 位置设为 captureCamera1（根据原逻辑）
            if (captureCamera0 != null && captureCamera1 != null)
                captureCamera0.transform.position = captureCamera1.transform.position;

            mouseClicked = true;

            // 显示下一步按钮并暂停 timeScale 以便被试读说明
            if (nextStepButton != null)
                nextStepButton.gameObject.SetActive(true);
            Time.timeScale = 0f;

            // 根据 currentStep 设置按钮文字
            if (nextStepButtonTextComponent != null)
            {
                switch (currentStep)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        nextStepButtonTextComponent.text = "Next Step";
                        break;
                    case 4:
                        nextStepButtonTextComponent.text = "Entering the next trial";
                        break;
                }
            }
        }
    }

    // FixedUpdate: 固定步进的时间推进（用于确定性逻辑）
    // EN: Advances fixed-step logical time and drives Continuous() / LuminanceMixtureFrame()
    // JP: 固定ステップ時間を進め、連続処理とフレーム混合を駆動
    void FixedUpdate()
    {
        // timeMs 以 fixedDeltaTime 推进，保持与 FixedUpdate 对齐
        timeMs = fixedUpdateCounter * Time.fixedDeltaTime * 1000f;
        float tSec = timeMs / 1000f;

        if (tSec > CaptureSeconds)
        {
            ResetCamerasAndBlendState();
        }

        // 计算当前处于第几个 step 区间（通常以 secondsPerStep 作为步长）
        stepIndex = Mathf.FloorToInt(tSec / Mathf.Max(1e-6f, secondsPerStep));

        // 运行连续刺激逻辑（使用 fixed 步进）
        Continuous();

        // 更新混合/渲染逻辑
        LuminanceMixtureFrame();

        if (Application.isPlaying)
        {
            velocityHistory.Add(v);
        }

        // 帧计数器在帧末自增，保证本帧使用当前计数值
        fixedUpdateCounter++;
    }

    // OnNextStep: 下一步按钮回调（控制试次推进）
    // EN: Advance to the next calibration step or show 2AFC intro at the end
    // JP: 次のステップへ進める（終了時に 2AFC を表示）
    void OnNextStep()
    {
        mouseClicked = false;
        Time.timeScale = 1f;
        currentStep++;
        responsePattern = ResponsePattern.Amplitude;
        ResetCamerasAndBlendState();

        switch (currentStep)
        {
            case 1: stepNumber = StepNumber.Option1; break;
            case 2: stepNumber = StepNumber.Option2; break;
            case 3: stepNumber = StepNumber.Option3; break;
            case 4: stepNumber = StepNumber.Option4; break;
            case 5:
                if (isEnd)
                {
                    // 显示 2AFC 过渡说明面板，等待被试点击“开始”
                    Debug.Log("Experiment finished. Showing 2AFC intro panel.");
                    Show2AfcIntroPanel();
                }
                else
                {
                    MarkTrialCompletedAndRestart();
                }
                break;
        }

        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(false);
    }

    // Continuous: 连续播放 / 调参 主逻辑（基于 fixed-step）
    // EN: Main continuous/stimulus update (should use fixedDeltaTime)
    // JP: 連続刺激のメイン更新（fixedDeltaTime を使用）
    void Continuous()
    {
        continuousImageRawImage.enabled = true;
        time += Time.fixedDeltaTime;

        knobValue = Mathf.Clamp01(SerialReader.lastSensorValue);
        int step = (int)stepNumber;
        V0 = 1.0f;

        if (responsePattern == ResponsePattern.Velocity)
        {
            // 速度模式：旋钮直接控制基线速度 V0
            V0 = knobValue * 2f;
            v = V0;
        }
        else if (responsePattern == ResponsePattern.Amplitude)
        {
            // 调参模式：根据 step 选择不同参数映射
            if (step == 1 || step == 3) amplitudeToSaveData = Mathf.Lerp(A_min, A_max, knobValue);
            if (step == 2 || step == 4) amplitudeToSaveData = knobValue * 2f * Mathf.PI;

            // 保存到 amplitudes 数组
            amplitudes[step] = amplitudeToSaveData;

            // 根据 step 叠加不同的谐波项计算瞬时速度 v
            if (step >= 1) v = V0 + amplitudes[1] * Mathf.Sin(omega * time);
            if (step >= 2) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);
            if (step >= 3) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time);
            if (step >= 4) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);
        }

        // 连续相机移动（在主显示上实时移动）
        // 注意：这里使用 Time.deltaTime 做可视更新，但时间基准最好与记录逻辑分离（见方案 A/B/C）
        if (captureCamera0 != null) captureCamera0.transform.position += direction * v * Time.deltaTime;

        // =========================
        // CaptureCamera0: 在特定时间窗口保存 PNG（简单实现，可能导致主线程阻塞）
        // EN: Save cam0 PNGs during a time window (synchronous; consider deterministic recording)
        // JP: 特定時間ウィンドウで cam0 の PNG を保存（同期処理。決定論的録画を推奨）
        if (SaveCam0ContinuousPng)
        {
            const float SAVE_START_SEC = 14f;
            const float SAVE_END_SEC = 22f;

            // 判断是否在保存窗口中（time 用 fixed 步长累加）
            if (time >= SAVE_START_SEC && time <= SAVE_END_SEC)
            {
                // 使用固定步长估算最大帧数
                int maxFrames = Mathf.CeilToInt((SAVE_END_SEC - SAVE_START_SEC) / Time.fixedDeltaTime);

                if (_cam0SavedCount < maxFrames)
                {
                    string file = $"cam0_{fixedUpdateCounter:000000}.png";
                    string path = Path.Combine(Cam0SaveDir, file);
                    Debug.Log($"Saving continuous cam0 png: {path}");
                    CaptureAndSavePng(captureCamera0, path);
                    _cam0SavedCount++;
                }
            }

            // 额外的时长限制（旧逻辑保留）
            int maxFramesFallback = CaptureSeconds * 40;
            if (_cam0SavedCount < maxFramesFallback)
            {
                string file = $"cam0_{fixedUpdateCounter:000000}.png";
                string path = Path.Combine(Cam0SaveDir, file);
                CaptureAndSavePng(captureCamera0, path);
                _cam0SavedCount++;
            }
        }
    }

    // LuminanceMixtureFrame: 根据 brightnessBlendMode 更新 RawImage 材质与历史记录
    // EN: Update blend materials and record wave/data
    // JP: ブレンドマテリアルを更新し、データを記録
    void LuminanceMixtureFrame()
    {
        // 如果 2AFC 正在等待响应，则停止更新以固定显示
        if (in2AfcMode && _2afcWaitingForResponse)
        {
            return;
        }

        float frameMs = updateInterval * 1000f;

        // 递增 frameNum 直到 timeMs < frameNum * frameMs
        while (timeMs >= frameNum * frameMs)
        {
            frameNum++;

            // 摄像机随时间滑动（按 updateInterval）
            targetPosition = direction * cameraSpeed * updateInterval;
            if (captureCamera1 != null) captureCamera1.transform.position += targetPosition;
            if (captureCamera2 != null) captureCamera2.transform.position += targetPosition;

            // CaptureCamera1 每个 updateInterval 记录一次 PNG / ROI
            if (SaveCam1IsiPng)
            {
                CaptureSeconds = 60; // 可配置
                int maxFrames = Mathf.CeilToInt(CaptureSeconds / Mathf.Max(1e-6f, updateInterval));
                if (_cam1SavedCount < maxFrames)
                {
                    string dir = Path.Combine(Application.dataPath, "Resources", resourcesFolder);
                    Directory.CreateDirectory(dir);
                    string file = $"cam1_{_cam1SavedCount:000}.png";
                    string path = Path.Combine(dir, file);
                    //CaptureAndSavePng(captureCamera1, path); // 若需保存 PNG 可启用
                    string roiCsvAll = Path.Combine(dir, "rois.csv");
                    SaveRoiMetadataForFrame(treeRenderers, captureCamera1, _cam1SavedCount, file, roiCsvAll);
                    _cam1SavedCount++;
                }
            }
        }

        // frames 必须预先加载
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("frames not loaded. Put images under Assets/Resources/... and load into frames[].");
            return;
        }

        // 若非 2AFC 模式，依据 brightnessBlendMode 自动切换显示（避免覆盖 2AFC 布局）
        if (!in2AfcMode)
        {
            if (CaptureCameraLinearBlendRawImage != null)
                CaptureCameraLinearBlendRawImage.gameObject.SetActive(brightnessBlendMode == BrightnessBlendMode.LinearOnly);

            if (CaptureCameraLinearBlendTopRawImage != null)
                CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(brightnessBlendMode == BrightnessBlendMode.GaussOnly);
        }

        switch (brightnessBlendMode)
        {
            case BrightnessBlendMode.LinearOnly:
                {
                    int n = frames.Length;
                    int prevIdx = Mathf.Clamp(frameNum - 1, 0, n - 1);
                    int nextIdx = Mathf.Clamp(frameNum, 0, n - 1);

                    Texture botTex = frames[prevIdx];
                    Texture topTex = frames[nextIdx];

                    var mat = CaptureCameraLinearBlendRawImage.material;
                    mat.SetTexture("_TopTex", topTex);
                    mat.SetTexture("_BottomTex", botTex);

                    float Image1ToNowDeltaTime = timeMs - (frameNum - 1) * updateInterval * 1000f;
                    float p = Mathf.Clamp01(Image1ToNowDeltaTime / (updateInterval * 1000f));
                    float alpha = p;

                    mat.SetColor("_TopColor", new Color(1, 1, 1, alpha));
                    mat.SetColor("_BottomColor", new Color(1, 1, 1, 1.0f));

                    float now = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

                    string line =
                        $"LinearOnly," +
                        $"cam1,{(1f - alpha):F6}," +
                        $"-1,{0f:F6}," +
                        $"cam2,{alpha:F6}," +
                        $"{timeMs:F3},{knobValue:F3},{responsePattern},{(int)stepNumber},{amplitudeToSaveData},{v:F6},{cameraSpeed:F3}";

                    RecordWaveAndData(now, alpha, line);
                    break;
                }

            case BrightnessBlendMode.GaussOnly:
                {
                    if (frames == null || frames.Length == 0 || _Gaussmat == null) break;

                    float step = Mathf.Max(1e-6f, secondsPerStep);
                    float sigmaStep = Mathf.Max(1e-4f, sigmaSec);

                    float tSec = timeMs / 1000f;
                    float halfFrame = 0.5f / 60f;
                    float u = (tSec + halfFrame) / step;

                    int c = Mathf.RoundToInt(u);
                    int n = frames.Length;

                    int i0 = Mathf.Clamp(c - 1, 0, n - 1);
                    int i1 = Mathf.Clamp(c, 0, n - 1);
                    int i2 = Mathf.Clamp(c + 1, 0, n - 1);

                    // Warm-up 避免首帧抖动
                    if (!_gaussWarmupDone)
                    {
                        if (i1 != _last1)
                        {
                            _Gaussmat.SetTexture("_Tex0", frames[i1]);
                            _Gaussmat.SetTexture("_Tex1", frames[i1]);
                            _Gaussmat.SetTexture("_Tex2", frames[i1]);
                            _last0 = _last1 = _last2 = i1;
                        }

                        _Gaussmat.SetFloat("_W0", 0f);
                        _Gaussmat.SetFloat("_W1", 1f);
                        _Gaussmat.SetFloat("_W2", 0f);

                        _gaussWarmupCount++;
                        if (_gaussWarmupCount >= Mathf.Max(1, _gaussWarmupFrames))
                        {
                            _gaussWarmupDone = true;
                        }

                        break;
                    }

                    float d0 = (i0 - u) / sigmaStep;
                    float d1 = (i1 - u) / sigmaStep;
                    float d2 = (i2 - u) / sigmaStep;

                    float w0 = Mathf.Exp(-0.5f * d0 * d0);
                    float w1 = Mathf.Exp(-0.5f * d1 * d1);
                    float w2 = Mathf.Exp(-0.5f * d2 * d2);

                    float s = w0 + w1 + w2;
                    if (s > 1e-12f) { w0 /= s; w1 /= s; w2 /= s; }
                    else { w0 = 0f; w1 = 1f; w2 = 0f; }

                    if (i0 != _last0 || i1 != _last1 || i2 != _last2)
                    {
                        _Gaussmat.SetTexture("_Tex0", frames[i0]);
                        _Gaussmat.SetTexture("_Tex1", frames[i1]);
                        _Gaussmat.SetTexture("_Tex2", frames[i2]);
                        _last0 = i0; _last1 = i1; _last2 = i2;
                    }

                    _Gaussmat.SetFloat("_W0", w0);
                    _Gaussmat.SetFloat("_W1", w1);
                    _Gaussmat.SetFloat("_W2", w2);

                    float now = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
                    float alphaToPlot = w1;

                    string line =
                        $"GaussOnly," +
                        $"cam1,{w0:F6}," +
                        $"cam2,{w1:F6}," +
                        $"cam3,{w2:F6}," +
                        $"{timeMs:F3},{knobValue:F3},{responsePattern},{(int)stepNumber},{amplitudeToSaveData},{v:F6},{cameraSpeed:F3}";

                    RecordWaveAndData(now, alphaToPlot, line);
                    break;
                }

            default:
                {
                    break;
                }
        }
    }

    // InitialSetup: 初始化摄像机位姿与变量
    // EN: Initialize camera poses and internal state
    // JP: カメラの姿勢と内部状態を初期化
    void InitialSetup()
    {
        frameNum = 1;
        startTime = Time.time;
        timeMs = (Time.time - startTime) * 1000;

        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);

        Vector3 worldRightDirection = rightMoveRotation * Vector3.right;
        Vector3 worldForwardDirection = forwardMoveRotation * Vector3.forward;
        switch (directionPattern)
        {
            case DirectionPattern.forward:
                direction = worldForwardDirection;
                captureCamera2.transform.rotation = Quaternion.Euler(0, 146.8f, 0);
                captureCamera1.transform.rotation = Quaternion.Euler(0, 146.8f, 0);
                captureCamera2.transform.position = new Vector3(30.5f, 28f, 160.4f);
                captureCamera1.transform.position = new Vector3(30.5f, 28f, 160.4f);
                break;
            case DirectionPattern.right:
                direction = worldRightDirection;
                captureCamera2.transform.rotation = Quaternion.Euler(0, 48.5f, 0);
                captureCamera1.transform.rotation = Quaternion.Euler(0, 48.5f, 0);
                captureCamera0.transform.rotation = Quaternion.Euler(0, 48.5f, 0);
                captureCamera2.transform.position = new Vector3(39f, 28f, 90f);
                captureCamera1.transform.position = new Vector3(39f, 28f, 90f);
                captureCamera0.transform.position = new Vector3(39f, 28f, 90f);
                break;
        }

        // 保存初始位姿以便后续 Reset 使用
        if (!initPoseSaved)
        {
            initPos0 = captureCamera0.transform.position;
            initPos1 = captureCamera1.transform.position;
            initPos2 = captureCamera2.transform.position;
            initRot0 = captureCamera0.transform.rotation;
            initRot1 = captureCamera1.transform.rotation;
            initRot2 = captureCamera2.transform.rotation;
            initPoseSaved = true;
        }
    }

    // ResetCamerasAndBlendState: 恢复初始位姿并重置混合状态
    // EN: Restore initial poses and reset blend state
    // JP: 初期姿勢へ復元しブレンド状態をリセット
    public void ResetCamerasAndBlendState()
    {
        if (!initPoseSaved) InitialSetup();

        // 恢复位姿
        captureCamera0.transform.position = initPos0;
        captureCamera0.transform.rotation = initRot0;
        captureCamera1.transform.position = initPos1;
        captureCamera1.transform.rotation = initRot1;
        captureCamera2.transform.position = initPos2;
        captureCamera2.transform.rotation = initRot2;

        captureCamera2.transform.position += direction * captureIntervalDistance;

        // 重置计时/历史
        frameNum = 1;
        fixedUpdateCounter = 0;
        timeMs = 0f;

        timeStamps.Clear();
        alphaHistory.Clear();
        velocityHistory.Clear();

        // 重置线性混合材质的贴图与 alpha
        if (CaptureCameraLinearBlendRawImage != null)
        {
            CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);
            CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);
            CaptureCameraLinearBlendRawImage.material.SetColor("_TopColor", new Color(1f, 1f, 1f, 1f));
            CaptureCameraLinearBlendRawImage.material.SetColor("_BottomColor", new Color(1f, 1f, 1f, 0f));
        }
    }

    // GetRawImage: 查找 Canvas 并缓存 RawImage Transform 与组件
    // EN: Find Canvas and cache RawImage components/transforms
    // JP: Canvas を見つけて RawImage コンポーネントをキャッシュ
    void GetRawImage()
    {
        canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogWarning("Canvas not found."); return; }

        continuousImageTransform = canvas.transform.Find("CaptureCamera0");
        Image1Transform = canvas.transform.Find("CaptureCamera1");
        Image2Transform = canvas.transform.Find("CaptureCamera2");
        CaptureCameraLinearBlendTransform = canvas.transform.Find("CaptureCameraLinearBlend");
        CaptureCameraLinearBlendTopTransform = canvas.transform.Find("CaptureCameraLinearBlendTop");

        continuousImageRawImage = continuousImageTransform.GetComponent<RawImage>();
        CaptureCameraLinearBlendRawImage = CaptureCameraLinearBlendTransform.GetComponent<RawImage>();
        CaptureCameraLinearBlendTopRawImage = CaptureCameraLinearBlendTopTransform.GetComponent<RawImage>();

        CaptureCameraLinearBlendRawImage.material = new Material(Mat_GrayscaleOverBlend);
        CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);
        CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);

        _Gaussmat = new Material(GaussBlendMat);
        CaptureCameraLinearBlendTopRawImage.material = _Gaussmat;

        continuousImageRawImage.enabled = false;
    }

    // QuitGame: 退出（Editor 与 Build 不同处理）
    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // OnDestroy: 程序结束时保存数据（与 SaveCurrentDataToCsv 重复请按需调整）
    void OnDestroy()
    {
        string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        experimentalCondition =
                            brightnessBlendMode.ToString() + "_" +
                             "ParticipantName_" + participantName.ToString() + "_"
                             + "TrialNumber_" + trialNumber.ToString();
        if (devMode == DevMode.Test)
        {
            experimentalCondition += "_" + "Test";
        }
        string fileName = $"{date}_{experimentalCondition}.csv";

        string filePath = Path.Combine("D:/vectionProject/public", folderName, fileName);
        try
        {
            File.WriteAllLines(filePath, data);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"OnDestroy write failed: {ex.Message}");
        }
    }

    // SaveCurrentDataToCsv: 在进入 2AFC 前单独保存一次（可由 Start2AfcTrials 调用）
    private void SaveCurrentDataToCsv()
    {
        try
        {
            if (data == null || data.Count == 0) return;
            string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string expCond = brightnessBlendMode.ToString() + "_" +
                              "ParticipantName_" + participantName.ToString() + "_" +
                              "TrialNumber_" + trialNumber.ToString();
            if (devMode == DevMode.Test) expCond += "_" + "Test";
            string fileName = $"{date}_{expCond}.csv";
            string filePath = Path.Combine(@"D:\vectionProject\public", folderName, fileName);

            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllLines(filePath, data, Encoding.UTF8);
            Debug.Log($"Saved current adjustment data before 2AFC: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveCurrentDataToCsv failed: {ex.Message}");
        }
    }

    // MarkTrialCompletedAndRestart: 试次完成后的重启机制（Editor friendly）
    public void MarkTrialCompletedAndRestart()
    {
        UpdateProgress();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            EditorApplication.isPlaying = true;
        };
#endif
    }

    // 简单的 Getter/Setter for amplitudes
    public float GetAmplitude(int index) => amplitudes[index];
    public void SetAmplitude(int index, float value) => amplitudes[index] = value;

    // BrightnessBlend 内部工具类（保留）
    public static class BrightnessBlend
    {
        /// <summary>
        /// Cosine → Linear → Acos → Cosine 动态或固定混合函数
        /// </summary>
        public static float GetMixedValue(float x, float knobValue, BrightnessBlendMode mode)
        {
            switch (mode)
            {
                case BrightnessBlendMode.CosineOnly:
                    return 0.5f * (1f - Mathf.Cos(Mathf.PI * x));
                case BrightnessBlendMode.LinearOnly:
                    return x;
                case BrightnessBlendMode.AcosOnly:
                    return Mathf.Acos(-2f * x + 1f) / Mathf.PI;
                case BrightnessBlendMode.Dynamic:
                default:
                    return GetDynamicBlend(x, knobValue);
            }
        }

        private static float GetDynamicBlend(float x, float knobValue)
        {
            if (knobValue <= 0.1f) return 0.5f * (1f - Mathf.Cos(Mathf.PI * x));
            else if (knobValue <= 0.5f)
            {
                float t = (knobValue - 0.1f) / 0.4f;
                float cosine = 0.5f * (1f - Mathf.Cos(Mathf.PI * x));
                float linear = x;
                return (1f - t) * cosine + t * linear;
            }
            else if (knobValue <= 0.9f)
            {
                float t = (knobValue - 0.5f) / 0.4f;
                float linear = x;
                float acos = Mathf.Acos(-2f * x + 1f) / Mathf.PI;
                return (1f - t) * linear + t * acos;
            }
            else return Mathf.Acos(-2f * x + 1f) / Mathf.PI;
        }
    }

    // 保存目录（保留）
    private string saveFolder = @"D:\vectionProject\public\ExperimentData3-Images";

    // Editor Gizmos: 可视化摄像机位置（仅 Editor）
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (captureCamera0 == null) return;
        Transform parent = captureCamera0.transform.parent;

        Vector3 localPos = new Vector3(4f, 28f, 130f);
        Quaternion localRot = Quaternion.Euler(0f, 48.5f, 0f);

        Vector3 worldPos;
        Quaternion worldRot;

        if (parent != null)
        {
            worldPos = parent.TransformPoint(localPos);
            worldRot = parent.rotation * localRot;
            Debug.Log($"captureCamera0 world position: {worldPos}, world rotation: {worldRot.eulerAngles}");
        }
        else
        {
            worldPos = localPos;
            worldRot = localRot;
            Debug.LogWarning("captureCamera0 has no parent; using local as world.");
        }

        Vector3 worldDir = worldRot * Vector3.right;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(worldPos, worldPos + worldDir * 2000f);
        Gizmos.DrawSphere(worldPos, 2f);
    }
#endif

    // AppendCsvLine helper (重复实现移除冲突请注意)
    private void AppendCsvLine(string filePath, string header, string line)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            bool writeHeader = !File.Exists(filePath);
            using (var sw = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                if (writeHeader && !string.IsNullOrEmpty(header)) sw.WriteLine(header);
                sw.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"AppendCsvLine failed: {ex.Message}");
        }
    }

    // RecordWaveAndData: 记录波形 / 时间序列并保存 data 行
    private void RecordWaveAndData(float now, float alphaToPlot, string dataLine)
    {
        timeStamps.Add(now);
        alphaHistory.Add(alphaToPlot);
        velocityHistory.Add(v);

        while (timeStamps.Count > 0 && timeStamps[0] < now - 5f)
        {
            timeStamps.RemoveAt(0);
            alphaHistory.RemoveAt(0);
            velocityHistory.RemoveAt(0);
        }

        if (timeStamps.Count > maxSamples)
        {
            timeStamps.RemoveAt(0);
            alphaHistory.RemoveAt(0);
            velocityHistory.RemoveAt(0);
        }

        if (!string.IsNullOrEmpty(dataLine))
            data.Add(dataLine);
    }

    // CaptureAndSavePng: 同步读取 RenderTexture 并保存 PNG（注意主线程阻塞）
    private void CaptureAndSavePng(Camera cam, string outPath)
    {
        if (cam == null || cam.targetTexture == null) return;

        cam.Render();

        var rt = cam.targetTexture;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false, true);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply(false, false);

        RenderTexture.active = prev;

        var px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            px[i].r = Mathf.LinearToGammaSpace(px[i].r);
            px[i].g = Mathf.LinearToGammaSpace(px[i].g);
            px[i].b = Mathf.LinearToGammaSpace(px[i].g);
            px[i].a = 1f;
        }
        tex.SetPixels(px);
        tex.Apply(false, false);

        Debug.Log($"Saving PNG to: {outPath}");
        try { File.WriteAllBytes(outPath, tex.EncodeToPNG()); } catch (Exception ex) { Debug.LogWarning($"Write PNG failed: {ex.Message}"); }
        Destroy(tex);
    }

    // LoadFramesFromResources + EnsureFramesLoaded
    private Texture2D[] LoadFramesFromResources(string resourcesFolder, string namePrefix, bool verbose)
    {
        string folder = (resourcesFolder ?? "").Trim();
        string prefix = (namePrefix ?? "").Trim();

        var allTex = Resources.LoadAll<Texture2D>(folder);
        Debug.Log($"[LoadAll<Texture2D>] folder='{folder}' count={allTex.Length}");

        if (allTex.Length > 0)
            Debug.Log($"[LoadAll<Texture2D>] first='{allTex[0].name}' last='{allTex[allTex.Length - 1].name}'");

        if (verbose)
        {
            int show = Mathf.Min(10, allTex.Length);
            for (int i = 0; i < show; i++)
                Debug.Log($"[AllTex] {i} name='{allTex[i].name}'");
        }

        var filtered = allTex
            .Where(t => t != null && t.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Debug.Log($"[Filter<Texture2D>] prefix='{prefix}' frames={filtered.Length}");

        return filtered;
    }

    private void EnsureFramesLoaded()
    {
        if (frames != null && frames.Length > 0) return;
        frames = LoadFramesFromResources(resourcesFolder, namePrefix, verboseLoadLog);
    }

    private void ResetGaussWarmup()
    {
        _gaussWarmupDone = false;
        _gaussWarmupCount = 0;
        _last0 = _last1 = _last2 = -1;
    }
}



