using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(MoveCamera))]
public class MoveCameraEditor : Editor
{
    private int graphWidth = 500;
    private int graphHeight = 200;
    private Texture2D graphTexture;
    private Queue<float> velocityHistory = new Queue<float>();

    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        MoveCamera script = (MoveCamera)target;

        SerializedProperty prop;

        prop = serializedObject.FindProperty("captureCamera0");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureCamera1");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureCamera2");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("canvas");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureImageTexture1");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureImageTexture2");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("Mat_GrayscaleOverBlend");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("cameraSpeed");
        EditorGUILayout.PropertyField(prop);

        // prop = serializedObject.FindProperty("updateInterval");
        // EditorGUILayout.PropertyField(prop);

        /*         prop = serializedObject.FindProperty("frameNum");
                EditorGUILayout.PropertyField(prop); */

        prop = serializedObject.FindProperty("fps");
        EditorGUILayout.PropertyField(prop);



        /*         prop = serializedObject.FindProperty("v");
                EditorGUILayout.PropertyField(prop); */

        prop = serializedObject.FindProperty("subject");
        EditorGUILayout.PropertyField(prop);

         prop = serializedObject.FindProperty("participantName");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("devMode");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("experimentPattern");
        EditorGUILayout.PropertyField(prop);

        /*         prop = serializedObject.FindProperty("trialNumber");
                EditorGUILayout.PropertyField(prop);
         */


        /*         prop = serializedObject.FindProperty("curveType");
                EditorGUILayout.PropertyField(prop); */

        prop = serializedObject.FindProperty("brightnessBlendMode");
        EditorGUILayout.PropertyField(prop);

        GUILayout.Space(10);
        prop = serializedObject.FindProperty("knobValue");
        EditorGUILayout.Slider(prop, -2f, 2f); // ← 使用 Slider



        /*                  SerializedProperty functionTypeProp = serializedObject.FindProperty("functionType");
                                                EditorGUILayout.PropertyField(functionTypeProp);

                                                prop = serializedObject.FindProperty("SpeedFunctionDistance");
                                                EditorGUILayout.Slider(prop, 0f, 10f); // ← 使用 Slider

                                                prop = serializedObject.FindProperty("SpeedFunctionFrequency");
                                                EditorGUILayout.Slider(prop, 0f, 5f); // ← 使用 Slider

                                                prop = serializedObject.FindProperty("SpeedFunctionAmplitude");
                                                EditorGUILayout.Slider(prop, 0f, 2f); // ← 使用 Slider

                                                prop = serializedObject.FindProperty("SpeedFunctionOffset");
                                                EditorGUILayout.Slider(prop, -1f, 1f); // ← 使用 Slider */


        serializedObject.ApplyModifiedProperties();

        //5-----輝度値の変化の表示
        GUILayout.Space(20);
        EditorGUILayout.LabelField("📷 Brightness", EditorStyles.boldLabel);
        GUILayout.Space(10);
        var times = script.timeStamps;
        var alphas = script.alphaHistory;
        var maxDuration = script.maxDuration;

        // 現在時刻取得//当前时间（同脚本里计算方式）
        float now = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

        // 波形描画用領域を確保
        Rect rect = GUILayoutUtility.GetRect(300, 150);
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));

        // Y軸の目盛りとラベルを描画//画 Y 轴刻度和标签
        Handles.color = Color.gray;
        int yTicks = 5;
        for (int i = 0; i <= yTicks; i++)
        {
            float t = i / (float)yTicks;
            float y = Mathf.Lerp(rect.yMax, rect.yMin, t);
            //  目盛り線//刻度线
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMin + 5, y));
            // ラベル表示（F2で小数点2桁）//标签
            GUI.Label(
                new Rect(rect.xMin + 8, y - 8, 40, 16),
                t.ToString("F2")
            );
        }

        // X軸の目盛りとラベルを描画
        int xTicks = 5;
        for (int i = 0; i <= xTicks; i++)
        {
            float t = i / (float)xTicks;
            float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
            Handles.DrawLine(new Vector3(x, rect.yMax), new Vector3(x, rect.yMax - 5));
            float timeLabel = now - maxDuration + t * maxDuration; // 1秒前から現在まで
            GUI.Label(new Rect(x - 20, rect.yMax + 2, 40, 16), timeLabel.ToString("F2") + "s");
        }

        // 波形をシアンで描画//画曲线
        Handles.color = Color.cyan;
        int count = times.Count;
        float w = rect.width;
        float h = rect.height;

        for (int i = 1; i < count; i++)
        {
            float t0 = times[i - 1], t1 = times[i];
            float x0 = rect.xMin + Mathf.Clamp01((t0 - (now - maxDuration)) / maxDuration) * w;
            float y0 = rect.yMax - alphas[i - 1] * h;
            float x1 = rect.xMin + Mathf.Clamp01((t1 - (now - maxDuration)) / maxDuration) * w;
            float y1 = rect.yMax - alphas[i] * h;
            Handles.DrawLine(new Vector3(x0, y0), new Vector3(x1, y1));
        }

        Handles.color = Color.white;
        GUILayout.Space(20);
        EditorGUILayout.LabelField($"最新5秒間のサンプル数: {count} ");

        //4.5-----
        prop = serializedObject.FindProperty("omega");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("A_min");
        EditorGUILayout.Slider(prop, -10f, 10f);

        prop = serializedObject.FindProperty("A_max");
        EditorGUILayout.Slider(prop, -10f, 10f);

        prop = serializedObject.FindProperty("time");
        EditorGUILayout.PropertyField(prop);
        // === 由 StepNumber 控制哪个参数用“大样式” ===
var current = script.stepNumber; // MoveCamera.StepNumber

// 1) V0
var propV0 = serializedObject.FindProperty("V0");
bool v0IsBig = (current == MoveCamera.StepNumber.Option0);
if (v0IsBig)
{
    DrawBigSliderWithNumber(propV0, "V0", -2f, 2f,
                            labelFontSize: 26, valueFontSize: 30,
                            valueColor: Color.red);
}
else
{
    EditorGUILayout.Slider(propV0, -2f, 2f);
}
serializedObject.ApplyModifiedProperties();

GUILayout.Space(10);

// 2) A1, φ1, A2, φ2
EditorGUILayout.LabelField("Amplitude Sliders (A1, φ1, A2, φ2)", EditorStyles.boldLabel);

// 标签 & 范

// 1基数组：index 0 占位
const string phi = "\u03C6";
string[] labels   = { "", "A1", phi + "1", "A2", phi + "2" };
float[] minValues = { 0f, -1f, -5f, -1f, -5f };
float[] maxValues = { 0f,  3f, 10f,  3f, 10f };

const float rowGap    = 6f;
const float bigRowGap = 12f;

for (int i = 1; i <= 4; i++)   // A1..φ2 一律 1基
{
    string label = labels[i];
    float  value = script.GetAmplitude(i);                 // ← 1基读取
    bool   isBig = ((int)script.stepNumber == i);          // Option1..4 分别聚焦 A1..φ2

    float newValue;
    if (isBig)
    {
        newValue = DrawBigSliderWithNumberFloat(
            label, value, minValues[i], maxValues[i],
            labelFontSize: 26, valueFontSize: 30, valueColor: Color.red);
        GUILayout.Space(bigRowGap);
    }
    else
    {
        newValue = EditorGUILayout.Slider(label, value, minValues[i], maxValues[i]);
        newValue = Round3(newValue);
        GUILayout.Space(rowGap);
    }

    if (!Mathf.Approximately(newValue, value))
    {
        Undo.RecordObject(script, "Change Amplitude");
        script.SetAmplitude(i, newValue);                  // ← 1基写回
        EditorUtility.SetDirty(script);
    }
}

        // prop = serializedObject.FindProperty("V0");
        // EditorGUILayout.PropertyField(prop);
        GUILayout.Space(10);
        // var propV0 = serializedObject.FindProperty("V0");

      /*   // 这里我先用与 knobValue 相同的范围 [-2, 2]，你需要的话改成自己的区间
        DrawBigSliderWithNumber(propV0, "V0", -2f, 2f,
                                labelFontSize: 26, valueFontSize: 30,
                                valueColor: Color.red);     // 想换色就改这里

        // 最后别忘了：
        serializedObject.ApplyModifiedProperties();
        GUILayout.Space(10);

        //4----- 表示 A1 ~ A4
        EditorGUILayout.LabelField("Amplitude Sliders (A1, φ1, A2, φ2)", EditorStyles.boldLabel);

        float[] minValues = { -1f, -5f, -1f, -5f };
        float[] maxValues = { 3f, 10f, 3f, 10f };

        // 显示用标签（索引与 i 对齐）
        string[] prettyLabels = { null, "A1", "φ1", "A2", "φ2" };
        // 如果担心编码，用：const string phi = "\u03C6"; string[] prettyLabels = { null, "A1", phi + "1", "A2", phi + "2" };

        for (int i = 1; i < script.amplitudes.Length; i++)
        {
            string label = (i < prettyLabels.Length && !string.IsNullOrEmpty(prettyLabels[i]))
                           ? prettyLabels[i] : $"A{i}";

            float value = script.GetAmplitude(i);
            float newValue = EditorGUILayout.Slider(label, value, minValues[i - 1], maxValues[i - 1]);

            if (!Mathf.Approximately(newValue, value))
            {
                Undo.RecordObject(script, "Change Amplitude");
                script.SetAmplitude(i, newValue);
                EditorUtility.SetDirty(script);
            }
        } */


        /*         //3-----ResponsePatternブロックを挿入
                GUILayout.Space(10);
                GUILayout.Label("📷 ResponsePattern", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                MoveCamera.ResponsePattern[] rmodes = (MoveCamera.ResponsePattern[])System.Enum.GetValues(typeof(MoveCamera.ResponsePattern));
                for (int i = 0; i < rmodes.Length; i++)
                {
                    bool isSelected = script.responsePattern == rmodes[i];
                    GUIStyle style = new GUIStyle(GUI.skin.button);
                    style.margin = new RectOffset(4, 4, 4, 4);
                    style.padding = new RectOffset(10, 10, 5, 5);

                    if (isSelected)
                    {
                        style.fontStyle = FontStyle.Bold;
                        style.normal.textColor = Color.black;
                        style.normal.background = MakeColoredTexture(new Color(0.6f, 1f, 0.6f)); // 蓝色底
                    }

                    if (GUILayout.Toggle(isSelected, rmodes[i].ToString(), style))
                    {
                        script.responsePattern = rmodes[i];
                    }
                }
                EditorGUILayout.EndHorizontal();
         */


        /*         // 2-----🔽 StepNumberブロックを挿入
                GUILayout.Space(10);
                GUILayout.Label("📷 StepNumber", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                MoveCamera.StepNumber[] modes = (MoveCamera.StepNumber[])System.Enum.GetValues(typeof(MoveCamera.StepNumber));
                for (int i = 0; i < modes.Length; i++)
                {
                    bool isSelected = script.stepNumber == modes[i];
                    GUIStyle style = new GUIStyle(GUI.skin.button);
                    style.margin = new RectOffset(4, 4, 4, 4);
                    style.padding = new RectOffset(10, 10, 5, 5);

                    if (isSelected)
                    {
                        style.fontStyle = FontStyle.Bold;
                        style.normal.textColor = Color.black;
                        style.normal.background = MakeColoredTexture(new Color(0.6f, 1f, 0.6f)); // 蓝色底
                    }

                    if (GUILayout.Toggle(isSelected, modes[i].ToString(), style))
                    {
                        script.stepNumber = modes[i];
                    }
                }
                EditorGUILayout.EndHorizontal(); */

        // … 你的其它 PropertyField / Slider …
GUILayout.Space(18);
DrawVelocityGraph(script);   // ← 在这里显示速度波形

        /*        //1-----描画速度波形----------------------start
               // 添加当前速度到历史记录
               if (Application.isPlaying)
               {
                   velocityHistory.Enqueue(script.v);
                   while (velocityHistory.Count > graphWidth)
                   {
                       velocityHistory.Dequeue();
                   }
               }

               // 初始化图像
               int dynamicWidth = (int)(EditorGUIUtility.currentViewWidth - 80); // 计算动态宽度
               if (graphTexture == null || graphTexture.width != dynamicWidth || graphTexture.height != graphHeight)
               {
                   graphWidth = dynamicWidth; // ✅ 把它赋给 graphWidth（变量）
                   graphTexture = new Texture2D(graphWidth, graphHeight);
                   graphTexture.filterMode = FilterMode.Point;
                   graphTexture.wrapMode = TextureWrapMode.Clamp;
               }


               // 清除图像
               Color backgroundColor = new Color(0.12f, 0.12f, 0.12f);
               Color[] pixels = new Color[graphWidth * graphHeight];
               for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;
               graphTexture.SetPixels(pixels);

               // 获取最大最小速度值（自动缩放）
               float maxV = Mathf.Max(script.V0 + script.A_max, script.v);
               float minV = Mathf.Min(script.V0 - script.A_max, script.v);

               // 坐标轴 Y=0线
               int zeroY = Mathf.RoundToInt(Mathf.InverseLerp(minV, maxV, 0f) * graphHeight);
               for (int x = 0; x < graphWidth; x++)
               {
                   if (zeroY >= 0 && zeroY < graphHeight)
                       graphTexture.SetPixel(x, zeroY, Color.gray);
               }

               // 绘制曲线
               float[] values = velocityHistory.ToArray();
               for (int x = 0; x < values.Length - 1; x++)
               {
                   float v1 = values[x];
                   float v2 = values[x + 1];
                   int y1 = Mathf.RoundToInt(Mathf.InverseLerp(minV, maxV, v1) * (graphHeight - 1));
                   int y2 = Mathf.RoundToInt(Mathf.InverseLerp(minV, maxV, v2) * (graphHeight - 1));

                   DrawLineOnTexture(graphTexture, x, y1, x + 1, y2, Color.cyan);
               }

               graphTexture.Apply();

               GUILayout.Label("📈 速度曲線 v(t)", EditorStyles.boldLabel);

               // ✅ 加入：刻度 + 图像 并排显示
               EditorGUILayout.BeginHorizontal();

               // 左：Y刻度区域
               GUILayout.BeginVertical(GUILayout.Width(60));
               int yDiv = 5;
               for (int i = yDiv; i >= 0; i--)
               {
                   float v = Mathf.Lerp(minV, maxV, i / (float)yDiv);
                   GUILayout.Label(v.ToString("F2"), GUILayout.Height(graphHeight / (float)yDiv));
               }
               GUILayout.EndVertical();

               // 右：图像区域（宽度自适应）
               GUILayout.Label(graphTexture, GUILayout.ExpandWidth(true), GUILayout.Height(graphHeight));

               EditorGUILayout.EndHorizontal();
               // 显示实时值
               float time = Time.time;
               if (Application.isPlaying)
               {
                   EditorGUILayout.LabelField("⏱ 時間:", time.ToString("F2") + " 秒");
                   EditorGUILayout.LabelField("📌 速度 v(t):", script.v.ToString("F3"));
                   Repaint(); // 每帧更新
               }
               //1-----描画速度波形----------------------end */

    }
    // 画速度曲线 v(t) —— y 轴刻度与曲线完全对齐
void DrawVelocityGraph(MoveCamera script)
{
    if (script == null) return;
    if (velocityHistory == null) velocityHistory = new Queue<float>(2048);

    // 动态宽度 & 纹理
    int desiredWidth = Mathf.Max(64, (int)(EditorGUIUtility.currentViewWidth - 80));
    if (graphTexture == null || graphTexture.width != desiredWidth || graphTexture.height != graphHeight)
    {
        graphWidth = desiredWidth;
        graphTexture = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp
        };
        velocityHistory.Clear();
    }

    // 采样历史（播放时）
    if (Application.isPlaying)
    {
        velocityHistory.Enqueue(script.v);
        while (velocityHistory.Count > graphWidth) velocityHistory.Dequeue();
    }

    // 背景
    var bg = new Color(0.12f, 0.12f, 0.12f);
    var pixels = new Color[graphWidth * graphHeight];
    for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
    graphTexture.SetPixels(pixels);

    // 历史数组（为空则用当前值）
    float[] values = (velocityHistory != null && velocityHistory.Count > 0)
        ? velocityHistory.ToArray()
        : new float[1] { script.v };

    // ===== 固定 Y 轴范围（不再随数据变化）=====
    float minV = -1f;
    float maxV =  3f;

    // y=0 轴（与曲线统一用 height-1）
    int zeroY = Mathf.RoundToInt(Mathf.InverseLerp(minV, maxV, 0f) * (graphHeight - 1));
    if (zeroY >= 0 && zeroY < graphHeight)
        for (int x = 0; x < graphWidth; x++)
            graphTexture.SetPixel(x, zeroY, new Color(0.7f, 0.7f, 0.7f, 1f));

    // 曲线
    for (int x = 0; x < values.Length - 1; x++)
    {
        int y1 = Mathf.RoundToInt(Mathf.InverseLerp(minV, maxV, values[x])     * (graphHeight - 1));
        int y2 = Mathf.RoundToInt(Mathf.InverseLerp(minV, maxV, values[x + 1]) * (graphHeight - 1));
        DrawLineOnTexture(graphTexture, x, y1, x + 1, y2, Color.cyan);
    }
    graphTexture.Apply();

    // 标题
    GUILayout.Label("📈 速度曲線 v(t)", EditorStyles.boldLabel);

    // 左刻度 + 右图像（同一坐标系）
    EditorGUILayout.BeginHorizontal();
    Rect yAxisRect = GUILayoutUtility.GetRect(60, graphHeight);
    Rect graphRect = GUILayoutUtility.GetRect(graphWidth, graphHeight, GUILayout.ExpandWidth(true));

    GUI.DrawTexture(graphRect, graphTexture, ScaleMode.StretchToFill);

    int yDiv = 5;
    var tickStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };

    Handles.BeginGUI();
    for (int i = 0; i <= yDiv; i++)
    {
        float t = i / (float)yDiv;           // 顶(0)→底(1)
        float v = Mathf.Lerp(maxV, minV, t); // 固定范围内的刻度值
        float y = Mathf.Lerp(graphRect.yMin, graphRect.yMax, t);

        GUI.Label(new Rect(yAxisRect.x, y - 8, yAxisRect.width - 4, 16), v.ToString("F2"), tickStyle);

        Handles.color = new Color(1f, 1f, 1f, 0.12f);
        Handles.DrawLine(new Vector2(graphRect.xMin, y), new Vector2(graphRect.xMax, y));
    }
    Handles.EndGUI();

    EditorGUILayout.EndHorizontal();

    // 运行时信息
    if (Application.isPlaying)
    {
        EditorGUILayout.LabelField("⏱ 時間:", Time.time.ToString("F2") + " 秒");
        EditorGUILayout.LabelField("📌 速度 v(t):", script.v.ToString("F3"));
        Repaint();
    }
}

    // 绘制线段（Bresenham風）
    void DrawLineOnTexture(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < tex.width && y0 >= 0 && y0 < tex.height)
                tex.SetPixel(x0, y0, col);

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private Texture2D MakeColoredTexture(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }
    private static float Round3(float x)
{
    return Mathf.Round(x * 1000f) / 1000f; // 三位小数
}

    // 可复用：大号标签 + 水平滑条 + 彩色大号数值框
    private static void DrawBigSliderWithNumber(
        SerializedProperty sp, string label,
        float min, float max,
        int labelFontSize = 26, int valueFontSize = 30,
        Color? valueColor = null, float labelWidth = 150f,
        float numberWidth = 120f, float rowHeight = 34f)
    {
        EditorGUILayout.BeginHorizontal();

        // 标签
        var bigLabel = new GUIStyle(EditorStyles.label)
        {
            fontSize = labelFontSize,
            fontStyle = FontStyle.Bold,
            fixedHeight = rowHeight
        };
        EditorGUILayout.LabelField(label, bigLabel, GUILayout.Width(labelWidth));

        // 滑条
        float v = sp.floatValue;
        v = GUILayout.HorizontalSlider(v, min, max, GUILayout.MinWidth(120));
        v = Round3(v); // ← 先取整

        // 数值框（放大 + 颜色）
        var big = new GUIStyle(EditorStyles.numberField)
        {
            fontSize = valueFontSize,
            fixedHeight = rowHeight,
            alignment = TextAnchor.MiddleCenter
        };
        var c = valueColor ?? Color.red;               // 默认红色
        big.normal.textColor = c;
        big.focused.textColor = c;                     // 获得焦点时也保持颜色
        big.hover.textColor = c;
        big.active.textColor = c;

        v = EditorGUILayout.FloatField(v, big, GUILayout.Width(numberWidth));

        if (!Mathf.Approximately(sp.floatValue, v))
            sp.floatValue = v;

        EditorGUILayout.EndHorizontal();
    }
    // 重载：用于普通 float 值（返回新值）
private static float DrawBigSliderWithNumberFloat(
    string label, float value, float min, float max,
    int labelFontSize = 26, int valueFontSize = 30,
    Color? valueColor = null, float labelWidth = 150f,
    float numberWidth = 120f, float rowHeight = 34f)
{
    EditorGUILayout.BeginHorizontal();

    var bigLabel = new GUIStyle(EditorStyles.label)
    {
        fontSize = labelFontSize,
        fontStyle = FontStyle.Bold,
        fixedHeight = rowHeight
    };
    EditorGUILayout.LabelField(label, bigLabel, GUILayout.Width(labelWidth));

    // 滑条
    float v = GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(120));
    v = Round3(v); // ← 先取整

    // 数值框
        var big = new GUIStyle(EditorStyles.numberField)
    {
        fontSize = valueFontSize,
        fixedHeight = rowHeight,
        alignment = TextAnchor.MiddleCenter
    };
    var c = valueColor ?? Color.red;
    big.normal.textColor  = c;
    big.focused.textColor = c;
    big.hover.textColor   = c;
    big.active.textColor  = c;

    v = EditorGUILayout.FloatField(v, big, GUILayout.Width(numberWidth));

    EditorGUILayout.EndHorizontal();
    return v;
}


}
