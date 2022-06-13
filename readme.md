#### 项目介绍

 manto-luaprofiler是一款基于easyhook开发的可跨引擎使用的lua性能分析工具，可用于实时分析lua函数耗时及内存分配情况。

#### 快速使用Getting Start

1. 启动目标进程（如Unity.exe,UE4Editor.exe等）
2. 注入目标进程

![image-20210719105856239](https://git.woa.com/triodehe/luaprofiler/raw/58636f7ef011cbb6e1f2ccb8b7f9d70ee95844de/Image/image-20210719105856239.png)

3. 注入成功后，弹出连接成功

![image-20210719110223489](https://git.woa.com/triodehe/luaprofiler/raw/58636f7ef011cbb6e1f2ccb8b7f9d70ee95844de/Image/image-20210719110223489.png)

4. 运行游戏（以UE4为例）

![image-20210719110338914](https://git.woa.com/triodehe/luaprofiler/raw/58636f7ef011cbb6e1f2ccb8b7f9d70ee95844de/Image/image-20210719110338914.png)

4. Luaprofiler中将呈现曲线图

![image-20210719110502896](https://git.woa.com/triodehe/luaprofiler/raw/58636f7ef011cbb6e1f2ccb8b7f9d70ee95844de/Image/image-20210719110502896.png)

#### 功能列表 Feature List

函数耗时分析

函数内存分配分析

内存快照（建设中）

#### 常见问题

1. 如遇无法hook到函数，请先attach到目标进程，后运行游戏。
