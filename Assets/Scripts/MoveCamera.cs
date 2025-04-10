using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;


    public class MoveCamera : MonoBehaviour
    {
        public enum Pattern
        {
            luminanceMixture,
            continuous,
            wobble,
            
        }
        public enum DirectionPattern
        {
            right,
            forward
        }
        public Camera captureCamera0; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
        public Camera captureCamera1; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
        public Camera captureCamera2; // 一定の距離ごとに写真を撮るためのカメラ // 用于间隔一定距离拍照的摄像机
        public Image grayImage;
        public GameObject canvas;
        public float cameraSpeed = 4f; // カメラが円柱の軸に沿って移動する速度 (m/s) // 摄像机沿圆柱轴线移动的速度，m/s
        public Material Image1BrightnessMaterial; // 在 Inspector 里赋值 Material
        public Material Image2BrightnessMaterial; // 在 Inspector 里赋值 Material

    private float trialTime = 1 * 180 * 1000f;//实验的总时间
        public float captureIntervalDistance; // 撮影間隔の距離 (m) // 拍摄间隔距离，m

        private Transform continuousImageTransform;
        private Transform Image1Transform;
        private Transform Image2Transform;
        private RawImage continuousImageRawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
        private RawImage Image1RawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
        private RawImage Image2RawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
        

        public float updateInterval; // 更新間隔 (秒) // 更新间隔，单位秒
        private float updateTimer = 0f;

        // データ保存用のフィールド // 保存数据用的字段
        // 現在のフレーム数と時間を取得 // 获取当前帧数和时间
        public int frameNum = 0;
        public string participantName;
        private string experimentalCondition;
        public int trialNumber;
        public float fps = 10f; // 他のfps // 其他的fps
        public Pattern movementPattern; // イメージの提示パターン // 图像提示的模式
        public DirectionPattern directionPattern; // イメージの提示パターン // 图像提示的模式

        private List<string> data = new List<string>();
        private float startTime;
        private bool vectionResponse = false;
        private string folderName = "Experiment2Data"; // サブフォルダ名 // 子文件夹名称
        private float timeMs; // 現在までの経過時間 // 运行到现在的时间
        private Vector3 direction;
        //private float bufferDurTime = 0f;//10000f;
        //private float bufferDurTime = 10000f;//10000f;
        private float bufferDurTime = 0f;//10000f;
        private Vector3 targetPosition;      // FixedUpdate 的目标位置
        private Quaternion rightMoveRotation = Quaternion.Euler(0, 48.5f, 0);
        private Quaternion forwardMoveRotation = Quaternion.Euler(0, 146.8f, 0);
        private bool isChanged = false;
        private SerialReader SerialReader;
        // Start is called before the first frame update

        // 数据保留的时长（例如，只保留最近10秒的数据）
        public float recordDuration = 1f;
        public AnimationCurve recordedCurve1 = new AnimationCurve();
        public AnimationCurve recordedCurve2 = new AnimationCurve();

        void Start()
        {
            startTime = Time.time;
            // 垂直同期を無効にする // 关闭垂直同步
            QualitySettings.vSyncCount = 0;
            // 目標フレームレートを60フレーム/秒に設定 // 设置目标帧率为60帧每秒
            Time.fixedDeltaTime = 1.0f / 60.0f;

            // captureCamera.enabled = false; // 初期状態でキャプチャカメラを無効にする // 初始化时禁用捕获摄像机

            updateInterval = 1 / fps; // 各フレームの表示間隔を計算 // 计算每一帧显示的间隔时间
            captureIntervalDistance = cameraSpeed / fps; // 各フレームの間隔距離を計算 // 计算每帧之间的间隔距离

            Vector3 worldRightDirection = rightMoveRotation * Vector3.right;
            //Debug.Log("worldRightDirection---"+ worldRightDirection);
            Vector3 worldForwardDirection = forwardMoveRotation * Vector3.forward;
            GetRawImage();
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
            switch (movementPattern)
            {
                case Pattern.continuous:
                    data.Add("FrameNum,Time,Vection Response");
                    experimentalCondition = movementPattern.ToString() + "_"
                                         + "cameraSpeed" + cameraSpeed.ToString() + "_"
                                         + "fps60";
                    break;
                case Pattern.wobble:
                    data.Add("FrameNum,Time,Vection Response");
                    frameNum++;
                    break;
                case Pattern.luminanceMixture:
                    data.Add("FrondFrameNum,FrondFrameLuminance,BackFrameNum,BackFrameLuminance,Time,Vection Response");
                    frameNum++;
                    continuousImageRawImage.enabled = true;
                    Image1RawImage.enabled = true;
                    Image2RawImage.enabled = true;
                    captureCamera2.transform.position += direction * captureIntervalDistance;
                    //Debug.Log("captureCamera2.transform.position----" + captureCamera2.transform.position);
                    experimentalCondition = movementPattern.ToString() + "_"
                                         + "cameraSpeed" + cameraSpeed.ToString() + "_"
                                         + "fps" + fps.ToString();
                    break;
            }

            SerialReader = GetComponent<SerialReader>();
            StartCoroutine(ShowGrayScreen(bufferDurTime / 1000));
        }
        IEnumerator ShowGrayScreen(float duration)
        {
            grayImage.gameObject.SetActive(true);  // 激活灰色背景
            yield return new WaitForSeconds(duration);
            grayImage.gameObject.SetActive(false); // 隐藏灰色背景
        }
        // Update is called once per frame

        void FixedUpdate()
        {
            timeMs = (Time.time - startTime) * 1000;

            // キーの状態をチェック // 检测按键状态
            if (Input.GetKey(KeyCode.Alpha1))
            {
                vectionResponse = true;
            }
            else
            {
                vectionResponse = false;
            }
            switch (movementPattern)
            {
                case Pattern.continuous:
                    Continuous();
                    break;
                case Pattern.wobble:
                    Wabble();
                    break;
                case Pattern.luminanceMixture:
                    Continuous();
                    LuminanceMixture();
                    break;
            }
        }
        void Continuous()
        {
            //Debug.Log("timeMs----" + timeMs);
            //Debug.Log("bufferDurTime----" + bufferDurTime);
            if (timeMs < bufferDurTime)
            {
                // データを記録 // 记录数据
               // data.Add($"0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
            }
            else if (timeMs >= bufferDurTime && timeMs <= (bufferDurTime + trialTime))
            {
                continuousImageRawImage.enabled = true;
                // 更新 FixedUpdate 起始位置
                // カメラが円柱の軸に沿って移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置right 
                //Vector3 targetPosition = captureCamera0.transform.position + direction * cameraSpeed * Time.fixedDeltaTime;
                Vector3 targetPosition = captureCamera0.transform.position + direction * (SerialReader.lastSensorValue + 0f) * cameraSpeed * Time.fixedDeltaTime;
                //Debug.Log("targetPosition----------" + targetPosition);
                // カメラを目標位置に移動 // 移动摄像机到目标位置
                captureCamera0.transform.position = targetPosition;

                //frameNum++;
                // データを記録 // 记录数据,这里是为了记录数据从1开始，所以用的frameNum而不是frameNum-1,因为list的下标是从0开始的
               // data.Add($"{frameNum}, {timeMs - bufferDurTime:F}, {(vectionResponse ? 1 : 0)}");
            }
            else if (timeMs > (bufferDurTime + trialTime) && timeMs <= (trialTime + 2 * bufferDurTime))
            {
                continuousImageRawImage.enabled = false;
                StartCoroutine(ShowGrayScreen(bufferDurTime / 1000));
                //data.Add($"0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
            }
            else if (timeMs > (trialTime + 2 * bufferDurTime))
            {
                //data.Add($"0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
                //QuitGame();
            }
        }
       
        void LuminanceMixture()
        {
        //Debug.Log("111111111captureCamera1.transform.position----" + captureCamera1.transform.position);
        //Debug.Log("1111111111111captureCamera2.transform.position----" + captureCamera2.transform.position);
        if (timeMs < bufferDurTime)
        {
            // データを記録 // 记录数据
            data.Add($"0, 0, 0, 0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
        }
        else if (timeMs >= bufferDurTime && timeMs <= (bufferDurTime + trialTime))
        {
            /*                Image1RawImage.enabled = false;
                            Image2RawImage.enabled = false;*/
            // 写真を撮る距離に達したかをチェック // 检查是否到了拍照的距离
            //Debug.Log("frameNum--" + frameNum + "dt------" + Mathf.Abs(timeMs - bufferDurTime - frameNum * updateInterval * 1000));
            if (Mathf.Abs((timeMs - bufferDurTime) - frameNum * updateInterval * 1000) < 0.1f)
            {
                frameNum++;
                Image1RawImage.enabled = false;
                Image2RawImage.enabled = false;
                Image1RawImage.color = new Color(Image1RawImage.color.r, Image1RawImage.color.g, Image1RawImage.color.b, 0);
                Image2RawImage.color = new Color(Image2RawImage.color.r, Image2RawImage.color.g, Image2RawImage.color.b, 0);
                // カメラが円柱の軸に沿って移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置
                targetPosition = direction * cameraSpeed * updateInterval * 2;
                //Debug.Log("captureCamera1.transform.position----" + captureCamera1.transform.position);
                //Debug.Log("captureCamera2.transform.position----" + captureCamera2.transform.position);

                // カメラを目標位置に移動 // 移动摄像机到目标位置
                if (frameNum % 2 == 0)
                {
                    captureCamera1.transform.position = captureCamera1.transform.position + targetPosition; ;
                }
                else
                {
                    captureCamera2.transform.position = captureCamera2.transform.position + targetPosition; ;
                }

            }
            //輝度値を計算する 
            float Image1ToNowDeltaTime = timeMs - bufferDurTime - (frameNum - 1) * updateInterval * 1000;
            float nextRatio = Image1ToNowDeltaTime / (updateInterval * 1000);
            float nextImageRatio = nextRatio > 1.0f ? 1f : nextRatio;
            nextImageRatio = nextImageRatio < 0 ? 0f : nextImageRatio;
 
            Debug.Log("nextImageRatio : " + nextImageRatio + "    timeMs : " + timeMs + "     frameNum : " + frameNum + "     updateInterval : "+ updateInterval);
  
                //nextImageRatio = nextImageRatio < 0.8f ? 0.8f : nextImageRatio;
                float previousImageRatio = 1.0f - nextImageRatio;
            //previousImageRatio = previousImageRatio < 0.8f ? 0.8f : previousImageRatio;

            Debug.Log("beforeImage1RawImage.color.r" + Image1RawImage.color.r + "  " + Image1RawImage.color.g + "  " + Image1RawImage.color.b + "  " + Image1RawImage.color.a);
            if (frameNum % 2 == 0)
                {
                   // Image1RawImage.color = new Color(Image1RawImage.color.r, Image1RawImage.color.g, Image1RawImage.color.b, nextImageRatio);
                    //Image2RawImage.color = new Color(Image2RawImage.color.r, Image2RawImage.color.g, Image2RawImage.color.b, previousImageRatio);
                    // 通过 `Material` 修改 Shader 变量 `_Brightness`    
                    Image1BrightnessMaterial.SetFloat("_Brightness", nextImageRatio);
                    Image2BrightnessMaterial.SetFloat("_Brightness", previousImageRatio);
            }
                else
                {
                    //Image1RawImage.color = new Color(Image1RawImage.color.r, Image1RawImage.color.g, Image1RawImage.color.b, previousImageRatio);
                   // Image2RawImage.color = new Color(Image2RawImage.color.r, Image2RawImage.color.g, Image2RawImage.color.b, nextImageRatio);
                    // 通过 `Material` 修改 Shader 变量 `_Brightness`
                    Image1BrightnessMaterial.SetFloat("_Brightness", previousImageRatio);
                    Image2BrightnessMaterial.SetFloat("_Brightness", nextImageRatio);

                }

            Debug.Log("Image1RawImage.color.r"+ Image1RawImage.color.r+"  "+ Image1RawImage.color.g +"  "+ Image1RawImage.color.b +"  " + Image1RawImage.color.a);
                // Canvasに親オブジェクトを設定し、元のローカル位置、回転、およびスケールを保持 // 设置父对象为 Canvas，并保持原始的本地位置、旋转和缩放
                Image1RawImage.transform.SetParent(canvas.transform, false);
                Image2RawImage.transform.SetParent(canvas.transform, false);
                Image1RawImage.enabled = true;
                Image2RawImage.enabled = true;

            RecordVariable(Image1RawImage.color.a, Image2RawImage.color.a);
            // データを記録 // 记录数据
            //data.Add($"{frameNum}, {previousImageRatio:F3}, {frameNum + 1}, {Image2Ratio:F3}, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
            data.Add($"{frameNum}, {Image1RawImage.color.a:F3}, {frameNum + 1}, {Image2RawImage.color.a:F3}, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
            }
            else if (timeMs > (bufferDurTime + trialTime) && timeMs <= (trialTime + 2 * bufferDurTime))
            {
                Image1RawImage.enabled = false;
                Image2RawImage.enabled = false;
                StartCoroutine(ShowGrayScreen(bufferDurTime / 1000));
                // データを記録 // 记录数
                data.Add($"0, 0, 0, 0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
            }
            else if (timeMs > (trialTime + 2 * bufferDurTime))
            {
                data.Add($"0, 0, 0, 0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
                QuitGame();
            }
        }


     void RecordVariable(float Image1RawImage, float Image2RawImage)
    {
        // 记录第一个变量
        Keyframe newKey1 = new Keyframe(Time.time, Image1RawImage);
        recordedCurve1.AddKey(newKey1);

        // 记录第二个变量
        Keyframe newKey2 = new Keyframe(Time.time, Image2RawImage);
        recordedCurve2.AddKey(newKey2);

        // 清理超时关键帧（只保留 recordDuration 秒内的数据）
        float threshold = Time.time - recordDuration;
        while (recordedCurve1.keys.Length > 0 && recordedCurve1.keys[0].time < threshold)
        {
            recordedCurve1.RemoveKey(0);
        }
        while (recordedCurve2.keys.Length > 0 && recordedCurve2.keys[0].time < threshold)
        {
            recordedCurve2.RemoveKey(0);
        }
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

            Image1RawImage.material = Image1BrightnessMaterial;
            Image2RawImage.material = Image2BrightnessMaterial;
        Image1BrightnessMaterial.SetTexture("_MainTex", Image1RawImage.texture);
        Image2BrightnessMaterial.SetTexture("_MainTex", Image2RawImage.texture);

        // RawImageコンポーネントを無効にする // 禁用 RawImage 组件
        continuousImageRawImage.enabled = false;  
            Image1RawImage.enabled = false;
            Image2RawImage.enabled = false;

            
        }
        void Wabble()
        {
            if (timeMs < bufferDurTime)
            {
                // データを記録 // 记录数据
                data.Add($"0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
            }
            else if (timeMs >= bufferDurTime && timeMs <= (bufferDurTime + trialTime))
            {
                Image1RawImage.enabled = true;
                if (Mathf.Abs(timeMs - bufferDurTime - frameNum * updateInterval * 1000) < 0.01f)
                {
                    frameNum++;
                    // カメラが円柱の軸に沿って移動する目標位置を計算 // 计算摄像机沿圆锥轴线移动的目标位置right 
                    Vector3 targetPosition = captureCamera1.transform.position + direction * cameraSpeed * updateInterval;
                    // カメラを目標位置に移動 // 移动摄像机到目标位置
                    captureCamera1.transform.position = targetPosition;
                    // カメラを常に円柱の頂点に向ける // 确保摄像机始终朝向圆锥顶点
                    //captureCamera1.transform.LookAt(cylinderTopCenter);
                }

                // データを記録 // 记录数据,这里是为了记录数据从1开始，所以用的frameNum而不是frameNum-1,因为list的下标是从0开始的
                data.Add($"{frameNum}, {timeMs - bufferDurTime:F4}, {(vectionResponse ? 1 : 0)}");
            }
            else if (timeMs > (bufferDurTime + trialTime) && timeMs <= (trialTime + 2 * bufferDurTime))
            {
                Image1RawImage.enabled = false;
                StartCoroutine(ShowGrayScreen(bufferDurTime / 1000));
                data.Add($"0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
            }
            else if (timeMs > (trialTime + 2 * bufferDurTime))
            {
                data.Add($"0, {timeMs - bufferDurTime:F3}, {(vectionResponse ? 1 : 0)}");
                QuitGame();
            }
        }
        void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // エディターでのプレイモードを停止 // 在编辑器中停止播放模式
#else
                    Application.Quit(); // アプリケーションでアプリを終了 // 在应用程序中退出应用
#endif
        }

        void OnApplicationQuit()
        {
            // 現在の日付を取得 // 获取当前日期
            string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // ファイル名を構築 // 构建文件名
            string fileName = $"{date}_Natural_{directionPattern}_{experimentalCondition}_{participantName}_trialNumber{trialNumber}.csv";

            // ファイルを保存（Application.dataPath：現在のプロジェクトのAssetsフォルダのパスを示す） // 保存文件（Application.dataPath：表示当前项目的Assets文件夹的路径）
            string filePath = Path.Combine("D:/vectionProject/public", folderName, fileName);
            File.WriteAllLines(filePath, data);

            //Debug.Log($"Data saved to {filePath}");
        }

    }

