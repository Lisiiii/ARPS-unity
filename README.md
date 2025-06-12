# ARPS-unity 
> 🕹 本仓库为RoboMaster机器人竞赛相关.

> 🛠 项目正在开发中，功能并不完善.
> ~考研去了，暂时不会更新了~

### 基于Unity的单目雷达站定位方案 （开发中）

Automatic Radar Positioning System based on Unity

---

- 5.22 - Update

  添加多相机支持

- 🎈 预览

<image src="https://github.com/user-attachments/assets/93acbfde-9e41-4c4a-beae-6dc43a1b6c7d" align="center" height="200"/>
<image src="https://github.com/user-attachments/assets/f7f4631d-283d-46de-9c2b-d43f0f4e53dc" align="center" height="200"/>

借助Unity的跨平台特性，使用任意单目相机和任意系统（Windows、Linux、Android）运行本方案，即可实现雷达站的所有功能。

- 🏅 已有功能
  - 单目相机读取
    
  - 机器人目标和装甲板分类识别（基于Unity的InferenceEngine和Ultralytics的Yolov8_onnx）

  - 多相机同时读取和识别
    
  - 机器人精确坐标定位
    
  - 裁判系统串口双向收发
 
  - 自主发动双倍易伤
    
  - 全局自定义的console和日志记录
    
  - 数据的UI展示
    
- 🛠 TODO: 
  - 写入/读取配置参数
    
  - 相机自动标定
    
  - UI完善

- 📢 介绍

  不同于使用较为广泛的PnP方案和双目相机/多相机+激光雷达联合标定方案，我采用了成本更低，不需要进行测距，更贴近符合人脑思维的定位方式。

  我们的大脑能够很容易的根据雷达相机的二维画面自动拟合机器人具体位置，这是因为我们其实在脑内根据场地的建模进行了一次碰撞检测，由于官方每年都会开源场地的高精度模型，我们可以利用游戏引擎的强大能力模仿这一过程。按照这个思路，我设计出了这一套单目相机+游戏引擎的定位方案。
  如今为了摆脱ROS2的依赖和如同史山般的神经网络识别，我将所有功能迁移至Unity完成，希望最终打包出一个全平台的简单易用，扩展性良好的雷达站程序。

  队伍之前并无雷达传承，而雷达程序又只有我一个人完成，本程序很多地方都不完善,更多的是希望能提供一些新思路。

- 🎯 效果展示
  
  - 在RM2024中部分区赛中，雷达标记准确度排名第二
![image](https://github.com/user-attachments/assets/52434ac5-1e85-4291-8b5a-6c27c7678b24)

  - 获得RM2024超级对抗赛机器人竞技奖雷达一等奖
![image](https://github.com/user-attachments/assets/3747ad4b-2504-4d47-8461-7e592e7105e5)
- 🎞 性能

  测试配置：CPU R7-5800H   GPU RTX3070Laptop  笔记本电脑
  
  - 不运行神经网络，只读取摄像头画面
    
  ![image](https://github.com/user-attachments/assets/a9345c1c-a7e2-49ac-b36c-744a91464029)
  - 全功能测试
    
  ![image](https://github.com/user-attachments/assets/37ad3e21-149b-48f6-ac1c-23b92f6272fe)

  瓶颈主要在读取图像和装甲板的推理，后续会优化推理性能。

  
- 🔧问题

  我们的方案虽然不需要激光雷达，规避了繁琐的解算和标定问题，但受制于相机镜头焦距及相机分辨率，远处装甲板在图像中的特征比较差，会出现较多识别错误/识别不出来的问题，本赛季我会继续尝试不同焦段多摄像头融合的方案。

  Unity作为游戏引擎，功能十分强大，然而Unity中的物理相机毕竟无法完全匹配现实的相机画面，我也没有找到一个较好的方法根据相机画面解出相机在unity世界中的准确位姿，PnP解算出的位置有一定误差，这个误差放在雷达画面上对远处目标精度的影响是毁灭性的，这是将来需要解决的问题。
