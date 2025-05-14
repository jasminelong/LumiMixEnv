using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(MoveCamera))]
public class MoveCameraEditor : Editor
{
    private const int graphWidth = 300;
    private const int graphHeight = 100;
    private Texture2D graphTexture;
    private Queue<float> velocityHistory = new Queue<float>();
    private const float maxDuration = 5f; // 显示最近5秒

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MoveCamera script = (MoveCamera)target;
        // 表示 A1 ~ A4
        EditorGUILayout.LabelField("Amplitude Sliders (A1 ~ A4)", EditorStyles.boldLabel);
        for (int i = 1; i < script.amplitudes.Length; i++)
        {
            float value = script.GetAmplitude(i);
            float newValue = EditorGUILayout.Slider($"A{i}", value, 0f, 5f);
            if (newValue != value)
            {
                Undo.RecordObject(script, "Change Amplitude");
                script.SetAmplitude(i, newValue);
                EditorUtility.SetDirty(script);
            }
        }

        //ResponsePatternブロックを挿入
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



        // 🔽 StepNumberブロックを挿入
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
        EditorGUILayout.EndHorizontal();

        //描画速度波形
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
        if (graphTexture == null)
        {
            graphTexture = new Texture2D(graphWidth, graphHeight);
            graphTexture.filterMode = FilterMode.Point;
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
        GUILayout.Label(graphTexture);

        // 显示实时值
        float time = Time.time;
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("⏱ 時間:", time.ToString("F2") + " 秒");
            EditorGUILayout.LabelField("📌 速度 v(t):", script.v.ToString("F3"));
            Repaint(); // 每帧更新
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



}
