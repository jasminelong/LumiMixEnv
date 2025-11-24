// MoveCamera.Trial.cs
using System.IO;
using UnityEngine;

public partial class MoveCamera : MonoBehaviour
{
    public void TrailSettings()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogError($"Trial file not found: {savePath}");
            return;
        }

        string json = File.ReadAllText(savePath);
        ExperimentData data = JsonUtility.FromJson<ExperimentData>(json);
        if (data == null)
        {
            Debug.LogError("Failed to parse trial file.");
            return;
        }

        Trial currentTrial = null;

        // 1) Practice: exp2_intro_test (LuminanceLinearMix) — always first if present
        if (data.exp2_intro_test != null && data.progress.exp2_intro_test < data.exp2_intro_test.Count)
        {
            currentTrial = data.exp2_intro_test[data.progress.exp2_intro_test];
            currentProgress = "exp2_intro_test";
            Debug.Log("Now exp2_intro_test (practice)");

            devMode = DevMode.Test;
            experimentPattern = ExperimentPattern.LuminanceLinearMix;
            brightnessBlendMode = BrightnessBlendMode.LinearOnly;

            // If this is the last item and no real trials exist, mark end
            if (data.progress.exp2_intro_test + 1 == data.exp2_intro_test.Count &&
                (data.exp2_trials == null || data.exp2_trials.Count == 0))
            {
                isEnd = true;
            }
        }
        // 2) Experimental trials: exp2_trials (randomized other conditions)
        else if (data.exp2_trials != null && data.progress.exp2_trials < data.exp2_trials.Count)
        {
            currentTrial = data.exp2_trials[data.progress.exp2_trials];
            currentProgress = "exp2_trials";
            Debug.Log("Now exp2_trials");

            devMode = DevMode.Normal;

            // Map condition number to ExperimentPattern / brightness mode
            // 1 = LuminanceLinearMix (should not normally appear here if used as practice)
            // 2 = CameraJumpMoveMinusCompensate
            // 3 = CameraJumpMovePlusCompensate
            // 4 = LuminanceMinusCompensate
            // 5 = LuminancePlusCompensate
            switch (currentTrial.condition)
            {
                case 1:
                    experimentPattern = ExperimentPattern.LuminanceLinearMix;
                    brightnessBlendMode = BrightnessBlendMode.LinearOnly;
                    break;
                case 2:
                    experimentPattern = ExperimentPattern.CameraJumpMoveMinusCompensate;
                    brightnessBlendMode = BrightnessBlendMode.Dynamic;
                    break;
                case 3:
                    experimentPattern = ExperimentPattern.CameraJumpMovePlusCompensate;
                    brightnessBlendMode = BrightnessBlendMode.Dynamic;
                    break;
                case 4:
                    experimentPattern = ExperimentPattern.LuminanceMinusCompensate;
                    brightnessBlendMode = BrightnessBlendMode.LinearOnly;
                    break;
                case 5:
                    experimentPattern = ExperimentPattern.LuminancePlusCompensate;
                    brightnessBlendMode = BrightnessBlendMode.LinearOnly;
                    break;
                default:
                    // safe fallback
                    experimentPattern = ExperimentPattern.LuminanceLinearMix;
                    brightnessBlendMode = BrightnessBlendMode.LinearOnly;
                    break;
            }

            // Mark end if this is the last experimental trial
            if (data.progress.exp2_trials + 1 == data.exp2_trials.Count)
            {
                isEnd = true;
            }
        }
        else
        {
            Debug.Log("Finished all trials or no trials defined.");
            return;
        }

        // apply trial-specific repetition number
        if (currentTrial != null)
            trialNumber = currentTrial.repetition;
    }

    public void UpdateProgress()
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

    switch (currentProgress)
    {
        case "exp1_intro_test":
            data.progress.exp1_intro_test++;
            break;
        case "exp1_trials":
            data.progress.exp1_trials++;
            break;
        case "exp2_intro_test":
            data.progress.exp2_intro_test++;
            break;
        case "exp2_trials":
            data.progress.exp2_trials++;
            break;
        default:
            Debug.LogWarning($"Unknown progress key: {currentProgress}");
            break;
    }

    // 保存
    string updatedJson = JsonUtility.ToJson(data, true);
    File.WriteAllText(savePath, updatedJson);
}

}
