# LumiMixEnv

**Unity Experimental Environment for Psychophysical Motion Perception in Luminance-Mixed Imagery**
**輝度混合映像における運動知覚の心理物理実験環境**
**亮度混合视觉中的运动知觉心理物理实验环境**

---

# 1. Overview｜概要｜项目概述

**EN**
LumiMixEnv is a Unity-based experimental platform for studying
**perceived motion speed and speed non-uniformity (速度ムラ)**
in **luminance-mixed teleoperation imagery**.
The project investigates how different temporal weighting strategies
(**linear vs. Gaussian**) influence subjective motion stability.

**JP**
LumiMixEnv は，**輝度混合遠隔視覚**における
**主観的運動速度および速度ムラ**を研究するための
Unity ベース心理物理実験環境である。
時間重み付け（線形／ガウス）が運動知覚の安定性に
与える影響を検証する。

**ZH**
LumiMixEnv 是一个基于 Unity 的心理物理实验平台，
用于研究**亮度混合远程视觉中的主观运动速度与速度非均匀性（速度ムラ）**，
重点探讨**线性时间混合与高斯时间加权**对运动稳定性的影响。

---

# 2. Experimental Structure｜実験構成｜实验结构

## Experiment 1 — Linear Mixing

**Branch:** `brightness_function_mixing`

* **EN:** Tests whether motion perceived under **linear luminance mixing**
  is subjectively **uniform** or exhibits fluctuation.
* **JP:** 線形輝度混合における運動知覚が
  **等速に感じられるか**を検証する。
* **ZH:** 验证**线性亮度混合**下的速度知觉是否为**匀速**。

🔗
https://github.com/jasminelong/LumiMixEnv/tree/brightness_function_mixing

---

## Experiment 2 — Gaussian Temporal Weighting

**Branch:** `Gauss`

* **EN:** Examines whether **Gaussian temporal weighting**
  reduces **perceived speed fluctuation (速度ムラ)**
  compared with linear mixing.
* **JP:** ガウス時間重み付けが
  **速度ムラ低減**に有効かを検証する。
* **ZH:** 验证**高斯时间加权**是否能减少**速度波动**。

🔗
https://github.com/jasminelong/LumiMixEnv/tree/Gauss

---

# 3. Contrast Analysis Code (Independent of Experiments 1 & 2)｜コントラスト解析コード｜对比度分析代码

Contrast-related analysis scripts used in Gaussian experiments:

https://github.com/maeda-lab/LumiMixEnvData/tree/master/public/py/gauss

---

# 4. Arduino Setup｜Arduino設定｜Arduino使用说明

## Hardware｜ハードウェア｜硬件

* **M5Stack-U005**
* **M5Stack-CPLUS 1.1**

---

## Arduino IDE Configuration

Arduino IDE 設定｜Arduino IDE 配置

**Board:**

```
M5Core2
```

**Port:**
Select the serial port corresponding to your computer environment.

（JP）使用する PC に対応するシリアルポートを選択する。
（ZH）根据自己电脑选择对应串口。

---

## Firmware Upload｜書き込み手順｜固件烧录

Upload the following sketch to the device:

https://github.com/jasminelong/LumiMixEnv/blob/brightness_function_mixing/Assets/arduino/sketch_jan29a.ino

**Steps**

1. Open Arduino IDE.
2. Set **Board = M5Core2**.
3. Select the correct **Port**.
4. Open `sketch_jan29a.ino`.
5. Upload to the M5Stack device.

---

# 5. Psychophysics Reading Path｜読書ガイド｜阅读路线

## Introductory Conceptual Foundation

**Sensation and Perception (10th Ed.) — Goldstein & Brockmole**

* **EN:** Provides a **systematic conceptual overview** of sensation and perception,
  helping situate psychophysical research questions within the broader field.
* **JP:** 感覚・知覚研究の**全体像を体系的に理解するための入門書**。
* **ZH:** 用于建立感觉与知觉研究**整体框架认知**的基础教材。

➡ **Role:** theoretical orientation rather than experimental methodology.

---

## Core Methodology (Most Important)

**Psychophysics (2nd Ed.) — Kingdom & Prins**

* **EN:** Explains **how psychophysical experiments are actually designed, conducted,
  analyzed, and interpreted**, forming the **primary methodological foundation**
  for perception research.
* **JP:** 心理物理実験を**設計・実施・解析・解釈するための中核的方法論書**。
* **ZH:** 系统讲解心理物理实验**从设计到数据分析与解释**的全过程，
  是开展实验研究的**最核心方法论基础**。

➡ **Role:** indispensable practical guide for conducting real psychophysical studies.

---

## Advanced Theoretical Connection

**Visual Psychophysics — Lu & Dosher**

* **EN:** Connects **experimental measurements** with
  **computational and theoretical observer models**,
  enabling interpretation beyond descriptive results.
* **JP:** 実験結果を**理論的観測者モデル**へ結びつける発展的内容。
* **ZH:** 将实验测量结果提升到**视觉计算与理论模型解释层面**的进阶著作。

➡ **Role:** transition from **experimental execution → theoretical interpretation**.



---

# 6. Related Learning Links｜関連リンク｜相关链接

### Binocular Disparity & Depth

https://demonstrations.wolfram.com/BinocularDisparityVisualDepthPerception7/

### Cerebral Cortex & Visual Neuroscience

https://www.akira3132.info/cerebral_cortex.html#%E5%81%B4%E9%A0%AD%E9%80%A3%E5%90%88%E9%87%8E

### Visual Illusion / Perception Lab

https://www.ritsumei.ac.jp/~akitaoka/index-j.html

---

