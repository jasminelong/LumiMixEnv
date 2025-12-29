using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
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

        updateInterval = 1 / fps; // 各フレームの表示間隔時間を計算 // 计算每一帧显示的间隔时间
        captureIntervalDistance = cameraSpeed / fps; // 各フレームの間隔距離を計算 // 计算每帧之间的间隔距离

        GetRawImage();
        InitialSetup();

        continuousImageRawImage.enabled = true;
        captureCamera2.transform.position += direction * captureIntervalDistance;
        SerialReader = GetComponent<SerialReader>();

        TrailSettings();
        nextStepButtonTextComponent = nextStepButton.GetComponentInChildren<TextMeshProUGUI>();
        nextStepButton.onClick.AddListener(OnNextStep); // ボタンがクリックされたときの処理を追加 // 添加按钮点击时的处理


        if (experimentPattern == ExperimentPattern.NoLuminanceBlendSingleCameraMove)
            data.Add("Time, Knob, ResponsePattern, StepNumber, Amplitude, Velocity, FunctionRatio, CameraSpeed");
        else
            data.Add("FrondFrameNum, FrondFrameLuminance, BackFrameNum, BackFrameLuminance, Time, Knob, ResponsePattern, StepNumber, Amplitude, Velocity, FunctionRatio, CameraSpeed");

    }
    void Awake()
    {
        // 强制运行时初始为 Option1（避免被旧序列化值影响）
        if ((int)stepNumber < 1) stepNumber = StepNumber.Option1;
        Debug.Log($"[MoveCamera] stepNumber = {(int)stepNumber} ({stepNumber}) in Awake");
    }
    void Update()
    {
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
                    if (experimentPattern == ExperimentPattern.FunctionMix)
                    {
                        nextStepButtonTextComponent.text = "Entering the next trial";
                    }
                    else
                    {
                        nextStepButtonTextComponent.text = "Next Step";
                    }
                    break;
                case 1:
                case 2:
                case 3:
                    nextStepButtonTextComponent.text = "Next Step";
                    break;
                case 4:
                    if (paramOrder == ParameterOrder.V0_A1_PHI1_A2_PHI2)
                    {
                        nextStepButtonTextComponent.text = "Entering the next trial";
                    }
                    break;
                case 5:

                    nextStepButtonTextComponent.text = "Next Step";
                    break;
                case 6:
                    if (paramOrder == ParameterOrder.V0_PHI1_A1_PHI1_PHI2_A2_PHI2)
                    {
                        nextStepButtonTextComponent.text = "Entering the next trial";
                    }
                    break;
                case 7:
                case 8:
                    nextStepButtonTextComponent.text = "Next Step";
                    break;
                case 9:
                    if (paramOrder == ParameterOrder.V0_A1_PHI1_A2_PHI2_A1_PHI1_A2_PHI2)
                    {
                        nextStepButtonTextComponent.text = "Entering the next trial";
                    }
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

        Continuous();
        // if (experimentPattern == ExperimentPattern.NoLuminanceBlendSingleCameraMove)
        // {
        //     NoLuminanceBlendSingleCameraMove();
        // }
        // else
        // {
        if (!isInGray && timeMs >= segmentMs)
        {
            StartCoroutine(GrayBreakRoutine());
        }

        LuminanceMixture();

        // }
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
                if (experimentPattern == ExperimentPattern.FunctionMix)
                {
                    if (isEnd)
                    {
                        QuitGame();
                    }
                    else
                    {
                        MarkTrialCompletedAndRestart();
                    }
                }
                else
                {
                    stepNumber = StepNumber.Option1;
                }
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
                if (paramOrder == ParameterOrder.V0_A1_PHI1_A2_PHI2)
                {
                    if (isEnd)
                    {
                        QuitGame();
                    }
                    else
                    {
                        MarkTrialCompletedAndRestart();
                    }
                }
                stepNumber = StepNumber.Option5;
                break;
            case 6:
                stepNumber = StepNumber.Option6;
                break;
            case 7:
                stepNumber = StepNumber.Option7;
                break;
            case 8:
                stepNumber = StepNumber.Option8;
                break;
            case 9:
                if (paramOrder == ParameterOrder.V0_A1_PHI1_A2_PHI2_A1_PHI1_A2_PHI2)
                {
                    if (isEnd)
                    {
                        QuitGame();
                    }
                    else
                    {
                        MarkTrialCompletedAndRestart();
                    }
                }
                stepNumber = StepNumber.Option9;
                break;
        }
        nextStepButton.gameObject.SetActive(false);
    }

    void Continuous()
    {
        continuousImageRawImage.enabled = true;
        time += Time.fixedDeltaTime;

        if (experimentPattern == ExperimentPattern.FunctionMix)
        {
            captureCamera0.transform.position += direction * cameraSpeed * Time.deltaTime;
        }
        else
        {
            // つまみセンサー値（0〜1）を取得し
            float knobValue = Mathf.Clamp01(SerialReader.lastSensorValue);
            int step = (int)stepNumber;
            V0 = 1.0f;
            // if (responsePattern == ResponsePattern.Velocity)
            // {
            //     // V0 = knobValue * 2f;
            //     V0 = 1.0f;
            //     v = V0;
            // }
            // else 
            // if (responsePattern == ResponsePattern.Amplitude)
            // {

            //2.v(t)=V0 + A1·sin(ωt + φ1 + Mathf.PI) + A2·sin(2ωt + φ2 + Mathf.PI)
            // 現在の速度を計算
            // Define parameter adjustment order
            // Parameter order is now defined at the top of the file
            // Change this to switch orders

            if (step == 1 || step == 3)
            {
                amplitudeToSaveData = Mathf.Lerp(A_min, A_max, knobValue);
            }
            if (step == 2 || step == 4)
            {
                amplitudeToSaveData = knobValue * 2f * Mathf.PI;
            }
            amplitudes[step] = amplitudeToSaveData;
            if (step >= 1) v = V0 + amplitudes[1] * Mathf.Sin(omega * time);// A1
            if (step >= 2) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);// φ1
            if (step >= 3) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time);// A2
            if (step >= 4) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// φ2 
                                                                                                                                                                                // }

            captureCamera0.transform.position += direction * v * Time.deltaTime;

        }
    }
    void NoLuminanceBlendSingleCameraMove()
    {
        float speed1 = CameraSpeedCompensation(1);
        Debug.Log($"Camera Speed: {speed1}");
        // 按计算得到的速度移动 captureCamera1（若需要同时移动 captureCamera2，也可加）
        Vector3 delta1 = direction * speed1 * Time.deltaTime;
        captureCamera1.transform.position += delta1;

        // 把 captureCamera1 的图像显示到 CaptureCameraLinearBlendRawImage 上（简单做法：把 captureImageTexture1 放到 Top）
        if (CaptureCameraLinearBlendRawImage != null)
        {
            CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);
            CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);
            // 确保完全显示 Top（若需要可改 alpha 或混合模式）
            CaptureCameraLinearBlendRawImage.material.SetColor("_TopColor", new Color(1f, 1f, 1f, 1f));
            CaptureCameraLinearBlendRawImage.material.SetColor("_BottomColor", new Color(1f, 1f, 1f, 0f));
        }
        // データを記録 // 记录数据
        data.Add($"{timeMs:F3}, {SerialReader.lastSensorValue}, {responsePattern}, {(int)stepNumber}, {amplitudeToSaveData}, {v}, {knobValue:F3}, {cameraSpeed:F3}");
    }
    void LuminanceMixture()
    {
        // 写真を撮る距離に達したかをチェック //
        if (Mathf.Abs(timeMs - frameNum * updateInterval * 1000) < 0.2f)
        {
            frameNum++;

            // カメラが移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置
            targetPosition = direction * cameraSpeed * updateInterval;
            captureCamera1.transform.position = captureCamera1.transform.position + targetPosition;
            captureCamera2.transform.position = captureCamera2.transform.position + targetPosition;
        }

        CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture2);       // 上层图
        CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture1);    // 下层图  

        //輝度値を計算する 
        float Image1ToNowDeltaTime = timeMs - (frameNum - 1) * updateInterval * 1000;
        float nextRatio = Image1ToNowDeltaTime / (updateInterval * 1000);

        float nextImageRatio = Math.Min(1f, Math.Max(0f, nextRatio));// x ∈ [0,1]浮動小数点の演算誤差により、減算の結果がわずかに0未満になる場合があります
        float previousImageRatio = 1.0f - nextImageRatio;

        float nonlinearPreviousImageRatio = previousImageRatio;
        float nonlinearNextImageRatio = nextImageRatio;
        knobValue = SerialReader.lastSensorValue;

        // --- 反相位补偿准备 ---
        // ② 算出当前这 1s 区间内的时间（秒）
        float tLocalSec = Image1ToNowDeltaTime / 1000f;
        ModParams p = GetParams(subject);
        if (brightnessBlendMode == BrightnessBlendMode.InverseMapLUT)
        {
            EnsureInverseLut(subject, updateInterval, p);
        }
        nonlinearNextImageRatio = BrightnessBlend.GetMixedValue(nextImageRatio, knobValue, brightnessBlendMode);
        nonlinearPreviousImageRatio = 1f - nonlinearNextImageRatio;

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
                captureCamera1.transform.rotation = Quaternion.Euler(0, 48.5f, 0);
                captureCamera0.transform.rotation = Quaternion.Euler(0, 48.5f, 0);
                captureCamera2.transform.position = new Vector3(39f, 28f, 90f);
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
            initRot0 = captureCamera0.transform.rotation;
            initRot1 = captureCamera1.transform.rotation;
            initRot2 = captureCamera2.transform.rotation;
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
        captureCamera2.transform.position += direction * captureIntervalDistance;
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
        continuousImageTransform.gameObject.SetActive(false);
        Image1Transform = canvas.transform.Find("CaptureCamera1");
        Image2Transform = canvas.transform.Find("CaptureCamera2");
        CaptureCameraLinearBlendTransform = canvas.transform.Find("CaptureCameraLinearBlend");

        // 子オブジェクトのRawImageコンポーネントを取得 // 获取子对象的 RawImage 组件
        continuousImageRawImage = continuousImageTransform.GetComponent<RawImage>();

        CaptureCameraLinearBlendRawImage = CaptureCameraLinearBlendTransform.GetComponent<RawImage>();

        CaptureCameraLinearBlendRawImage.material = new Material(Mat_GrayscaleOverBlend);

        CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);       // 上层图
        CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);    // 下层图  

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
                              "ExperimentPattern_" + experimentPattern.ToString() + "_"
                             + "ParticipantName_" + participantName.ToString() + "_"
                             + "Subject_Name_" + subject.ToString() + "_"
                             + compensationClassification.ToString() + "_"
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
                case BrightnessBlendMode.PhaseLinearized:
                    {
                        float dEffRad = GetDEffRad(knobValue);   // 用旋钮控制 d

                        float w0 = PhaseLinearizedWeight(x, dEffRad);

                        // 你原本 gamma 也用 knobValue，这会冲突（一个旋钮控制两个参数）
                        // 建议先固定 gamma，专心调 d
                        float gamma = 1f;

                        return SharpenWeight(w0, gamma);

                    }


                case BrightnessBlendMode.InverseMapLUT:
                    {
                        // x 视为 u（线性进度 0..1）
                        float u = Mathf.Clamp01(x);

                        // 逆映射 alpha
                        float a = LookupLut(u);

                        // knobValue 用来控制“补偿强度”（0=不用补偿，1=全补偿）
                        return Mathf.Lerp(u, a, knobValue);
                    }
                case BrightnessBlendMode.Dynamic:
                default:
                    return GetDynamicBlend(x, knobValue);
            }
        }
        static float WithDeadZone(float p, float dead)
        {
            p = Mathf.Clamp01(p);
            dead = Mathf.Clamp(dead, 0f, 0.49f);

            if (p <= dead) return 0f;
            if (p >= 1f - dead) return 1f;
            return (p - dead) / (1f - 2f * dead);
        }

        static float SharpenWeight(float w, float gamma)
        {
            w = Mathf.Clamp01(w);
            gamma = Mathf.Max(1f, gamma);

            float a = Mathf.Pow(w, gamma);
            float b = Mathf.Pow(1f - w, gamma);
            float denom = a + b;
            if (denom < 1e-6f) return w;
            return a / denom;
        }
        static float PhaseLinearizedWeight(float p, float dEff)
        {
            p = Mathf.Clamp01(p);
            if (p <= 0f) return 0f;
            if (p >= 1f) return 1f;

            // 目标：让相位 k = dEff * p 线性变化
            float k = dEff * p;

            float a = Mathf.Sin(k);
            float b = Mathf.Sin(dEff - k);
            float denom = a + b;

            // 极端情况下避免除0
            if (Mathf.Abs(denom) < 1e-6f) return p;

            float w = a / denom;
            return Mathf.Clamp01(w);
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

        // ============================================================
        // ✅ 新增：带“个体补偿”的混合函数
        // ============================================================

        /// <summary>
        /// 加入个体化反相位补偿 (A1, φ1, A2, φ2)
        /// </summary>
        public static float BrightnessCompensation(
            float linearRatio,
            float tLocalSec,
            float updateInterval,
            ModParams p,
            CompensationClassification compensationClassification = CompensationClassification.V0_A1A2,
            ExperimentPattern experimentPattern = ExperimentPattern.LuminanceMinusCompensate
        )
        {
            const float TAU = 6.283185307179586f; // 2π

            // 建议先用更温和的强度，比如 0.05~0.1
            float compensationStrength = 0.1f;

            float u = Mathf.Clamp01(tLocalSec / Mathf.Max(updateInterval, 1e-3f));

            float s1 = Mathf.Sin(TAU * u + p.PHI1 + Mathf.PI);
            float s2 = Mathf.Sin(2f * TAU * u + p.PHI2 + Mathf.PI);

            float m = 0f;
            switch (compensationClassification)
            {
                case CompensationClassification.A1:
                    m = p.A1 * s1;
                    break;
                case CompensationClassification.A2:
                    m = p.A2 * s2;
                    break;
                case CompensationClassification.A1A2:
                    m = p.A1 * s1 + p.A2 * s2;
                    break;
            }

            float m_norm = m / Mathf.Max(Mathf.Abs(p.V0), 1e-3f);

            // 新增：在 0~1 上加一个平滑窗，使补偿在端点自动收敛到 0
            // 简单版：w(u) = u*(1-u)，在 0 和 1 为 0，中间最大 0.25，很柔和
            // 0〜1 の区間で滑らかに効くウィンドウを掛けて，端点では補償量が 0 になるようにする
            // シンプルな形：w(u) = u * (1 - u)（u=0,1 で 0，中点 u=0.5 で最大 0.25 のなだらかな山形）
            float window = 4f * u * (1f - u);          // 0→中间峰→0
                                                       // 可以按需要放大一点：
                                                       // float window = 4f * u * (1f - u);  // 0→中间1→0

            // 根据实验模式反向
            if (experimentPattern == ExperimentPattern.LuminancePlusCompensate)
                compensationStrength = -compensationStrength;

            // ★ 把窗函数乘进去，让补偿主作用在区间中部，端点变小
            float rawDelta = -compensationStrength * window * m_norm;

            // clamp 防止 alpha 溢出
            float maxDown = linearRatio;
            float maxUp = 1f - linearRatio;
            float clampedDelta = Mathf.Clamp(rawDelta, -maxDown, maxUp);

            float compensated = linearRatio + clampedDelta;
            return Mathf.Clamp01(compensated);
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
                return new ModParams(1.129f, 0.815f, 3.462f, 0.860f, 5.854f);
        }
        return new ModParams(0.992f, 0.540f, 1.849f, -0.528f, 1.462f);
    }

    public float CameraSpeedCompensation(int classification)
    {
        ModParams p = GetParams(subject);

        float s1 = Mathf.Sin(omega * time + p.PHI1 + Mathf.PI);
        float s2 = Mathf.Sin(2f * omega * time + p.PHI2 + Mathf.PI);

        // 确保 p, s1, s2 是类的成员；按你的公式直接返回
        float speed = 0f;
        switch (compensationClassification)
        {
            case CompensationClassification.V0:
                speed = p.V0;
                break;
            case CompensationClassification.V0_A1:
                speed = p.V0 + p.A1 * s1;
                break;
            case CompensationClassification.V0_A2:
                speed = p.V0 + p.A2 * s2;
                break;
            case CompensationClassification.V0_A1A2:
                speed = p.V0 + p.A1 * s1 + p.A2 * s2;
                break;
            case CompensationClassification.A1:
                speed = p.A1 * s1;
                break;
            case CompensationClassification.A2:
                speed = p.A2 * s2;
                break;
            case CompensationClassification.A1A2:
                speed = p.A1 * s1 + p.A2 * s2;
                break;
        }

        return classification == 1 ? speed : -speed;
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

    // 建议 256 或 512
    const int LUT_M = 256;
    const int SAMPLE_N = 2000;

    static float[] _alphaLut = null;
    static SubjectOption _lutForSubject;
    static float _lutForInterval = -1f;

    public static void EnsureInverseLut(SubjectOption subject, float updateInterval, ModParams p)
    {
        if (_alphaLut != null && _lutForSubject == subject && Mathf.Approximately(_lutForInterval, updateInterval))
            return;

        _alphaLut = BuildInverseAlphaLut(updateInterval, p, SAMPLE_N, LUT_M);
        _lutForSubject = subject;
        _lutForInterval = updateInterval;
    }

    static float[] BuildInverseAlphaLut(float T, ModParams p, int N, int M)
    {
        // 1) sample t -> v(t)
        float[] S = new float[N];     // normalized cumulative progress
        float dt = T / (N - 1);

        float omega = 2f * Mathf.PI / Mathf.Max(T, 1e-6f);

        float vPrev = EvalV(0f, omega, p);
        vPrev = Mathf.Max(vPrev, 1e-4f);

        float cum = 0f;
        S[0] = 0f;

        for (int i = 1; i < N; i++)
        {
            float t = i * dt;
            float v = EvalV(t, omega, p);
            v = Mathf.Max(v, 1e-4f); // 保证单调（很重要）

            // trapezoid integral
            cum += 0.5f * (vPrev + v) * dt;
            S[i] = cum;

            vPrev = v;
        }

        float total = S[N - 1];
        if (total <= 1e-8f) total = 1e-8f;
        for (int i = 0; i < N; i++) S[i] /= total; // normalize to [0,1]

        // 2) invert S(t): for each u in [0,1], find t s.t. S(t)=u
        float[] lut = new float[M];
        int k = 0;

        for (int j = 0; j < M; j++)
        {
            float u = (float)j / (M - 1);

            while (k < N - 2 && S[k + 1] < u) k++;

            float s0 = S[k];
            float s1 = S[k + 1];
            float t0 = (float)k / (N - 1);       // normalized time
            float t1 = (float)(k + 1) / (N - 1);

            float a;
            if (s1 <= s0 + 1e-8f) a = t0;
            else
            {
                float w = Mathf.Clamp01((u - s0) / (s1 - s0));
                a = Mathf.Lerp(t0, t1, w);
            }

            lut[j] = a; // already normalized to [0,1]
        }

        return lut;
    }

    static float EvalV(float t, float omega, ModParams p)
    {
        // 你已有 V0,A1,PHI1,A2,PHI2；可继续加更多项
        return p.V0
               + p.A1 * Mathf.Sin(omega * t + p.PHI1)
               + p.A2 * Mathf.Sin(2f * omega * t + p.PHI2);
    }

    // LUT lookup (linear interpolation)
    static float LookupLut(float u)
    {
        if (_alphaLut == null) return u;
        u = Mathf.Clamp01(u);
        float x = u * (LUT_M - 1);
        int i = Mathf.FloorToInt(x);
        int i1 = Mathf.Min(i + 1, LUT_M - 1);
        float w = x - i;
        return Mathf.Lerp(_alphaLut[i], _alphaLut[i1], w);
    }
    // 旋钮 knobValue 期望是 [0,1]
    private static float GetDEffRad(float knobValue)
    {
        // 你之前试的范围：0.10π ~ 0.90π（可改）
        float dMin = 0.10f * Mathf.PI;
        float dMax = 0.90f * Mathf.PI;

        // 线性映射（如果你想让低端更细腻，可用 SmoothStep）
        return Mathf.Lerp(dMin, dMax, Mathf.Clamp01(knobValue));
    }

    private void SetCaptureViewsActive(bool active)
    {
        if (continuousImageTransform != null) continuousImageTransform.gameObject.SetActive(active);
        if (CaptureCameraLinearBlendTransform != null) CaptureCameraLinearBlendTransform.gameObject.SetActive(active);
    }
    private IEnumerator GrayBreakRoutine()
    {
        isInGray = true;

        ResetCamerasAndBlendState();

        // OFF: 隐藏两个显示对象（Transform -> gameObject）
        // if (continuousImageTransform != null)
        //     continuousImageTransform.gameObject.SetActive(false);

        if (CaptureCameraLinearBlendTransform != null)
            CaptureCameraLinearBlendTransform.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(0.2f);

        // ON
        // if (continuousImageTransform != null)
        //     continuousImageTransform.gameObject.SetActive(true);

        if (CaptureCameraLinearBlendTransform != null)
            CaptureCameraLinearBlendTransform.gameObject.SetActive(true);

        // 如果你要开始下一段 25s，记得重置计时
        fixedUpdateCounter = 0;  // 或者 timeMs=0，取决于你怎么计时
        timeMs = 0;

        isInGray = false;
    }



}



