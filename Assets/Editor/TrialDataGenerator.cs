#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class TrialDataGenerator
{
    [MenuItem("Tools/Generate initial trial file")]
    public static void GenerateInitialTrialFile()
    {
        string savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");
        
        // 每次生成都重新随机一个顺序（用当前时间做 seed）
        int seed = Environment.TickCount;

        ExperimentData data = new ExperimentData
        {
            exp1_intro_test = new List<Trial>(),
            exp1_trials = new List<Trial>(),

            // practice：linear 1 次 + gauss 1 次（可按需删减）
            exp2_intro_test = new List<Trial> {
                new Trial { condition = 1, repetition = 1 }
            },

            // ✅ 正式：2条件×3次
            exp2_trials = GenerateExp2Trials_2Cond_3Reps(seed),

            // ✅ 重置进度；并标记已经随机过（避免运行时再 shuffle）
            progress = new Progress {
                exp1_intro_test = 0,
                exp1_trials = 0,
                exp2_intro_test = 0,
                exp2_trials = 0,
                exp2_trials_randomized = true
            }
        };

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
        Debug.Log($"Generated trial file (OVERWRITE): {savePath}");
        Debug.Log($"Seed={seed}, Order={string.Join(",", data.exp2_trials.Select(t => t.condition))}");
    }

    private static List<Trial> GenerateExp2Trials_2Cond_3Reps(int seed)
    {
        var trials = new List<Trial>();
        for (int rep = 1; rep <= 3; rep++) trials.Add(new Trial { condition = 1, repetition = rep });
        for (int rep = 1; rep <= 3; rep++) trials.Add(new Trial { condition = 2, repetition = rep });
        return ShuffleAvoidTriple(trials, seed);
    }

    private static List<Trial> ShuffleAvoidTriple(List<Trial> src, int seed, int maxTry = 200)
    {
        var rng = new System.Random(seed);
        var arr = src.ToList();

        bool HasTriple(List<Trial> a)
        {
            for (int i = 2; i < a.Count; i++)
                if (a[i-2].condition == a[i-1].condition && a[i-1].condition == a[i].condition)
                    return true;
            return false;
        }

        void ShuffleInPlace(List<Trial> a)
        {
            for (int i = a.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        for (int k = 0; k < maxTry; k++)
        {
            ShuffleInPlace(arr);
            if (!HasTriple(arr)) return arr;
        }
        return arr;
    }
}
#endif
