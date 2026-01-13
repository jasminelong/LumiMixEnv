using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public partial class MoveCamera : MonoBehaviour
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
        LuminanceLinearMix,
        FunctionMix,
        NoLuminanceBlendSingleCameraMove,
        CameraJumpMoveMinusCompensate,
        CameraJumpMovePlusCompensate,
        LuminanceMinusCompensate,
        LuminancePlusCompensate,
    }
    public enum StepNumber
    {
        None = 0,
        Option1 = 1,
        Option2 = 2,
        Option3 = 3,
        Option4 = 4,
        Option5 = 5,
        Option6 = 6,
        Option7 = 7,
        Option8 = 8,
        Option9 = 9,
    }
    public enum BrightnessBlendMode
    {
        Dynamic,      // Cosine → Linear → Acos → Cosine
        CosineOnly,
        AcosOnly,
        LinearOnly,
        GaussOnly,
        PhaseLinearized, // 相位线性化,
    }
    public enum DevMode
    {
        Test,         // 测试模式
        FunctionRation,    // 函数模式
        Normal,       // 正常模式
    }

    [SerializeField] DevMode devMode = DevMode.Test;
    [SerializeField] BrightnessBlendMode brightnessBlendMode = BrightnessBlendMode.PhaseLinearized;

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
    private Transform CaptureCameraLinearBlendTopTransform;
    private RawImage continuousImageRawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    private RawImage CaptureCameraLinearBlendRawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件
    private RawImage CaptureCameraLinearBlendTopRawImage;// 撮影した画像を表示するためのUIコンポーネント // 用于显示拍摄图像的UI组件

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
    private string folderName = "ExperimentData55"; // サブフォルダ名 // 子文件夹名称
    private float timeMs; // 現在までの経過時間 // 运行到现在的时间
    private Vector3 direction;

    private Vector3 targetPosition;      // FixedUpdate 的目标位置
    private Quaternion rightMoveRotation = Quaternion.Euler(0, 48.5f, 0);
    private Quaternion forwardMoveRotation = Quaternion.Euler(0, 146.8f, 0);
    private int currentStep = 1;
    public float v;
    public float[] amplitudes = new float[10];
    public SerialReader SerialReader;
    // Start is called before the first frame update

    // 数据保留的时长（例如，只保留最近10秒的数据） 輝度値の変化の表示
    /*        public float recordDuration = 1f;
    public AnimationCurve recordedCurve1 = new AnimationCurve();
    public AnimationCurve recordedCurve2 = new AnimationCurve();*/

    public ResponsePattern responsePattern = ResponsePattern.Amplitude;

    [Header("🔧記録するデータ")]
    public StepNumber stepNumber = StepNumber.Option1; // 現在のステップ番号   // 当前步骤编号

    public ExperimentPattern experimentPattern = ExperimentPattern.NoLuminanceBlendSingleCameraMove;
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
    [HideInInspector] public List<float> velocityHistory = new List<float>();


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
    public Material GaussBlendMat;
    private Texture2D blackTexture;
    private Texture2D whiteTexture;
    private int trailsCount = 0; // 试次总数
    private int currentIndex = 0; // 当前试次索引
    private string savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");
    private bool isEnd = false; // 是否结束实验
    private string currentProgress; // 


    [Header("Subject / Condition")]
    public SubjectOption subject = SubjectOption.OMU_B;  // Inspector 里选   择被试   
                                                         // T = 1 s → ω = 2π rad/s
    private const float OMEGA = 2f * Mathf.PI;

    public float cameraSpeedReverse;

    public enum SubjectOption
    {
        KK_F,         // 参与者 KK  -F       
        YAMA_A,   // 新增：参与者 YAMA -A
        OMU_B,    // 参与者 OMU -B
        ONO_C,    // 参与者 ONO -C
        HOU_D,    // 参与者 HOU -D
        LL_E      // 参与者 LL  -E

    }

    // ===== 逆函数补偿（独立小函数，可直接调用） =====
    // ---- 工具：logit / sigmoid ----
    static float Sigmoid(float z) => 1f / (1f + Mathf.Exp(-z));
    static float Logit(float a) => Mathf.Log(a / (1f - a)); // a∈(0,1)

    // 你的被试参数

    public float eta1 = 0.15f, eta2 = 0.15f;   // 灵敏度（可校准）
    [Range(0, 3)] public float compScale = 1.0f; // 全局增益
    [Range(0, 1)] public float smooth = 0.25f;
    public float maxDeltaZPerSec = 8f;   // z域最大变化速率，抑制颤抖

    float _zCorrPrev = 0f;   // 上一帧的补偿z（用于平滑）
    float _tPrev = -1f;

    public enum CompensationClassification
    {
        V0_A1,
        A1A2,
        A2,

        A1,
        V0_A1A2,
        V0_A2,

        V0,

    }

    public CompensationClassification compensationClassification = CompensationClassification.A1A2;
    public enum ParameterOrder
    {
        V0_A1_PHI1_A2_PHI2, // Original order
        V0_A1_PHI1_A1_A2_PHI2_A2,
        V0_A1_PHI1_A2_PHI2_A1_PHI1_A2_PHI2,
        V0_PHI1_A1_PHI2_A2,
        V0_PHI1_A1_PHI1_PHI2_A2_PHI2,
    }
    public ParameterOrder paramOrder = ParameterOrder.V0_A1_PHI1_A2_PHI2; // Change this to switch orders

    private const int N = 1000;
    private float[] timeMap = new float[N];
    private bool mapReady = false;

    private Vector3 initPos0, initPos1, initPos2;
    private Quaternion initRot0, initRot1, initRot2;
    private bool initPoseSaved = false;
    // 新增：标记刚刚重置时间
    private int fixedUpdateCounter = 0;


    public float dEffRad = 0.60f * Mathf.PI;

private bool isInGray = false;
[SerializeField] private int segmentMs = 25000;   // 25s
[SerializeField] private int grayMs = 200;        // 200ms

//test grating
public bool UseGrating = false ;
public int GratingW = 800;     // 对应 Python W=800
public int GratingH = 140;     // 对应 Python H=140

public float Cycles = 10f;     // 对应 cycles=10.0

// 关键：用“弧度相位差”，对应 Python 的 d_step（默认 0.9π）
public float DStepRad = 0.9f * Mathf.PI;

public bool VerticalStripes = true; // Python 是沿 x 变化 => 竖条
// 对齐 Python ampnorm 的 scale=2.5 => amp=1/2.5=0.4
public float GratingAmp = 0.4f;

public Texture2D gratingA, gratingB;
public int NumImages = 11;
int seg = 0;


public bool SaveCam1Png = true;
public bool SaveCam2Png = false;          // 需要就开
public int CaptureDurationSeconds = 60;   // 你要 60s
public string SaveFolderName = "CamCapture60s";

private bool _capturing = false;
private int _savedCount = 0;
private float _captureStartTime = 0f;
private const string Camera1SaveDir = @"D:\vectionProject\public\camera2images";

[SerializeField] public Renderer[] treeRenderers;   // 拖拽树的 MeshRenderer(s)

public float secondsPerStep = 1.0f;   // 1Hz keyframe
public float sigmaSec = 0.6f;         // sigma0p6 => 0.6s

public string resourcesFolder = "CamFrames";
public string namePrefix = "cam2_";

private Texture2D[] frames;

}