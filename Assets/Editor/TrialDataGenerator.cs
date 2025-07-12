#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class TrialDataGenerator
{
    [MenuItem("Tools/ Generate initial trail file")]
    public static void GenerateInitialTrialFile()
    {
        ExperimentData data = new ExperimentData
        {
            // exp1_intro_test = new List<Trial> { new Trial { condition = 1, repetition = 1 } },

            // exp1_trials = new List<Trial>
            // {
            //     new Trial { condition = 1, repetition = 1 },
            //     new Trial { condition = 1, repetition = 2 },
            //     new Trial { condition = 1, repetition = 3 }
            // },

            exp2_intro_test = new List<Trial> { new Trial { condition = 1, repetition = 1 } },
            exp2_trials = GenerateRandomExp2Trials(),

            progress = new Progress()
        };
        string savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"Generate initial trail file: {savePath}");
    }

    private static List<Trial> GenerateRandomExp2Trials()
    {
        List<Trial> trials = new List<Trial>();

        for (int cond = 1; cond <= 2; cond++)
            for (int rep = 1; rep <= 3; rep++)
                trials.Add(new Trial { condition = cond, repetition = rep });

        // 洗牌
        for (int i = trials.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (trials[i], trials[j]) = (trials[j], trials[i]);
        }

        return trials;
    }
}
#endif
