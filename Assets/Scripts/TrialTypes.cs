using System.Collections.Generic;

[System.Serializable]
public class Trial
{
    public int condition;
    public int repetition;
}

[System.Serializable]
public class Progress
{
    public int exp1_intro_test = 0;
    public int exp1_trials = 0;
    public int exp2_intro_test = 0;
    public int exp2_trials = 0;
}

[System.Serializable]
public class ExperimentData
{
    public List<Trial> exp1_intro_test;
    public List<Trial> exp1_trials;
    public List<Trial> exp2_intro_test;
    public List<Trial> exp2_trials;
    public Progress progress;
}
[System.Serializable]
public class TrialBlock
{
    public List<Trial> trials = new List<Trial>();
    public int currentIndex = 0;  // 当前进行到第几个试次
}
