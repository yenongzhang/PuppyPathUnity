# PuppyPath V2 实施步骤文档

最后更新：2026-07-01

## 当前技术阅读结论

现有项目里已经有不少可以复用的部分：

- `UIBootSequence` 已经负责 logo 淡入、停留、淡出，并在 logo 结束后安全显示主 UI。
- `CanvasFollowHead` 可以让一个 canvas 保持在用户头部前方。
- `PuppyPathSelectionUI` 支持地图点击和 marker 放置，但它现在是基于网格的旧 Europa-Park 地图方案。
- `PathPreviewController` 可以实例化路径 prefab，并用 `LineRenderer` 画路线。
- `RouteRootSpawner` 会把路线内容生成在用户前方，这对旧原型有用，但 V2 需要真实场地固定坐标，因此必须改变。
- `NavigationRuntimeController` 已经能判断路径进度、到达、偏离路线、等待、小狗导航状态。
- `NavigationHUDController` 能在主 UI 和导航 HUD 文字之间切换。
- `NavigationController` 负责旧版选择、预览、开始导航、到达烟花、返回菜单等流程。
- `DogGuideController` 会生成小狗、播放动画状态、切换脸部贴图、播放叫声、根据路线方向移动、响应导航状态、执行随机行为。
- 狗模型、动画 FBX、脸部贴图、音频、UI 图片、logo、地图图片、路线 prefab、烟花 prefab 都已经存在。

V2 应该复用这些好的动画和 UI 基础，但核心场景模型必须改成“真实场地固定坐标 + 景点系统”。

## 推荐架构

### 新数据层

需要创建数据资产或可序列化场景数据，用于描述：

- 场地地图比例尺和坐标变换。
- 从黄色区域提取出的一个或多个可行走 polygon。
- 从不可行走区域提取出的障碍物 / 墙体 polygon。
- 景点定义。
- 景点虚拟物品。
- 小狗饰品插槽。
- 奖励内容。

推荐新增脚本：

- `VenueMapDefinition`
- `VenueCoordinateMapper`
- `WalkableArea`
- `AttractionDefinition`
- `AttractionRegistry`
- `CollectibleItemDefinition`
- `DogAccessoryDefinition`
- `RewardDefinition`

稳定内容数据优先使用 `ScriptableObject`。空间锚点、手工 waypoint、调试点可以使用场景对象。

当前已开始实现：

- `Assets/PuppyPath/Scripts/V2/VenueMapDefinition.cs`：`ScriptableObject` 场地地图定义，保存地图尺寸、原点像素坐标、3.45 m 比例尺端点、朝向和景点数据。
- `Assets/PuppyPath/Scripts/V2/VenueCoordinateMapper.cs`：地图像素坐标和 Unity 世界坐标之间的转换工具。
- `Assets/PuppyPath/Scripts/V2/VenueCalibrationDebugView.cs`：Scene 视图调试绘制工具，用于检查原点、地图边界、比例尺和景点 marker。
- `Assets/PuppyPath/Scripts/V2/VenuePathfinder.cs`：第一版手工导航图寻路工具，基于 waypoint graph 生成地图像素路线和 Unity 世界路线。
- `Assets/PuppyPath/Scripts/V2/VenueRouteLineController.cs`：第一版路线 LineRenderer 绘制组件，可用测试起点和目标景点画路线。
- `Assets/PuppyPath/Scripts/V2/VenueNavigationRuntime.cs`：第一版 V2 场地导航运行时，负责从 HMD 世界位置生成到景点的真实场地路线、刷新地面路线 line，并向小狗控制器提供推荐方向。
- `Assets/PuppyPath/Scripts/V2/VenueAlignmentManager.cs`：第一版现场校准组件，将 `VenueContentRoot` 对齐到当前 HMD 所在的真实 `VenueOrigin`。
- `Assets/PuppyPath/Scripts/V2/VenueSpatialAnchorBootstrap.cs`：第一版 Meta Spatial Anchor bootstrap，可在 `VenueOrigin` 创建 `OVRSpatialAnchor` 并把场地内容挂到 anchor 下。
- `Assets/PuppyPath/Scripts/V2/VenueWalkableGridVisualizer.cs`：真机可视化工具，用黄色格子铺出当前可行走区域，方便在 Quest 中验证地图对齐。
- `Assets/PuppyPath/Scripts/V2/VenueMapReferencePlane.cs`：把地图图片按当前标定比例铺到 Scene 的 XZ 平面，方便人工校准。
- `Assets/PuppyPath/Scripts/V2/Editor/VenueCalibrationDebugViewEditor.cs`：Scene 视图拖拽编辑工具，可直接移动景点点位、polygon 顶点和 nav graph 节点。

已确认第一版坐标约定：

- `VenueOrigin` 使用 Photo Wall 右上角红点。
- Unity `+Z` 对应地图北方 / 图片向上方向。
- 地图像素坐标以图片左上角为 `(0, 0)`，`+X` 向右，`+Y` 向下。

### 新游戏流程层

创建一个高层状态控制器：

- `PuppyPathV2GameController`

建议状态：

- `Boot`
- `Intro`
- `FreeWalk`
- `MapOpen`
- `Navigating`
- `AttractionReveal`
- `ItemGrab`
- `Reward`

这个控制器负责协调 UI、小狗行为、景点显示、收集状态和导航模式。

### 新场地导航层

旧版导航使用的是相对用户生成的路径 prefab。V2 需要真实场地中的世界固定路径：

- 使用 3.45 m 红色墙体作为比例尺，把地图点转换成 Unity 世界坐标。
- 在黄色区域上创建可行走图结构。
- 用图搜索 / pathfinding 从用户当前位置生成到目标景点的路线。
- 把小狗目标位置限制在可行走区域内。
- 防止小狗和地面路线穿过墙体或不可行走区域。

推荐新增脚本：

- `VenueNavGraph`
- `VenuePathfinder`
- `VenueRouteLineController`
- `DogVenueFollower`

现有 `PathPreviewController` 的地面路线绘制方式可以作为参考，但 V2 不应继续依赖旧 path prefab id。

### 新小地图层

用真实场地小地图替换旧网格地图：

- 右下角小地图。
- 用户当前位置 marker。
- 每个景点一个狗爪 marker。
- 点击或触碰小地图后，打开居中的大地图。
- 在大地图中点击狗爪 marker 开始导航。
- 导航模式中必须提供退出 / 取消导航按钮，用户不想继续前往当前目标时可以随时回到自由行走。

推荐新增脚本：

- `VenueMinimapController`
- `VenueMapMarker`
- `VenueMapPanelController`

旧的 `PuppyPathSelectionUI` 可以作为 pointer 点击和 marker 放置的参考，但 V2 应使用景点坐标，而不是固定网格行列。

### 新景点和收集层

每个景点需要包含：

- 名称。
- 世界坐标位置。
- 虚拟物品出现点位置。当前新版地图中以黄色圆点为准，景点文字 label 只作为命名参考。
- 透明度距离曲线，而不是单一显示半径。
- 建议第一版透明度规则：3 m 内 alpha = 1，6 m alpha = 0.5，10 m 以上 alpha = 0，中间平滑插值。
- 悬浮物品 prefab。
- 物品距离显隐逻辑。
- 提示 UI。
- 是否已收集。
- 对应小狗饰品位置。
- 奖励内容。

推荐新增脚本：

- `AttractionTrigger`
- `FloatingCollectibleItem`
- `CollectibleGrabHandler`
- `DogAccessoryManager`
- `RewardRevealController`

Meta Quest / XR 交互应基于当前项目已经使用的 XR 设置来实现。已确认当前版本只使用手势追踪，不使用手柄。旧版 PuppyPath 已使用 pointing pinch，V2 需要在此基础上增加 grab 手势，用于抓取并拖动物品到小狗身上。

### 小狗行为层

`DogGuideController` 是第一优先复用对象，但建议给 V2 包一层或逐步拆分：

- 自由行走：小狗保持在用户前方 1-3 m，并在黄色可行走区域内。
- 导航：小狗沿生成路线走在用户前方。
- 抓物品：小狗坐下等待。
- 奖励：小狗开心反应。
- 饰品：小狗显示已经收集到的穿戴物。

推荐做法：

1. 增加一个 V2 wrapper，向现有小狗控制器发送高层命令。
2. 保留当前动画 state 名称和表情贴图切换逻辑。
3. 只有当 `DogGuideController` 与旧路线逻辑耦合太深时，再进一步重构。

旧导航 / 小狗脚本阅读结论：

- `PathPreviewController` 负责根据旧 path id 实例化静态 path prefab，并用 `LineRenderer` 绘制路线。
- `NavigationRuntimeController` 负责读取当前 path waypoints，判断用户离路线中心线的距离、沿路线进度、移动方向、等待状态、到达状态，并把 `NavState` 和推荐方向发给 `DogGuideController`。
- `DogGuideController` 负责生成小狗、播放动画、切换表情、移动到用户前方目标点、响应等待 / 走远 / 迷路 / 到达等旧状态。
- V2 可以复用小狗生成、动画、表情、移动和叫声代码思路，但不能继续依赖旧 path prefab 和旧“生气 / 等待 / 迷路”反馈逻辑。

V2 小狗行为新规则：

- 自由行走模式：小狗跟随 HMD 水平移动方向，保持在用户前方约 1-3 m。
- 如果 HMD 移动速度足够明显，用移动方向决定小狗前方目标；如果用户基本静止，使用 HMD forward 作为 fallback。
- 小狗移动速度应接近或略快于 HMD 水平速度，必要时从 walk 切到 trot / canter。
- 用户没有停下时，小狗不应停下等待，也不应坐下。
- 小狗不应跑到用户身后；如果落后，应优先追到用户前方可行走点。
- 前方目标点必须通过 `VenueMapDefinition.IsMapPixelWalkable` 或后续等价 API 检查。
- 如果正前方不可行走，应依次尝试左前方、右前方、更近的前方点、最近可行走 nav node。
- 导航模式：路线来源改为 `VenuePathfinder` / `VenueRouteLineController` 生成的真实场地路线，但小狗运动和动画可以沿用 `DogGuideController` 的 locomotion 代码。
- 快到宝藏时，小狗朝宝藏方向开心大叫；这属于正向发现反馈，不使用旧 `GettingFarther` / `Lost` 的负面反馈。
- “随便逛逛”模式没有偏航概念，小狗不会因为用户离开某条路线而生气。

建议实现方式：

1. 新增 `DogVenueFollower`，作为 V2 wrapper，负责 HMD 速度检测、前方目标选择、可行走区域约束和宝藏接近反馈。
2. 给现有 `DogGuideController` 增加少量公开方法或轻量 wrapper API，用于复用生成小狗、播放 walk / trot / canter / happy / bark 动画。
3. 暂时不要重写旧 `NavigationRuntimeController`；先为 V2 写一个新的场地导航 runtime，输入为 `VenuePathfinder` 生成的路线点。
4. 保留旧脚本供参考和回滚，但 V2 不再使用旧 path prefab 作为真实导航数据源。

## 开发阶段

### 阶段 1：场地标定原型

目标：创建一个与真实地图比例一致的 Unity 场地场景。

当前状态：基础数据结构和调试可视化脚本已建立，下一步需要在 Unity 中创建 `VenueMapDefinition` 资产，并在 Inspector 中填写 Photo Wall 原点、3.45 m 比例线端点和各景点像素坐标。

当前测试方法：

1. 在 `VenueMapDefinition` 中确认 `Map Pixel Size = 2468 x 2160`，`Sync Map Pixel Size From Imported Texture` 不勾选。
2. 填写 `Map Origin Pixel`、`Scale Point A Pixel`、`Scale Point B Pixel`。
3. 在场景中创建或选择 `VenueCalibrationDebug` 空物体，挂载 `VenueCalibrationDebugView`，并引用当前 `VenueMapDefinition`。
4. 打开 Scene 视图右上角 `Gizmos`。
5. 在 Project 视图选中 `VenueMapDefinition`，通过组件右键菜单执行 `Log Calibration Summary`。
6. Console 中 `Scale world distance` 应显示约 `3.45 m`。
7. Scene 视图中应能看到 Photo Wall 原点、3.45 m 比例线、地图边界；填写景点坐标后还应看到 10 个景点 marker。

当前已填写的第一批标定值：

- `Map Origin Pixel = (716, 820)`。
- `Scale Point A Pixel = (1003, 715)`。
- `Scale Point B Pixel = (1003, 833)`。

步骤：

1. 导入或放置最新活动场地地图图片，作为参考平面或 UI overlay。
2. 在地图坐标中标记红色墙体的参考线段。
3. 根据 3.45 m 墙体计算地图到米的比例。
4. 选择 Unity 原点和场地朝向。
5. 根据黄色区域创建简单地面 / 可行走区域可视化。
6. 给所有当前已标记景点和黄色虚拟物品出现点添加 debug marker。
7. 用测量工具验证 Unity 中的距离。
8. 把所有选定的原点、朝向、比例写入 `03_Site_Calibration_And_Data.md`。

验收标准：

- Unity 中测量红色墙体，应为 3.45 Unity unit，也就是 3.45 m。
- 所有已知景点和黄色虚拟物品出现点都出现在合理的相对位置。
- 黄色可行走区域已经变成场地固定区域。

### 阶段 2：可行走区域和寻路

目标：导航和小狗位置必须遵守真实可行走区域。

当前状态：已新增 `WalkableAreaDefinition`、`VenueNavGraphDefinition`、`VenueNavNodeDefinition` 数据结构；`VenueCalibrationDebugView` 可以绘制绿色可行走 polygon、蓝色 waypoint graph、橙色测试路线。`VenuePathfinder` 当前使用手工 waypoint graph，并会检查直线段采样点是否处在可行走 polygon 内。

2026-07-01 更新：地图负责人已在 Scene 中完成可行走区域和 nav graph 手工处理。下一步从数据编辑进入运行时验证：用 HMD / XR Camera 当前世界位置生成到目标景点的场地固定路线，并画出地面 LineRenderer。

已新增 `VenueNavigationRuntime`：

- 输入：`VenueMapDefinition`、`xrCamera`、`VenueRouteLineController`，可选 `DogGuideController`。
- 对外 API：`StartNavigationToAttraction(string attractionId)` 和 `StopNavigation()`。
- Inspector 右键菜单：`Start Test Navigation` / `Stop Navigation`，用于不接 UI 时先测试任意景点路线。
- 行为：把 HMD 世界位置转成地图像素坐标，调用 `VenuePathfinder` 生成路线，再用 `VenueRouteLineController.ShowWorldRoute` 画线。
- 运行中会按间隔重新规划路线；如果新路线规划失败，会保留上一条有效路线，避免现场测试时路线突然消失。
- 如果用户当前位置或目标点略微落在 walkable polygon 外，可临时吸附到最近的可行走 nav node，降低现场标定微小误差造成的失败概率。
- 当前小狗接入是过渡方案：复用 `DogGuideController.BeginGuiding` / `ApplyNavigationState`，只发送 `Neutral`、`GettingCloser`、`Arrived` 等非负面状态；后续仍应实现专门的 `DogVenueFollower`。

2026-07-01 真机可视化 / 现场校准更新：

- 新增 `VenueContentRoot` 作为所有场地固定内容的父物体。`VenueCalibrationDebug`、`VenueRouteLine`、`VenueWalkableGridVisualizer`、后续景点物品和小狗目标点都应放到这个 root 下。
- 新增 `VenueAlignmentManager`：快速测试时，用户站在真实 Photo Wall 右上角原点，面朝地图北方 / Unity `+Z`，启动后自动把 `VenueContentRoot` 对齐到当前 HMD。
- 新增 `VenueSpatialAnchorBootstrap`：在 `VenueOrigin` 创建 Meta `OVRSpatialAnchor`，并可把 `VenueContentRoot` 挂到 anchor 下，作为后续持久化场地对齐的基础。
- 新增 `VenueWalkableGridVisualizer`：根据 `VenueMapDefinition.IsMapPixelWalkable` 生成黄色半透明格子，让 Quest 内能看见可行动区域。
- Meta Quest Spatial Anchor 前置设置：在 `OVRCameraRig` 的 `OVRManager > Quest Features > General` 开启 `Anchor Support`；只有需要共享 anchor 时才开启 `Anchor Sharing Support`。

2026-07-01 更新：已增加自动检测草稿入口。在 `VenueMapDefinition` 的右键 / 齿轮菜单执行 `Populate Detected Draft Map Data`，会从当前 `map_with_spawn_points.jpg` 自动检测结果中填入：

- 10 个 `collectibleSpawnPixel`。
- 1 个主可行走外轮廓 `main_walkable_auto_draft`。
- 1 个中央障碍区域 `central_block_auto_draft`，用于防止路线穿过中间大灰块。
- 14 个第一版 waypoint graph 节点。

这些数据是草稿，不是最终现场标定结果。橙色圆点检测置信度较高；可行走轮廓和 waypoint 需要在 Scene 视图中人工检查。

蓝色 waypoint graph 说明：

- 蓝色点和蓝线不是墙体，也不是可行走边界。
- 它们是寻路中心线：路线会从一个蓝点走到相邻蓝点。
- 如果蓝线穿过红色障碍区或灰色不可行走区，该边在调试视图中会显示为红橙色，需要移动节点或删除邻居连接。
- `Edit Nav Graph In Scene` 开启后，Scene 左上角会出现 `Nav Graph Editing` 面板。点击蓝点旁边的小青色选择点选中两个 waypoint 后，可用 `Connect` 手动连接，也可用 `Disconnect` 删除不能走的连接。

推荐人工修正流程：

1. 选中场景中的 `VenueCalibrationDebug`。
2. 在组件右键菜单执行 `Create Map Reference Plane`，把当前地图图片铺到 Scene 下方。
3. 打开 Scene 视图 `Gizmos`。
4. 在 `Scene Editing` 中按需开启：
   - `Edit Calibration Points In Scene`
   - `Edit Attractions In Scene`
   - `Edit Walkable Areas In Scene`
   - `Edit Obstacle Areas In Scene`
   - `Edit Nav Graph In Scene`
5. 直接拖动 Scene 中的红色原点 / 比例尺端点、黄色点、绿色 polygon 顶点、红色 obstacle 顶点或蓝色 nav node。
6. 如果需要手动改蓝线，在 Scene 左上角 `Nav Graph Editing` 面板中先点击两个蓝点旁边的小青色选择点，再点 `Connect` 或 `Disconnect`。
7. 修改会写回 `VenueMapDefinition`，可用 Undo 撤销。
8. 优先修正红橙色的 nav edge，因为它们代表当前 graph 中不可通行或穿墙的连接。

2026-07-01 更新：`Populate Detected Nav Graph Draft` 已改为更密集的 waypoint 草稿。它会先放置更多走廊中心线节点，再用 `IsMapSegmentWalkable` 自动过滤穿过不可行区域的连接。目标是让小狗后续拥有更多自由移动选择，同时避免默认 graph 直接穿墙。

2026-07-01 标定编辑规则更新：

- Scene 中拖动 `mapOriginPixel` 时，保持已设置点位和线条的世界布局不漂移；代码会同步调整 `originWorldPosition`。
- Scene 中拖动 `scalePointAPixel` / `scalePointBPixel` 时，保持当前 meters-per-pixel 不变，避免其他点和线被重新缩放。
- 如果之后需要真正重新计算比例尺，应增加一个明确的“重新标定比例”操作，而不是在普通拖动时隐式改变比例。
- `VenueCalibrationDebugView` 提供 `Show Map Reference Plane` / `Hide Map Reference Plane`，用于显示或隐藏 Scene 下方的地图底图。
- `VenueCalibrationDebugViewEditor` 提供 nav graph 手动连线 / 断线工具，用于修补自动草稿中断开的合法通路，或删除人工确认不能走的边。

需要地图负责人提供的数据：

- 可行走区域 polygon：黄色区域外轮廓的关键拐角像素坐标。第一版不需要极度精细，但必须覆盖用户和小狗可走的主通道。
- 如黄色区域分成多个不连续块，需要每个块单独一个 `WalkableAreaDefinition`。
- 导航 waypoint：沿可行走区域中心线放置的关键转折点像素坐标。waypoint 数量可以少于 polygon 拐角，重点是每个走廊转弯、岔路口、景点附近都要有点。
- waypoint 之间的连接关系：每个 `VenueNavNodeDefinition.neighborNodeIds` 填写可直接通行的相邻节点 id。

最小可测试数据：

1. 一个覆盖 Photo Wall 到红色比例线附近走廊的粗略 `WalkableAreaDefinition`。
2. 3-5 个 waypoint，形成一条能从 Photo Wall 走到任意一个测试景点的路径。
3. 至少一个景点填好 `collectibleSpawnPixel` 或 `arrivalPixel`。
4. 在 `VenueCalibrationDebugView` 的 `Test Path` 中填写 `testStartPixel` 和 `testDestinationAttractionId`，Scene 视图应显示橙色路线。

推荐命名：

- 主走廊 waypoint 使用 `main_01`、`main_02`、`main_03`。
- 分支点使用景点缩写，例如 `photo_wall_arrival`、`drink_shop_arrival`。
- 邻居关系先双向填写，例如 `main_01` 连接 `main_02`，同时 `main_02` 也连接 `main_01`。

步骤：

1. 把黄色区域边界转换成 polygon 数据。
2. 从不可行走区域添加障碍物 / 墙体 polygon。
3. 初期可以手工创建导航图，后续再考虑从 polygon 自动采样。
4. 实现从用户位置到景点的路线生成。
5. 在地面渲染路线 line。
6. 使用 `VenueNavigationRuntime` 从 HMD 位置测试到 10 个景点的路线。
7. 把路线点和小狗目标点 clamp 到可行走区域内。
8. 添加 debug 工具，显示最近合法点和被阻挡的路径边。

验收标准：

- 生成的路线保持在黄色区域内。
- 小狗目标点不会出现在墙后或穿墙位置。
- 从多个测试位置都能生成到每个景点的路线。

### 阶段 3：V2 UI 流程

目标：用 boot、intro、小地图、大地图、顶部状态文字替换旧选择流程。

步骤：

1. 复用 `UIBootSequence` 播放 logo。
2. 添加 V2 主 HUD：
   - 顶部状态文字。
   - 右下角小地图。
   - 小狗对话气泡。
3. 实现开场介绍的时间和流程。
4. 实现小地图用户位置 marker。
5. 实现各景点的狗爪 marker。
6. 实现大地图打开 / 关闭。
7. 实现景点 marker 选择。
8. 把 marker 选择连接到导航模式。
9. 实现导航模式退出按钮：清除当前路线、取消目标、隐藏导航 UI，并回到自由行走状态。

验收标准：

- Logo 最先出现。
- Logo 后小狗和小地图出现。
- 开场对话可以播放。
- 小地图可以展开为大地图。
- 点击狗爪 marker 后进入导航状态。
- 导航状态下可以点击退出 / 取消导航，并立即回到自由行走状态。

### 阶段 4：小狗自由行走和导航行为

目标：让小狗在真实场地中既可靠又有存在感。

步骤：

1. 开场介绍开始后生成小狗。
2. 自由行走目标选择：
   - 优先选择用户前方 1.5-2 m。
   - 允许距离范围为 1-3 m。
   - 如果前方被阻挡，测试左前方和右前方。
   - 拒绝用户身后的目标点。
   - 把目标点限制在可行走区域内。
3. 导航目标选择：
   - 小狗放在生成路线前方。
   - 与用户保持易观察距离。
   - 使用现有 walk / trot 动画。
4. 物品交互行为：
   - 小狗停止移动。
   - 小狗坐下。
   - 小狗看向用户或物品。
5. 到达行为：
   - 小狗短暂庆祝。
   - 几秒后系统回到自由行走。

验收标准：

- 正常行走时小狗保持可见。
- 小狗不会穿墙。
- 小狗不会站到黄色区域外。
- 用户操作收集物时，小狗坐着等待。

### 阶段 5：景点显示和可收集物品

目标：用户靠近景点时显示悬浮虚拟物品。

步骤：

1. 实现每个景点物品的距离透明度控制，而不是单一触发开关。
2. 第一版透明度曲线：
   - 3 m 内 100% 显示。
   - 6 m 左右 50% 显示。
   - 10 m 以上 0% 显示。
   - 3-10 m 之间平滑插值。
3. 在黄色圆点对应的物品出现点生成或激活悬浮物品。
4. 根据透明度同步控制物品材质、提示 UI 或可交互状态。
5. 在物品附近显示提示 UI。
6. 支持用户通过手势 grab 抓取和拖动，不使用手柄。
7. 检测物品是否被放到小狗目标 collider / 区域上。
8. 收集成功后隐藏提示。
9. 标记该景点已收集。

验收标准：

- 物品只在对应景点附近出现。
- 物品透明度随用户距离连续变化：近处清楚，远处逐渐消失。
- 提示清晰可读，不干扰整个场景。
- 用户可以用手势 grab 抓住物品并拖到小狗身上。
- 除非主动 reset，否则同一景点不能重复收集。

### 阶段 6：小狗饰品和奖励

目标：每个收集物都能改变小狗外观，并显示对应奖励。

步骤：

1. 在小狗 prefab 上添加饰品 anchor：
   - 头部。
   - 眼睛 / 脸部。
   - 脖子。
   - 身体 / 背部。
   - 尾巴或侧面，如有需要。
2. 为每个收集物定义饰品 slot。
3. 把收集到的饰品 prefab attach 到对应 slot。
4. 保存当前 session 的收集状态；暂时不需要 app 重启后持久化。
5. 播放 1-2 秒 shaking / surprise 动画。
6. 显示景点奖励 UI。
7. 用户关闭奖励或配置时间结束后回到自由行走。

验收标准：

- 每个已收集物品都会继续显示在小狗身上。
- 多个饰品可以同时存在。
- 奖励显示与景点收集动作明确关联。

### 阶段 7：小狗动画和表情优化

目标：让小狗更有表情和情绪。

步骤：

1. 检查现有 animation controller 的状态。
2. 把 V2 状态映射到动画 clip：
   - Idle / curious。
   - Walk ahead。
   - Guide。
   - Sit wait。
   - Happy reward。
   - Curious reveal。
3. 检查脸部贴图分组。
4. 为景点显示和奖励时刻添加表情过渡。
5. 需要时添加叫声或轻量音效。

验收标准：

- 小狗状态变化清楚可读。
- 动画不频繁闪烁。
- 抓物品时的小狗坐下等待显得自然、有意图。

### 阶段 8：现场测试和迭代

目标：验证真实活动场地和 Unity 场景完全匹配。

步骤：

1. 在场地角落或参考点放置已知测试 marker。
2. 用 3.45 m 墙体测试地图比例。
3. 在各景点之间行走，对比真实距离和 Unity 距离。
4. 调整景点触发半径。
5. 调整小狗带路距离和障碍物 fallback 逻辑。
6. 用真实移动测试小地图方向。
7. 和现场工作人员一起测试所有奖励流程。
8. 每次场地地图或奖励变化后，更新所有相关文档。

验收标准：

- 小地图中的用户位置感觉正确。
- 景点在预期真实位置触发。
- 小狗在狭窄通道中仍然可信。
- 现场工作人员不需要额外技术解释也能理解奖励流程。

## 建议优先实现顺序

1. 锁定场地坐标系和比例尺。
2. 建立可行走区域和景点 marker。
3. 用景点数据生成小地图 / 大地图。
4. 实现路线生成和地面路线 line。
5. 改造小狗自由行走和导航位置逻辑。
6. 添加可收集物的显示、抓取、放置。
7. 添加饰品 attachment 和奖励显示。
8. 优化小狗动画和表情。

## 并行工作分配建议

当前建议分成两条互不阻塞的开发线：

### 开发线 A：地图 / 场地坐标 / 导航基础

负责人：地图负责人。

当前任务：

- 完成 `VenueMapDefinition` 的 10 个景点 / collectible spawn point 像素坐标填写。
- 使用 `VenueCalibrationDebugView` 验证比例尺、原点、地图方向和景点相对位置。
- 下一步建立黄色可行走区域的手工 polygon 或 waypoint 草稿。
- 后续实现 `VenueNavGraph`、`VenuePathfinder`、`VenueRouteLineController`。
- 当前代码已提供第一版 `VenuePathfinder` 和 `VenueRouteLineController`，下一步重点是填入 polygon 和 waypoint 数据，并用 Scene 视图测试路线。

交付物：

- 可被代码读取的场地坐标数据。
- Scene 视图中可信的地图 marker 和比例尺。
- 第一版可行走区域 / 导航图。

### 开发线 B：小狗饰品 / 奖励 / 动画测试

负责人：第二位程序员。

这条线可以并行推进，因为它主要依赖小狗 prefab、动画和临时测试按钮，不依赖最终地图坐标。

当前任务：

- 在小狗 prefab 上整理饰品 anchor：`Head`、`Face`、`Neck`、`Back` 等。
- 新增 `DogAccessoryDefinition` 和 `DogAccessoryManager` 草稿，用测试键或 Inspector 按钮把饰品 prefab attach 到指定 anchor。
- 在 `DogTestScene` 或复制出的 V2 测试场景里测试坐下、开心、惊讶、摇晃等动画状态。
- 整理每个景点对应的临时饰品 slot，例如 Photo Wall -> glasses / camera frame，Drink Shop -> collar charm。
- 新增简单 `RewardRevealController` 草稿，可以显示一张测试奖励面板或触发已有烟花 prefab。

边界约束：

- 不修改 `VenueMapDefinition`、`VenueCoordinateMapper`、`VenueCalibrationDebugView` 的坐标逻辑。
- 不把奖励流程强接到地图导航；先做可独立测试的 API，例如 `ShowReward(string attractionId)`。
- 如需改 `DogGuideController`，优先新增 wrapper 或小范围公开方法，避免重写现有导航行为。

交付物：

- 小狗 prefab 上可用的饰品 anchor。
- 能在测试场景中手动 attach / detach 饰品的管理器。
- 奖励显示和小狗动画反应的独立 demo。

## 风险

- 当前地图只是草图，后续会修改；空间数据必须设计成容易更新。
- 如果真实 XR tracking origin 没有和地图对齐，所有景点触发都会错位。
- 旧路线系统是相对用户生成的，无法直接满足真实场地导航。
- 小狗可见性和避障必须现场走测，仅靠 Editor 模拟不够。
- 抓取交互只使用手势追踪，需要重点验证 pointing pinch 与 grab 的识别稳定性。

## 必须遵守的文档同步规则

当实现过程中改变任何架构、阶段、任务、验收标准或风险判断时，必须在同一次代码 / 场景修改中同步更新本文档。
