using System.Collections.Generic;

[System.Serializable]
public class Trial
{
    // CN: 试次条件编号（例如 1 = LinearOnly, 2 = GaussOnly）
    // EN: Condition id for the trial (e.g. 1 = LinearOnly, 2 = GaussOnly)
    // JP: 試行の条件ID（例: 1 = LinearOnly, 2 = GaussOnly）
    public int condition;

    // CN: 重复/序号（该条件下的重复次数或序号）
    // EN: Repetition/index (repeat count or index within the condition)
    // JP: 繰り返し/インデックス（条件ごとの繰返し回数またはインデックス）
    public int repetition;
}

[System.Serializable]
public class Progress
{
    // CN: 各子实验/阶段已完成的试次计数（用于记录进度）
    // EN: Counters of completed trials for each sub-experiment/phase (used to track progress)
    // JP: サブ実験／フェーズごとの完了した試行数（進捗管理用）

    public int exp1_intro_test = 0;
    public int exp1_trials = 0;
    public int exp2_intro_test = 0;
    public int exp2_trials = 0;

    // CN: 标记 exp2_trials 是否已被随机化并写回文件（避免重复随机化）
    // EN: Flag indicating whether exp2_trials has been randomized and persisted (to avoid reshuffling)
    // JP: exp2_trials が既にランダム化されファイルに書き戻されたかを示すフラグ（再シャッフル回避）
    public bool exp2_trials_randomized = false;
}

[System.Serializable]
public class ExperimentData
{
    // CN: 各阶段的试次列表（可包含 practice/introduction 与正式试次）
    // EN: Trial lists for each phase (may include practice/intro and main trials)
    // JP: 各フェーズの試行リスト（導入/練習と本試行などを含む可能性あり）
    public List<Trial> exp1_intro_test;
    public List<Trial> exp1_trials;
    public List<Trial> exp2_intro_test;
    public List<Trial> exp2_trials;

    // CN: 记录进度与随机化标记
    // EN: Progress counters and randomization flags
    // JP: 進捗カウンタとランダム化フラグ
    public Progress progress;
}

[System.Serializable]
public class TrialBlock
{
    // CN: 试次块内的试次数组（便于分块管理）
    // EN: List of trials inside a trial block (useful for block-wise management)
    // JP: 試行ブロック内の試行リスト（ブロック単位での管理に便利）

    public List<Trial> trials = new List<Trial>();

    // CN: 当前块中进行到的试次索引（从 0 开始）
    // EN: Current index within the block (0-based)
    // JP: ブロック内の現在の試行インデックス（0始まり）
    public int currentIndex = 0;  // 当前进行到第几个试次
}
