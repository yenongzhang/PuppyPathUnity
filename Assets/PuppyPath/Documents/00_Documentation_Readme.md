# PuppyPath V2 文档说明

最后更新：2026-06-30

## 文档目的

这个文件夹用于在正式实现前记录 PuppyPath 第二版的设计与开发方案。V2 会把当前项目从“地图选择 / 路径预览”的原型，改造成一个与真实活动场地精确匹配的混合现实寻宝体验：

- Unity 场景必须在比例、可行走区域、景点位置上匹配真实活动场地。
- 小狗需要一直保持在用户视野中；用户需要导航时它负责带路，用户抓取虚拟物品时它乖乖坐下等待。
- 每个景点附近会有悬浮的可收集虚拟物品。用户把物品拖到小狗身上后，物品会变成小狗的穿戴饰品，并解锁对应惊喜。
- UI 流程从现有 `logoCanvas` 开始，然后进入小地图、自由行走、导航、景点收集的体验。

## 文档列表

- `01_Concept.md`：产品概念、用户流程、交互设计、体验氛围、第一版文案。
- `02_Implementation_Plan.md`：开发阶段、需要实现的系统、可复用脚本、测试步骤。
- `03_Site_Calibration_And_Data.md`：场地比例尺、坐标映射、黄色可行走区域、景点和数据规范。
- `04_Existing_Project_Audit.md`：当前资源、脚本、场景的阅读整理，以及它们如何迁移到 V2。

## 必须遵守的文档同步规则

当用户提出新需求，或修改景点名称、真实地图、奖励内容、UI 文案、交互规则、实现方案时，必须在同一次开发步骤里同步修改本文件夹中对应的文档。

这个规则是 V2 工作流的一部分。代码、Unity 场景和文档不能各走各的，不然后续现场调试会很容易失控。

## 当前输入资料

- 用户在 2026-06-30 提供的活动场地地图草图。
- 红色标记墙体是真实比例尺，真实世界长度为 3.45 m。
- 地图中的黄色区域是人和狗可以活动行走的区域。
- 当前新版地图上可见的景点 / 黄色虚拟物品出现点：
  - Chess -> Checkmate Corner / 棋遇小屋
  - Couch -> Cozy Couch Cove / 软乎乎沙发湾
  - Photo Wall -> Snapshot Studio / 咔嚓照相馆
  - Goodies -> Treat Trove / 甜甜补给站
  - Book Wall -> Storybook Wall / 故事书墙
  - Tap Water -> Splash Stop / 汪汪补水站
  - Ice Cream Shop -> Scoop Station / 冰淇淋小站
  - Drink Shop -> Fizzy Fridge / 气泡饮料铺
  - Piano -> Melody Corner / 音符小舞台
  - Plants -> Garden Patch / 小狗花园

## 当前实现状态

V2 已开始实现。当前已完成第一版场地坐标 / 地图数据层、Scene 标定调试工具、可行走区域 / nav graph 编辑工具、`VenuePathfinder`、`VenueRouteLineController`，并新增 `VenueNavigationRuntime` 用于从 HMD 当前世界位置生成到景点的真实场地路线。

真机测试方面，已新增 `VenueAlignmentManager`、`VenueSpatialAnchorBootstrap` 和 `VenueWalkableGridVisualizer`。当前可以先在 Quest 中用黄色格子可视化可行走区域，并通过站在 Photo Wall 右上角原点、面朝地图北方的方式临时对齐场地。

下一步重点是完善 Spatial Anchor 的保存 / 加载恢复流程，把 `VenueNavigationRuntime` 接入实际 UI 选择流程，并继续开发专门的 `DogVenueFollower`，让小狗在自由行走和导航模式下都保持在用户前方且不穿墙。
