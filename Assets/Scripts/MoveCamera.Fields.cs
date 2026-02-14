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
    }
    public enum StepNumber
    {
          None = 0, 
        Option1 = 1,
        Option2 = 2,
        Option3 = 3,
        Option4 = 4,
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
    [SerializeField] BrightnessBlendMode brightnessBlendMode = BrightnessBlendMode.LinearOnly;

    public Camera captureCamera0; // CN: 用于按固定距离间隔拍照的摄像机。EN: Camera used to capture images at fixed distance intervals. JP: 一定の距離ごとに写真を撮るためのカメラ。
    public Camera captureCamera1; // CN: 用于按固定距离间隔拍照的摄像机（第二摄像机）。EN: Secondary camera used for interval captures. JP: 間隔撮影用のセカンダリカメラ。
    public Camera captureCamera2; // CN: 第三摄像机（可用于预位移或多摄像机流水）。EN: Third camera (used for offsetting / multi-camera pipeline). JP: 3番目のカメラ（オフセットや複数カメラ処理用）。
    public GameObject canvas; // CN: UI Canvas 的引用。EN: Reference to UI Canvas. JP: UI Canvas の参照。
    public Texture captureImageTexture1; // CN: 显示拍摄图像用的纹理（UI）。EN: Texture used to display captured images in UI. JP: 撮影画像を表示するためのテクスチャ（UI用）。
    public Texture captureImageTexture2; // CN: 备用显示用的纹理（UI）。EN: Secondary texture for display. JP: 補助表示用テクスチャ。
    public Button nextStepButton; // CN: “下一步”按钮引用。EN: Reference to Next Step button. JP: 次のステップボタンの参照。
    public float cameraSpeed = 1f; // CN: 摄像机沿轴线移动速度（m/s）。EN: Camera translational speed along axis (m/s). JP: カメラが軸に沿って移動する速度 (m/s)。


    public float captureIntervalDistance; // CN: 拍摄间隔距离（米）。EN: Distance between captures (m). JP: 撮影間隔の距離（m）。

    private Transform continuousImageTransform; // CN: Continuous image UI 的 Transform 引用。EN: Transform for continuous image UI. JP: 連続表示用イメージの Transform。
    private Transform Image1Transform; // CN: CaptureCamera1 对应的 UI Transform。EN: UI Transform for CaptureCamera1. JP: CaptureCamera1 のUI Transform。
    private Transform Image2Transform; // CN: CaptureCamera2 对应的 UI Transform。EN: UI Transform for CaptureCamera2. JP: CaptureCamera2 のUI Transform。
    private Transform CaptureCameraLinearBlendTransform; // CN: 线性混合 RawImage 的 Transform。EN: Transform for linear blend RawImage. JP: 線形ブレンド用 RawImage の Transform。
    private Transform CaptureCameraLinearBlendTopTransform; // CN: 高斯混合 Top RawImage 的 Transform。EN: Transform for gauss/top RawImage. JP: ガウスブレンド上層 RawImage の Transform。

    private RawImage continuousImageRawImage; // CN: 显示连续画面的 RawImage 组件。EN: RawImage component showing continuous view. JP: 連続表示用 RawImage コンポーネント。
    private RawImage CaptureCameraLinearBlendRawImage; // CN: 线性混合显示的 RawImage 组件。EN: RawImage for linear blend display. JP: 線形ブレンド表示用 RawImage。
    private RawImage CaptureCameraLinearBlendTopRawImage; // CN: 线性/高斯混合的顶层 RawImage。EN: Top RawImage used by blending shaders. JP: ブレンドシェーダで使用する上層 RawImage。

    public float updateInterval; // CN: 更新间隔（秒）。EN: Update interval in seconds. JP: 更新インターバル（秒）。

    // 数据保存用的字段 / Fields used for data saving / データ保存用フィールド
    // 当前帧数及时间信息 / Current frame/time tracking / 現在のフレーム・時間情報
    public int frameNum = 0;
    public string participantName; // CN: 被试姓名/编号。EN: Participant name/ID. JP: 被験者名/ID。
    private string experimentalCondition; // CN: 本次实验条件描述。EN: Description of experimental condition. JP: 実験条件の記述。
    private TextMeshProUGUI nextStepButtonTextComponent; // CN: 下一步按钮上的文字组件引用。EN: Text component for the Next Step button. JP: 次のステップボタンのテキストコンポーネント。

    public float fps = 1f; // CN: (备用) 帧率设置。EN: Fallback / configured frames-per-second. JP: フレームレート設定（予備）。
    public DirectionPattern directionPattern; // CN: 运动/提示方向模式。EN: Pattern for movement / presentation direction. JP: 動き/提示方向のパターン。

    private List<string> data = new List<string>(); // CN: 保存行数据列表。EN: List of data lines to save. JP: 保存用データ行のリスト。
    private float startTime; // CN: 记录开始时间。EN: Recording start time. JP: 記録開始時刻。
    private string folderName = "AAAGaussDatav0"; // CN: 数据子文件夹名。EN: Subfolder name for data. JP: データ用サブフォルダ名。
    private float timeMs; // CN: 已过时间（毫秒）。EN: Elapsed time in milliseconds. JP: 経過時間（ミリ秒）。
    private Vector3 direction; // CN: 全局运动方向向量。EN: Global movement direction vector. JP: グローバル移動方向ベクトル。

    private Vector3 targetPosition;      // CN: FixedUpdate 的目标位置增量。EN: Target position delta used in FixedUpdate. JP: FixedUpdate で使用する目標位置のデルタ。
    private Quaternion rightMoveRotation = Quaternion.Euler(0, 48.5f, 0); // CN: 向右移动时使用的旋转。EN: Rotation to apply for right movement. JP: 右方向移動時の回転。
    private Quaternion forwardMoveRotation = Quaternion.Euler(0, 146.8f, 0); // CN: 向前移动时使用的旋转。EN: Rotation to apply for forward movement. JP: 前方移動時の回転。
    private int currentStep = 1; // CN: 当前调参步骤索引。EN: Current calibration/step index. JP: 現在の調整ステップ番号。
    public float v; // CN: 当前瞬时速度（用于运动计算/记录）。EN: Current instantaneous velocity used for motion and logging. JP: 現在の瞬時速度（移動と記録用）。
    public float[] amplitudes = new float[10]; // CN: 储存各步幅值的数组（从索引 1 开始使用）。EN: Array storing amplitudes (indexing may start at 1). JP: 各ステップの振幅を格納する配列（インデックスは1始まりの想定あり）。
    public SerialReader SerialReader; // CN: 外部序列/传感器读数组件引用。EN: Reference to external serial/sensor reader component. JP: 外部シリアル/センサ読み取りコンポーネントの参照。

    // 以下为 Image1RawImage 透明度记录相关变量 / Variables for recording Image1 alpha over time / Image1 のアルファ記録用変数
    [Space(20)]
    [Header("🔧 Image1RawImageの輝度値の記録")]
    [Range(-10, 10)]
    public float knobValue = 0f; // CN: 旋钮值（0..1），用于合成权重/非线性度。EN: Knob value (0..1) used for weighting / nonlinearity. JP: つまみ値（0..1）。重み付け/非線形性に使用。
    public int maxSamples = 500; // CN: 历史缓冲最大样本数。EN: Max history samples to keep. JP: 履歴バッファの最大サンプル数。
    public float maxDuration = 5f; // CN: 保留最近时长（秒）。EN: Duration of recent data to display (s). JP: 最近のデータを保持する時間（秒）。
    [HideInInspector] public List<float> timeStamps = new List<float>(); // CN: 时间戳（秒）。EN: Timestamps in seconds. JP: タイムスタンプ（秒）。
    [HideInInspector] public List<float> alphaHistory = new List<float>(); // CN: 对应 alpha 历史。EN: Corresponding alpha history. JP: 対応するアルファの履歴。
    [HideInInspector] public List<float> velocityHistory = new List<float>(); // CN: 速度历史。EN: Velocity history. JP: 速度の履歴。

    // 速度与参数配置 / Speed & parameter configuration / 速度とパラメータ設定
    [Space(20)]
    [Header("🔧 基本パラメータ（調整可能）")]
    [Range(0.1f, 10f)]
    public float omega = 2 * Mathf.PI; // CN: 基本角速度（频率）。EN: Angular frequency used in speed modulation. JP: 基本角速度（周波数）。

    [Range(-1f, 5f)]
    public float A_min = -2f; // CN: 振幅最小值（映射区间下界）。EN: Minimum amplitude value. JP: 振幅の最小値。

    [Range(0f, 5f)]
    public float A_max = 2.0f; // CN: 振幅最大值（映射区间上界）。EN: Maximum amplitude value. JP: 振幅の最大値。
    public float time = 0f; // CN: 连续逻辑的累积时间（s）。EN: Accumulated time for continuous logic (s). JP: 連続処理での累積時間（秒）。

    [Range(0f, 5f)]
    public float V0 = 1.0f;  // CN: 基线速度（m/s）。EN: Baseline speed. JP: 基本速度（m/s）。

    private bool mouseClicked = false; // CN: 全局鼠标已点击标志（防止一帧多次触发）。EN: Global flag indicating mouse was clicked this frame. JP: マウスがクリックされたことを示すフラグ（一フレーム抑止用）。
    private float amplitudeToSaveData; // CN: 当前用于保存的振幅值临时变量。EN: Temporary amplitude value to save. JP: 保存用の一時振幅値。

    //------------ Speed function configuration ------------
    public enum SpeedFunctionType
    {
        Linear,
        EaseInOut,    // (1−cosπx)/2
        Triangle,    // 1−|m−1|
        Arccos       // 分段 arccos 波形
    }
    public SpeedFunctionType functionType = SpeedFunctionType.Linear;
    [Range(0f, 10f)]
    public float SpeedFunctionDistance = 5f; // CN: 整体位移/函数作用的总距离（用于速度函数）。EN: Total distance for speed function. JP: 速度関数における総距離。

    public Vector3 SpeedFunctionleftLimit = Vector3.zero; // CN: 速度函数左边界位置。EN: Left-limit position for speed function. JP: 速度関数の左側制約。

    [Range(0f, 5f)]
    public float SpeedFunctionFrequency = 1f; // CN: 速度函数频率参数。EN: Frequency parameter for speed function. JP: 速度関数の周波数パラメータ。

    [Range(0f, 2f)]
    public float SpeedFunctionAmplitude = 1f; // CN: 速度函数振幅。EN: Amplitude of speed function. JP: 速度関数の振幅。

    [Range(-1f, 1f)]
    public float SpeedFunctionOffset = 0f; // CN: 速度函数偏移量。EN: Offset for speed function. JP: 速度関数のオフセット。
    private float SpeedFunctionTime = 0f; // CN: 速度函数内部计时器。EN: Internal timer for speed function. JP: 速度関数の内部時間。

    //------------- end Speed function ------------

    Material _mat; // CN: 临时材料引用。EN: Temporary material reference. JP: 一時的なマテリアル参照。
    private Material matInstance; // CN: 材质实例。EN: Material instance. JP: マテリアルのインスタンス。
    public Material Mat_GrayscaleOverBlend; // CN: 线性混合所用材质（Shader）。EN: Material used for grayscale/linear blending shader. JP: グレースケール/線形ブレンド用のマテリアル。
    public Material GaussBlendMat; // CN: 高斯混合材质。EN: Material used for gaussian blending. JP: ガウスブレンド用マテリアル。
    private Texture2D blackTexture; // CN: 黑色纹理占位（可用于初始化）。EN: Placeholder black texture. JP: 黒テクスチャのプレースホルダ。
    private Texture2D whiteTexture; // CN: 白色纹理占位。EN: Placeholder white texture. JP: 白テクスチャのプレースホルダ。
    private int trailsCount = 0; // CN: 总试次数（trial counter）。EN: Total number of trials. JP: 試行回数。
    private int currentIndex = 0; // CN: 当前试次索引。EN: Current trial index. JP: 現在の試行インデックス。
    private string savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json"); // CN: 试次配置/保存路径。EN: Path to trials configuration/save file. JP: 試行設定/保存ファイルのパス。
    private bool isEnd = false; // CN: 实验是否已经结束的标志。EN: Flag indicating experiment end. JP: 実験が終了しているかのフラグ。
    private string currentProgress; // CN: 当前进度描述（可用于 UI）。EN: Current progress description (for UI). JP: 現在の進捗説明（UI用）。

    private const float OMEGA = 2f * Mathf.PI; // CN: 常量 2π。EN: Constant 2π. JP: 定数 2π。

    private const int N = 1000; // CN: 内部常量 N（保留）。EN: Internal constant N. JP: 内部定数 N。
    private Vector3 initPos0, initPos1, initPos2; // CN: 初始位姿位置缓存（用于 reset）。EN: Cached initial positions for reset. JP: リセット用の初期位置キャッシュ。
    private Quaternion initRot0, initRot1, initRot2; // CN: 初始旋转缓存。EN: Cached initial rotations. JP: 初期回転のキャッシュ。
    private bool initPoseSaved = false; // CN: 初始位姿是否已保存。EN: Whether initial poses have been saved. JP: 初期姿勢が保存済みかどうか。

    // 新增：标记 FixedUpdate 计数器
    private int fixedUpdateCounter = 0; // CN: FixedUpdate 自增计数器。EN: Counter incremented in FixedUpdate. JP: FixedUpdate でインクリメントされるカウンタ。

    public bool SaveCam1Png = true; // CN: 是否保存 Cam1 PNG（开关）。EN: Toggle to save Cam1 PNGs. JP: Cam1 PNG を保存するかの切り替え。
    public bool SaveCam2Png = false;      // CN: 是否保存 Cam2 PNG（需要时开启）。EN: Toggle for saving Cam2 PNGs (enable if needed). JP: 必要なら Cam2 PNG を保存するか。
    public string SaveFolderName = "CamCapture60s"; // CN: 保存文件夹名（用于默认路径）。EN: Folder name used for saving captures. JP: 保存用フォルダ名。

    private bool _capturing = false; // CN: 是否正在捕获的标志。EN: Flag indicating capture in progress. JP: キャプチャ中かどうかのフラグ。
    private int _savedCount = 0; // CN: 已保存帧计数。EN: Count of saved frames. JP: 保存済みフレーム数。
    private float _captureStartTime = 0f; // CN: 捕获开始时间戳。EN: Capture start time. JP: キャプチャ開始時刻。

    [SerializeField] public Renderer[] treeRenderers;   // CN: 场景中树的 Renderer 列表（用于 ROI / 元数据）。EN: Renderers for scene trees (for ROI/metadata). JP: シーン内の木のレンダラ配列（ROI/メタデータ用）。

    public float secondsPerStep = 1.0f;   // CN: 每个 step 的秒数（关键帧间隔）。EN: Seconds per step (keyframe interval). JP: ステップごとの秒数（キーフレーム間隔）。
    public float sigmaSec = 0.6f;         // CN: 高斯混合的 sigma（秒）。EN: Sigma for gaussian blending in seconds. JP: ガウスブレンドのシグマ（秒）。

    public string resourcesFolder = "CamFrames"; // CN: Resources 下帧图像所在文件夹名。EN: Folder name under Resources for frame images. JP: Resources 内のフレーム画像フォルダ名。
    public string namePrefix = "cam1_"; // CN: 资源名前缀，用于过滤帧文件名。EN: Name prefix used to filter frame filenames. JP: フレーム名のフィルタ用プレフィックス。

    private Texture2D[] frames; // CN: 已加载的帧纹理数组。EN: Loaded frame textures array. JP: 読み込まれたフレームテクスチャ配列。

    [Header("Capture Settings")]
    public bool SaveCam0ContinuousPng = false;   // CN: 是否实时连续保存 Cam0（可能阻塞主线程）。EN: Whether to continuously save Cam0 PNGs (may block main thread). JP: Cam0 を連続保存するか（メインスレッドをブロックする可能性あり）。
    public bool SaveCam1IsiPng = false;   // CN: 是否按 isi/间隔保存 Cam1（1Hz 或 updateInterval）。EN: Whether to save Cam1 at ISI interval (1Hz or updateInterval). JP: Cam1 を ISI 間隔で保存するか（1Hz または updateInterval）。
    public int CaptureSeconds = 40;     // CN: 保存时长上限（帧数上限估算用）。EN: Upper limit in seconds for capturing (used for max frames estimation). JP: 保存時間の上限（フレーム上限推定用）。
    public string Cam0SaveDir = @"D:\vectionProject\public\A-continuous-images"; // CN: Cam0 保存路径。EN: Save directory for Cam0. JP: Cam0 の保存先ディレクトリ。
    public string Cam1SaveDir = @"D:\vectionProject\public\A-isi-images"; // CN: Cam1 保存路径。EN: Save directory for Cam1. JP: Cam1 の保存先ディレクトリ。

    private int _cam0SavedCount = 0; // CN: 已为 Cam0 保存的帧数。EN: Number of frames saved for Cam0. JP: Cam0 に保存したフレーム数。
    private bool _recordingCam0 = false; // CN: 是否已为 Cam0 启动确定性录制协程。EN: Whether deterministic recording for Cam0 has been started. JP: Cam0 の決定論的録画が開始済みかどうか。
    private int _cam1SavedCount = 0; // CN: 已为 Cam1 保存的帧数。EN: Number of frames saved for Cam1. JP: Cam1 に保存したフレーム数。
    RenderTexture freezePrev, freezeCur, freezeNext; // CN: 三帧冻结用临时 RT。EN: Temporary rendertextures for freeze buffers. JP: フリーズ用の一時的な RenderTexture。
    bool freezeReady = false; // CN: 冻结缓冲是否准备好。EN: Whether freeze buffers are ready. JP: フリーズバッファが準備済みかどうか。

    int stepIndex = 0; // CN: 当前 second-step 索引（基于 secondsPerStep）。EN: Current step index (based on secondsPerStep). JP: 現在のステップインデックス（secondsPerStep 基準）。
    int lastStepIndex = int.MinValue; // CN: 上一次 stepIndex（用于检测变化）。EN: Last step index for change detection. JP: 前回のステップインデックス。

    int framesN = 0; // CN: 已加载帧数量缓存。EN: Cached number of loaded frames. JP: 読み込んだフレーム数のキャッシュ。
    public bool verboseLoadLog = false; // CN: 是否显示加载详细日志。EN: Toggle verbose logging for frame loading. JP: フレーム読み込み時の詳細ログを出すかどうか。
    private Material _Gaussmat; // CN: 高斯混合材质实例。EN: Material instance used for gaussian blending. JP: ガウスブレンド用のマテリアルインスタンス。
    private int _last0 = -1, _last1 = -1, _last2 = -1; // CN: 上次用于高斯贴图的索引缓存（避免频繁 SetTexture）。EN: Cached indices used by gauss textures to avoid redundant SetTexture. JP: ガウステクスチャに使用した前回のインデックスキャッシュ。
    private bool _gaussWarmupDone = false; // CN: 高斯 warmup 是否完成（避免首帧抖动）。EN: Whether gauss warmup completed to avoid first-frame artifacts. JP: ガウスのウォームアップが完了したかどうか。
    private int _gaussWarmupFrames = 2;   // CN: warmup 帧数阈值（1 或 2）。EN: Number of warmup frames. JP: ウォームアップフレーム数（1または2）。
    private int _gaussWarmupCount = 0; // CN: 当前已完成的 warmup 帧计数。EN: Counter for completed warmup frames. JP: 完了したウォームアップフレーム数。
}