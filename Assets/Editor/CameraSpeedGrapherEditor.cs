#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraSpeedGrapher))]
public class CameraSpeedGrapherEditor : Editor
{
    private float[] _buf;

    public override void OnInspectorGUI()
    {
        var t = (CameraSpeedGrapher)target;

        // Draw default fields first
        DrawDefaultInspector();
        EditorGUILayout.Space();

        // Live readouts
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Live", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current Speed", $"{t.CurrentSpeed:F4}");
            EditorGUILayout.LabelField("Min Seen", $"{t.MinSeen:F4}");
            EditorGUILayout.LabelField("Max Seen", $"{t.MaxSeen:F4}");
        }

        EditorGUILayout.Space();

        // Sparkline
        Rect r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(140));
        DrawChart(r, t);
        Repaint(); // keep updating while Inspector is visible
    }

    private void DrawChart(Rect r, CameraSpeedGrapher g)
    {
        // Background
        EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f, 1f));

        int n = g.GetSamples(ref _buf);
        if (n <= 1)
        {
            DrawCenterText(r, "No data yet…");
            return;
        }

        var (y0, y1) = g.GetYRange();
        if (Mathf.Approximately(y1 - y0, 0f))
        {
            y1 = y0 + 1f;
        }

        // Axes (zero line if visible)
        if (y0 < 0f && y1 > 0f)
        {
            float yZero = Mathf.InverseLerp(y0, y1, 0f);
            float yPix = Mathf.Lerp(r.yMax, r.yMin, yZero);
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            Handles.DrawLine(new Vector3(r.xMin, yPix), new Vector3(r.xMax, yPix));
        }

        // Polyline
        Vector3[] pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float xT = (n <= 1) ? 0f : (float)i / (n - 1);
            float yT = Mathf.InverseLerp(y0, y1, _buf[i]);
            float x = Mathf.Lerp(r.xMin, r.xMax, xT);
            float y = Mathf.Lerp(r.yMax, r.yMin, yT);
            pts[i] = new Vector3(x, y, 0f);
        }

        Handles.color = new Color(0.5f, 0.9f, 1f, 0.95f);
        Handles.DrawAAPolyLine(2.5f, pts);

        // Border
        Handles.color = new Color(1f, 1f, 1f, 0.2f);
        Handles.DrawLine(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin));
        Handles.DrawLine(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax));
        Handles.DrawLine(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax));
        Handles.DrawLine(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin));

        // Y labels
        var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = new Color(1,1,1,0.7f) } };
        GUI.Label(new Rect(r.xMin + 4, r.yMin + 2, 100, 16), $"{y1:F3}", labelStyle);
        GUI.Label(new Rect(r.xMin + 4, r.yMax - 16, 100, 16), $"{y0:F3}", labelStyle);
    }

    private void DrawCenterText(Rect r, string msg)
    {
        var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter };
        EditorGUI.LabelField(r, msg, style);
    }
}
#endif
