// MoveCamera.Trial.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class MoveCamera : MonoBehaviour
{
    // CN: 随机种子，用于试次顺序随机化（可在 Inspector 设置）
    // EN: Random seed used for trial-order randomization (configurable in Inspector)
    // JP: 試行順序のランダム化に使うシード（Inspectorで設定可能）
    public int subjectSeed = 0;

    /// <summary>
    /// TrailSettings
    /// CN: 读取试次配置文件，根据 progress 选择当前试次（practice 或 正式），并应用试次条件到实验参数。
    /// EN: Read trial configuration, pick current trial based on progress (practice or main), and apply trial condition to parameters.
    /// JP: 試行設定ファイルを読み込み、進捗に基づいて現在の試行（練習または本試行）を選び、試行条件を適用する。
    /// </summary>
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

        // 只随机一次，并保存顺序
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

    /// <summary>
    /// ApplyTrialCondition
    /// CN: 根据 Trial.condition 设置 brightnessBlendMode（1 => LinearOnly, 2 => GaussOnly），并打印当前设置。
    /// EN: Set brightnessBlendMode according to Trial.condition (1 => LinearOnly, 2 => GaussOnly) and log the applied setting.
    /// JP: Trial.condition に基づいて brightnessBlendMode を設定（1 => LinearOnly, 2 => GaussOnly）し、設定をログ出力する。
    /// </summary>
    private void ApplyTrialCondition(Trial t)
    {
        // condition: 1=LinearOnly, 2=GaussOnly
        switch (t.condition)
        {
            case 1:
                brightnessBlendMode = BrightnessBlendMode.LinearOnly;
                break;

            case 2:
                brightnessBlendMode = BrightnessBlendMode.GaussOnly;
                // 如果你希望 GaussOnly 时强制 sigma 固定，可以在这里写死：
                // sigmaSec = 0.6f;
                break;

            default:
                Debug.LogWarning($"Unknown condition={t.condition}, fallback to LinearOnly.");
                brightnessBlendMode = BrightnessBlendMode.LinearOnly;
                break;
        }

        Debug.Log($"[Trial] condition={t.condition} => brightnessBlendMode={brightnessBlendMode} (sigmaSec={sigmaSec}, step={secondsPerStep})");
    }


    // =========================
    // Randomize exp2_trials once and persist
    // =========================

    /// <summary>
    /// EnsureExp2TrialsRandomizedOnce
    /// CN: 如果 exp2_trials 尚未被随机化（progress 标志为 false），则基于 subjectSeed 打乱顺序并写回文件，避免每次运行再洗牌。
    /// EN: If exp2_trials haven't been randomized yet (progress flag false), shuffle them using subjectSeed and write back to file to avoid reshuffling at runtime.
    /// JP: exp2_trials がまだランダム化されていない場合（progress フラグが false）、subjectSeed を使ってシャッフルしファイルへ書き戻すことで実行時の再シャッフルを防ぐ。
    /// </summary>
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

    /// <summary>
    /// ShuffleAvoidTriple
    /// CN: 在列表上执行 Fisher-Yates 洗牌，尝试避免出现连续 3 次相同 condition（最多尝试 maxTry 次），并返回最终顺序。
    /// EN: Perform Fisher-Yates shuffle on the list, attempting to avoid any run of three identical conditions (retry up to maxTry times), and return the final order.
    /// JP: リストに対して Fisher-Yates シャッフルを行い、同一条件が3回連続する並びを避けるよう最大 maxTry 回まで再試行して最終順序を返す。
    /// </summary>
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

    /// <summary>
    /// UpdateProgress
    /// CN: 根据 currentProgress 更新保存文件中的 progress 计数并写回（用于记录已完成的试次）。
    /// EN: Increment the appropriate progress counter in the saved trial file according to currentProgress and write it back (used to record completed trials).
    /// JP: currentProgress に応じて保存ファイル内の進捗カウンタを増やして書き戻す（完了した試行の記録用）。
    /// </summary>
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
            case "exp2_trials": data.progress.exp2_trials++; break;
            case "exp1_intro_test": data.progress.exp1_intro_test++; break;
            case "exp1_trials": data.progress.exp1_trials++; break;
            default: Debug.LogWarning($"Unknown progress key: {currentProgress}"); break;
        }

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
    }
}
