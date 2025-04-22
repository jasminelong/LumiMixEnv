/*using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MoveCamera))]
public class MoveCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MoveCamera moveCamera = (MoveCamera)target;
        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        while (property.NextVisible(true))
        {
            if (property.name != "recordedCurve1" && property.name != "recordedCurve2")
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }

        if (moveCamera.recordedCurve1.keys.Length > 0 || moveCamera.recordedCurve2.keys.Length > 0)
        {
            float minTime = Mathf.Min(
                moveCamera.recordedCurve1.keys.Length > 0 ? moveCamera.recordedCurve1.keys[0].time : float.MaxValue,
                moveCamera.recordedCurve2.keys.Length > 0 ? moveCamera.recordedCurve2.keys[0].time : float.MaxValue
            );

            float maxTime = Mathf.Max(
                moveCamera.recordedCurve1.keys.Length > 0 ? moveCamera.recordedCurve1.keys[moveCamera.recordedCurve1.keys.Length - 1].time : float.MinValue,
                moveCamera.recordedCurve2.keys.Length > 0 ? moveCamera.recordedCurve2.keys[moveCamera.recordedCurve2.keys.Length - 1].time : float.MinValue
            );

            Rect curveRect = new Rect(minTime, 0f, maxTime - minTime, 1.1f);

            EditorGUILayout.LabelField("The brightness value of Image1RawImage (Green)", EditorStyles.boldLabel);
            moveCamera.recordedCurve1 = EditorGUILayout.CurveField(
                moveCamera.recordedCurve1,
                Color.green,
                curveRect,
                GUILayout.Height(100)
            );

            EditorGUILayout.LabelField("The brightness value of Image2RawImage (Red)", EditorStyles.boldLabel);
            moveCamera.recordedCurve2 = EditorGUILayout.CurveField(
                moveCamera.recordedCurve2,
                Color.red,
                curveRect,
                GUILayout.Height(100)
            );

            // **第三张图：在同一张曲线图中绘制两条曲线**
            EditorGUILayout.LabelField("Both Curves", EditorStyles.boldLabel);
            Rect combinedCurveFieldRect = GUILayoutUtility.GetRect(300, 150);
            Handles.BeginGUI();
            Handles.color = Color.green;
            DrawCurve(moveCamera.recordedCurve1, combinedCurveFieldRect, curveRect);
            Handles.color = Color.red;
            DrawCurve(moveCamera.recordedCurve2, combinedCurveFieldRect, curveRect);
            Handles.EndGUI();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // 手动绘制曲线
    private void DrawCurve(AnimationCurve curve, Rect guiRect, Rect curveBounds)
    {
        if (curve.keys.Length < 2)
            return;

        Vector3[] points = new Vector3[curve.keys.Length];

        for (int i = 0; i < curve.keys.Length; i++)
        {
            float x = Mathf.InverseLerp(curveBounds.x, curveBounds.x + curveBounds.width, curve.keys[i].time);
            float y = Mathf.InverseLerp(curveBounds.y, curveBounds.y + curveBounds.height, curve.keys[i].value);

            points[i] = new Vector3(
                guiRect.x + x * guiRect.width,
                guiRect.y + (1 - y) * guiRect.height,
                0
            );
        }

        Handles.DrawAAPolyLine(2f, points);
    }
}
*/