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

        float knobValue = 0.5f;
        if (Application.isPlaying && script.SerialReader != null)
        {
            knobValue = Mathf.Clamp01(script.SerialReader.lastSensorValue);
        }

        float A = script.A_min + knobValue * (script.A_max - script.A_min);
        float time = Time.time;
        float v = Mathf.Max(0f, script.v0 + A * Mathf.Sin(script.omega * script.t)); 

        // 添加当前速度到历史记录
        if (Application.isPlaying)
        {
            velocityHistory.Enqueue(v);
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
        float maxV = Mathf.Max(script.v0 + script.A_max, v);
        float minV = Mathf.Min(script.v0 - script.A_max, v);

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
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("⏱ 時間:", time.ToString("F2") + " 秒");
            EditorGUILayout.LabelField("📌 速度 v(t):", v.ToString("F3"));
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
}
