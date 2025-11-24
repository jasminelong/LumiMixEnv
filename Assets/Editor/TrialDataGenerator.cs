#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class TrialDataGenerator
{
    [MenuItem("Tools/Generate initial trial file")]
    public static void GenerateInitialTrialFile()
    {
        ExperimentData data = new ExperimentData
        {
            // practice: LuminanceLinearMix -> condition 1
            exp2_intro_test = new List<Trial> { new Trial { condition = 1, repetition = 1 } },
            // experimental trials: other 4 conditions (2..5), each repeated 3 times and randomized
            exp2_trials = GenerateRandomExp2Trials(),
            progress = new Progress()
        };

        string savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"Generate initial trial file: {savePath}");
    }

    private static List<Trial> GenerateRandomExp2Trials()
    {
        List<Trial> trials = new List<Trial>();

        // Conditions:
        // 1 = LuminanceLinearMix (practice)
        // 2 = CameraJumpMoveMinusCompensate
        // 3 = CameraJumpMovePlusCompensate
        // 4 = LuminanceMinusCompensate
        // 5 = LuminancePlusCompensate

        // Repeat conditions 1..5 three times
        for (int cond = 1; cond <= 5; cond++)
            for (int rep = 1; rep <= 3; rep++)
                trials.Add(new Trial { condition = cond, repetition = rep });

        // Fisher–Yates shuffle using UnityEngine.Random
        for (int i = trials.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var tmp = trials[i];
            trials[i] = trials[j];
            trials[j] = tmp;
        }

        return trials;
    }
}
#endif
