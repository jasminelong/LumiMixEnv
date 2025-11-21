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
        timeMs = (Time.time - startTime) * 1000;
        Continuous();
        if (experimentPattern == ExperimentPattern.NoLuminanceBlendSingleCameraMove)
        {
            NoLuminanceBlendSingleCameraMove();
        }
        else
        {
            LuminanceMixture();
        }
        if (Application.isPlaying)
        {
            velocityHistory.Add(v);           // 新增
        }
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

            if (responsePattern == ResponsePattern.Velocity)
            {
                V0 = knobValue * 2f;
                v = V0;
            }
            else if (responsePattern == ResponsePattern.Amplitude)
            {

                //2.v(t)=V0 + A1·sin(ωt + φ1 + Mathf.PI) + A2·sin(2ωt + φ2 + Mathf.PI)
                // 現在の速度を計算
                // Define parameter adjustment order
                // Parameter order is now defined at the top of the file
                // Change this to switch orders

                switch (paramOrder)
                {
                    case ParameterOrder.V0_A1_PHI1_A2_PHI2: // Original orderV0,  A1, φ1, A2, φ2 
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
                        break;
                    case ParameterOrder.V0_A1_PHI1_A2_PHI2_A1_PHI1_A2_PHI2:
                        if (step == 1 || step == 3 || step == 5 || step == 7)
                        {
                            amplitudeToSaveData = Mathf.Lerp(A_min, A_max, knobValue);
                            if (step == 1 || step == 5)
                            {
                                amplitudes[1] = amplitudeToSaveData;
                            }
                            else if (step == 3 || step == 7)
                            {
                                amplitudes[3] = amplitudeToSaveData;
                            }
                        }
                        if (step == 2 || step == 4 || step == 6 || step == 8)
                        {
                            amplitudeToSaveData = knobValue * 2f * Mathf.PI;
                            if (step == 2 || step == 6)
                            {
                                amplitudes[2] = amplitudeToSaveData;
                            }
                            else if (step == 4 || step == 8)
                            {
                                amplitudes[4] = amplitudeToSaveData;
                            }
                        }
                        if (step >= 1) v = V0 + amplitudes[1] * Mathf.Sin(omega * time);// A1
                        if (step >= 2) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);// φ1
                        if (step >= 3) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time);// A2
                        if (step >= 4) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// φ2 
                        if (step >= 5) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// A1
                        if (step >= 6) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// φ1 
                        if (step >= 7) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// A2
                        if (step >= 8) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// φ2 
                        break;
                    case ParameterOrder.V0_PHI1_A1_PHI2_A2: // V0, φ1, A1, φ2, A2
                        if (step == 1 || step == 3)
                        {
                            amplitudeToSaveData = knobValue * 2f * Mathf.PI;
                            amplitudes[step + 1] = amplitudeToSaveData;
                        }
                        if (step == 2 || step == 4)
                        {
                            amplitudeToSaveData = Mathf.Lerp(A_min, A_max, knobValue);
                            amplitudes[step - 1] = amplitudeToSaveData;
                        }
                        if (step >= 1) v = V0 + V0 / 2 * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);// φ1
                        if (step >= 2) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);// A1
                        if (step >= 3) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[1] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// φ2
                        if (step >= 4) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// A2
                        break;

                    case ParameterOrder.V0_PHI1_A1_PHI1_PHI2_A2_PHI2: // V0, φ1, A1, φ1, φ2, A2, φ2
                        if (step == 1 || step == 3 || step == 4 || step == 6)
                        {
                            amplitudeToSaveData = knobValue * 2f * Mathf.PI;
                            if (step == 1)
                            {
                                amplitudes[2] = amplitudeToSaveData;
                            }
                            else if (step == 3)
                            {
                                amplitudes[4] = amplitudeToSaveData;
                            }
                            else if (step == 4)
                            {
                                amplitudes[2] = amplitudeToSaveData;
                            }
                            else if (step == 6)
                            {
                                amplitudes[4] = amplitudeToSaveData;
                            }
                        }
                        if (step == 2 || step == 5)
                        {
                            amplitudeToSaveData = Mathf.Lerp(A_min, A_max, knobValue);
                            if (step == 2)
                            {
                                amplitudes[1] = amplitudeToSaveData;
                            }
                            else if (step == 5)
                            {
                                amplitudes[3] = amplitudeToSaveData;
                            }
                        }
                        if (step >= 1) v = V0 + V0 / 2 * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);// φ1
                        if (step >= 2) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);// A1
                        if (step >= 3) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI);// φ1
                        if (step >= 4) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[1] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// φ2
                        if (step >= 5) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// A2
                        if (step >= 6) v = V0 + amplitudes[1] * Mathf.Sin(omega * time + amplitudes[2] + Mathf.PI) + amplitudes[3] * Mathf.Sin(2 * omega * time + amplitudes[4] + Mathf.PI);// φ2
                        break;
                }
            }
            captureCamera0.transform.position += direction * v * Time.deltaTime;

        }
    }
    void NoLuminanceBlendSingleCameraMove()
    {
        float speed1 = CameraSpeedCompensation(1);

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
        // 写真を撮る距離に達したかをチェック // 检查是否到了拍照的距离
        if (experimentPattern == ExperimentPattern.CameraJumpMovePlusCompensate || experimentPattern == ExperimentPattern.CameraJumpMoveMinusCompensate)
        {
            // 仅负的正弦调制项：
            cameraSpeedReverse = experimentPattern == ExperimentPattern.CameraJumpMovePlusCompensate ? CameraSpeedCompensation(1) : CameraSpeedCompensation(0);

            Vector3 delta = direction * cameraSpeedReverse * Time.deltaTime;
            captureCamera1.transform.position += delta;
            captureCamera2.transform.position += delta;
        }
        if (Mathf.Abs(timeMs - frameNum * updateInterval * 1000) < 0.2f)
        {
            frameNum++;

            // カメラが移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置
            targetPosition = direction * cameraSpeed * updateInterval;
            captureCamera1.transform.position = captureCamera1.transform.position + targetPosition;
            captureCamera2.transform.position = captureCamera2.transform.position + targetPosition;
        }

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

        //輝度値を計算する 
        float Image1ToNowDeltaTime = timeMs - (frameNum - 1) * updateInterval * 1000;
        float nextRatio = Image1ToNowDeltaTime / (updateInterval * 1000);

        float nextImageRatio = Math.Min(1f, Math.Max(0f, nextRatio));// x ∈ [0,1]浮動小数点の演算誤差により、減算の結果がわずかに0未満になる場合があります
        float previousImageRatio = 1.0f - nextImageRatio;

        float nonlinearPreviousImageRatio = previousImageRatio;
        float nonlinearNextImageRatio = nextImageRatio;
        knobValue = SerialReader.lastSensorValue;

        if (experimentPattern == ExperimentPattern.LuminanceMinusCompensate || experimentPattern == ExperimentPattern.LuminancePlusCompensate)
        {
            // --- 反相位补偿准备 ---
            // ② 算出当前这 1s 区间内的时间（秒）
            float tLocalSec = Image1ToNowDeltaTime / 1000f;
            ModParams p = GetParams(subject);
            nonlinearPreviousImageRatio = BrightnessBlend.BrightnessCompensation(previousImageRatio, tLocalSec, updateInterval, p, compensationClassification, experimentPattern);
            nonlinearNextImageRatio = BrightnessBlend.BrightnessCompensation(nextImageRatio, tLocalSec, updateInterval, p, compensationClassification, experimentPattern);
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

            // ★ 新增：在 0~1 上加一个平滑窗，使补偿在端点自动收敛到 0
            // 简单版：w(u) = u*(1-u)，在 0 和 1 为 0，中间最大 0.25，很柔和
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
        float beta = 0.5f;
        return classification == 1 ? beta * speed : -beta * speed;
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




}



