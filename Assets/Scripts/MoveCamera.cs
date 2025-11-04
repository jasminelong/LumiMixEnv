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

        data.Add("FrondFrameNum, FrondFrameLuminance, BackFrameNum, BackFrameLuminance, Time, Knob, ResponsePattern, StepNumber, Amplitude, Velocity, FunctionRatio, CameraSpeed");
        experimentalCondition = "Fps" + fps.ToString() + "_"
                             + "CameraSpeed" + cameraSpeed.ToString() + "_"
                             + "ExperimentPattern_" + experimentPattern.ToString() + "_"
                             + "ParticipantName_" + participantName.ToString() + "_"
                             + "subjectName_" + subject.ToString() + "_"
                             + "TrialNumber_" + trialNumber.ToString();
        if (devMode == DevMode.Test)
        {
            experimentalCondition += "_" + "Test";
        }
    }
    void Update()
    {
        /// マウス入力は1フレームのみ検出されるため、Update() で処理する必要があります。
        // マウスの左ボタンが押されたときの処理 // 处理鼠标左键按下时的操作

        if (!mouseClicked && Input.GetMouseButtonDown(0))
        {
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
                    if (experimentPattern == ExperimentPattern.Phase || experimentPattern == ExperimentPattern.CameraMove || experimentPattern == ExperimentPattern.CameraJumpMove)
                    {
                        nextStepButtonTextComponent.text = "Entering the next trial";
                    }
                    else
                    {
                        nextStepButtonTextComponent.text = "Next Step";
                    }
                    break;

            }
        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        timeMs = (Time.time - startTime) * 1000;
        Continuous();
        LuminanceMixture();
    }

    void OnNextStep()
    {
        mouseClicked = false;
        Time.timeScale = 1f;
        currentStep++;
        responsePattern = ResponsePattern.Amplitude;
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
                if (experimentPattern == ExperimentPattern.Phase || experimentPattern == ExperimentPattern.CameraMove || experimentPattern == ExperimentPattern.CameraJumpMove)
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
                    //stepNumber = StepNumber.Option5;
                }

                break;
        }
        nextStepButton.gameObject.SetActive(false);
    }

    void Continuous()
    {

        continuousImageRawImage.enabled = true;
        // カメラが移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置right 
        //Vector3 targetPosition = captureCamera0.transform.position + direction * cameraSpeed * Time.fixedDeltaTime;

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

            if (responsePattern == ResponsePattern.Velocity)
            {
                V0 = knobValue * 2f;
                v = V0;
            }
            else if (responsePattern == ResponsePattern.Amplitude)
            {

                //2.v(t)=V0 + A1·sin(ωt + φ1 + Mathf.PI) + A2·sin(2ωt + φ2 + Mathf.PI)
                // 現在の速度を計算
                if (step == 1 || step == 3)
                {
                    // amplitudeToSaveData = A_min + knobValue * (A_max - A_min);
                    amplitudeToSaveData = Mathf.Lerp(A_min, A_max, knobValue);
                }
                if (step == 2 || step == 4)
                {
                    amplitudeToSaveData = knobValue * 2f * Mathf.PI; ;  // 0 … 2π
                }
                amplitudes[step] = amplitudeToSaveData;
                if (step >= 1) v = V0 + amplitudes[1] * Mathf.Sin(omega * time);//step1,amplitudes[1] 
                if (step >= 2) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);//step2,amplitudes[2]
                if (step >= 3) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2]) + amplitudes[3] * Mathf.Sin(2 * omega * time);//step3,amplitudes[3]
                if (step >= 4) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2]) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);//step4,amplitudes[4] 
            }
            captureCamera0.transform.position += direction * v * Time.deltaTime;
        }
    }

    void LuminanceMixture()
    {
        // 写真を撮る距離に達したかをチェック // 检查是否到了拍照的距离
        if (experimentPattern == ExperimentPattern.CameraMove || experimentPattern == ExperimentPattern.CameraJumpMove)
        {
            // 仅负的正弦调制项：
            // cameraSpeedReverse = experimentPattern == ExperimentPattern.CameraMove ? GetRealtimeCameraSpeedReverse():GetRealtimeCameraJumpSpeedReverse();
            cameraSpeedReverse = GetRealtimeCameraJumpSpeedReverse();
            // cameraSpeedReverse = GetRealtimeCameraSpeed();
            
            Vector3 delta = direction * cameraSpeedReverse * Time.deltaTime;
            captureCamera1.transform.position += delta;
            captureCamera2.transform.position += delta;
        }
        if (Mathf.Abs(timeMs - frameNum * updateInterval * 1000) < 0.2f)
        {
            frameNum++;
            // if (experimentPattern != ExperimentPattern.CameraMove)
            // {
                // カメラが移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置
            targetPosition = direction * cameraSpeed * updateInterval;

            captureCamera1.transform.position = captureCamera1.transform.position + targetPosition;
            captureCamera2.transform.position = captureCamera2.transform.position + targetPosition;
            Debug.Log("Camera Position:  " + captureCamera1.transform.position);
            // }

        }

        // if (experimentPattern == ExperimentPattern.CameraMove)
        // {
        //     CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);       // 上层图
        //     CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);    // 下层图  
        // }
        // else
        // {
            if (frameNum % 2 == 0)
            {
                CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture2);       // 上层图
                CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture1);    // 下层图  
            }
            else
            {
                CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);       // 上层图
                CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);    // 下层图  
            }
        // }

        //輝度値を計算する 
        float Image1ToNowDeltaTime = timeMs - (frameNum - 1) * updateInterval * 1000;
        float nextRatio = Image1ToNowDeltaTime / (updateInterval * 1000);

        float nextImageRatio = Math.Min(1f, Math.Max(0f, nextRatio));// x ∈ [0,1]浮動小数点の演算誤差により、減算の結果がわずかに0未満になる場合があります
        float previousImageRatio = 1.0f - nextImageRatio;

        float nonlinearPreviousImageRatio = previousImageRatio;
        float nonlinearNextImageRatio = nextImageRatio;
        knobValue = SerialReader.lastSensorValue;
        //knobValue = 0.583f;//0.517, 0.713, 0.581, 0.583, 0.684, 1.0 ONO-C
        // knobValue = 0.218f;//0.0 0.492 0.471 0.231 0.178 0.205 LL-E
        // knobValue = 0.316f;//0.163 0.206 0.555 0.336 0.295 0.712 HOU-D
        // knobValue = 0.734f;//0.817 0.651 0.551 0.84 0.582 0.841 OMU-B
        // knobValue = 0.615f;//0.683 0.616 0.785 0.583 0.613 0.581 YAMA-A
        
        if (experimentPattern == ExperimentPattern.BrightnessCompensation)
        {
            nonlinearPreviousImageRatio = ApplyInverseCompLogit(previousImageRatio, timeMs);
            nonlinearNextImageRatio = ApplyInverseCompLogit(nextImageRatio, timeMs);
        }
        else
        {
            nonlinearPreviousImageRatio = BrightnessBlend.GetMixedValue(previousImageRatio, knobValue, brightnessBlendMode);
            nonlinearNextImageRatio = BrightnessBlend.GetMixedValue(nextImageRatio, knobValue, brightnessBlendMode);
        }


        if (frameNum % 2 == 0)
        {
            CaptureCameraLinearBlendRawImage.material.SetColor("_TopColor", new Color(1, 1, 1, nonlinearNextImageRatio)); // 透明度
            CaptureCameraLinearBlendRawImage.material.SetColor("_BottomColor", new Color(1, 1, 1, 1.0f));
            alphaHistory.Add(nonlinearPreviousImageRatio);
        }
        else
        {
            CaptureCameraLinearBlendRawImage.material.SetColor("_TopColor", new Color(1, 1, 1, nonlinearPreviousImageRatio)); // 透明度
            CaptureCameraLinearBlendRawImage.material.SetColor("_BottomColor", new Color(1, 1, 1, 1.0f));
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
        }

        // 上限を超えた場合は最古データから削除 //如果依然过多，按最早移除
        if (timeStamps.Count > maxSamples)
        {
            timeStamps.RemoveAt(0);
            alphaHistory.RemoveAt(0);
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
                captureCamera2.transform.position = new Vector3(4f, 28f, 130f);
                captureCamera1.transform.position = new Vector3(4f, 28f, 130f);
                captureCamera0.transform.position = new Vector3(4f, 28f, 130f);
                break;
        }
    }

    void GetRawImage()
    {
        // Canvas内で指定された名前の子オブジェクトを検索 // 在 Canvas 中查找指定名称的子对象
        canvas = GameObject.Find("Canvas");
        continuousImageTransform = canvas.transform.Find("CaptureCamera0");
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

    void UpdateProgress()
    {
        string json = File.ReadAllText(savePath);
        ExperimentData data = JsonUtility.FromJson<ExperimentData>(json);
        switch (currentProgress)
        {
            case "exp1_intro_test":
                data.progress.exp1_intro_test++;
                break;
            case "exp1_trials":
                data.progress.exp1_trials++;
                break;
            case "exp2_intro_test":
                data.progress.exp2_intro_test++;
                break;
            case "exp2_trials":
                data.progress.exp2_trials++;
                break;
        }

        // 保存
        string updatedJson = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, updatedJson);
    }

    void OnDestroy()
    {
        // 現在の日付を取得 // 获取当前日期
        string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // ファイル名を構築 // 构建文件名
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
        public float A1, PHI1, A2, PHI2;
        public ModParams(float a1, float phi1, float a2, float phi2)
        { A1 = a1; PHI1 = phi1; A2 = a2; PHI2 = phi2; }
    }
    private ModParams GetParams(SubjectOption opt)
    {
        switch (opt)
        {
            case SubjectOption.YAMA_A: //  目前用“平均值”占位 —— 拿到 YAMA 的真实参数后改这里
                return new ModParams(0.540f, 1.849f, -0.528f, 1.462f);

            case SubjectOption.OMU_B:
                return new ModParams(0.522f, 2.528f, -0.223f, 3.525f);

            case SubjectOption.ONO_C:
                return new ModParams(0.632f, 3.663f, 0.461f, 5.123f);

            case SubjectOption.HOU_D:
                return new ModParams(0.275f, 3.031f, 0.920f, 5.982f);

            case SubjectOption.LL_E:
                return new ModParams(-0.278f, 1.849f, -0.292f, 3.728f);

            case SubjectOption.KK_F:
                return new ModParams(0.815f, 3.462f, 0.860f, 5.854f);
        }
        return new ModParams(0.540f, 1.849f, -0.528f, 1.462f);
    }
    public float GetRealtimeCameraSpeedReverse()
    {
        ModParams p = GetParams(subject);

        float period = 1.0f;   // 或者 2.0f，取决于你希望完整重复周期的长度
        float t = Time.time % period;   //  归一化时间，确保波形周期性重复
        float s1 = Mathf.Sin(omega * t + p.PHI1 + Mathf.PI);
        float s2 = Mathf.Sin(2f * omega * t + p.PHI2 + Mathf.PI);

        // 确保 p, s1, s2 是类的成员；按你的公式直接返回
        return cameraSpeed - (p.A1 * s1 + p.A2 * s2);
    }
    public float GetRealtimeCameraJumpSpeedReverse()
    {
        ModParams p = GetParams(subject);

        float period = 1.0f;   // 或者 2.0f，取决于你希望完整重复周期的长度
        float t = Time.time % period;   //  归一化时间，确保波形周期性重复
        float s1 = Mathf.Sin(omega * t + p.PHI1 + Mathf.PI);
        float s2 = Mathf.Sin(2f * omega * t + p.PHI2 + Mathf.PI);

        // 确保 p, s1, s2 是类的成员；按你的公式直接返回
        return - (p.A1 * s1 + p.A2 * s2);
    }
    public float GetRealtimeCameraSpeed()
    {
        ModParams p = GetParams(subject);

        float period = 1.0f;   // 或者 2.0f，取决于你希望完整重复周期的长度
        float t = Time.time % period;   //  归一化时间，确保波形周期性重复
        float s1 = Mathf.Sin(omega * t + p.PHI1 + Mathf.PI);
        float s2 = Mathf.Sin(2f * omega * t + p.PHI2 + Mathf.PI);

        // 确保 p, s1, s2 是类的成员；按你的公式直接返回
        return cameraSpeed + (p.A1 * s1 + p.A2 * s2);
    }
/// <summary>
/// 在 logit 域做前馈补偿：alpha' = σ( logit(alpha_base) + Δz(t) )
/// baseAlpha 必须在 (0,1)；若恰为0/1，先做极小量偏置。
/// 返回值严格∈(0,1)，无硬裁剪失真。
/// </summary>
public float ApplyInverseCompLogit(float baseAlpha, float timeMs)
    {
    ModParams p = GetParams(subject);
    // 1) 保障 baseAlpha 处在可逆域
    const float EPS = 1e-5f;
    float a = Mathf.Clamp(baseAlpha, EPS, 1f - EPS);

    // 2) 当前时间与 dt
    float t = timeMs * 0.001f;
    float dt;
    if (_tPrev < 0) { dt = Mathf.Max(1e-4f, Time.deltaTime); }
    else            { dt = Mathf.Max(1e-4f, t - _tPrev); }
    _tPrev = t;

    // 3) 目标在 α 域的小信号补偿：Δα*(t) = -(A1/η1)sin(...) - (A2/η2)sin(...)
    float dAlphaTarget =
        -(p.A1 / Mathf.Max(1e-6f, eta1)) * Mathf.Sin(omega * t + p.PHI1)
        -(p.A2 / Mathf.Max(1e-6f, eta2)) * Mathf.Sin(2f * omega * t + p.PHI2);

    dAlphaTarget *= compScale;

    // 4) 由 α = σ(z)，有 dα/dz = α(1-α) ⇒ 期望 Δz ≈ Δα* / (α(1-α))
    float gain = a * (1f - a) + 1e-6f;
    float dZtarget = dAlphaTarget / gain;

    // 5) 平滑 + 限速（在 z 域做）
    float k = 1f - Mathf.Exp(-smooth * dt);                    // 指数平滑
    float zCorrSmoothed = Mathf.Lerp(_zCorrPrev, dZtarget, k); // 目标到当前
    float maxStep = maxDeltaZPerSec * dt;                      // 每帧最大步长
    float zCorr = Mathf.Clamp(zCorrSmoothed, _zCorrPrev - maxStep, _zCorrPrev + maxStep);
    _zCorrPrev = zCorr;

    // 6) 叠加到 logit，再映回 α
    float zBase  = Logit(a);
    float aPrime = Sigmoid(zBase + zCorr);
    return aPrime; // ∈(0,1)
}

}

