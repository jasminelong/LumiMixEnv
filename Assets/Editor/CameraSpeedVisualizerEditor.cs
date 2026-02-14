using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// CameraSpeedVisualizerEditor / MoveCamera inspector helper
/// CN: 自定义 Inspector，用于在编辑器中显示 MoveCamera 的参数与实时波形（亮度/速度）。
/// EN: Custom inspector to display MoveCamera parameters and realtime graphs (brightness / velocity) in the Editor.
/// JP: MoveCamera 用のカスタムインスペクタ。エディタ上でパラメータとリアルタイムグラフ（輝度/速度）を表示。
/// </summary>
[CustomEditor(typeof(MoveCamera))]
public class MoveCameraEditor : Editor
{
    // 绘图尺寸与缓存
    // CN: Inspector 内绘制波形用的纹理与队列缓存
    // EN: Texture and queues used to draw graphs in the inspector
    // JP: インスペクタでグラフを描画するためのテクスチャとキュー
    private int graphWidth = 500;
    private int graphHeight = 200;
    private Texture2D graphTexture;
    private Queue<float> velocityHistory = new Queue<float>();
    private Queue<float> camReverseJumpSpeedTimeHistory = new Queue<float>();
    private Queue<float> camReverseJumpSpeedHistory = new Queue<float>();
    private float camSpeedyMin = -1.5f, camSpeedyMax = 2.5f;

    /// <summary>
    /// OnInspectorGUI
    /// CN: 自定义 Inspector 主入口：显示序列化字段、滑条、并调用绘图函数。
    /// EN: Main Inspector entry: show serialized fields, sliders and call graph drawing helpers.
    /// JP: カスタムインスペクタの主入口。シリアライズされたフィールドやスライダを表示し、グラフ描画関数を呼ぶ。
    /// </summary>
    public override void OnInspectorGUI()
    {
        MoveCamera script = (MoveCamera)target;

        SerializedProperty prop;

        prop = serializedObject.FindProperty("captureCamera0");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureCamera1");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureCamera2");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureCamera3");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("canvas");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureImageTexture1");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("captureImageTexture2");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("Mat_GrayscaleOverBlend");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("GaussBlendMat");
        EditorGUILayout.PropertyField(prop);


        prop = serializedObject.FindProperty("treeRenderers");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("twoAfcTrials");
        EditorGUILayout.PropertyField(prop);

        // prop = serializedObject.FindProperty("cameraSpeed");
        // EditorGUILayout.PropertyField(prop);

        // prop = serializedObject.FindProperty("fps");
        // EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("participantName");
        EditorGUILayout.PropertyField(prop);

        // prop = serializedObject.FindProperty("subject");
        // EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("devMode");
        EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("brightnessBlendMode");
        EditorGUILayout.PropertyField(prop);

        // prop = serializedObject.FindProperty("paramOrder");
        // EditorGUILayout.PropertyField(prop);

        prop = serializedObject.FindProperty("stepNumber");
        EditorGUILayout.PropertyField(prop);
        // prop = serializedObject.FindProperty("brightnessBlendMode");
        // EditorGUILayout.PropertyField(prop);

        // prop = serializedObject.FindProperty("compensationClassification");
        // EditorGUILayout.PropertyField(prop);

        GUILayout.Space(10);
        prop = serializedObject.FindProperty("knobValue");
        EditorGUILayout.Slider(prop, -2f, 2f); // ← 使用 Slider

        serializedObject.ApplyModifiedProperties();

        // 绘制 Brightness 曲线
        DrawBrightnessGraph(script);


        //4.5-----
        prop = serializedObject.FindProperty("omega");
        EditorGUILayout.PropertyField(prop);
        prop = serializedObject.FindProperty("A_max");
        EditorGUILayout.Slider(prop, -10f, 10f);
        prop = serializedObject.FindProperty("A_min");
        EditorGUILayout.Slider(prop, -10f, 10f);

        prop = serializedObject.FindProperty("time");
        EditorGUILayout.PropertyField(prop);
        // === 由 StepNumber 控制哪个参数用“大样式” ===
        var current = script.stepNumber; // MoveCamera.StepNumber
        // 1) V0
        var propV0 = serializedObject.FindProperty("V0");
        // bool v0IsBig = (current == MoveCamera.StepNumber.Option0);
        // if (v0IsBig)
        // {
        //     DrawBigSliderWithNumber(propV0, "V0", -2f, 2f,
        //                             labelFontSize: 26, valueFontSize: 30,
        //                             valueColor: Color.red);
        // }
        // else
        // {
        EditorGUILayout.Slider(propV0, -2f, 2f);
        // }
        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);

        // 2) A1, φ1, A2, φ2
        EditorGUILayout.LabelField("Amplitude Sliders (A1, φ1, A2, φ2)", EditorStyles.boldLabel);

        // 标签 & 范

        // 1基数组：index 0 占位
        const string phi = "\u03C6";
        string[] labels = { "", "A1", phi + "1", "A2", phi + "2" };
        float[] minValues = { 0f, -2f, -5f, -2f, -5f };
        float[] maxValues = { 0f, 3f, 10f, 3f, 10f };

        const float rowGap = 6f;
        const float bigRowGap = 12f;

        for (int i = 1; i <= 4; i++)   // A1..φ2 一律 1基
        {
            string label = labels[i];
            float value = script.GetAmplitude(i);                 // ← 1基读取
            bool isBig = ((int)script.stepNumber == i);          // Option1..4 分别聚焦 A1..φ2

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


        GUILayout.Space(10);

        GUILayout.Space(18);
        DrawVelocityGraph(script);   // ← 在这里显示速度波形
    }

    /// <summary>
    /// DrawBrightnessGraph
    /// CN: 在 Inspector 中绘制最近 maxDuration 时间内的亮度（alpha）曲线，包含坐标轴与时间刻度。
    /// EN: Draw brightness (alpha) trace for the recent maxDuration in the Inspector, with axes/time ticks.
    /// JP: インスペクタに最近の maxDuration 内の輝度(alpha) トレースを軸付きで描画する。
    /// </summary>
    void DrawBrightnessGraph(MoveCamera script)
    {
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Brightness", EditorStyles.boldLabel);
        GUILayout.Space(10);

        var times = script.timeStamps;
        var alphas = script.alphaHistory;
        var maxDuration = script.maxDuration;
        float now = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

        Rect rect = GUILayoutUtility.GetRect(300, 150);
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));

        Handles.color = Color.gray;
        int yTicks = 5;
        for (int i = 0; i <= yTicks; i++)
        {
            float t = i / (float)yTicks;
            float y = Mathf.Lerp(rect.yMax, rect.yMin, t);
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMin + 5, y));
            GUI.Label(new Rect(rect.xMin + 8, y - 8, 40, 16), t.ToString("F2"));
        }

        int xTicks = 5;
        for (int i = 0; i <= xTicks; i++)
        {
            float t = i / (float)xTicks;
            float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
            Handles.DrawLine(new Vector3(x, rect.yMax), new Vector3(x, rect.yMax - 5));
            float timeLabel = now - maxDuration + t * maxDuration;
            GUI.Label(new Rect(x - 20, rect.yMax + 2, 40, 16), timeLabel.ToString("F2") + "s");
        }

        Handles.color = Color.cyan;
        int count = (times != null) ? times.Count : 0;
        float w = rect.width;
        float h = rect.height;
        if (count > 1)
        {
            for (int i = 1; i < count; i++)
            {
                float t0 = times[i - 1], t1 = times[i];
                float x0 = rect.xMin + Mathf.Clamp01((t0 - (now - maxDuration)) / maxDuration) * w;
                float y0 = rect.yMax - alphas[i - 1] * h;
                float x1 = rect.xMin + Mathf.Clamp01((t1 - (now - maxDuration)) / maxDuration) * w;
                float y1 = rect.yMax - alphas[i] * h;
                Handles.DrawLine(new Vector3(x0, y0), new Vector3(x1, y1));
            }
        }

        Handles.color = Color.white;
        GUILayout.Space(20);
        EditorGUILayout.LabelField($"最新5秒間のサンプル数: {count} ");
    }

    /// <summary>
    /// DrawVelocityGraph
    /// CN: 在 Inspector 中绘制速度 v(t) 曲线，Y 轴为固定范围以便比较与视觉化（包含坐标轴标签）。
    /// EN: Draw velocity v(t) trace in the Inspector. Y axis uses a fixed range for consistent visualization.
    /// JP: インスペクタに速度 v(t) 曲線を描画。Y軸は比較しやすいよう固定レンジを使用。
    /// </summary>
    void DrawVelocityGraph(MoveCamera script)
    {
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Velocity v(t)", EditorStyles.boldLabel);
        GUILayout.Space(10);

        var times = script.timeStamps;
        var vel = script.velocityHistory;   // 你需要同样保存一个 List<float>，与 alphaHistory 同步追加
        var maxDuration = script.maxDuration;
        float now = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

        // ==== 与 Brightness 完全一致的大小 ====
        Rect rect = GUILayoutUtility.GetRect(300, 150);
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));

        float w = rect.width;
        float h = rect.height;

        // 固定 Y 范围（便于比较）
        float minV = -2f, maxV = 3f;

        Handles.color = Color.gray;
        int yTicks = 5;
        for (int i = 0; i <= yTicks; i++)
        {
            float t = i / (float)yTicks;
            float y = Mathf.Lerp(rect.yMax, rect.yMin, t);
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMin + 5, y));

            float v = Mathf.Lerp(minV, maxV, t);
            GUI.Label(new Rect(rect.xMin + 8, y - 8, 40, 16), v.ToString("F2"));
        }

        // ------- X ticks (time axis) -------
        int xTicks = 5;
        for (int i = 0; i <= xTicks; i++)
        {
            float t = i / (float)xTicks;
            float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
            Handles.DrawLine(new Vector3(x, rect.yMax), new Vector3(x, rect.yMax - 5));

            float timeLabel = now - maxDuration + t * maxDuration;
            GUI.Label(new Rect(x - 20, rect.yMax + 2, 40, 16), timeLabel.ToString("F2") + "s");
        }

        // ====== 曲线 ======
        Handles.color = Color.cyan;
        int count = (times != null) ? times.Count : 0;

        if (count > 1)
        {
            for (int i = 1; i < count; i++)
            {
                float t0 = times[i - 1];
                float t1 = times[i];

                float x0 = rect.xMin + Mathf.Clamp01((t0 - (now - maxDuration)) / maxDuration) * w;
                float x1 = rect.xMin + Mathf.Clamp01((t1 - (now - maxDuration)) / maxDuration) * w;

                float y0 = rect.yMax - Mathf.InverseLerp(minV, maxV, vel[i - 1]) * h;
                float y1 = rect.yMax - Mathf.InverseLerp(minV, maxV, vel[i]) * h;

                Handles.DrawLine(new Vector3(x0, y0), new Vector3(x1, y1));
            }
        }

        GUILayout.Space(20);
        EditorGUILayout.LabelField($"最新5秒間の速度サンプル数: {count} ");
    }



    /// <summary>
    /// DrawLineOnTexture
    /// CN: 在指定纹理上画像素线段（Bresenham 风格），用于离屏绘制（目前未被频繁使用）。
    /// EN: Draw a pixel line on a texture (Bresenham-like), used for offscreen drawing.
    /// JP: テクスチャ上にピクセル線を描画（Bresenham風）。オフスクリーン描画用。
    /// </summary>
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

    /// <summary>
    /// MakeColoredTexture
    /// CN: 生成 1x1 单色纹理（常用于 GUI 背景）。
    /// EN: Create a 1x1 colored texture (useful for GUI backgrounds).
    /// JP: 1x1 の単色テクスチャを生成（GUI 背景などに利用）。
    /// </summary>
    private Texture2D MakeColoredTexture(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Round3
    /// CN: 将浮点数保留三位小数（用于 UI 显示与微调）。
    /// EN: Round float to 3 decimal places (for UI display / tidy values).
    /// JP: 小数点以下3桁に丸める（UI表示や微調整用）。
    /// </summary>
    private static float Round3(float x)
    {
        return Mathf.Round(x * 1000f) / 1000f; // 三位小数
    }

    /// <summary>
    /// DrawBigSliderWithNumber
    /// CN: 在 Inspector 中绘制“大号”标签、水平滑条和显眼数值框。用于突出当前聚焦参数。
    /// EN: Draw a large label + horizontal slider + prominent numeric field in Inspector (highlighted parameter).
    /// JP: インスペクタに大きなラベル、水平スライダ、目立つ数値フィールドを描画（注目パラメータ用）。
    /// </summary>
    private static void DrawBigSliderWithNumber(
        SerializedProperty sp, string label,
        float min, float max,
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
    /// <summary>
    /// DrawBigSliderWithNumberFloat
    /// CN: 上述 DrawBigSlider 的重载版本，接收并返回普通 float 值（便于脚本端调用）。
    /// EN: Overload of DrawBigSliderWithNumber that accepts and returns a float value (convenient for code usage).
    /// JP: DrawBigSliderWithNumber のオーバーロード。通常の float 値を受け取り返す形式。
    /// </summary>
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
        big.normal.textColor = c;
        big.focused.textColor = c;
        big.hover.textColor = c;
        big.active.textColor = c;

        v = EditorGUILayout.FloatField(v, big, GUILayout.Width(numberWidth));

        EditorGUILayout.EndHorizontal();
        return v;
    }
}
