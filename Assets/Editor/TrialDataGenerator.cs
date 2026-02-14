#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// TrialDataGenerator 工具（Editor only）
/// CN: 在 Unity Editor 的 Tools 菜单下生成初始试次配置文件（full_trials.json）。
/// EN: Editor utility that generates an initial trial configuration file (full_trials.json) under Tools menu.
/// JP: Unity Editor の Tools メニューから初期試行設定ファイル（full_trials.json）を生成するエディタユーティリティ。
/// </summary>
public static class TrialDataGenerator
{
    /// <summary>
    /// GenerateInitialTrialFile
    /// CN: 生成并覆盖保存路径下的 full_trials.json，包含 practice（引导）与正式试次，并记录随机 seed 与顺序。
    /// EN: Generate and overwrite full_trials.json with practice and main trials; logs random seed and order.
    /// JP: full_trials.json を生成・上書きし、導入試行と本試行を含め、乱数シードと順序をログ出力する。
    /// </summary>
    [MenuItem("Tools/Generate initial trial file")]
    public static void GenerateInitialTrialFile()
    {
        string savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");
        
        // CN: 使用当前时间（TickCount）作为随机种子以每次生成不同顺序
        // EN: Use Environment.TickCount as seed so each generation gets a different randomized order
        // JP: Environment.TickCount をシードにして毎回異なる順序を生成
        int seed = Environment.TickCount;

        ExperimentData data = new ExperimentData
        {
            exp1_intro_test = new List<Trial>(),
            exp1_trials = new List<Trial>(),

            // CN: practice：先播放 linear 一次，再播放 gauss 一次（可按需调整）
            // EN: practice: one linear then one gauss (adjustable)
            // JP: 練習: linear を1回、gauss を1回（必要に応じて調整可能）
            exp2_intro_test = new List<Trial> {
                new Trial { condition = 2, repetition = 1 },
                new Trial { condition = 1, repetition = 1 }
            },

            // CN: 正式试次：两种条件各 3 次（函数 GenerateExp2Trials_2Cond_3Reps 负责生成与洗牌）
            // EN: Main trials: 2 conditions × 3 reps (generated & shuffled by helper)
            // JP: 本試行: 2 条件 × 3 回（ヘルパーで生成・シャッフル）
            exp2_trials = GenerateExp2Trials_2Cond_3Reps(seed),

            // CN: 进度置零，并标记 exp2_trials 已经被随机化（避免运行时再 shuffle）
            // EN: Reset progress and flag that exp2_trials is randomized to avoid runtime reshuffle
            // JP: 進捗をリセットし、exp2_trials が既にランダム化済みであることをフラグ
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

    /// <summary>
    /// GenerateExp2Trials_2Cond_3Reps
    /// CN: 为 exp2 生成 2 条件各 3 次的试次列表并返回经过避免连续三次相同条件的随机排列。
    /// EN: Build a list with 2 conditions × 3 reps, then shuffle avoiding triples.
    /// JP: 2 条件 × 3 回の試行リストを生成し、連続3回同一条件を避けるシャッフルを行う。
    /// </summary>
    private static List<Trial> GenerateExp2Trials_2Cond_3Reps(int seed)
    {
        var trials = new List<Trial>();
        for (int rep = 1; rep <= 3; rep++) trials.Add(new Trial { condition = 1, repetition = rep });
        for (int rep = 1; rep <= 3; rep++) trials.Add(new Trial { condition = 2, repetition = rep });
        return ShuffleAvoidTriple(trials, seed);
    }

    /// <summary>
    /// ShuffleAvoidTriple
    /// CN: 将列表就地洗牌（Fisher-Yates），最多尝试 maxTry 次以避免出现连续三个相同 condition 的情况。
    /// EN: Shuffle in-place (Fisher-Yates) and retry up to maxTry times to avoid any run of three identical conditions.
    /// JP: 配列をインプレースでシャッフル（Fisher-Yates）、最大 maxTry 回再試行して3連続同一条件を避ける。
    /// </summary>
    private static List<Trial> ShuffleAvoidTriple(List<Trial> src, int seed, int maxTry = 200)
    {
        var rng = new System.Random(seed);
        var arr = src.ToList();

        // CN: 检查序列中是否存在连续三项 condition 相同的情况
        // EN: Check whether the sequence contains any triple with same condition
        // JP: シーケンスに同一条件が3回連続する箇所があるかチェック
        bool HasTriple(List<Trial> a)
        {
            for (int i = 2; i < a.Count; i++)
                if (a[i-2].condition == a[i-1].condition && a[i-1].condition == a[i].condition)
                    return true;
            return false;
        }

        // CN: Fisher-Yates 随机置换实现（原地）
        // EN: Fisher-Yates in-place shuffle implementation
        // JP: Fisher-Yates のインプレースシャッフル実装
        void ShuffleInPlace(List<Trial> a)
        {
            for (int i = a.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        // CN: 最多尝试 maxTry 次，若找到不含三连的排列则返回
        // EN: Try up to maxTry times and return arrangement without triples if found
        // JP: 最大 maxTry 回試行し、3連が無い並びが見つかれば返す
        for (int k = 0; k < maxTry; k++)
        {
            ShuffleInPlace(arr);
            if (!HasTriple(arr)) return arr;
        }
        // CN: 超过尝试次数仍未找到则返回最后结果（降级处理）
        // EN: If no valid arrangement found after retries, return the last result (best-effort)
        // JP: 試行回数を超えても見つからなければ最後の結果を返す（ベストエフォート）
        return arr;
    }
}
#endif
