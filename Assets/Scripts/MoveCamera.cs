using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;




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
    public enum StepNumber
    {
        Option0 = 0,
        Option1 = 1,
        Option2 = 2,
        Option3 = 3,
        Option4 = 4,
    }
    public Camera captureCamera0; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
    public Camera captureCamera1; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
    public Camera captureCamera2; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
    public GameObject canvas;
    public Button nextStepButton;
    public float cameraSpeed = 1f; // カメラが円柱の軸に沿って移動する速度 (m/s) // 摄像机沿圆柱轴线移动的速度，m/s


    public float captureIntervalDistance; // 撮影間隔の距離 (m) // 拍摄间隔距离，m

    private Transform continuousImageTransform;
    private Transform Image1Transform;
    private Transform Image2Transform;
    private RawImage continuousImageRawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    private RawImage Image1RawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    private RawImage Image2RawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件

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
    private string folderName = "Experiment2Data"; // サブフォルダ名 // 子文件夹名称
    private float timeMs; // 現在までの経過時間 // 运行到现在的时间
    private Vector3 direction;

    private Vector3 targetPosition;      // FixedUpdate 的目标位置
    private Quaternion rightMoveRotation = Quaternion.Euler(0, 48.5f, 0);
    private Quaternion forwardMoveRotation = Quaternion.Euler(0, 146.8f, 0);
    private int currentStep = 0;
    public float v;
    public float[] amplitudes = new float[5];
    public SerialReader SerialReader;
    // Start is called before the first frame update

    // 数据保留的时长（例如，只保留最近10秒的数据） 輝度値の変化の表示
    /*        public float recordDuration = 1f;
    public AnimationCurve recordedCurve1 = new AnimationCurve();
    public AnimationCurve recordedCurve2 = new AnimationCurve();*/

    public ResponsePattern responsePattern;

    [Header("🔧記録するデータ")]
    public StepNumber stepNumber;
    public int trialNumber = 1;

    [Header("🔧 基本パラメータ（調整可能）")]


    [Range(0.1f, 10f)]
    public float omega = 2 * Mathf.PI; // 角速度（頻度）

    [Range(0f, 5f)]
    public float A_min = 0f;

    [Range(0f, 5f)]
    public float A_max = 3.0f;
    public float t = 0f;

    [Range(0f, 5f)]
    public float V0 = 1.0f;  // 基本速度

    private bool mouseClicked = false;
    private float amplitude;
    void Start()
    {

        // 垂直同期を無効にする // 关闭垂直同步
        QualitySettings.vSyncCount = 0;
        // 目標フレームレートを60フレーム/秒に設定 // 设置目标帧率为60帧每秒
        Time.fixedDeltaTime = 1.0f / 60.0f;

        nextStepButtonTextComponent = nextStepButton.GetComponentInChildren<TextMeshProUGUI>();
        nextStepButton.onClick.AddListener(OnNextStep); // ボタンがクリックされたときの処理を追加 // 添加按钮点击时的处理
        if (PlayerPrefs.HasKey("TrialNumber"))
        {
            if (PlayerPrefs.GetInt("TrialNumber") > 0 && PlayerPrefs.GetInt("TrialNumber") <= 3)
            {
                trialNumber = PlayerPrefs.GetInt("TrialNumber");
            }
            else if(PlayerPrefs.GetInt("TrialNumber") > 3)
            {
                trialNumber = 1;
            }
        }

        // captureCamera.enabled = false; // 初期状態でキャプチャカメラを無効にする // 初始化时禁用捕获摄像机

        updateInterval = 1 / fps; // 各フレームの表示間隔時間を計算 // 计算每一帧显示的间隔时间
        captureIntervalDistance = cameraSpeed / fps; // 各フレームの間隔距離を計算 // 计算每帧之间的间隔距离

        GetRawImage();
        InitialSetup();

        continuousImageRawImage.enabled = true;
        Image1RawImage.enabled = true;
        Image2RawImage.enabled = true;
        captureCamera2.transform.position += direction * captureIntervalDistance;
        data.Add("FrondFrameNum, FrondFrameLuminance, BackFrameNum, BackFrameLuminance, Time, Knob, ResponsePattern, StepNumber, Amplitude, Velocity");
        experimentalCondition = "fps" + fps.ToString() + "_"
                             + "ParticipantName_" + participantName.ToString() + "_"
                             + "TrialNumber_" + trialNumber.ToString();


        SerialReader = GetComponent<SerialReader>();

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
                case 1:
                case 2:
                case 3:
                    nextStepButtonTextComponent.text = "Next Step Amplitude" + ((int)stepNumber + 1).ToString();
                    break;
                case 4:
                    trialNumber++;
                    nextStepButtonTextComponent.text = "Entering the " + trialNumber + " trial";
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
    void OnNextStep()
    {
        mouseClicked = false;
        Time.timeScale = 1f;
        currentStep++;
        responsePattern = ResponsePattern.Amplitude;
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
                if (trialNumber < 3)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                else
                {
                    QuitGame();
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
        //予備実験
        //Vector3 targetPosition = captureCamera0.transform.position + direction * (SerialReader.lastSensorValue + 1f) * cameraSpeed * Time.fixedDeltaTime;
        //captureCamera0.transform.position = targetPosition;

        t += Time.fixedDeltaTime;

        // つまみセンサー値（0〜1）を取得し
        float knobValue = Mathf.Clamp01(SerialReader.lastSensorValue);
        // Amplitudeを計算
        amplitude = knobValue * (A_max - A_min);

        int step = (int)stepNumber;

        if (responsePattern == ResponsePattern.Velocity)
        {
            V0 = knobValue * 2f;
            v = V0;
        }
        else if (responsePattern == ResponsePattern.Amplitude)
        {
            // 現在のstepのAmplitudeを計算
            if (step >= 1 && step < amplitudes.Length)
            {
                amplitudes[step] = amplitude;
            }

            // 计算 v
            v = V0;

            // 現在の速度を計算
            if (step >= 1) v += amplitudes[1] * Mathf.Sin(omega * t);
            if (step >= 2) v += amplitudes[2] * Mathf.Cos(omega * t);
            if (step >= 3) v += amplitudes[3] * Mathf.Sin(2 * omega * t);
            if (step >= 4) v += amplitudes[4] * Mathf.Cos(2 * omega * t);
        }



        captureCamera0.transform.position += direction * v * Time.deltaTime;
        //data.Add($"{timeMs:F3}, {SerialReader.lastSensorValue}, {responsePattern}, {step}, {amplitude}, {v}");
    }

    void LuminanceMixture()
    {

        // 写真を撮る距離に達したかをチェック // 检查是否到了拍照的距离
        //Debug.Log("frameNum--" + frameNum + "-----dt------" + Mathf.Abs(timeMs - frameNum * updateInterval * 1000));
        if (Mathf.Abs(timeMs - frameNum * updateInterval * 1000) < 0.1f)
        {
            frameNum++;
            Image1RawImage.enabled = false;
            Image2RawImage.enabled = false;
            // カメラが移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置
            targetPosition = direction * cameraSpeed * updateInterval;

            // カメラを目標位置に移動 // 移动摄像机到目标位置
            captureCamera1.transform.position = captureCamera1.transform.position + targetPosition; ;
            captureCamera2.transform.position = captureCamera2.transform.position + targetPosition; ;
        }
        //輝度値を計算する 
        float Image1ToNowDeltaTime = timeMs - (frameNum - 1) * updateInterval * 1000;
        float nextRatio = Image1ToNowDeltaTime / (updateInterval * 1000);
        float nextImageRatio = Math.Min(1, Math.Max(0, nextRatio));// 浮動小数点の演算誤差により、減算の結果がわずかに0未満になる場合があります

        //Debug.Log("nextImageRatio : " + nextImageRatio + "    timeMs : " + timeMs + "     frameNum : " + frameNum + "     updateInterval : "+ updateInterval);

        float previousImageRatio = 1.0f - nextImageRatio;

        //Debug.Log("beforeImage1RawImage.color.r" + Image1RawImage.color.r + "  " + Image1RawImage.color.g + "  " + Image1RawImage.color.b + "  " + Image1RawImage.color.a);

        // Image1RawImage.color = new Color(Image1RawImage.color.r, Image1RawImage.color.g, Image1RawImage.color.b, previousImageRatio);
        // Image2RawImage.color = new Color(Image2RawImage.color.r, Image2RawImage.color.g, Image2RawImage.color.b, nextImageRatio);
        
        Image1RawImage.color = new Color(1, 1, 1, previousImageRatio);
        Image2RawImage.color = new Color(1, 1, 1, 1);

        //Debug.Log("Image1RawImage.color.r"+ Image1RawImage.color.r+"  "+ Image1RawImage.color.g +"  "+ Image1RawImage.color.b +"  " + Image1RawImage.color.a);
        // Canvasに親オブジェクトを設定し、元のローカル位置、回転、およびスケールを保持 // 设置父对象为 Canvas，并保持原始的本地位置、旋转和缩放
        Image1RawImage.transform.SetParent(canvas.transform, false);
        Image2RawImage.transform.SetParent(canvas.transform, false);
        Image1RawImage.enabled = true;
        Image2RawImage.enabled = true;

        // 輝度値の変化の表示
        //RecordVariable(Image1RawImage.color.a, Image2RawImage.color.a); 
        // データを記録 // 记录数据
            // data.Add("FrondFrameNum, FrondFrameLuminance, BackFrameNum, BackFrameLuminance, Time, FrameNum, Knob, ResponsePattern, StepNumber, Amplitude, Velocity");
        data.Add($"{frameNum}, {previousImageRatio:F3}, {frameNum + 1}, {nextImageRatio:F3}, {timeMs :F3}, {SerialReader.lastSensorValue}, {responsePattern}, {(int)stepNumber}, {amplitude}, {v}");
        //data.Add($"{frameNum}, {Image1RawImage.color.a:F3}, {frameNum + 1}, {Image2RawImage.color.a:F3}, {timeMs :F3}, {(vectionResponse ? 1 : 0)}");

    }

    void GetRawImage()
    {
        // Canvas内で指定された名前の子オブジェクトを検索 // 在 Canvas 中查找指定名称的子对象
        canvas = GameObject.Find("Canvas");
        continuousImageTransform = canvas.transform.Find("CaptureCamera0");
        Image1Transform = canvas.transform.Find("CaptureCamera1");
        Image2Transform = canvas.transform.Find("CaptureCamera2");

        // 子オブジェクトのRawImageコンポーネントを取得 // 获取子对象的 RawImage 组件
        continuousImageRawImage = continuousImageTransform.GetComponent<RawImage>();
        Image1RawImage = Image1Transform.GetComponent<RawImage>();
        Image2RawImage = Image2Transform.GetComponent<RawImage>();

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

    void OnDestroy()
    {
        PlayerPrefs.SetInt("TrialNumber", trialNumber);
        PlayerPrefs.Save();
        // 現在の日付を取得 // 获取当前日期
        string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // ファイル名を構築 // 构建文件名
        string fileName = $"{date}_{experimentalCondition}.csv";

        // ファイルを保存（Application.dataPath：現在のプロジェクトのAssetsフォルダのパスを示す） // 保存文件（Application.dataPath：表示当前项目的Assets文件夹的路径）
        string filePath = Path.Combine("D:/vectionProject/public", folderName, fileName);
        File.WriteAllLines(filePath, data);

        //Debug.Log($"Data saved to {filePath}");
    }
    public float GetAmplitude(int index)
    {
        return amplitudes[index];
    }

    public void SetAmplitude(int index, float value)
    {
        amplitudes[index] = value;
    }
}

