// MoveCamera.Trial.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class MoveCamera : MonoBehaviour
{
    // ====== Randomization ======
    [Header("Randomization")]
    public int subjectSeed = 12345;

    // ====== Mixing mode (NO ExperimentPattern) ======
    public enum MixMode { Linear = 1, Gauss = 2 }

    [Header("Mix Parameters")]
    public MixMode mixMode = MixMode.Linear;

    public void TrailSettings()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogError($"Trial file not found: {savePath}");
            return;
        }

        string json = File.ReadAllText(savePath);
        ExperimentData data = JsonUtility.FromJson<ExperimentData>(json);
        if (data == null || data.progress == null)
        {
            Debug.LogError("Failed to parse trial file or progress is null.");
            return;
        }

        // ✅ 只随机一次，并保存顺序
        EnsureExp2TrialsRandomizedOnce(data);

        Trial currentTrial = null;

        // 1) Practice
        if (data.exp2_intro_test != null && data.progress.exp2_intro_test < data.exp2_intro_test.Count)
        {
            currentTrial = data.exp2_intro_test[data.progress.exp2_intro_test];
            currentProgress = "exp2_intro_test";
            devMode = DevMode.Test;
            Debug.Log("Now exp2_intro_test (practice)");
        }
        // 2) Real trials
        else if (data.exp2_trials != null && data.progress.exp2_trials < data.exp2_trials.Count)
        {
            currentTrial = data.exp2_trials[data.progress.exp2_trials];
            currentProgress = "exp2_trials";
            devMode = DevMode.Normal;
            Debug.Log("Now exp2_trials");
        }
        else
        {
            Debug.Log("Finished all trials or no trials defined.");
            return;
        }

        if (currentTrial == null)
        {
            Debug.LogError("currentTrial is null.");
            return;
        }

        // ✅ 不再设置 experimentPattern，只设置混合模式/参数
        ApplyTrialCondition(currentTrial);

        trialNumber = currentTrial.repetition;

        // End flag
        if (currentProgress == "exp2_intro_test")
        {
            if (data.progress.exp2_intro_test + 1 == data.exp2_intro_test.Count &&
                (data.exp2_trials == null || data.exp2_trials.Count == 0))
                isEnd = true;
        }
        else if (currentProgress == "exp2_trials")
        {
            if (data.progress.exp2_trials + 1 == data.exp2_trials.Count)
                isEnd = true;
        }
    }

    private void ApplyTrialCondition(Trial t)
    {
        // 1=linear, 2=gauss
        if (t.condition == 1)
        {
            mixMode = MixMode.Linear;
            // sigmaSec 可以保持不变也可以无视
        }
        else if (t.condition == 2)
        {
            mixMode = MixMode.Gauss;
            // sigmaSec = 0.6f;  // 若你想强制固定，可在这里写死
        }
        else
        {
            Debug.LogWarning($"Unknown condition={t.condition}, fallback to Linear.");
            mixMode = MixMode.Linear;
        }

        Debug.Log($"[Trial] condition={t.condition} => mixMode={mixMode} (sigmaSec={sigmaSec}, step={secondsPerStep})");
    }

    // =========================
    // Randomize exp2_trials once and persist
    // =========================
    private void EnsureExp2TrialsRandomizedOnce(ExperimentData data)
    {
        if (data.exp2_trials == null || data.exp2_trials.Count == 0) return;

        if (data.progress.exp2_trials_randomized) return;
        if (data.progress.exp2_trials != 0) return;

        data.exp2_trials = ShuffleAvoidTriple(data.exp2_trials, subjectSeed, maxTry: 200);
        data.progress.exp2_trials_randomized = true;

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));

        Debug.Log("[Randomize] exp2_trials randomized once and saved.");
        Debug.Log("[Randomize] Order: " + string.Join(",", data.exp2_trials.Select(x => x.condition)));
    }

    private List<Trial> ShuffleAvoidTriple(List<Trial> src, int seed, int maxTry = 200)
    {
        var rng = new System.Random(seed);
        var arr = src.ToList();

        bool HasTriple(List<Trial> a)
        {
            for (int i = 2; i < a.Count; i++)
            {
                int c0 = a[i - 2].condition;
                int c1 = a[i - 1].condition;
                int c2 = a[i].condition;
                if (c0 == c1 && c1 == c2) return true;
            }
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

        Debug.LogWarning("[Randomize] Could not satisfy no-triple constraint. Using last shuffle.");
        return arr;
    }

    // =========================
    // Progress update
    // =========================
    public void UpdateProgress()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogError($"Trial file not found: {savePath}");
            return;
        }

        var data = JsonUtility.FromJson<ExperimentData>(File.ReadAllText(savePath));
        if (data == null || data.progress == null)
        {
            Debug.LogError("Failed to parse trial file or progress is null.");
            return;
        }

        switch (currentProgress)
        {
            case "exp2_intro_test": data.progress.exp2_intro_test++; break;
            case "exp2_trials":     data.progress.exp2_trials++;     break;
            case "exp1_intro_test": data.progress.exp1_intro_test++; break;
            case "exp1_trials":     data.progress.exp1_trials++;     break;
            default: Debug.LogWarning($"Unknown progress key: {currentProgress}"); break;
        }

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
    }
}
