# LumiMix Gaussian Experiment

**Luminance Mixing Speed Perception Study with Gaussian Temporal Weighting**
**輝度混合における速度知覚とガウス時間重み付けの検討**
**亮度混合速度知觉中的高斯时间加权研究**

---

# 1. Research Background / 研究背景 / 研究背景

**English**

In teleoperation systems, communication delay causes temporal mismatch between
operator motion and visual feedback, which degrades spatial perception and task performance.
Luminance mixing between delayed viewpoints can partially compensate for this mismatch.
However, **linear temporal mixing** often produces **perceived speed fluctuation**
(non-uniform motion perception).

This project investigates whether **Gaussian temporal weighting**
can reduce perceived speed non-uniformity and improve subjective motion consistency.

---

**日本語**

遠隔操作（teleoperation）では，通信遅延により
操作者の運動と視覚フィードバックの間に時間的不整合が生じ，
空間知覚や作業効率が低下する。
遅延視点間の**輝度混合**はこの不整合を部分的に補償できるが，
**線形時間混合**では**主観的速度ムラ**が生じる可能性がある。

本研究では，**ガウス時間重み付け**により
主観的速度の非一様性を低減できるかを検証する。

---

**中文**

在远程操作系统中，通信延迟会导致
操作者运动与视觉反馈之间产生时间不一致，
从而降低空间感知能力与操作效率。
通过对延迟视点进行**亮度混合**可以部分补偿该不一致，
但**线性时间混合**常会产生**主观速度波动**。

本研究旨在验证：
**高斯时间加权**是否能够降低速度非均匀知觉并提升运动一致性。

---

# 2. Experiment Overview / 実験概要 / 实验说明

**English**

This experiment compares **Gaussian temporal weighting** with **linear mixing**.

* 3 Gaussian conditions
* 3 linear-mixing conditions
* Total: **6 randomized sequences**

Each participant generates a new configuration via:

**Tools → Generate initial trial file**

which produces a new `full_trials.json` used to run the experiment.

During the experiment:

* Base velocity **v₀** is fixed
* Participants adjust the remaining **four parameters**

Motion model:

$$
v(t)=V_0 + A_1\sin(\omega t+\phi_1) + A_2\sin(2\omega t+\phi_2)
$$

---

**日本語**

本実験では，**ガウス時間重み付け**と**線形混合**を比較する。

* ガウス条件：3 種類
* 線形混合条件：3 種類
* 合計：**6 種類のランダム系列**

各被験者は：

**Tools → Generate initial trial file**

により `full_trials.json` を生成し，その設定で実験を実施する。

実験では：

* 基準速度 **v₀** を固定
* 残り **4 パラメータ**を調整

運動モデル：

$$
v(t)=V_0 + A_1\sin(\omega t+\phi_1) + A_2\sin(2\omega t+\phi_2)
$$

---

**中文**

本实验比较**高斯时间加权**与**线性混合**两种条件。

* 3 个高斯条件
* 3 个线性混合条件
* 共 **6 条随机序列**

每位被试通过：

**Tools → Generate initial trial file**

生成新的 `full_trials.json` 并据此运行实验。

实验过程中：

* 固定基准速度 **v₀**
* 调节其余 **4 个参数**

运动模型：

$$
v(t)=V_0 + A_1\sin(\omega t+\phi_1) + A_2\sin(2\omega t+\phi_2)
$$

---

# 3. Project Structure / プロジェクト構成 / 项目结构

## Arduino

* `sketch_jan29a.ino`
  Arduino firmware for data acquisition and serial transmission to Unity.

## Unity Editor Scripts

* `CameraSpeedVisualizerEditor.cs` — visualize camera speed in editor
* `FullscreenGameView.cs` — fullscreen Game View extension
* `TrialDataGenerator.cs` — generate trial configuration

## Core Scripts (`Scripts/`)

### Camera Control

* `MoveCamera.cs` — main motion controller
* `MoveCamera.Fields.cs` — parameters and fields
* `MoveCamera.Trial.cs` — per-trial logic
* `MoveCamera2Afc.cs` — controller for 2AFC paradigm

### Data & Communication

* `SerialReader.cs` — read Arduino serial data
* `TrialTypes.cs` — trial data structures

## Configuration

* `full_trials.json` — full experimental configuration

---

# 4. How to Run / 実行方法 / 运行方法

1. Open the Unity project.
2. Connect the Arduino device (serial communication).
3. In Unity menu:
   **Tools → Generate initial trial file**
4. Load the main experimental scene.
5. Press **Play** to start the experiment.

---

# 5. Data & Analysis / データと解析 / 数据与分析

**Experimental data**
https://github.com/maeda-lab/LumiMixEnvData/tree/master/public/AAAGaussDatav0

**Analysis script**
https://github.com/maeda-lab/LumiMixEnvData/blob/master/public/py/gauss/analyze_mse_by_subject_grouped_bar.py

---

# 6. Results / 実験結果 / 实验结果

## RMSE by Participant

<p align="center">
  <img src="https://raw.githubusercontent.com/maeda-lab/LumiMixEnvData/master/public/py/gauss/mse_by_subject_grouped_bar_paper.png" width="720">
</p>

**English**

The grouped bar chart presents the **RMSE of perceived speed fluctuation**  
for each participant under **linear mixing** and **Gaussian temporal weighting**.

For the **majority of participants**, Gaussian weighting yields  
**smaller RMSE values** than linear mixing, indicating a tendency toward  
reduced perceived speed non-uniformity and improved motion stability.

---

**日本語**

本図は，各被験者における**主観的速度ムラの RMSE**を  
**線形混合**と**ガウス時間重み付け**で比較したものである。

**多くの被験者**において，ガウス条件は線形混合よりも  
**RMSE が小さい傾向**を示し，  
主観的速度非一様性の低減および運動知覚の安定化を示唆する。

---

**中文**

该图展示了各被试在**线性混合**与**高斯时间加权**条件下的  
**主观速度波动 RMSE**。

在**大多数被试**中，高斯条件表现出  
**更小的 RMSE 值**，表明其在降低速度非均匀性、  
提升运动稳定性方面具有一定趋势。


