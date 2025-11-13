// MoveCamera.Trial.cs
using System.IO;
using UnityEngine;

public partial class MoveCamera : MonoBehaviour
{
    public void TrailSettings()
    {
        string json = File.ReadAllText(savePath);
        ExperimentData data = JsonUtility.FromJson<ExperimentData>(json);

        Trial currentTrial = null;

        if (data.progress.exp1_intro_test < data.exp1_intro_test.Count)
        {
            currentTrial = data.exp1_intro_test[data.progress.exp1_intro_test];
            currentProgress = "exp1_intro_test";
            Debug.Log("Now exp1_intro_test");

            devMode = DevMode.Test;
            experimentPattern = ExperimentPattern.FunctionMix;
            brightnessBlendMode = BrightnessBlendMode.Dynamic;
        }
        else if (data.progress.exp1_trials < data.exp1_trials.Count)
        {
            currentTrial = data.exp1_trials[data.progress.exp1_trials];
            currentProgress = "exp1_trials";
            Debug.Log("Now exp1_trials");

            devMode = DevMode.FunctionRation;
            experimentPattern = ExperimentPattern.FunctionMix;
            brightnessBlendMode = BrightnessBlendMode.Dynamic;

            if (data.progress.exp1_trials + 1 == data.exp1_trials.Count &&
                data.exp2_intro_test.Count == 0 && data.exp2_trials.Count == 0)
            {
                isEnd = true;
            }
        }
        else if (data.progress.exp2_intro_test < data.exp2_intro_test.Count)
        {
            currentTrial = data.exp2_intro_test[data.progress.exp2_intro_test];
            currentProgress = "exp2_intro_test";
            Debug.Log("Now exp2_intro_test");

            devMode = DevMode.Test;
            experimentPattern = ExperimentPattern.NoLuminanceBlend;
            // experimentPattern = ExperimentPattern.Phase;
            brightnessBlendMode = BrightnessBlendMode.LinearOnly;
        }
        else if (data.progress.exp2_trials < data.exp2_trials.Count)
        {
            currentTrial = data.exp2_trials[data.progress.exp2_trials];
            currentProgress = "exp2_trials";
            Debug.Log("Now exp2_trials");

            devMode = DevMode.Normal;
            experimentPattern = ExperimentPattern.NoLuminanceBlend;
            // experimentPattern = ExperimentPattern.Phase;
            switch (currentTrial.condition)
            {
                case 1: brightnessBlendMode = BrightnessBlendMode.LinearOnly; break;
                case 2: brightnessBlendMode = BrightnessBlendMode.Dynamic;    break;
            }
            if (data.progress.exp2_trials + 1 == data.exp2_trials.Count)
            {
                isEnd = true;
            }
        }
        else
        {
            Debug.Log("finished all trials");
            return;
        }

        trialNumber = currentTrial.repetition;
    }
}
