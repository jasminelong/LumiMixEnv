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


public class MoveCamera : MonoBehaviour
{
    public enum DirectionPattern
    {
        right,
        forward
    }
    public enum ResponsePattern
    {
        Velocity,
        Amplitude,
    }
    public enum ExperimentPattern
    {
        Phase,
        FunctionMix,
        Fourier,
    }
    public enum StepNumber
    {
        Option0 = 0,
        Option1 = 1,
        Option2 = 2,
        Option3 = 3,
        Option4 = 4
    }
    public enum BrightnessBlendMode
    {
        Dynamic,      // Cosine → Linear → Acos → Cosine
        CosineOnly,
        LinearOnly,
        AcosOnly
    }
    public enum DevMode
    {
        Test,         // 测试模式
        FunctionRation,    // 函数模式
        Normal,       // 正常模式

    }

    public enum CurveType  // 选择曲线
    {
        Linear,
        Cosine,
        Cubic,
        Quintic,
        Acos     // 老师原来的 acos 曲线
    }
    [SerializeField] DevMode devMode = DevMode.Normal;
    [SerializeField] BrightnessBlendMode brightnessBlendMode = BrightnessBlendMode.Dynamic;
    [SerializeField] CurveType curveType = CurveType.Cosine;
    public Camera captureCamera0; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
    public Camera captureCamera1; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
    public Camera captureCamera2; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
    public GameObject canvas;
    public Texture captureImageTexture1; // 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    public Texture captureImageTexture2; // 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    public Button nextStepButton;
    public float cameraSpeed = 1f; // カメラが円柱の軸に沿って移動する速度 (m/s) // 摄像机沿圆柱轴线移动的速度，m/s


    public float captureIntervalDistance; // 撮影間隔の距離 (m) // 拍摄间隔距离，m

    private Transform continuousImageTransform;
    private Transform Image1Transform;
    private Transform Image2Transform;
    private Transform CaptureCameraLinearBlendTransform;
    private RawImage continuousImageRawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    private RawImage Image1RawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    private RawImage Image2RawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    private RawImage CaptureCameraLinearBlendRawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件

    public float updateInterval; // 更新間隔 (秒) // 更新间隔，单位秒

    // データ保存用のフィールド // 保存数据用的字段
    // 現在のフレーム数と時間を取得 // 获取当前帧数和时间
    public int frameNum = 0;
    public string participantName;
    private string experimentalCondition;
    private TextMeshProUGUI nextStepButtonTextComponent;

    public float fps = 1f; // 他のfps // 其他的fps
    public DirectionPattern directionPattern; // イメージの提示パターン // 图像提示的模式

    private List<string> data = new List<string>();
    private float startTime;
    private string folderName = "BrightnessFunctionMixAndPhaseData"; // サブフォルダ名 // 子文件夹名称
    private float timeMs; // 現在までの経過時間 // 运行到现在的时间
    private Vector3 direction;

    private Vector3 targetPosition;      // FixedUpdate 的目标位置
    private Quaternion rightMoveRotation = Quaternion.Euler(0, 48.5f, 0);
    private Quaternion forwardMoveRotation = Quaternion.Euler(0, 146.8f, 0);
    private int currentStep = 0;
    public float v;
    public float[] amplitudes = new float[10];
    public SerialReader SerialReader;
    // Start is called before the first frame update

    // 数据保留的时长（例如，只保留最近10秒的数据） 輝度値の変化の表示
    /*        public float recordDuration = 1f;
    public AnimationCurve recordedCurve1 = new AnimationCurve();
    public AnimationCurve recordedCurve2 = new AnimationCurve();*/

    public ResponsePattern responsePattern;

    [Header("🔧記録するデータ")]
    public StepNumber stepNumber = StepNumber.Option0; // 現在のステップ番号 // 当前步骤编号
    public ExperimentPattern experimentPattern;
    public int trialNumber = 1;

    //记录Image1RawImage的透明度使用的相关变量
    [Space(20)]
    [Header("🔧 Image1RawImageの輝度値の記録")]
    [Range(-10, 10)]
    public float knobValue = 0f; // 非线性度合成比 // 非线性度合成比
    public int maxSamples = 500;
    public float maxDuration = 5f; // 显示最近5秒
    // 存时间戳（秒）和对应的 alpha
    [HideInInspector] public List<float> timeStamps = new List<float>();
    [HideInInspector] public List<float> alphaHistory = new List<float>();


    //速度を調整
    [Space(20)]
    [Header("🔧 基本パラメータ（調整可能）")]
    [Range(0.1f, 10f)]
    public float omega = 2 * Mathf.PI; // 角速度（頻度）

    [Range(-1f, 5f)]
    public float A_min = -2f;

    [Range(0f, 5f)]
    public float A_max = 2.0f;
    public float time = 0f;

    [Range(0f, 5f)]
    public float V0 = 1.0f;  // 基本速度

    private bool mouseClicked = false;
    private float amplitudeToSaveData;

    //------------Speed ​​function start-------------
    public enum SpeedFunctionType
    {
        Linear,
        EaseInOut,    // (1−cosπx)/2
        Triangle,    // 1−|m−1|
        Arccos       // 分段 arccos 波形
    }
    public SpeedFunctionType functionType = SpeedFunctionType.Linear;
    [Range(0f, 10f)]
    public float SpeedFunctionDistance = 5f;

    public Vector3 SpeedFunctionleftLimit = Vector3.zero;

    [Range(0f, 5f)]
    public float SpeedFunctionFrequency = 1f;

    [Range(0f, 2f)]
    public float SpeedFunctionAmplitude = 1f;

    [Range(-1f, 1f)]
    public float SpeedFunctionOffset = 0f;
    private float SpeedFunctionTime = 0f;
    //-------------Speed ​​function end------------
    Material _mat;
    private Material matInstance;
    public Material Mat_GrayscaleOverBlend;
    private Texture2D blackTexture;
    private Texture2D whiteTexture;
    private int trailsCount = 0; // 试次总数
    private int currentIndex = 0; // 当前试次索引
    private string savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");
    private bool isEnd = false; // 是否结束实验
    private string currentProgress; // 
    // 对数刻度
    void Start()
    {
        /* //test
        // 创建纯黑纹理
        blackTexture = new Texture2D(1, 1);
        blackTexture.SetPixel(0, 0, Color.black);
        blackTexture.Apply();

        // 创建纯白纹理
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply(); */

        // 垂直同期を無効にする // 关闭垂直同步
        QualitySettings.vSyncCount = 0;
        // 目標フレームレートを60フレーム/秒に設定 // 设置目标帧率为60帧每秒
        Time.fixedDeltaTime = 1.0f / 60.0f;



        // captureCamera.enabled = false; // 初期状態でキャプチャカメラを無効にする // 初始化时禁用捕获摄像机

        updateInterval = 1 / fps; // 各フレームの表示間隔時間を計算 // 计算每一帧显示的间隔时间
        captureIntervalDistance = cameraSpeed / fps; // 各フレームの間隔距離を計算 // 计算每帧之间的间隔距离

        GetRawImage();
        InitialSetup();

        continuousImageRawImage.enabled = true;
        Image1RawImage.enabled = true;
        Image2RawImage.enabled = true;
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
                             + "TrialNumber_" + trialNumber.ToString();
        if (experimentPattern == ExperimentPattern.Phase)
        {
            experimentalCondition += "_" + "BrightnessBlendMode_" + brightnessBlendMode.ToString();
        }
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
            //Debug.Log("Mouse Clicked");
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
                    if (experimentPattern == ExperimentPattern.Fourier || experimentPattern == ExperimentPattern.Phase)
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
                if (experimentPattern == ExperimentPattern.Fourier || experimentPattern == ExperimentPattern.Phase)
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

        if (experimentPattern == ExperimentPattern.Fourier || experimentPattern == ExperimentPattern.Phase)
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
                switch (experimentPattern)
                {
                    case ExperimentPattern.Fourier:
                        //1.v(t)=V0+A1·sin(ωt)+A2·cos(ωt)+A3·sin(2ωt)+A4·cos(2ωt)
                        //現在のstepのAmplitudeを計算
                        // Amplitudeを計算
                        amplitudeToSaveData = A_min + knobValue * (A_max - A_min);
                        if (step >= 1 && step < amplitudes.Length)
                        {
                            amplitudes[step] = amplitudeToSaveData;
                        }

                        // 現在の速度を計算
                        if (step >= 1) v += amplitudes[1] * Mathf.Sin(omega * time);
                        if (step >= 2) v += amplitudes[2] * Mathf.Cos(omega * time);
                        if (step >= 3) v += amplitudes[3] * Mathf.Sin(2 * omega * time);
                        if (step >= 4) v += amplitudes[4] * Mathf.Cos(2 * omega * time);

                        break;
                    case ExperimentPattern.Phase:
                        //2.v(t)=V0 + A1·sin(ωt + φ1 + Mathf.PI) + A2·sin(2ωt + φ2 + Mathf.PI)
                        //v(t)=V0 + A1·sin(ωt + A2) + A3·sin(2ωt + A4)
                        // 現在の速度を計算
                        if (step == 1 || step == 3)
                        {
                            amplitudeToSaveData = A_min + knobValue * (A_max - A_min);
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

                        break;
                    case ExperimentPattern.FunctionMix:
                        //NonlinearResponse(step, knobValue);
                        break;
                }


            }
            captureCamera0.transform.position += direction * v * Time.deltaTime;
        }
        else if (experimentPattern == ExperimentPattern.FunctionMix)
        {
            captureCamera0.transform.position += direction * Time.deltaTime;
        }

        //data.Add($"{timeMs:F3}, {SerialReader.lastSensorValue}, {responsePattern}, {step}, {amplitudeToSaveData}, {v}");
    }

    void LuminanceMixture()
    {

        // 写真を撮る距離に達したかをチェック // 检查是否到了拍照的距离
        //Debug.Log("frameNum--" + frameNum + "-----dt------" + Mathf.Abs(timeMs - frameNum * updateInterval * 1000));
        if (Mathf.Abs(timeMs - frameNum * updateInterval * 1000) < 0.2f)
        {
            frameNum++;
            Image1RawImage.enabled = false;
            Image2RawImage.enabled = false;
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
        //Debug.Log("nextImageRatio : " + nextImageRatio + "    timeMs : " + timeMs + "     frameNum : " + frameNum + "     updateInterval : "+ updateInterval);
        //Debug.Log("beforeImage1RawImage.color.r" + Image1RawImage.color.r + "  " + Image1RawImage.color.g + "  " + Image1RawImage.color.b + "  " + Image1RawImage.color.a);

        float nonlinearPreviousImageRatio = previousImageRatio;
        float nonlinearNextImageRatio = nextImageRatio;


        knobValue = SerialReader.lastSensorValue;
        //knobValue = 0.583f;//0.517, 0.713, 0.581, 0.583, 0.684, 1.0 ONO-C
        // knobValue = 0.218f;//0.0 0.492 0.471 0.231 0.178 0.205 LL-E
        // knobValue = 0.316f;//0.163 0.206 0.555 0.336 0.295 0.712 HOU-D
        // knobValue = 0.734f;//0.817 0.651 0.551 0.84 0.582 0.841 OMU-B
        // knobValue = 0.615f;//0.683 0.616 0.785 0.583 0.613 0.581 YAMA-A
        nonlinearPreviousImageRatio = BrightnessBlend.GetMixedValue(previousImageRatio, knobValue, brightnessBlendMode);
        nonlinearNextImageRatio = BrightnessBlend.GetMixedValue(nextImageRatio, knobValue, brightnessBlendMode);
        /*         if (experimentPattern == ExperimentPattern.FunctionMix)

                {
                    knobValue = Mathf.Clamp(SerialReader.lastSensorValue * 2f - 0.5f, -0.5f, 1.5f);//[-0.5, 2]
                    nonlinearPreviousImageRatio = BlendCurves.BlendCurve(previousImageRatio, knobValue, curveType);
                    nonlinearNextImageRatio = BlendCurves.BlendCurve(nextImageRatio, knobValue, curveType);
                } */

        /*         if (experimentPattern == ExperimentPattern.FunctionMix)
                {
                    SpeedFunctionTime += Time.deltaTime * SpeedFunctionFrequency;
                    Vector3 basePos = new Vector3(0f, 0f, 0f);
                    // 计算非线性混合比（t 可以是 previousImageRatio 和 nextImageRatio）
                    nonlinearPreviousImageRatio = CalculateZ(previousImageRatio, functionType, SpeedFunctionDistance, basePos, SpeedFunctionFrequency, SpeedFunctionAmplitude, SpeedFunctionOffset);
                    nonlinearNextImageRatio = CalculateZ(nextImageRatio, functionType, SpeedFunctionDistance, basePos, SpeedFunctionFrequency, SpeedFunctionAmplitude, SpeedFunctionOffset);

                } */

        if (frameNum % 2 == 0)
        {
            Image1RawImage.color = new Color(1, 1, 1, nonlinearNextImageRatio);
            Image2RawImage.color = new Color(1, 1, 1, 1.0f);

            CaptureCameraLinearBlendRawImage.material.SetColor("_TopColor", new Color(1, 1, 1, nonlinearNextImageRatio)); // 透明度
            CaptureCameraLinearBlendRawImage.material.SetColor("_BottomColor", new Color(1, 1, 1, 1.0f));
            alphaHistory.Add(nonlinearPreviousImageRatio);
        }
        else
        {
            Image1RawImage.color = new Color(1, 1, 1, nonlinearPreviousImageRatio);
            Image2RawImage.color = new Color(1, 1, 1, 1.0f);

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

        //Debug.Log("Image1RawImage.color.r"+ Image1RawImage.color.r+"  "+ Image1RawImage.color.g +"  "+ Image1RawImage.color.b +"  " + Image1RawImage.color.a);
        // Canvasに親オブジェクトを設定し、元のローカル位置、回転、およびスケールを保持 // 设置父对象为 Canvas，并保持原始的本地位置、旋转和缩放
        Image1RawImage.transform.SetParent(canvas.transform, false);
        Image2RawImage.transform.SetParent(canvas.transform, false);
        Image1RawImage.enabled = true;
        Image2RawImage.enabled = true;

        // データを記録 // 记录数据
        // data.Add("FrondFrameNum, FrondFrameLuminance, BackFrameNum, BackFrameLuminance, Time, FrameNum, Knob, ResponsePattern, StepNumber, Amplitude, Velocity");
        data.Add($"{frameNum}, {nonlinearPreviousImageRatio:F3}, {frameNum + 1}, {nonlinearNextImageRatio:F3}, {timeMs:F3}, {SerialReader.lastSensorValue}, {responsePattern}, {(int)stepNumber}, {amplitudeToSaveData}, {v}, {knobValue:F3}, {cameraSpeed:F3}");
        //data.Add($"{frameNum}, {Image1RawImage.color.a:F3}, {frameNum + 1}, {Image2RawImage.color.a:F3}, {timeMs :F3}, {(vectionResponse ? 1 : 0)}");

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
        Image1RawImage = Image1Transform.GetComponent<RawImage>();
        Image2RawImage = Image2Transform.GetComponent<RawImage>();
        CaptureCameraLinearBlendRawImage = CaptureCameraLinearBlendTransform.GetComponent<RawImage>();

        CaptureCameraLinearBlendRawImage.material = new Material(Mat_GrayscaleOverBlend);

        CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex", captureImageTexture1);       // 上层图
        CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", captureImageTexture2);    // 下层图  


        /*  test        
        CaptureCameraLinearBlendRawImage.material.SetTexture("_TopTex",  whiteTexture);       // 上层图
        CaptureCameraLinearBlendRawImage.material.SetTexture("_BottomTex", blackTexture );    // 下层图  */
        // RawImageコンポーネントを無効にする // 禁用 RawImage 组件
        continuousImageRawImage.enabled = false;
        Image1RawImage.enabled = false;
        Image2RawImage.enabled = false;


    }
    void QuitGame()
    {
#if UNITY_EDITOR

                    UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void TrailSettings()
    {
        string json = File.ReadAllText(savePath);
        ExperimentData data = JsonUtility.FromJson<ExperimentData>(json);

        Trial currentTrial = null;

        if (data.progress.exp1_intro_test < data.exp1_intro_test.Count)
        {
            currentTrial = data.exp1_intro_test[data.progress.exp1_intro_test];
            currentProgress = "exp1_intro_test";
            Debug.Log("Now exp1_intro_test");

            devMode = DevMode.Test; // 设置为测试模式 set to test mode, 1condition, 1 trial
            experimentPattern = ExperimentPattern.FunctionMix;
            brightnessBlendMode = BrightnessBlendMode.Dynamic;
        }
        else if (data.progress.exp1_trials < data.exp1_trials.Count)
        {
            currentTrial = data.exp1_trials[data.progress.exp1_trials];
            currentProgress = "exp1_trials";
            Debug.Log("Now exp1_trials");

            devMode = DevMode.FunctionRation; // set to function ratio mode, 1condition, 3 trials
            experimentPattern = ExperimentPattern.FunctionMix;
            brightnessBlendMode = BrightnessBlendMode.Dynamic;
            if (data.progress.exp1_trials + 1 == data.exp1_trials.Count && data.exp2_intro_test.Count == 0 && data.exp2_trials.Count == 0)
            {
                isEnd = true; // 最后一次试次
            }
        }
        else if (data.progress.exp2_intro_test < data.exp2_intro_test.Count)
        {
            currentTrial = data.exp2_intro_test[data.progress.exp2_intro_test];
            currentProgress = "exp2_intro_test";
            Debug.Log("Now exp2_intro_test");

            devMode = DevMode.Test; // 设置为测试模式 set to test mode, 1condition, 1 trial
            experimentPattern = ExperimentPattern.Phase; // set to phase mode
            brightnessBlendMode = BrightnessBlendMode.LinearOnly;
        }
        else if (data.progress.exp2_trials < data.exp2_trials.Count)
        {
            currentTrial = data.exp2_trials[data.progress.exp2_trials];
            currentProgress = "exp2_trials";
            Debug.Log("Now exp2_trials");

            devMode = DevMode.Normal; // 设置为测试模式 set to mode, 3condition, 3 trials
            experimentPattern = ExperimentPattern.Phase; // set to phase mode
            switch (currentTrial.condition)
            {
                case 1:
                    brightnessBlendMode = BrightnessBlendMode.LinearOnly;
                    break;
                case 2:
                    brightnessBlendMode = BrightnessBlendMode.Dynamic;
                    break;
            }
            if (data.progress.exp2_trials + 1 == data.exp2_trials.Count)
            {
                isEnd = true; // 最后一次试次
            }

        }
        else
        {
            Debug.Log("finished all trials");
            return;
        }

        trialNumber = currentTrial.repetition;

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
        //File.WriteAllLines(filePath, data);

        //Debug.Log($"Data saved to {filePath}");
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


    public static class BlendCurves
    {
        // -------- 单条曲线公式 --------
        static float Cosine(float x) => 0.5f * (1f - Mathf.Cos(Mathf.PI * x));//Cosine 缓动函数：输出 y = 0.5 * (1 - cos(πx))
        static float Cubic(float x) => 3f * x * x - 2f * x * x * x;//Cubic（SmoothStep）缓动函数：输出 y = 3x² - 2x³
                                                                   //Quintic（SmootherStep）缓动函数：输出 y = 6t⁵ - 15t⁴ + 10t³
        static float Quintic(float x)
        {
            float t = x;
            return t * t * t * (t * (6f * t - 15f) + 10f);
        }
        //Acos 曲线函数：输出 y = acos(-2x + 1) / π，定义域 x ∈ [0,1]
        static float AcosCurve(float x) =>
            (float)(Math.Acos(-2f * x + 1f) / Math.PI);

        // -------- 核心统一接口 --------
        /// <summary>
        /// 返回混合后的 y 值  
        /// x           : 0-1 的线性进度  
        /// funcRatio p : 0=线性 1=完全目标曲线（中间值 = 插值弯曲）  
        /// curveType   : 选用哪条曲线
        /// </summary>
        public static float BlendCurve(float x, float p, CurveType curveType)
        {
            p = Mathf.Clamp01(p);          // 保险
            if (curveType == CurveType.Linear || p == 0f)
                return x;                  // 纯线性直接返回

            float yCurve = curveType switch
            {
                CurveType.Cosine => Cosine(x),
                CurveType.Cubic => Cubic(x),
                CurveType.Quintic => Quintic(x),
                CurveType.Acos => AcosCurve(x),
                _ => x
            };
            return Mathf.Lerp(x, yCurve, p);   // 线性 ↔ 曲线 之间插值
        }
    }

    float GammaApprox(float z)
    {
        if (z <= 0f) return float.NaN;

        float[] p = {
        1.000000000190015f,
        76.18009172947146f,
        -86.50532032941677f,
        24.01409824083091f,
        -1.231739572450155f,
        0.001208650973866179f,
        -0.5395239384953e-5f
    };

        float x = p[0];
        for (int i = 1; i < p.Length; i++)
            x += p[i] / (z + i);

        float t = z + 5.5f;
        return Mathf.Sqrt(2 * Mathf.PI) * Mathf.Pow(t, z + 0.5f) * Mathf.Exp(-t) * x;
    }

    float GammaFunc(float t, float alpha, float beta)
    {
        if (t < 0f || alpha <= 0f || beta <= 0f) return 0f;  // 非法输入直接返回0

        float norm = GammaApprox(alpha);
        if (float.IsNaN(norm) || norm <= 0f) return 0f;      // 安全保护

        return Mathf.Pow(t, alpha - 1f) * Mathf.Exp(-t / beta) / (Mathf.Pow(beta, alpha) * norm);
    }

    float TriGamma(float phase, float g)
    {
        float y = 1f - Mathf.Abs(1f - Mathf.Repeat(phase, 2f * Mathf.PI) / Mathf.PI);
        return 1f - Mathf.Pow(y, g);
    }
    float CalculateZ(
    float SpeedFunctionTime,
    SpeedFunctionType functionType,
    float SpeedFunctionDistance,
    Vector3 SpeedFunctionleftLimit,
    float SpeedFunctionFrequency = 1f,
    float SpeedFunctionAmplitude = 1f,
    float SpeedFunctionOffset = 0f
    )
    {
        // 1. 让 t 在 [0, 2) 范围内循环往返
        float tt = SpeedFunctionTime * SpeedFunctionFrequency;
        // 2. 把往返做成 0→1→0 的区间：先对 2 取余，再对 1 作镜像
        float m = tt % 2f;
        if (m < 0f) m += 2f;
        // m ∈ [0,2)，当 m>1 时我们需要“回过头”，用 2-m
        float x = (m <= 1f) ? m : (2f - m);

        // 3. 根据 functionType 计算“规范化”输出 y0 ∈ [0,1]
        float y0;
        switch (functionType)
        {
            case SpeedFunctionType.Linear:
                y0 = x;
                break;

            case SpeedFunctionType.EaseInOut:
                y0 = (1f - Mathf.Cos(Mathf.PI * x)) * 0.5f;
                break;

            case SpeedFunctionType.Triangle:
                y0 = 1f - Mathf.Abs(2f * x - 1f); ;  // 此处 x∈[0,1]，也可直接用 x 或 1−|2x−1|
                break;

            case SpeedFunctionType.Arccos:
                // 把原来两个分段合并到同一个 x 上
                y0 = Mathf.Acos(-2f * x + 1f) / Mathf.PI;
                break;

            default:
                y0 = x;
                break;
        }

        // 4. 振幅 & 偏移
        float y = y0 * SpeedFunctionAmplitude + SpeedFunctionOffset;

        // 5. 映射到 Z 轴：leftLimit.z → leftLimit.z + distance
        return SpeedFunctionleftLimit.z + SpeedFunctionDistance * y;
    }
}

