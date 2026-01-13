// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;

// public class ExperimentManager : MonoBehaviour
// {
//     private string savePath;
//     private TrialBlock trialBlock;

//     void Awake()
//     {
//         savePath = Path.Combine(Application.dataPath, "Scripts/full_trials.json");

//         if (File.Exists(savePath))
//         {
//             string json = File.ReadAllText(savePath);
//             trialBlock = JsonUtility.FromJson<TrialBlock>(json);
//             Debug.Log("已加载保存的试次顺序");
//         }
//         else
//         {
//             trialBlock = GenerateRandomTrials();
//             SaveTrialBlock();
//             Debug.Log("生成并保存了新顺序");
//         }
//     }

//     void Start()
//     {
//         if (trialBlock.currentIndex >= trialBlock.trials.Count)
//         {
//             Debug.Log(" 实验全部完成！");
//             return;
//         }

//         Trial currentTrial = trialBlock.trials[trialBlock.currentIndex];
//         Debug.Log($" 当前试次：条件 = {currentTrial.condition}, 重复 = {currentTrial.repetition}");

//         // TODO: 在这里调用你实际的实验逻辑，例如切换刺激、初始化状态等
//     }

//     public void MarkTrialCompleted()
//     {
//         trialBlock.currentIndex++;
//         SaveTrialBlock();
//     }

//     void SaveTrialBlock()
//     {
//         string json = JsonUtility.ToJson(trialBlock, true);
//         File.WriteAllText(savePath, json);
//     }

//     TrialBlock GenerateRandomTrials()
//     {
//         TrialBlock block = new TrialBlock();
//         for (int condition = 0; condition < 3; condition++)
//         {
//             for (int rep = 0; rep < 3; rep++)
//             {
//                 block.trials.Add(new Trial { condition = condition, repetition = rep });
//             }
//         }

//         // 洗牌
//         for (int i = block.trials.Count - 1; i > 0; i--)
//         {
//             int j = Random.Range(0, i + 1);
//             (block.trials[i], block.trials[j]) = (block.trials[j], block.trials[i]);
//         }

//         return block;
//     }
// }
