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
    // 对数刻度
    void Start()
    {
        // 垂直同期を無効にする // 关闭垂直同步
        QualitySettings.vSyncCount = 0;
        // 目標フレームレートを60フレーム/秒に設定 // 设置目标帧率为60帧每秒
        Time.fixedDeltaTime = 1.0f / 60.0f;

        updateInterval = 1f / fps; // 各フレームの表示間隔時間を計算 // 计算每一帧显示的间隔时间
        captureIntervalDistance = cameraSpeed / fps; // 各フレームの間隔距離を計算 // 计算每帧之间的间隔距离

        GetRawImage();
        InitialSetup();

        continuousImageRawImage.enabled = true;
        captureCamera2.transform.position += direction * captureIntervalDistance;
        captureCamera3.transform.position += direction * captureIntervalDistance * 2f;
        SerialReader = GetComponent<SerialReader>();

        TrailSettings();
        nextStepButtonTextComponent = nextStepButton.GetComponentInChildren<TextMeshProUGUI>();
        nextStepButton.onClick.AddListener(OnNextStep); // ボタンがクリックされたときの処理を追加 // 添加按钮点击时的处理

        // Start() 里
        data.Add(
          "Mode," +
          "BackFrameNum,BackWeight," +
          "MidFrameNum,MidWeight," +
          "FrontFrameNum,FrontWeight," +
          "TimeMs,Knob,ResponsePattern,StepNumber,Amplitude,Velocity,CameraSpeed"
        );

        // 加载 Resources 里的所有帧纹理
        resourcesFolder = "CamFrames";
        namePrefix = "cam1_";
        Debug.Log($"[ForceConfig] folder='{resourcesFolder}' prefix='{namePrefix}'");
        EnsureFramesLoaded();

    }

    private IEnumerator Show2AfcIntroNextFrame()
    {
        yield return null; // 等一帧，确保 Canvas 已初始化
        Debug.Log($"Show2AfcIntroNextFrame: only2AfcMode={only2AfcMode}");
        Show2AfcIntroPanel();
    }

    void Awake()
    {
        // 强制运行时初始为 Option0（避免被旧序列化值影响）
        if ((int)stepNumber < 1) stepNumber = StepNumber.Option1;
        Debug.Log($"[MoveCamera] stepNumber = {(int)stepNumber} ({stepNumber}) in Awake");

        savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");
        Debug.Log($"[MoveCamera] trial savePath = {savePath}");

        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Trial file not found. Please run: Tools → Generate initial trial file");
            // 这里不强制退出也行，但最好不要让实验继续
            isEnd = true;
            return;
        }
        // 如果只做 2AFC（only2AfcMode），直接把流程状态置为“已到结束 step”，
        // 这样不会进入调参的正常流程（OnNextStep 会在 case 5 显示过渡面板）
        if (only2AfcMode)
        {
            Debug.Log("Awake: only2AfcMode enabled -> forcing currentStep=5 and isEnd=true to skip calibration.");
            currentStep = 5;
            isEnd = true;
        }
        twoAfcDurationSec = 10f;
    }
    void Update()
    {
        // 在 2AFC 模式下不要响应这个全局点击（否则会中断 2AFC 的点击/按钮逻辑）
        if (in2AfcMode) return;

        /// マウス入力は1フレームのみ検出されるため、Update() で処理する必要があります。
        // マウスの左ボタンが押されたときの処理 // 处理鼠标左键按下时的操作

        if (!mouseClicked && Input.GetMouseButtonDown(0))
        {
            //上部カメラの位置を下部カメラの位置に設定します。
            captureCamera0.transform.position = captureCamera1.transform.position;

            mouseClicked = true;
            // ボタンがクリックされたときの処理を追加 // 添加按钮点击时的处理
            nextStepButton.gameObject.SetActive(true);
            Time.timeScale = 0f;
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
    // Update is called once per frame
    void FixedUpdate()
    {
        // timeMs = (Time.time - startTime) * 1000;
        // 使用固定步长计算 timeMs，确保每次增加为 Time.fixedDeltaTime（约 16.6667ms）
        timeMs = fixedUpdateCounter * Time.fixedDeltaTime * 1000f;
        float tSec = timeMs / 1000f;
        if (tSec > CaptureSeconds)
        {
            ResetCamerasAndBlendState();
        }
        // 当前处于第几个 1s 区间（从 0 开始）
        stepIndex = Mathf.FloorToInt(tSec / Mathf.Max(1e-6f, secondsPerStep));
        Continuous();

        // LuminanceMixture();
        LuminanceMixtureFrame();

        if (Application.isPlaying)
        {
            velocityHistory.Add(v);           // 新增
        }
        // 在帧末自增计数器（保证本帧使用当前计数器值）
        fixedUpdateCounter++;
    }

    void OnNextStep()
    {
        mouseClicked = false;
        Time.timeScale = 1f;
        currentStep++;
        responsePattern = ResponsePattern.Amplitude;
        ResetCamerasAndBlendState();
        switch (currentStep)
        {
            case 1:
                stepNumber = StepNumber.Option1;
                break;
            case 2:
                stepNumber = StepNumber.Option2;
                break;
            case 3:
                stepNumber = StepNumber.Option3;
                break;
            case 4:
                stepNumber = StepNumber.Option4;
                break;
            case 5:
                if (isEnd)
                {
                    // QuitGame();
                    // 显示 2AFC 过渡说明面板，等待被试点击“开始”再真正进入 Start2AfcTrials
                    Debug.Log("Experiment finished. Showing 2AFC intro panel.");
                    Show2AfcIntroPanel();
                }
                else
                {
                    MarkTrialCompletedAndRestart();
                }
                break;
        }
        nextStepButton.gameObject.SetActive(false);
    }

    void Continuous()
    {
        continuousImageRawImage.enabled = true;
        time += Time.fixedDeltaTime;

        knobValue = Mathf.Clamp01(SerialReader.lastSensorValue);
        int step = (int)stepNumber;
        V0 = 1.0f;
        // if (responsePattern == ResponsePattern.Velocity)
        // {
        //     V0 = knobValue * 2f;
        //     // V0 = knobValue;
        //     v = V0;
        // }
        // else if (responsePattern == ResponsePattern.Amplitude)
        // {
        if (step == 1 || step == 3) amplitudeToSaveData = Mathf.Lerp(A_min, A_max, knobValue);
        if (step == 2 || step == 4) amplitudeToSaveData = knobValue * 2f * Mathf.PI;

        amplitudes[step] = amplitudeToSaveData;

        if (step >= 1) v = V0 + amplitudes[1] * Mathf.Sin(omega * time);
        if (step >= 2) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);
        if (step >= 3) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time);
        if (step >= 4) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);

        // }

        // 连续相机移动
        captureCamera0.transform.position += direction * v * Time.deltaTime;

        // =========================
        // CaptureCamera0: 60fps 保存（每次 FixedUpdate 一张）
        // =========================

        if (SaveCam0ContinuousPng)
        {
            const float SAVE_START_SEC = 14f;
            const float SAVE_END_SEC = 22f;
            // time 是用 fixedDeltaTime 累加的“逻辑时间”
            if (time >= SAVE_START_SEC && time <= SAVE_END_SEC)
            {
                // 建议用 FixedUpdate 的 fixedDeltaTime 来估算帧数上限
                int maxFrames = Mathf.CeilToInt((SAVE_END_SEC - SAVE_START_SEC) / Time.fixedDeltaTime);

                if (_cam0SavedCount < maxFrames)
                {
                    string file = $"cam0_{fixedUpdateCounter:000000}.png";
                    string path = Path.Combine(Cam0SaveDir, file);
                    // CaptureAndSavePng(captureCamera0, path);
                    _cam0SavedCount++;
                }
            }
            // // 时长限制：60s * 60fps = 3600 张
            // int maxFrames = CaptureSeconds * 40;
            // if (_cam0SavedCount < maxFrames)
            // {
            //     // 用 fixedUpdateCounter 做帧号（与 timeMs 对齐）
            //     string file = $"cam0_{fixedUpdateCounter:000000}.png";
            //     string path = Path.Combine(Cam0SaveDir, file);
            //     CaptureAndSavePng(captureCamera0, path);
            //     _cam0SavedCount++;
            // }
        }
    }

    void LuminanceMixtureFrame()
    {
        // 如果正在进行 2AFC 的“等待响应”阶段，暂停帧更新/播放（防止视频继续变化）
        if (in2AfcMode && _2afcWaitingForResponse)
        {
            return;
        }

        float frameMs = updateInterval * 1000f;
        // if (Mathf.Abs(timeMs - frameNum * updateInterval * 1000) < 0.2f)
        while (timeMs >= frameNum * frameMs)
        {
            frameNum++;

            targetPosition = direction * cameraSpeed * updateInterval;
            captureCamera1.transform.position += targetPosition;
            captureCamera2.transform.position += targetPosition;

            // =========================
            // CaptureCamera1: 每 updateInterval 保存一次（通常 1Hz）
            // =========================
            if (SaveCam1IsiPng)
            {
                // 60s * 1Hz = 60 张（如果 updateInterval=1）
                CaptureSeconds = 60; // 可改
                int maxFrames = Mathf.CeilToInt(CaptureSeconds / Mathf.Max(1e-6f, updateInterval));
                if (_cam1SavedCount < maxFrames)
                {

                    string dir = Path.Combine(Application.dataPath, "Resources", resourcesFolder);
                    Directory.CreateDirectory(dir);
                    string file = $"cam1_{_cam1SavedCount:000}.png";

                    string path = Path.Combine(dir, file);
                    // CaptureAndSavePng(captureCamera1, path);
                    // 已保存 PNG 之后，生成 ROI csv
                    string roiCsvAll = Path.Combine(dir, "rois.csv");
                    SaveRoiMetadataForFrame(treeRenderers, captureCamera1, _cam1SavedCount, file, roiCsvAll);
                    _cam1SavedCount++;
                }
            }
        }

        // frames[] must be loaded (Resources or elsewhere)
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("frames not loaded. Put images under Assets/Resources/... and load into frames[].");
            return;
        }

        // Optional: show only the relevant RawImage (prevents “both draw” confusion)
        // 在 2AFC 模式下不要自动根据 brightnessBlendMode 切换 active —— 会覆盖 Set2AfcLayout 的设置
        if (!in2AfcMode)
        {
            if (CaptureCameraLinearBlendRawImage != null)
                CaptureCameraLinearBlendRawImage.gameObject.SetActive(brightnessBlendMode == BrightnessBlendMode.LinearOnly);

            if (CaptureCameraLinearBlendTopRawImage != null)
                CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(brightnessBlendMode == BrightnessBlendMode.GaussOnly);
        }

        switch (brightnessBlendMode)
        {
            // =========================================================
            // 1) TWO-FRAME LINEAR (prev/next) — uses _TopTex/_BottomTex
            // =========================================================
            case BrightnessBlendMode.LinearOnly:
                {
                    // frameNum is 1-based in your loop; clamp safely
                    int n = frames.Length;
                    int prevIdx = Mathf.Clamp(frameNum - 1, 0, n - 1);
                    int nextIdx = Mathf.Clamp(frameNum, 0, n - 1);

                    Texture botTex = frames[prevIdx];
                    Texture topTex = frames[nextIdx];

                    var mat = CaptureCameraLinearBlendRawImage.material;
                    mat.SetTexture("_TopTex", topTex);
                    mat.SetTexture("_BottomTex", botTex);

                    // Linear alpha by time within current interval
                    float Image1ToNowDeltaTime = timeMs - (frameNum - 1) * updateInterval * 1000f;
                    float p = Mathf.Clamp01(Image1ToNowDeltaTime / (updateInterval * 1000f));

                    // If you want EXACT linear: alpha = p
                    // If you want to keep your “knob + mapping to [0.1,0.9]”, do it here.
                    float alpha = p;

                    // Example: keep your previous behavior (optional)
                    // knobValue = SerialReader.lastSensorValue;
                    // alpha = BrightnessBlend.GetMixedValue(p, knobValue, BrightnessBlendMode.LinearOnly);
                    // alpha = Mathf.Lerp(0.1f, 0.9f, Mathf.Clamp01(alpha));

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

            // =========================================================
            // 2) THREE-FRAME GAUSSIAN (k-1,k,k+1) — uses _Tex0/1/2 + _W0/1/2
            // =========================================================
            case BrightnessBlendMode.GaussOnly:
                {
                    if (frames == null || frames.Length == 0 || _Gaussmat == null) break;

                    float step = Mathf.Max(1e-6f, secondsPerStep);  // STEP_SEC
                    float sigmaStep = Mathf.Max(1e-4f, sigmaSec);   // SIGMA（按step单位）

                    // --- time -> u (match Python: u = (t + 0.5*DT)/STEP_SEC ) ---
                    float tSec = timeMs / 1000f;

                    // 若你不是固定60fps，把这行改成：float halfFrame = 0.5f * Time.deltaTime;
                    float halfFrame = 0.5f / 60f;

                    float u = (tSec + halfFrame) / step;

                    // Python: c = round(u)
                    int c = Mathf.RoundToInt(u);
                    int n = frames.Length;

                    int i0 = Mathf.Clamp(c - 1, 0, n - 1);
                    int i1 = Mathf.Clamp(c, 0, n - 1);
                    int i2 = Mathf.Clamp(c + 1, 0, n - 1);

                    // =========================
                    // Warm-up: prevent first-frame jerk
                    // =========================
                    if (!_gaussWarmupDone)
                    {
                        // 首帧/前几帧先强制显示中心帧，避免“未初始化参数 -> 高斯混合”突变
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

                    // =========================
                    // Weights: exp(-0.5*((idx-u)/sigma)^2)  (match Python)
                    // =========================
                    float d0 = (i0 - u) / sigmaStep;
                    float d1 = (i1 - u) / sigmaStep;
                    float d2 = (i2 - u) / sigmaStep;

                    float w0 = Mathf.Exp(-0.5f * d0 * d0);
                    float w1 = Mathf.Exp(-0.5f * d1 * d1);
                    float w2 = Mathf.Exp(-0.5f * d2 * d2);

                    float s = w0 + w1 + w2;
                    if (s > 1e-12f)
                    {
                        w0 /= s; w1 /= s; w2 /= s;
                    }
                    else
                    {
                        w0 = 0f; w1 = 1f; w2 = 0f;
                    }

                    // 只在索引变化时更新纹理（减少卡顿/尖峰）
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


            // =========================================================
            // 3) Others: either do nothing or fall back to LinearOnly
            // =========================================================
            default:
                {
                    // If you prefer fallback:
                    // brightnessBlendMode = BrightnessBlendMode.LinearOnly;
                    // LuminanceMixtureFrame();
                    break;
                }
        }
    }



    void InitialSetup()
    {
        frameNum = 1;
        startTime = Time.time;
        timeMs = (Time.time - startTime) * 1000;

        nextStepButton.gameObject.SetActive(false);
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
                captureCamera3.transform.rotation = Quaternion.Euler(0, 48.5f, 0);
                captureCamera1.transform.rotation = Quaternion.Euler(0, 48.5f, 0);
                captureCamera0.transform.rotation = Quaternion.Euler(0, 48.5f, 0);
                captureCamera2.transform.position = new Vector3(39f, 28f, 90f);
                captureCamera3.transform.position = new Vector3(39f, 28f, 90f);
                captureCamera1.transform.position = new Vector3(39f, 28f, 90f);
                captureCamera0.transform.position = new Vector3(39f, 28f, 90f);
                break;
        }
        // 保存初始位姿（第一次初始化）
        if (!initPoseSaved)
        {
            initPos0 = captureCamera0.transform.position;
            initPos1 = captureCamera1.transform.position;
            initPos2 = captureCamera2.transform.position;
            initPos3 = captureCamera3.transform.position;
            initRot0 = captureCamera0.transform.rotation;
            initRot1 = captureCamera1.transform.rotation;
            initRot2 = captureCamera2.transform.rotation;
            initRot3 = captureCamera3.transform.rotation;
            initPoseSaved = true;
        }
    }
    public void ResetCamerasAndBlendState()
    {
        if (!initPoseSaved) InitialSetup(); // 保底

        // 恢复位姿
        captureCamera0.transform.position = initPos0;
        captureCamera0.transform.rotation = initRot0;
        captureCamera1.transform.position = initPos1;
        captureCamera1.transform.rotation = initRot1;
        captureCamera2.transform.position = initPos2;
        captureCamera2.transform.rotation = initRot2;
        captureCamera3.transform.position = initPos3;
        captureCamera3.transform.rotation = initRot3;
        captureCamera2.transform.position += direction * captureIntervalDistance;
        captureCamera3.transform.position += direction * captureIntervalDistance * 2f;
        // 重置帧/时间/历史，使混合从干净状态开始（避免残留上一组的 alpha/帧计数）
        frameNum = 1;

        // startTime = Time.time;
        // 关键：把固定帧计数器清零，timeMs 将从 0 严格按 Time.fixedDeltaTime 递增
        fixedUpdateCounter = 0;
        timeMs = 0f;

        timeStamps.Clear();
        alphaHistory.Clear();
        velocityHistory.Clear();

        // 重新初始化 CaptureCameraLinearBlendRawImage 的贴图与 alpha（保证上/下层确定）
        if (CaptureCameraLinearBlendRawImage != null)
        {
            CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);
            CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);
            CaptureCameraLinearBlendRawImage.material.SetColor("_TopColor", new Color(1f, 1f, 1f, 1f));
            CaptureCameraLinearBlendRawImage.material.SetColor("_BottomColor", new Color(1f, 1f, 1f, 0f));
        }

        // 重新计算与摄像机速度相关的参数（如需要）
        // captureIntervalDistance = cameraSpeed / Mathf.Max(fps, 1f);
    }

    void GetRawImage()
    {
        // Canvas内で指定された名前の子オブジェクトを検索 // 在 Canvas 中查找指定名称的子对象
        canvas = GameObject.Find("Canvas");
        continuousImageTransform = canvas.transform.Find("CaptureCamera0");
        Image1Transform = canvas.transform.Find("CaptureCamera1");
        Image2Transform = canvas.transform.Find("CaptureCamera2");
        CaptureCameraLinearBlendTransform = canvas.transform.Find("CaptureCameraLinearBlend");
        CaptureCameraLinearBlendTopTransform = canvas.transform.Find("CaptureCameraLinearBlendTop");

        // 子オブジェクトのRawImageコンポーネントを取得 // 获取子对象的 RawImage 组件
        continuousImageRawImage = continuousImageTransform.GetComponent<RawImage>();

        CaptureCameraLinearBlendRawImage = CaptureCameraLinearBlendTransform.GetComponent<RawImage>();
        CaptureCameraLinearBlendTopRawImage = CaptureCameraLinearBlendTopTransform.GetComponent<RawImage>();

        CaptureCameraLinearBlendRawImage.material = new Material(Mat_GrayscaleOverBlend);

        CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);       // 上层图
        CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);    // 下层图  

        _Gaussmat = new Material(GaussBlendMat);
        CaptureCameraLinearBlendTopRawImage.material = _Gaussmat;

        // RawImageコンポーネントを無効にする // 禁用 RawImage 组件
        continuousImageRawImage.enabled = false;
    }
    void QuitGame()
    {
#if UNITY_EDITOR

                    UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        // 現在の日付を取得 // 获取当前日期
        string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // ファイル名を構築 // 构建文件名
        experimentalCondition =
                            brightnessBlendMode.ToString() + "_" +
                             "ParticipantName_" + participantName.ToString() + "_"
                             + "TrialNumber_" + trialNumber.ToString();
        if (devMode == DevMode.Test)
        {
            experimentalCondition += "_" + "Test";
        }
        string fileName = $"{date}_{experimentalCondition}.csv";

        // ファイルを保存（Application.dataPath：現在のプロジェクトのAssetsフォルダのパスを示す） // 保存文件（Application.dataPath：表示当前项目的Assets文件夹的路径）
        string filePath = Path.Combine("D:/vectionProject/public", folderName, fileName);
        File.WriteAllLines(filePath, data);

    }
    private void SaveCurrentDataToCsv()
    {
        try
        {
            string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            // 保持与 OnDestroy 一致的文件名格式
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
    public void MarkTrialCompletedAndRestart()
    {
        // TrialState.MarkTrialCompleted();
        UpdateProgress();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            EditorApplication.isPlaying = true;
        };
#endif
    }
    public float GetAmplitude(int index)
    {
        return amplitudes[index];
    }

    public void SetAmplitude(int index, float value)
    {
        amplitudes[index] = value;
    }

    public static class BrightnessBlend
    {
        /// <summary>
        /// Cosine → Linear → Acos → Cosine 动态或固定混合函数
        /// </summary>
        /// <param name="x">归一化时间 ∈ [0,1]</param>
        /// <param name="knobValue">旋钮值 ∈ [0,2]（Dynamic 模式下有效）</param>
        /// <param name="mode">混合模式（动态 or 固定函数）</param>
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

        /// <summary>
        /// Cosine → Linear → Acos 的动态混合实现
        /// </summary>
        private static float GetDynamicBlend(float x, float knobValue)
        {
            // --- 保留两端 ---
            if (knobValue <= 0.1f)
            {
                return 0.5f * (1f - Mathf.Cos(Mathf.PI * x)); // Cosine 固定
            }
            else if (knobValue <= 0.5f)
            {
                // Phase 1: Cosine → Linear
                float t = knobValue - 0.1f / 0.4f;
                float cosine = 0.5f * (1f - Mathf.Cos(Mathf.PI * x));//Cosine 混合曲线：y = 0.5 × (1 − cos(πx))
                float linear = x;
                return (1f - t) * cosine + t * linear;
            }
            else if (knobValue <= 0.9f)
            {
                // Phase 2: Linear → Acos
                float t = (knobValue - 0.5f) / 0.4f;
                float linear = x;
                float acos = Mathf.Acos(-2f * x + 1f) / Mathf.PI;//Acos 曲线：y = acos(−2x + 1) / π
                return (1f - t) * linear + t * acos;
            }
            else
            {
                return Mathf.Acos(-2f * x + 1f) / Mathf.PI;
            }
        }

    }

    public struct ModParams
    {
        public float V0, A1, PHI1, A2, PHI2;
        public ModParams(float v0, float a1, float phi1, float a2, float phi2)
        { V0 = v0; A1 = a1; PHI1 = phi1; A2 = a2; PHI2 = phi2; }
    }

    private ModParams GetParams(SubjectOption opt)
    {
        switch (opt)
        {
            case SubjectOption.YAMA_A:
                return new ModParams(0.992f, 0.540f, 1.849f, -0.528f, 1.462f);

            case SubjectOption.OMU_B:
                return new ModParams(1.131f, 0.522f, 2.528f, -0.223f, 3.525f);

            case SubjectOption.ONO_C:
                return new ModParams(1.067f, 0.632f, 3.663f, 0.461f, 5.123f);

            case SubjectOption.HOU_D:
                return new ModParams(0.951f, 0.275f, 3.031f, 0.920f, 5.982f);

            case SubjectOption.LL_E:
                return new ModParams(1.027f, -0.278f, 1.849f, -0.292f, 3.728f);

            case SubjectOption.KK_F:
                return new ModParams(1f, 0.524f, 2.777f, -0.31f, 1.859f);
                // return new ModParams(1.129f, 0.815f, 3.462f, 0.860f, 5.854f);
        }
        return new ModParams(0.992f, 0.540f, 1.849f, -0.528f, 1.462f);
    }


    private int frameCount = 0;

    // 保存目录（修改为你指定的路径）
    private string saveFolder = @"D:\vectionProject\public\ExperimentData3-Images";
    void SaveRenderTexture(Camera cam)
    {
        RenderTexture rt = cam.targetTexture;
        if (rt == null)
        {
            Debug.LogWarning("No RenderTexture assigned to this camera!");
            return;
        }

        // 设置当前激活的 RenderTexture
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        // 恢复
        RenderTexture.active = null;

        // 保存文件
        string filename = Path.Combine(saveFolder, $"capture1_{frameCount:000}.png");
        File.WriteAllBytes(filename, tex.EncodeToPNG());
        Debug.Log($"Saved: {filename}");

        frameCount++;
    }


    // 在编辑器中绘制 Gizmos 以可视化摄像头位置和方向
#if UNITY_EDITOR
private void OnDrawGizmos()
{
    // 摄像头的“父物体”
    Transform parent = captureCamera0.transform.parent;

    // 你 script 里设置的“local 值”
    Vector3 localPos = new Vector3(4f, 28f, 130f);
    Quaternion localRot = Quaternion.Euler(0f, 48.5f, 0f);

    Vector3 worldPos;
    Quaternion worldRot;

    if (parent != null)
    {
        // local → world
        worldPos = parent.TransformPoint(localPos);
        worldRot = parent.rotation * localRot;
        Debug.Log($"captureCamera0 world position: {worldPos}, world rotation: {worldRot.eulerAngles}");
    }
    else
    {
        // 没有父对象，local = world
        worldPos = localPos;
        worldRot = localRot;
        Debug.LogWarning("captureCamera0 has no parent; using local as world.");
    }

    // 世界方向
    Vector3 worldDir = worldRot * Vector3.right;

    // 画线
    Gizmos.color = Color.red;
    Gizmos.DrawLine(worldPos, worldPos + worldDir * 2000f);
    Gizmos.DrawSphere(worldPos, 2f);
}
#endif

    static float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }


    // 生成正弦条纹（相位用弧度），保持 Texture2D 为 linear=true 的前提下：
    // 1) 先按“要显示的灰度”在 sRGB 空间算 g_srgb ∈ [0,1]
    // 2) 再用 GammaToLinearSpace 转成 g_lin 写入贴图
    //
    // 这样能让 Unity(Linear Color Space) 的观感更接近你 Python 生成的 8bit 灰度图/视频。
    Texture2D MakeSineGratingRad(
        int w,
        int h,
        float cycles,
        float phaseRad,
        bool vertical,
        float amp,
        bool useSrgbToLinear = true, // 
        bool clamp01 = true          // 默认 clamp 到 [0,1]
    )
    {
        // 注意：最后一个参数 linear=true（你原来就是这样）
        // linear=true 表示：写入的数据被解释为“线性空间”的颜色值
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false, linear: true);

        // 防止 cycles/amp 奇怪值
        cycles = Mathf.Max(0f, cycles);
        amp = Mathf.Clamp(amp, 0f, 1.5f); // 允许略超 1，但一般 0~1 最安全

        var cols = new Color32[w * h];

        // 逐像素生成
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float v = (y + 0.5f) / h;

                // vertical=true: 条纹随 x 变化；false: 条纹随 y 变化
                float t = vertical ? u : v;

                // 正弦角度
                float ang = 2f * Mathf.PI * cycles * t + phaseRad;

                // 1) 先在“显示域(sRGB)”里算灰度
                // g_srgb = 0.5 + 0.5 * amp * sin()
                float g_srgb = 0.5f + 0.5f * (amp * Mathf.Sin(ang));

                if (clamp01) g_srgb = Mathf.Clamp01(g_srgb);

                // 2) 如果 Texture2D 是 linear=true，为了显示正确，需要把“想显示的灰度(sRGB)”
                // 转成线性再写入。
                float g_lin = useSrgbToLinear ? Mathf.GammaToLinearSpace(g_srgb) : g_srgb;

                if (clamp01) g_lin = Mathf.Clamp01(g_lin);

                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(g_lin * 255f), 0, 255);
                cols[y * w + x] = new Color32(b, b, b, 255);
            }
        }

        tex.SetPixels32(cols);
        tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        return tex;
    }

    void SaveRenderTextureToPng(RenderTexture rt, string path)
    {
        if (rt == null)
        {
            Debug.LogError("SaveRenderTextureToPng: RenderTexture is null");
            return;
        }

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply(false, false);

        RenderTexture.active = prev;

        byte[] png = tex.EncodeToPNG();
        Destroy(tex);

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, png);
    }

    static Bounds CombineBounds(Renderer[] rs)
    {
        if (rs == null || rs.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
        {
            if (rs[i] == null) continue;
            b.Encapsulate(rs[i].bounds);
        }
        return b;
    }
    static void BoundsCorners(Bounds b, Vector3[] corners8)
    {
        Vector3 min = b.min;
        Vector3 max = b.max;
        int idx = 0;
        for (int xi = 0; xi < 2; xi++)
            for (int yi = 0; yi < 2; yi++)
                for (int zi = 0; zi < 2; zi++)
                {
                    corners8[idx++] = new Vector3(
                        xi == 0 ? min.x : max.x,
                        yi == 0 ? min.y : max.y,
                        zi == 0 ? min.z : max.z
                    );
                }
    }

    /// <summary>
    /// 输出两个坐标系的 bbox：
    /// 1) bottom-left 原点（Unity viewport 乘像素）
    /// 2) top-left 原点（和 PNG/多数图像处理一致）
    /// </summary>
    static bool ComputeBboxOnRenderTexture(
        Camera cam, int rtW, int rtH, Bounds worldBounds,
        out RectInt bboxBottomLeft, out RectInt bboxTopLeft)
    {
        bboxBottomLeft = new RectInt();
        bboxTopLeft = new RectInt();

        if (rtW <= 1 || rtH <= 1) return false;

        Vector3[] c = new Vector3[8];
        BoundsCorners(worldBounds, c);

        float minX = 1e9f, minY = 1e9f;
        float maxX = -1e9f, maxY = -1e9f;
        bool anyInFront = false;

        for (int i = 0; i < 8; i++)
        {
            Vector3 v = cam.WorldToViewportPoint(c[i]); // x,y:0..1 (理论上), z>0 表示在前方
            if (v.z > 0) anyInFront = true;

            // 仍然统计（即使部分点在外面），最后再 clamp
            minX = Mathf.Min(minX, v.x);
            minY = Mathf.Min(minY, v.y);
            maxX = Mathf.Max(maxX, v.x);
            maxY = Mathf.Max(maxY, v.y);
        }

        if (!anyInFront) return false;

        // clamp 到画面内（0..1）
        minX = Mathf.Clamp01(minX);
        minY = Mathf.Clamp01(minY);
        maxX = Mathf.Clamp01(maxX);
        maxY = Mathf.Clamp01(maxY);

        int x0 = Mathf.FloorToInt(minX * rtW);
        int y0 = Mathf.FloorToInt(minY * rtH);
        int x1 = Mathf.CeilToInt(maxX * rtW);
        int y1 = Mathf.CeilToInt(maxY * rtH);

        int w = Mathf.Max(1, x1 - x0);
        int h = Mathf.Max(1, y1 - y0);

        // bottom-left 原点 bbox
        bboxBottomLeft = new RectInt(x0, y0, w, h);

        // top-left 原点 bbox（图像常用坐标）
        int yTop = rtH - (y0 + h);
        bboxTopLeft = new RectInt(x0, yTop, w, h);

        return true;
    }

    // Helper: append a CSV line, create folder and header when needed
    private void AppendCsvLine(string filePath, string header, string line)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            bool writeHeader = !File.Exists(filePath);
            using (var sw = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                if (writeHeader && !string.IsNullOrEmpty(header))
                    sw.WriteLine(header);
                sw.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"AppendCsvLine failed: {ex.Message}");
        }
    }
    void LuminanceMixture()
    {

        // 写真を撮る距離に達したかをチェック //
        float frameMs = updateInterval * 1000f;
        // if (Mathf.Abs(timeMs - frameNum * updateInterval * 1000) < 0.2f)
        while (timeMs >= frameNum * frameMs)
        {
            frameNum++;

            // カメラが移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置
            targetPosition = direction * cameraSpeed * updateInterval;
            captureCamera1.transform.position = captureCamera1.transform.position + targetPosition;
            captureCamera2.transform.position = captureCamera2.transform.position + targetPosition;
        }

        // CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", topTex);
        // CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", botTex);

        // CaptureCameraLinearBlendTopRawImage.material.SetTexture("_TopTex", topTex);
        // CaptureCameraLinearBlendTopRawImage.material.SetTexture("_BottomTex", botTex);

        //輝度値を計算する 
        float Image1ToNowDeltaTime = timeMs - (frameNum - 1) * updateInterval * 1000;
        float nextRatio = Image1ToNowDeltaTime / (updateInterval * 1000);

        float nextImageRatio = Mathf.Clamp01(nextRatio);
        float previousImageRatio = 1.0f - nextImageRatio;

        float nonlinearPreviousImageRatio = previousImageRatio;
        float nonlinearNextImageRatio = nextImageRatio;
        knobValue = SerialReader.lastSensorValue;
        Debug.Log($"updateInterval={updateInterval:F4}s frameNum={frameNum} timeMs={timeMs:F1} nextRatio={nextRatio:F3}");
        // ---top 准备 ---
        nonlinearNextImageRatio = BrightnessBlend.GetMixedValue(nextImageRatio, knobValue, BrightnessBlendMode.LinearOnly);
        // 将混合权重从 [0,1] 映射到 [0.1,0.9]
        nonlinearNextImageRatio = Mathf.Lerp(0.1f, 0.9f, Mathf.Clamp01(nonlinearNextImageRatio));
        nonlinearPreviousImageRatio = 1f - nonlinearNextImageRatio;

        // CaptureCameraLinearBlendTopRawImage.material.SetColor("_TopColor", new Color(1, 1, 1, nonlinearNextImageRatio)); // 透明度
        // CaptureCameraLinearBlendTopRawImage.material.SetColor("_BottomColor", new Color(1, 1, 1, 1.0f));

        // ---bottom 反相位补偿准备 ---
        // ② 算出当前这 1s 区间内的时间（秒）
        float tLocalSec = Image1ToNowDeltaTime / 1000f;
        ModParams p = GetParams(subject);
        nonlinearNextImageRatio = BrightnessBlend.GetMixedValue(nextImageRatio, knobValue, brightnessBlendMode);
        // 再次映射最终值到 [0.1,0.9]
        nonlinearNextImageRatio = Mathf.Lerp(0.1f, 0.9f, Mathf.Clamp01(nonlinearNextImageRatio));
        nonlinearPreviousImageRatio = 1f - nonlinearNextImageRatio;

        Debug.Log($"p={nextImageRatio:F3}  wPh={nonlinearNextImageRatio:F3}  |w-p|={Mathf.Abs(nonlinearNextImageRatio - nextImageRatio):F3}");
        CaptureCameraLinearBlendRawImage.material.SetColor("_TopColor", new Color(1, 1, 1, nonlinearNextImageRatio)); // 透明度
        CaptureCameraLinearBlendRawImage.material.SetColor("_BottomColor", new Color(1, 1, 1, 1.0f));


        if (frameNum % 2 == 0)
        {
            alphaHistory.Add(nonlinearPreviousImageRatio);
        }
        else
        {
            alphaHistory.Add(nonlinearNextImageRatio);
        }

        //------------波形start
        float now = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
        // 現在の alpha 値をサンプルに追加//添加当前样本
        timeStamps.Add(now);

        // 1秒より前のデータを削除//剔除 1 秒以前的数据
        while (timeStamps.Count > 0 && timeStamps[0] < now - 5f)
        {
            timeStamps.RemoveAt(0);
            alphaHistory.RemoveAt(0);
            velocityHistory.RemoveAt(0);
        }

        // 上限を超えた場合は最古データから削除 //如果依然过多，按最早移除
        if (timeStamps.Count > maxSamples)
        {
            timeStamps.RemoveAt(0);
            alphaHistory.RemoveAt(0);
            velocityHistory.RemoveAt(0);
        }
        //------------波形end

        // データを記録 // 记录数据
        data.Add($"{frameNum}, {nonlinearPreviousImageRatio:F3}, {frameNum + 1}, {nonlinearNextImageRatio:F3}, {timeMs:F3}, {SerialReader.lastSensorValue}, {responsePattern}, {(int)stepNumber}, {amplitudeToSaveData}, {v}, {knobValue:F3}, {cameraSpeed:F3}");
    }
    private void RecordWaveAndData(
    float now,
    float alphaToPlot,
    string dataLine
)
    {
        // ---- 波形：追加 ----
        timeStamps.Add(now);
        alphaHistory.Add(alphaToPlot);

        // 如果 velocityHistory 你目前没用，可以先 append 0 或者你实际的 v
        velocityHistory.Add(v); // 或 0f

        // ---- 滑动窗口：删除 5 秒前 ----
        while (timeStamps.Count > 0 && timeStamps[0] < now - 5f)
        {
            timeStamps.RemoveAt(0);
            alphaHistory.RemoveAt(0);
            velocityHistory.RemoveAt(0);
        }

        // ---- 样本上限 ----
        if (timeStamps.Count > maxSamples)
        {
            timeStamps.RemoveAt(0);
            alphaHistory.RemoveAt(0);
            velocityHistory.RemoveAt(0);
        }

        // ---- 记录字符串 ----
        if (!string.IsNullOrEmpty(dataLine))
            data.Add(dataLine);
    }
    private void CaptureAndSavePng(Camera cam, string outPath)
    {
        if (cam == null || cam.targetTexture == null) return;

        cam.Render();

        var rt = cam.targetTexture;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        // 关键1：linear=true（最后一个参数），表示这张Texture2D内容按“线性”处理
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false, true);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply(false, false);

        RenderTexture.active = prev;

        // 关键2：把线性值编码成sRGB再写PNG
        var px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            px[i].r = Mathf.LinearToGammaSpace(px[i].r);
            px[i].g = Mathf.LinearToGammaSpace(px[i].g);
            px[i].b = Mathf.LinearToGammaSpace(px[i].b);
            px[i].a = 1f;
        }
        tex.SetPixels(px);
        tex.Apply(false, false);
        Debug.Log($"Saving PNG to: {outPath}");
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Destroy(tex);
    }

    /// <summary>
    /// Load frames from Assets/Resources/{resourcesFolder}/ with namePrefix.
    /// Returns loaded frames array (may be empty).
    /// </summary>
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


    // 你原来加载 frames 的地方改成调用这个
    private void EnsureFramesLoaded()
    {
        if (frames != null && frames.Length > 0) return;
        frames = LoadFramesFromResources(resourcesFolder, namePrefix, verboseLoadLog);
    }
    private void ResetGaussWarmup()
    {
        _gaussWarmupDone = false;
        _gaussWarmupCount = 0;
        _last0 = _last1 = _last2 = -1; // 如果你有缓存索引
    }

    // ------ 2AFC fields ------
    private bool in2AfcMode = false;
    private GameObject _2afcPanel;
    private Button _2afcUpperButton;
    private Button _2afcLowerButton;
    private int _2afcTrialIndex = 0;
    private List<bool> _2afcOrder = new List<bool>(); // true => top shows Linear, bottom Gauss; false => reversed
    // 当正在等待被试在 2AFC 界面做出响应时为 true —— 用于暂停帧播放/更新
    private bool _2afcWaitingForResponse = false;
    public float twoAfcDurationSec = 10f; // 显示时长
    // 2AFC 过渡说明面板
    private GameObject _2afcIntroPanel;
    private Button _2afcIntroStartButton;
    // 如果为 true 则跳过调参阶段，直接进入 2AFC 过渡说明（供已完成调参的被试重复只做 2AFC）
    public bool only2AfcMode = false;
    // 2AFC 总试次数（可在 Inspector 调整，默认 20）
    public int twoAfcTrials = 20;
    // ------------------------

    // 在调参结束时调用：开始两组 2AFC（顺序随机/或固定）
    public void Start2AfcTrials()
    {
        // 恢复 timeScale，防止前面某处将时间暂停导致 2AFC 不更新
        Time.timeScale = 1f;
        // 防止遗留的调参按钮显示或被误触发
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        // 确保 Update 不会因为上一帧的输入重新激活调参 UI
        mouseClicked = true;

        // 如果不是 only2AfcMode，则保存当前调参数据；若只做 2AFC（重复被试），跳过保存
        if (!only2AfcMode) SaveCurrentDataToCsv();
        if (frames == null || frames.Length == 0) { Debug.LogError("Cannot start 2AFC: frames not loaded."); return; }

        Set2AfcLayout(true); // 切换到上下显示布局

        // 生成 N 次试次（使用可配置字段 twoAfcTrials，默认 20）
        int trials = Mathf.Max(1, twoAfcTrials);
        _2afcOrder.Clear();
        int half = trials / 2;
        for (int i = 0; i < half; i++) _2afcOrder.Add(true);   // top Linear
        for (int i = 0; i < trials - half; i++) _2afcOrder.Add(false); // top Gauss

        // 随机化顺序（可用 subjectSeed 固定化）
        var rnd = new System.Random(subjectSeed == 0 ? DateTime.Now.GetHashCode() : subjectSeed);
        _2afcOrder = _2afcOrder.OrderBy(x => rnd.Next()).ToList();

        _2afcTrialIndex = 0;
        StartCoroutine(Run2AfcSequence());
    }


    // 布局工具：切换为上下各半屏（用于 2AFC）
    void ArrangeUIFor2Afc()
    {
        // 调参画面隐藏
        if (continuousImageRawImage != null) continuousImageRawImage.enabled = false;

        if (CaptureCameraLinearBlendRawImage != null)
        {
            var rt = CaptureCameraLinearBlendRawImage.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            CaptureCameraLinearBlendRawImage.gameObject.SetActive(true);
        }

        if (CaptureCameraLinearBlendTopRawImage != null)
        {
            var rt = CaptureCameraLinearBlendTopRawImage.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(true);
        }
    }

    // Helper: put the two RawImages at the top/bottom positions without changing their materials.
    void PositionForTopIsLinear(bool topIsLinear)
    {
        if (CaptureCameraLinearBlendTopRawImage == null || CaptureCameraLinearBlendRawImage == null) return;

        RectTransform rtLinear = CaptureCameraLinearBlendRawImage.GetComponent<RectTransform>();
        RectTransform rtGauss = CaptureCameraLinearBlendTopRawImage.GetComponent<RectTransform>();

        // screen Y values you provided
        float topY = 353f;
        float botY = -356f;

        if (topIsLinear)
        {
            rtLinear.anchoredPosition = new Vector2(rtLinear.anchoredPosition.x, topY);
            rtGauss.anchoredPosition = new Vector2(rtGauss.anchoredPosition.x, botY);

            // draw order: put the top one last so it renders above if overlapping
            CaptureCameraLinearBlendRawImage.transform.SetAsLastSibling();
            CaptureCameraLinearBlendTopRawImage.transform.SetSiblingIndex(0);
        }
        else
        {
            rtGauss.anchoredPosition = new Vector2(rtGauss.anchoredPosition.x, topY);
            rtLinear.anchoredPosition = new Vector2(rtLinear.anchoredPosition.x, botY);

            CaptureCameraLinearBlendTopRawImage.transform.SetAsLastSibling();
            CaptureCameraLinearBlendRawImage.transform.SetSiblingIndex(0);
        }
    }

    private IEnumerator Run2AfcSequence()
    {
        // Prevent normal luminance loop from changing materials
        in2AfcMode = true;

        // Create material instances for the whole 2AFC session (reuse to avoid GC)
        Material matLinearInstance = new Material(Mat_GrayscaleOverBlend);
        Material matGaussInstance = new Material(GaussBlendMat);

        // Ensure both UI raw images are active for two-viewport display
        if (CaptureCameraLinearBlendRawImage != null) CaptureCameraLinearBlendRawImage.gameObject.SetActive(true);
        if (CaptureCameraLinearBlendTopRawImage != null) CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(true);

        while (_2afcTrialIndex < _2afcOrder.Count)
        {
            bool topIsLinear = _2afcOrder[_2afcTrialIndex];
            Debug.Log($"2AFC trial {_2afcTrialIndex + 1}/{_2afcOrder.Count} topIsLinear={topIsLinear}");

            // layout & position
            Set2AfcLayout(true);
            PositionForTopIsLinear(topIsLinear);

            // ------- Fix: keep material instances tied to their RawImage (linear->CaptureCameraLinearBlendRawImage,
            // gauss->CaptureCameraLinearBlendTopRawImage) and only swap positions / sibling order.
            // Previously code swapped the materials between the two RawImages which can lead to the second
            // trial not showing the expected swap. Assign once per trial to be safe.
            if (matLinearInstance != null && CaptureCameraLinearBlendRawImage != null)
            {
                CaptureCameraLinearBlendRawImage.material = matLinearInstance;
            }
            if (matGaussInstance != null && CaptureCameraLinearBlendTopRawImage != null)
            {
                CaptureCameraLinearBlendTopRawImage.material = matGaussInstance;
            }

            // Ensure the visual stacking matches topIsLinear:
            // if topIsLinear -> make the linear RawImage sit at top; otherwise make gauss RawImage on top.
            if (topIsLinear)
            {
                CaptureCameraLinearBlendRawImage.transform.SetAsLastSibling();        // linear on top
                CaptureCameraLinearBlendTopRawImage.transform.SetSiblingIndex(0);     // gauss below
            }
            else
            {
                CaptureCameraLinearBlendTopRawImage.transform.SetAsLastSibling();     // gauss on top
                CaptureCameraLinearBlendRawImage.transform.SetSiblingIndex(0);        // linear below
            }

            // Play the sequence
            float elapsed = 0f;
            int localFrameNum = 1;
            float frameMs = updateInterval * 1000f;

            Debug.Log($"Run2AfcSequence: twoAfcDurationSec={twoAfcDurationSec} Time.timeScale={Time.timeScale}");

            // reset any warmup to avoid spikes
            ResetGaussWarmup();

            // 使用 unscaled 时间以防 timeScale 被改动（UI 暂停等）
            while (elapsed < twoAfcDurationSec)
            {
                elapsed += Time.unscaledDeltaTime;
                float tGlobalMs = elapsed * 1000f;
                while (tGlobalMs >= localFrameNum * frameMs) localFrameNum++;

                // 更新 linear / gauss 材质与权重（保持原有逻辑）
                int n = frames.Length;
                int prevIdx = Mathf.Clamp(localFrameNum - 1, 0, n - 1);
                int nextIdx = Mathf.Clamp(localFrameNum, 0, n - 1);
                Texture linearBotTex = frames[prevIdx];
                Texture linearTopTex = frames[nextIdx];
                float Image1ToNowDeltaTime = tGlobalMs - (localFrameNum - 1) * updateInterval * 1000f;
                float linearAlpha = Mathf.Clamp01(Image1ToNowDeltaTime / (updateInterval * 1000f));

                // gauss 计算（保持原样）
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
            } // end playing duration

            // 显示选择按钮并等待响应
            bool _waitingFor2AfcResponse = true;

            // 注意：不能在有 catch 的 try 块里使用 yield（会编译错误 CS1626）。
            // 这里使用 try...finally 保证无论如何都能 Cleanup（且允许 yield）。
            bool uiShown = false;
            try
            {
                // 告知其他更新逻辑暂停帧播放
                _2afcWaitingForResponse = true;

                Show2AfcButtons();
                uiShown = true;

                void LocalHandler(bool upperSelected)
                {
                    string setting = topIsLinear ? "TopLinear_BottomGauss" : "TopGauss_BottomLinear";
                    string choice = upperSelected ? "Upper" : "Lower";
                    data.Add($"2AFC,Trial{_2afcTrialIndex + 1},{setting},Choice,{choice},duration,{twoAfcDurationSec:F1}");
                    // save immediately to CSV in D:\vectionProject\public\<folderName>\ParticipantName_<name>_2AFC_results.csv
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
                // 退出等待状态（确保不会一直暂停）
                _2afcWaitingForResponse = false;

                // 始终移除监听并清理 UI（即使 Show2AfcButtons 抛异常也会执行）
                if (uiShown)
                {
                    if (_2afcUpperButton != null) _2afcUpperButton.onClick.RemoveAllListeners();
                    if (_2afcLowerButton != null) _2afcLowerButton.onClick.RemoveAllListeners();
                }
                Cleanup2AfcUI();
            }

            _2afcTrialIndex++;
        }

        // sequence done
        in2AfcMode = false;

        Set2AfcLayout(false); // 恢复调参布局

        // restore original materials (optional)
        CaptureCameraLinearBlendRawImage.material = new Material(Mat_GrayscaleOverBlend);
        CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);
        CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);
        _Gaussmat = new Material(GaussBlendMat);
        CaptureCameraLinearBlendTopRawImage.material = _Gaussmat;

        Debug.Log("2AFC sequence finished.");
        // 两次 2AFC 选择完成后结束程序（在编辑器下停止 Play Mode）
        QuitGame();
    }

    private Font GetSafeUiFont()
    {
        // Try to create a dynamic font from OS first (more robust on modern Unity)
        try
        {
            var f = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (f != null) return f;
        }
        catch { }

        // Fallback: try builtin resource but guard exception
        try
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        catch
        {
            Debug.LogWarning("GetSafeUiFont: could not load Arial.ttf; UI text may be plain or invisible.");
            return null;
        }
    }

    private void Show2AfcButtons()
    {
        // create panel if needed
        if (_2afcPanel == null)
        {
            _2afcPanel = new GameObject("2AFC_Panel");
            var rt = _2afcPanel.AddComponent<RectTransform>();
            _2afcPanel.transform.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            // make sure panel is on top
            _2afcPanel.transform.SetAsLastSibling();
        }

        Font uiFont = GetSafeUiFont();

        // Full-screen gray background (covers video)
        var bgGO = new GameObject("2AFC_Background");
        bgGO.transform.SetParent(_2afcPanel.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.5f, 0.5f, 0.5f, 1f); // solid mid-gray
        // put background as first child so everything else draws above it
        bgGO.transform.SetSiblingIndex(0);

        // Question label (placed above upper button)
        if (uiFont != null)
        {
            var qGO = new GameObject("2AFC_Question");
            qGO.transform.SetParent(_2afcPanel.transform, false);
            var qRT = qGO.AddComponent<RectTransform>();
            qRT.anchorMin = new Vector2(0.1f, 0.92f);
            qRT.anchorMax = new Vector2(0.9f, 0.99f);
            qRT.offsetMin = Vector2.zero;
            qRT.offsetMax = Vector2.zero;
            var qText = qGO.AddComponent<Text>();
            qText.alignment = TextAnchor.MiddleCenter;
            qText.font = uiFont;
            qText.text = "どちらが等速に近いでしょうか?";
            qText.fontSize = 48;
            qText.color = Color.white;
            qGO.transform.SetAsLastSibling();
        }

        // Upper button
        var upperGO = new GameObject("2AFC_UpperButton");
        upperGO.transform.SetParent(_2afcPanel.transform, false);
        var upperRT = upperGO.AddComponent<RectTransform>();
        upperRT.anchorMin = new Vector2(0.25f, 0.55f);
        upperRT.anchorMax = new Vector2(0.75f, 0.9f);
        upperRT.offsetMin = Vector2.zero;
        upperRT.offsetMax = Vector2.zero;
        var upperImg = upperGO.AddComponent<Image>();
        // subtle translucent background so text is legible over gray
        upperImg.color = new Color(0f, 0f, 0f, 0.15f);
        _2afcUpperButton = upperGO.AddComponent<Button>();

        if (uiFont != null)
        {
            var upperTextGO = new GameObject("Text");
            upperTextGO.transform.SetParent(upperGO.transform, false);
            var upperText = upperTextGO.AddComponent<Text>();
            upperText.alignment = TextAnchor.MiddleCenter;
            upperText.font = uiFont;
            upperText.text = "上";
            upperText.fontSize = 64;
            upperText.color = Color.white;
            var upperTextRT = upperTextGO.GetComponent<RectTransform>();
            upperTextRT.anchorMin = Vector2.zero;
            upperTextRT.anchorMax = Vector2.one;
            upperTextRT.offsetMin = Vector2.zero;
            upperTextRT.offsetMax = Vector2.zero;
        }

        // Lower button
        var lowerGO = new GameObject("2AFC_LowerButton");
        lowerGO.transform.SetParent(_2afcPanel.transform, false);
        var lowerRT = lowerGO.AddComponent<RectTransform>();
        lowerRT.anchorMin = new Vector2(0.25f, 0.1f);
        lowerRT.anchorMax = new Vector2(0.75f, 0.45f);
        lowerRT.offsetMin = Vector2.zero;
        lowerRT.offsetMax = Vector2.zero;
        var lowerImg = lowerGO.AddComponent<Image>();
        lowerImg.color = new Color(0f, 0f, 0f, 0.15f);
        _2afcLowerButton = lowerGO.AddComponent<Button>();

        if (uiFont != null)
        {
            var lowerTextGO = new GameObject("Text");
            lowerTextGO.transform.SetParent(lowerGO.transform, false);
            var lowerText = lowerTextGO.AddComponent<Text>();
            lowerText.alignment = TextAnchor.MiddleCenter;
            lowerText.font = uiFont;
            lowerText.text = "下";
            lowerText.fontSize = 64;
            lowerText.color = Color.white;
            var lowerTextRT = lowerTextGO.GetComponent<RectTransform>();
            lowerTextRT.anchorMin = Vector2.zero;
            lowerTextRT.anchorMax = Vector2.one;
            lowerTextRT.offsetMin = Vector2.zero;
            lowerTextRT.offsetMax = Vector2.zero;
        }
    }

    private void Cleanup2AfcUI()
    {
        if (_2afcPanel != null)
        {
            Destroy(_2afcPanel);
            _2afcPanel = null;
            _2afcUpperButton = null;
            _2afcLowerButton = null;
        }
    }



    /// <summary>
    /// 切换 2AFC 布局：true -> 隐藏 continuous，TopRawImage 放到 y=353，BotRawImage 放到 y=-356
    /// false -> 恢复调参布局（continuous 显示，两个 RawImage 都放到 y=-356）
    /// </summary>
    void Set2AfcLayout(bool enable2Afc)
    {
        // hide/show continuous image
        if (continuousImageRawImage != null)
            continuousImageRawImage.gameObject.SetActive(!enable2Afc);

        RectTransform rtTop = CaptureCameraLinearBlendTopRawImage != null
            ? CaptureCameraLinearBlendTopRawImage.GetComponent<RectTransform>()
            : null;
        RectTransform rtBot = CaptureCameraLinearBlendRawImage != null
            ? CaptureCameraLinearBlendRawImage.GetComponent<RectTransform>()
            : null;

        if (enable2Afc)
        {
            if (rtTop != null) rtTop.anchoredPosition = new Vector2(rtTop.anchoredPosition.x, 353f);
            if (rtBot != null) rtBot.anchoredPosition = new Vector2(rtBot.anchoredPosition.x, -356f);

            if (CaptureCameraLinearBlendRawImage != null)
            {
                CaptureCameraLinearBlendRawImage.gameObject.SetActive(true);
                CaptureCameraLinearBlendRawImage.enabled = true;
                var c = CaptureCameraLinearBlendRawImage.color; c.a = 1f; CaptureCameraLinearBlendRawImage.color = c;
                // 把底层放在父物体中靠下，保证 top 在上面
                CaptureCameraLinearBlendRawImage.transform.SetSiblingIndex(0);
            }

            if (CaptureCameraLinearBlendTopRawImage != null)
            {
                CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(true);
                CaptureCameraLinearBlendTopRawImage.enabled = true;
                var c2 = CaptureCameraLinearBlendTopRawImage.color; c2.a = 1f; CaptureCameraLinearBlendTopRawImage.color = c2;
                // 把 top 放到最上层
                CaptureCameraLinearBlendTopRawImage.transform.SetAsLastSibling();
            }
        }
        else
        {
            // 调参时两个 blend 都放在下面，continuous 显示在上（y=353）
            if (rtTop != null) rtTop.anchoredPosition = new Vector2(rtTop.anchoredPosition.x, -356f);
            if (rtBot != null) rtBot.anchoredPosition = new Vector2(rtBot.anchoredPosition.x, -356f);

            if (CaptureCameraLinearBlendRawImage != null)
            {
                CaptureCameraLinearBlendRawImage.gameObject.SetActive(true);
                CaptureCameraLinearBlendRawImage.enabled = true;
                var c = CaptureCameraLinearBlendRawImage.color; c.a = 1f; CaptureCameraLinearBlendRawImage.color = c;
                CaptureCameraLinearBlendRawImage.transform.SetAsLastSibling(); // 放到上层以便在下面被 continuous 遮挡时可见调试
            }

            if (CaptureCameraLinearBlendTopRawImage != null)
            {
                CaptureCameraLinearBlendTopRawImage.gameObject.SetActive(true);
                CaptureCameraLinearBlendTopRawImage.enabled = true;
                var c2 = CaptureCameraLinearBlendTopRawImage.color; c2.a = 1f; CaptureCameraLinearBlendTopRawImage.color = c2;
                CaptureCameraLinearBlendTopRawImage.transform.SetSiblingIndex(0);
            }

            if (continuousImageRawImage != null)
            {
                var crt = continuousImageRawImage.GetComponent<RectTransform>();
                if (crt != null) crt.anchoredPosition = new Vector2(crt.anchoredPosition.x, 353f);
                continuousImageRawImage.gameObject.SetActive(true);
                continuousImageRawImage.enabled = true;
                continuousImageRawImage.transform.SetAsLastSibling(); // continuous 放最上（调参时）
            }
        }

        // Debug 帮助：在控制台打印状态（可临时打开）
        Debug.Log($"Set2AfcLayout({enable2Afc}) topActive={CaptureCameraLinearBlendTopRawImage?.gameObject.activeSelf} topEnabled={CaptureCameraLinearBlendTopRawImage?.enabled} botActive={CaptureCameraLinearBlendRawImage?.gameObject.activeSelf} botEnabled={CaptureCameraLinearBlendRawImage?.enabled} continuousActive={continuousImageRawImage?.gameObject.activeSelf}");
    }

    /// <summary>
    /// 计算多个 Renderer 的联合 Bounds
    /// </summary>
    private bool TryGetCombinedBounds(Renderer[] renderers, out Bounds combined)
    {
        combined = new Bounds();
        if (renderers == null || renderers.Length == 0) return false;
        bool inited = false;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (!inited) { combined = r.bounds; inited = true; }
            else combined.Encapsulate(r.bounds);
        }
        return inited;
    }

    /// <summary>
    /// 计算给定 Bounds 在 RenderTexture 上的包围盒（以左下角为原点的坐标系）
    /// </summary>
    private void SaveRoiMetadataForFrame(Renderer[] renderers, Camera cam, int secIndex, string frameName, string csvPath)
    {
        try
        {
            int rtW = (cam != null && cam.targetTexture != null) ? cam.targetTexture.width : Screen.width;
            int rtH = (cam != null && cam.targetTexture != null) ? cam.targetTexture.height : Screen.height;

            bool valid = false;
            RectInt bboxBL = new RectInt(0, 0, 0, 0);
            RectInt bboxTL = new RectInt(0, 0, 0, 0);

            if (renderers != null && renderers.Length > 0 && cam != null)
            {
                Bounds b;
                if (TryGetCombinedBounds(renderers, out b))
                {
                    valid = ComputeBboxOnRenderTexture(cam, rtW, rtH, b, out bboxBL, out bboxTL);
                }
            }

            // ensure directory exists
            var dir = Path.GetDirectoryName(csvPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            bool writeHeader = !File.Exists(csvPath);
            using (var sw = new StreamWriter(csvPath, true, Encoding.UTF8))
            {
                if (writeHeader)
                {
                    sw.WriteLine("secIndex,frameName,rtW,rtH,x_bl,y_bl,w,h,x_tl,y_tl,w_tl,h_tl,valid");
                }

                string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
                    secIndex,
                    frameName,
                    rtW,
                    rtH,
                    bboxBL.x, bboxBL.y, bboxBL.width, bboxBL.height,
                    bboxTL.x, bboxTL.y, bboxTL.width, bboxTL.height,
                    valid ? 1 : 0
                );
                sw.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveRoiMetadataForFrame failed: {ex}");
        }
    }
    // ...existing code...
    private void Show2AfcIntroPanel()
    {
        if (_2afcIntroPanel != null) return;
        if (canvas == null) canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogWarning("Show2AfcIntroPanel: Canvas not found."); return; }

        _2afcIntroPanel = new GameObject("2AFC_IntroPanel");
        var rt = _2afcIntroPanel.AddComponent<RectTransform>();
        _2afcIntroPanel.transform.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = _2afcIntroPanel.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // 深灰背景，覆盖画面
        _2afcIntroPanel.transform.SetAsLastSibling();

        Font uiFont = GetSafeUiFont();

        // 说明文字
        var textGO = new GameObject("2AFC_IntroText");
        textGO.transform.SetParent(_2afcIntroPanel.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.1f, 0.55f);
        textRT.anchorMax = new Vector2(0.9f, 0.85f);
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
        var txt = textGO.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = uiFont;
        txt.text = "これから2AFC課題を行います。\n\n各試行では2つの刺激映像が上下提示されますので、\n\nどちらがより等速に見えるかを判断してください。\n\n準備ができましたら、下の「start」ボタンを押してください。";
        txt.fontSize = 50;
        txt.color = Color.white;

        // 开始按钮
        var btnGO = new GameObject("2AFC_StartButton");
        btnGO.transform.SetParent(_2afcIntroPanel.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.35f, 0.2f);
        btnRT.anchorMax = new Vector2(0.65f, 0.32f);
        btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.1f, 0.6f, 0.9f, 1f);
        _2afcIntroStartButton = btnGO.AddComponent<Button>();

        if (uiFont != null)
        {
            var btTextGO = new GameObject("Text");
            btTextGO.transform.SetParent(btnGO.transform, false);
            var btText = btTextGO.AddComponent<Text>();
            btText.font = uiFont;
            btText.text = "start";
            btText.alignment = TextAnchor.MiddleCenter;
            btText.fontSize = 50;
            btText.color = Color.white;
            var btRT = btTextGO.GetComponent<RectTransform>();
            btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
            btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
        }

        _2afcIntroStartButton.onClick.AddListener(() =>
        {
            // 防止同一帧的鼠标点击被 Update() 误判为调参点击
            mouseClicked = true;
            if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);

            // 恢复游戏时间（如果之前被暂停）
            Time.timeScale = 1f;

            Cleanup2AfcIntroPanel();
            // 启动 2AFC 序列
            Start2AfcTrials();
        });
    }

    private void Cleanup2AfcIntroPanel()
    {
        if (_2afcIntroStartButton != null) _2afcIntroStartButton.onClick.RemoveAllListeners();
        if (_2afcIntroPanel != null)
        {
            Destroy(_2afcIntroPanel);
            _2afcIntroPanel = null;
            _2afcIntroStartButton = null;
        }
    }
}



