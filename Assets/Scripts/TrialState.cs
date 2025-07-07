using System.Collections.Generic;

public static class TrialState
{
    public static List<Trial> trials = null;
    public static int currentIndex = 0;

    public static bool IsInitialized => trials != null && trials.Count > 0;

    public static void Reset()
    {
        trials = null;
        currentIndex = 0;
    }
}
