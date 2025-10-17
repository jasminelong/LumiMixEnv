# Brightness Function Mixing and Perceived Velocity  
### Analysis of Linear and Nonlinear Luminance Blending for Self-Motion Perception  
（輝度混合関数と知覚速度の解析 / 辉度混合函数与感知速度分析）

---

## 🧭 Overview / 概要 / 概述

This project investigates **how luminance blending functions affect perceived self-motion speed** under different temporal modulation patterns.  
Two experiments were conducted to compare linear and nonlinear blending methods and their effects on velocity perception stability.

本プロジェクトは、**異なる時間変調下における輝度混合関数が自己運動速度知覚に与える影響**を解析することを目的としています。  
線形および非線形の混合手法を比較し、速度むら（知覚的速度の非一様性）が最小になる条件を検討しました。

本项目旨在研究**不同时间调制下的亮度混合函数如何影响自我运动速度的感知**。  
通过对比线性与非线性混合方式，探讨哪种混合方式能使主观速度变化最平滑、速度起伏（速度むら）最小。

---

## 🧪 Experiments / 実験内容 / 实验内容

### Experiment 1 – Linear Luminance Mixing (線形輝度混合)
Participants adjusted a rotary knob to match their perceived motion speed in a linear luminance-blending condition.  
The resulting velocity curve was modeled as:

$$
v(t)=V_0 + A_1\sin(\omega t+\phi_1) + A_2\sin(2\omega t+\phi_2)
$$

被験者は、線形輝度混合条件において**回転ノブ**を操作し、主観的に等価な速度になるように調整しました。  
得られた速度曲線は以下の式で表されます：

$$
v(t)=V_0 + A_1\sin(\omega t+\phi_1) + A_2\sin(2\omega t+\phi_2)
$$

实验1中，被试在**线性亮度混合**条件下，通过旋钮调节速度，使上下两段视觉刺激的主观速度一致。  
所得感知速度曲线符合下式：

\[
$$
v(t)=V_0 + A_1\sin(\omega t+\phi_1) + A_2\sin(2\omega t+\phi_2)
$$


📂 Data: [BrightnessFunctionMixAndPhaseData](https://github.com/jasminelong/expDataHub/tree/8e72e8e9680dc8ba884980344c53c79b2c80cd93/public/BrightnessFunctionMixAndPhaseData)  
📊 Analysis script: [velocity_curve_linear_only_analysis.py](https://github.com/jasminelong/ExpDataHub/blob/9f55e3aadcab465175a3e1026faf0711b0bee1c3/public/velocity_curve_analysis/velocity_curve_linear_only_analysis.py)  
📈 Result:  
![Linear Velocity Curve](https://github.com/jasminelong/ExpDataHub/blob/090d690b3767d53ee4a7fb5797df1f32f3e8ca63/public/velocity_curve_analysis/velocity_curves_linear_only_mean_background_opaque.png?raw=true)

---

### Experiment 2 – Function Mixing (関数混合)
In the second experiment, three base functions were combined — **cosine**, **linear**, and **arccosine** — to explore which blending function minimizes perceived velocity fluctuation (“速度むら”).  
Each function represents a different nonlinear luminance interpolation model.

第2実験では、**cosine・linear・arccosine** の3種類の基本関数を組み合わせ、  
どの関数混合が最も速度むらを抑制できるかを検討しました。  
各関数は異なる非線形輝度補間モデルを表します。

实验2中，使用三种基础函数（**cosine、linear、arccos**）进行混合，  
以探讨哪种混合方式能最有效地减小主观速度波动（“速度むら”）。  
每种函数代表不同类型的非线性亮度插值模型。

📊 Analysis script: [function_mix_analysis.py](https://github.com/jasminelong/ExpDataHub/blob/9f55e3aadcab465175a3e1026faf0711b0bee1c3/public/velocity_curve_analysis/function_mix_analysis.py)  
📈 Result: ![Function Mix Analysis](https://github.com/jasminelong/ExpDataHub/blob/9f55e3aadcab465175a3e1026faf0711b0bee1c3/public/velocity_curve_analysis/function_mix_analysis.png?raw=true)

---

## 🧩 Findings / 結果概要 / 实验结果

- Linear mixing produced consistent yet slightly biased perceived speed curves, characterized by strong first-harmonic dominance.  
- Nonlinear blending (especially cosine-weighted) reduced high-frequency fluctuations, indicating smoother motion perception.  
- Arccos-based blending, while nonlinear, sometimes amplified low-frequency distortions.

線形混合では一貫した速度曲線が得られたが、一次成分の卓越が見られました。  
一方、非線形混合（特に cosine 加重）は高調波成分を抑制し、より滑らかな運動知覚を示しました。  
arccos 型は非線形ではあるものの、低周波ゆらぎが強調される傾向がありました。

线性混合下的感知速度曲线较稳定，但一阶分量（基本频率）较强；  
非线性混合（尤其是 cosine 加权）显著减弱了高频起伏，使速度感更平滑；  
arccos 混合虽为非线性，但在部分条件下放大了低频波动。

---

## ⚙️ Environment / 実験環境 / 实验环境

- **Platform:** Unity 2022.3 (C#)
- **Display:** 34-inch curved ultra-wide monitor (3440×1440, 3800R)
- **Input Device:** Rotary knob controller (for continuous velocity adjustment)
- **Task:** Match the perceived motion speed between two vertically displayed stimuli (upper: adjustable, lower: reference blending)

---

## 📁 Repository Structure / フォルダ構成 / 文件结构
