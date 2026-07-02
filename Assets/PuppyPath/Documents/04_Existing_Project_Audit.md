# PuppyPath 现有项目 V2 审计文档

最后更新：2026-07-01

## 阅读范围

已阅读和整理 `Assets/PuppyPath` 下的内容，包括：

- `3dModel`
- `Animations`
- `Audio`
- `Images`
- `Logo`
- `Material`
- `Prefabs`
- `Scenes`
- `Scripts`
- `UI`

二进制、模型、音频、图片、prefab、scene 资源主要通过文件结构和命名进行检查。C# 脚本已阅读其当前行为和公开接口。

## 资源清单

### 3dModel

包含 beagle FBX 文件、动画 FBX 文件、小狗眼睛和嘴巴贴图、小狗材质文件、texture map。这是 V2 小狗角色的基础。

V2 可复用：

- 小狗模型。
- 现有移动、坐下、叫、开心、转身、嗅闻动画文件。
- 用于表情变化的脸部贴图。

V2 需要补充：

- 在小狗 prefab 上添加饰品 anchor transform。
- 添加帽子、眼镜、衣服、项圈挂饰等饰品模型。
- 针对物品出现、坐下等待、奖励时刻优化动画和表情。

### Animations

包含 `DogAnimationController.controller`。

V2 可复用：

- 现有小狗动画控制器可以作为基础。

V2 需要补充：

- 确认动画 state 名称与 `DogGuideController` 中序列化字段匹配。
- 如有需要，为 V2 状态新增或调整动画过渡。

### Audio

包含狗叫音效和 `track1.mp3`。

V2 可复用：

- 狗叫音效可用于小狗反应和奖励时刻。

V2 需要补充：

- 决定开场介绍和奖励是否需要额外音效。
- 根据活动现场环境调整音量。

### Images

包含旧图标和旧地图资源，包括 `EuropaParkMap1-modified.png`。

V2 可复用：

- 现有地图 UI 代码思路和 marker / icon 资源。

V2 需要补充：

- 导入最新活动场地地图图片。
- 如果没有现成资源，需要制作狗爪 marker 图标。
- 用真实活动场地地图替换旧 Europa-Park 地图数据。

### Logo

包含 PuppyPath logo PDF 和 PNG 文件。

V2 可复用：

- 现有 logo 资源应继续用于启动流程。

### Material

包含小狗眼睛 / 嘴巴材质、路径预览材质、shadow receiver 材质 / shader、烟花 additive 材质。

V2 可复用：

- `M_PathPreview` 或类似材质可以用于地面路线 line。
- 眼睛 / 嘴巴材质用于表情切换。
- 烟花材质可用于奖励或惊喜特效。

### Prefabs

包含：

- 小狗 prefabs。
- 地图 marker prefab。
- OVRCameraRig variant。
- RouteRoot prefab。
- Firework prefab。
- 旧路径 prefabs。

V2 可复用：

- 小狗 prefabs 是主角基础。
- Firework prefab 可以作为惊喜特效参考，虽然用户当前优先要求 shaking 动画。
- Map marker prefab 可能可以替换成狗爪 marker。

V2 需要补充：

- 旧路径 prefabs 是旧地图的静态路线定义，基本不适合真实活动场地。
- Route root 当前是相对用户生成的，应替换为场地固定路线生成。

### Scenes

包含：

- `DogTestScene.unity`
- `UIScene.unity`

V2 可复用：

- `DogTestScene` 可用于测试小狗动画和表情。
- `UIScene` 很可能包含当前 app UI 流程。

V2 需要补充：

- 实现开始后，建议新增或复制一个 V2 scene / prefab setup。
- 在 V2 稳定前保留旧 scene。

### UI

包含字体资源，以及气泡、按钮、地图、位置、朋友、箭头等 UI 图片。

V2 可复用：

- 对话气泡资源可用于小狗自我介绍和物品提示。
- 现有字体和按钮风格可保持视觉一致性。

V2 需要补充：

- 新的小地图布局。
- 居中的大地图。
- 顶部状态文字。
- 景点物品提示 UI。
- 奖励 UI。

## 脚本审计

### `UIBootSequence`

当前行为：

- 显示 logo root。
- 控制 logo canvas group 淡入、停留、淡出。
- Logo 期间隐藏主 canvas。
- 在 `CanvasFollowHead` snap 后安全显示主 canvas。

V2 用法：

- 复用启动流程。
- 增加完成事件或 callback，让 V2 controller 在 logo 结束后生成小狗并播放开场对话。

### `CanvasFollowHead`

当前行为：

- 把 canvas 放在用户头部前方。
- 平滑跟随位置和旋转。
- 当距离太远、在身后或太近时 snap。

V2 用法：

- 可复用给跟随头部的 UI。
- 小地图 / 顶部 HUD 是否使用它，取决于最终 XR UI 设置。

### `PuppyPathSelectionUI`

当前行为：

- 处理旧地图点击。
- 把地图点击转换成网格行 / 列。
- 从 3x5 网格中选择 path id。
- 支持 friend marker。
- 更新旧流程阶段文字和按钮。

V2 用法：

- 可作为 UI pointer 点击和 marker 放置逻辑参考。

V2 替换方向：

- 用景点 marker 选择替代网格选择。
- 除非后续需要恢复找朋友功能，否则移除旧 Europa-Park 地名和 friend-based 流程。

### `NavigationController`

当前行为：

- 管理旧版 intro phases、preview、start、complete、fireworks、destination beacon、reset。

V2 用法：

- 可复用流程组织思路。
- 如有帮助，可复用到达 / 奖励特效思路。

V2 替换方向：

- 新的高层 V2 game controller 应管理 `Boot`、`Intro`、`FreeWalk`、`MapOpen`、`Navigating`、`AttractionReveal`、`ItemGrab`、`Reward`。

### `PathPreviewController`

当前行为：

- 把 path id 映射到 `PathDefinition` prefab。
- 生成 route root。
- 在 route root 下实例化 path prefab。
- 使用 waypoint 通过 `LineRenderer` 绘制动画路线。
- 对外提供当前路径和目的地。

V2 用法：

- 复用路线绘制思路。

V2 替换方向：

- 路线应从场地 graph 数据生成，而不是从旧静态 path prefab 选择。
- 路线应固定在真实场地坐标中，而不是相对用户生成。

### `RouteRootSpawner`

当前行为：

- 在 XR camera 前方生成 route root。
- 使用 eye-to-ground offset。

V2 用法：

- 只适用于旧原型。

V2 替换方向：

- 场地固定的 route parent 应存在于真实场地坐标系中。

### `NavigationRuntimeController`

当前行为：

- 读取当前路径 waypoints。
- 跟踪用户沿路径的进度。
- 计算状态：`Neutral`、`Waiting`、`GettingCloser`、`GettingFarther`、`Lost`、`Arrived`。
- 更新 HUD。
- 通知 `DogGuideController`。

V2 用法：

- 可复用导航状态概念。

V2 修改方向：

- 使用生成的场地路线，而不是旧 path prefab waypoints。
- 到达后应在几秒后回到自由行走。
- 景点物品显示应该优先于普通到达完成逻辑。

### `NavigationHUDController`

当前行为：

- 导航时隐藏旧 friend / map / intro panels。
- 显示 navigation HUD 和状态文字。

V2 用法：

- 可复用文字更新模式。

V2 替换方向：

- 用 V2 UI 组替代当前旧 panel 假设。

### `DogGuideController`

当前行为：

- 实例化小狗 prefab。
- 保存运行时路径。
- 应用导航状态。
- 播放 stand / walk / trot / canter / sniff / bark / happy / sit / turn 动画。
- 播放狗叫音频。
- 执行随机行为。
- 切换眼睛 / 嘴巴表情贴图。
- 根据路线方向和状态让小狗在用户附近移动。

V2 用法：

- 是小狗动画、表情、音频和跟随行为的强复用候选。

V2 修改方向：

- 小狗移动目标必须使用场地可行走区域约束。
- 添加自由行走、带路、坐下等待、景点显示、奖励等明确命令。
- 接入饰品管理器。
- 避免旧行为中让小狗跑到用户身后的逻辑；V2 要求小狗不应在用户身后。

### `DogStateTester`

当前行为：

- 用键盘测试小狗表情和动画 bool / trigger。

V2 用法：

- 保留用于测试小狗动画和表情。

### `DogNavStateTester`

当前行为：

- 创建假路径，用按键测试小狗导航状态。

V2 用法：

- 保留或复制一个 V2 版本，用于测试小狗行为。

### `DogEyeFollowRay`

当前行为：

- 让 UI 眼睛 `RectTransform` 朝向鼠标、手柄或手部射线在 canvas 上的 hit point。

V2 用法：

- 如果小狗 UI 或开场脸部图形需要 pointer-aware 眼睛动作，可以复用。

### `FriendButtonUI`

当前行为：

- 旧版 friend button 选择辅助。

V2 用法：

- 除非后续重新加入找朋友功能，否则大概率不需要。

### `PathDefinition`

当前行为：

- 保存 path id 和子物体 waypoints。

V2 用法：

- 仍可用于手工测试路径，但不足以支持动态真实场地导航。

### `FireworkAutoDestroy`

当前行为：

- 延迟销毁特效对象。

V2 用法：

- 可复用于临时奖励 / 惊喜 VFX。

## V2 关键重构总结

保留：

- Logo 启动流程。
- 小狗模型、prefab、动画、表情资源。
- 小狗动画和表情逻辑基础。
- `LineRenderer` 路线可视化思路。
- UI 视觉资源和字体。
- 烟花 / 特效自动清理思路。

替换或大幅改造：

- 旧网格地图。
- 旧 friend / location 选择。
- 旧 path prefab library。
- 相对用户生成的 route root。
- 旧 Europa-Park 地图内容。
- 旧 NavigationController 阶段模型。

需要新增系统：

- 场地坐标标定。
- 可行走区域和障碍物数据。
- 景点 registry。
- 基于景点数据的小地图和大地图。
- 动态路线生成。
- 小狗可行走区域内的位置选择。
- 可收集物显示 / 抓取 / 放置。
- 小狗饰品 attach。
- 奖励显示流程。

## 2026-07-01 V2 新增脚本记录

本次开始实现阶段 1：场地标定原型，新增脚本位于 `Assets/PuppyPath/Scripts/V2`：

- `VenueMapDefinition.cs`：新增 `ScriptableObject` 数据资产类型，用于保存真实场地地图尺寸、Photo Wall 原点、3.45 m 比例线、地图到 Unity 的坐标转换参数以及景点 / collectible spawn point 数据。
- `VenueCoordinateMapper.cs`：新增纯转换工具，统一地图像素坐标和 Unity 世界坐标之间的换算，当前约定为地图北方对应 Unity `+Z`。
- `VenueCalibrationDebugView.cs`：新增 Scene 视图调试组件，用 Gizmos 绘制 VenueOrigin、地图边界、3.45 m 比例线、可行走区域、导航图、测试路线和景点 marker。
- `VenuePathfinder.cs`：新增第一版手工 waypoint graph 寻路工具。
- `VenueRouteLineController.cs`：新增第一版场地固定路线 LineRenderer 绘制组件；可接收地图像素起终点，也可接收已计算好的世界坐标路线。
- `VenueNavigationRuntime.cs`：新增第一版 V2 场地导航运行时，用 HMD 世界位置生成到景点的真实场地路线，刷新 `VenueRouteLineController`，并可临时驱动 `DogGuideController`。
- `VenueMapUiController.cs`：新增第一版 V2 小地图 / 大地图 UI 控制器，将用户和景点的真实场地坐标映射到 UI marker，并把大地图景点点击接入 `VenueNavigationRuntime`。
- `VenueMapMarker.cs`：新增地图 marker 组件，用于保存 attraction id、显示选中状态和处理点击。
- `VenueMapOpenButton.cs`：新增小地图打开大地图的轻量点击入口。
- `PuppyPathV2FlowController.cs`：早期 storyboard flow 原型。2026-07-02 决定当前 UI flow 弃用该脚本，改回由旧 `NavigationController` / `NavigationHUDController` 管理 Intro、Map、NavigationHudPanel。
- `VenueAlignmentManager.cs`：新增现场校准组件，可在用户站到真实 `VenueOrigin` 并面朝地图北方时，将 `VenueContentRoot` 对齐到当前 HMD。
- `VenueSpatialAnchorBootstrap.cs`：新增 Meta Spatial Anchor bootstrap，可在 `VenueOrigin` 创建 `OVRSpatialAnchor`，并把 `VenueContentRoot` 挂到 anchor 下。
- `VenueWalkableGridVisualizer.cs`：新增黄色可行走区域网格可视化，用于 Quest 真机内确认地图对齐、比例和方向。
- `VenueMapReferencePlane.cs`：新增地图参考平面组件，可把当前地图图片按场地坐标铺到 XZ 平面。
- `Editor/VenueCalibrationDebugViewEditor.cs`：新增 Scene 视图编辑工具，可拖拽修改 `VenueMapDefinition` 中的点位数据，并可在 Scene 左上角 `Nav Graph Editing` 面板中手动连接 / 断开 nav graph 蓝线。

这些脚本不替换现有 `DogGuideController`、旧 UI 或旧路径系统，只是为 V2 的真实场地坐标层打基础。

当前 `VenueMapDefinition` 还提供调试菜单：

- `Use PuppyPath Source Map Size`：将地图尺寸设为原图 `2468 x 2160`。
- `Use Confirmed V2 Orientation`：将 `originWorldPosition` 设为 `(0, 0, 0)`，`venueYawDegrees` 设为 `0`。
- `Populate Default Attractions`：生成 10 个默认景点数据条目。
- `Populate Detected Attraction Spawn Pixels`：填入从 `map_with_spawn_points.jpg` 自动检测出的 10 个橙色圆点坐标。
- `Populate Detected Walkable Draft`：填入自动检测出的第一版可行走外轮廓和中央障碍区域。
- `Populate Detected Nav Graph Draft`：填入第一版 waypoint graph 草稿。
- `Populate Detected Draft Map Data`：一次性执行以上地图草稿填充。
- `Log Calibration Summary`：在 Console 打印当前比例尺、米/像素、世界比例线距离和景点数量。

## 2026-07-01 开发线 B 新增脚本记录

开发线 B（小狗饰品 / 奖励 / 动画测试，含并入的景点渐显 + 抓取收集层）新增脚本位于 `Assets/PuppyPath/Scripts/V2/Accessory`：

- `DogAccessoryAnchors.cs`：挂在小狗 prefab 根节点，统一暴露一个挂载点（2026-07-02 根据设计师意见调整：不再按 Head/Face/Neck/Back/Tail 查找具体骨骼，`GetAnchor(slot)` 现在忽略 slot 参数，统一返回根节点 transform，也可用 `Root Anchor` 字段手动覆盖）。`DogAccessorySlot` 枚举保留在 `DogAccessoryDefinition` 上作为描述性分类标签，不再驱动挂载点查找。
- `DogAccessoryDefinition.cs`：`ScriptableObject`，描述一件饰品（`id`、描述性 slot 标签、prefab、位置/旋转/缩放偏移）。**注意**：多个饰品要同时共存必须使用不同的 `id`——`DogAccessoryManager` 按 `id` 去重，同 `id` 重复 attach 会先 detach 旧的再挂新的。
- `DogAccessoryManager.cs`：订阅 `DogGuideController.DogSpawned` 事件，提供 `AttachAccessory`/`DetachAccessory`/`HasAccessory`/`ClearAll`，挂载时把饰品实例化为挂载点的子物体（跟随小狗整体位置/朝向移动，不跟随骨骼自身动画细节）。
- `DogAccessoryTestKeys.cs`：测试专用键盘脚本，风格对齐现有 `DogStateTester`/`DogNavStateTester`，只应放在测试场景里。
- `RewardDefinition.cs`：`ScriptableObject`，把"景点 -> 饰品 -> 奖励"合一，不额外建 mapping 资产。
- `RewardRevealController.cs`：提供独立可测的 `ShowReward(string attractionId)` API，挂饰品 + 触发 `DogGuideController.PlayOneShotState` + 可选烟花 + 显示 TMP 奖励面板，不依赖 `VenueMapDefinition`/`AttractionDefinition`。
- `CollectibleItemDefinition.cs`：`ScriptableObject`，描述一个景点的悬浮收藏物（item prefab、3 档渐显距离参数）。
- `FloatingCollectibleItem.cs`：挂在收藏物 prefab 上，按传入 alpha 控制材质透明度和可见性，收集后隐藏并禁用交互组件。
- `AttractionTrigger.cs`：测试阶段的景点出现点（挂在场景里手动摆放的 Transform 上，不读取 `VenueMapDefinition` 真实坐标），按用户距离计算渐显 alpha。
- `CollectibleGrabHandler.cs`：挂在收藏物 prefab 上，订阅 Meta XR Interaction SDK `Oculus.Interaction.Grabbable` 的 `WhenPointerEventRaised` 事件，释放时判断是否放到小狗身上，命中则标记 session 内已收集并调用 `RewardRevealController.ShowReward`。

对 `DogGuideController.cs` 的改动（仅追加，未修改任何现有私有逻辑）：

- `public event System.Action<GameObject> DogSpawned`：`BeginGuiding` 完成 dog 初始化后触发。
- `public GameObject CurrentDog`：只读属性，暴露当前生成的小狗实例。
- `public void PlayOneShotState(string stateName)`：调用现有私有 `PlayAnimation` 的公开包装，供 `RewardRevealController` 等 V2 脚本从外部触发一次性动画状态。

抓取交互确认基于项目实际在用的 **Meta XR Interaction SDK**（`com.meta.xr.sdk.interaction`，`PointableCanvasModule` 已经用于现有 UI 点击），而不是 XR Interaction Toolkit（已安装但未接入任何现有场景）。`Grabbable` + `HandGrabInteractable`/`DistanceHandGrabInteractable` 组件需要在 Unity 编辑器里手动挂到收藏物 prefab 上，代码侧只依赖 `Grabbable` 的事件接口，不强绑定具体 interactable 类型。

小狗 prefab 上的饰品锚点、占位测试数据资产、`DogAccessoryTestScene` 测试场景、收藏物 prefab 上的 Meta SDK 组件挂载，均为纯 Unity 编辑器操作，未随本次代码提交自动生成，需要在编辑器中手动完成。

## 必须遵守的文档同步规则
当前 `VenueCalibrationDebugView` 还提供：

- `Create Map Reference Plane`：在当前 debug object 下创建或更新 `VenueMapReferencePlane` 子物体。
- `Show Map Reference Plane` / `Hide Map Reference Plane`：显示或隐藏地图参考平面。
- Scene Editing 开关：允许在 Scene 视图中直接拖动 calibration points、attraction、walkable polygon、obstacle polygon、nav graph；`Edit Nav Graph In Scene` 开启时还支持选中两个 waypoint 后手动 `Connect` / `Disconnect` 邻居连接。

当前 `VenueNavigationRuntime` 提供：

- `StartNavigationToAttraction(string attractionId)`：从当前 HMD / XR Camera 位置开始，生成到指定景点的路线。
- `StopNavigation()`：清除路线并停止临时小狗导航。
- `Start Test Navigation` / `Stop Navigation` 右键菜单：用于不接 UI 时在场景中直接测试。
- 只向旧 `DogGuideController` 发送非负面导航状态，避免 V2 自由移动阶段触发旧的生气 / 迷路反馈。

当前真机校准 / 可视化工具提供：

- `VenueContentRoot` 结构约定：所有场地固定内容都应作为这个 root 的子物体，由校准组件统一移动和旋转。
- `VenueAlignmentManager`：快速现场测试时使用 HMD 当前位置和朝向对齐场地。
- `VenueSpatialAnchorBootstrap`：为后续持久化 Spatial Anchor 对齐打基础；使用前需要在 `OVRManager` 开启 `Anchor Support`。
- `VenueWalkableGridVisualizer`：用半透明黄色格子显示当前 `walkableAreas - obstacleAreas` 结果，解决 Quest 内没有可视化内容的问题。

## 2026-07-01 Storyboard / Canvas 复用结论

Storyboard 明确了 `MiniMap` 是游戏 HUD 式局部小地图，不是完整场地缩略图。旧 `Canvas` 应继续作为 V2 主 UI 容器使用：

- 保留 `UIBootSequence`、`CanvasFollowHead`、`OVROverlayCanvas`、`GraphicRaycaster`、`PointableCanvasModule`。
- 保留旧按钮、字体、气泡和地图图片资源作为视觉资产。
- 停用旧 `PuppyPathSelectionUI` 的网格选择逻辑、旧 `NavigationController` 的 path prefab 流程、旧 `PathPreviewController` 的静态路径库。
- 在旧 Canvas 中新增或改造 panel：`IntroPanel`、`FreeRoamHud`、`BigMapPanel`、`NavigationHud`、`RewardPanel`、`ItemGrabPanel`、`RewardPopupPanel`。
- 当前不再使用 `PuppyPathV2FlowController` 管理 storyboard 状态流。使用旧 `NavigationController` 管理 flow，`VenueMapUiController` 只负责地图、景点 marker、地图路线显示和触发场地导航。

## 2026-07-01 旧导航复用边界

旧导航系统可复用的部分：

- `DogGuideController` 中的小狗 prefab 实例化、动画 state 播放、表情贴图切换、叫声音效、基础移动插值。
- `NavigationRuntimeController` 中的沿路线进度、距离路线中心线、到达距离、HMD 移动检测等思路。
- `PathPreviewController` 中的 `LineRenderer` 路线绘制思路。

旧导航系统不应直接沿用的部分：

- 静态 path prefab 作为真实场地路线数据源。
- `RouteRootSpawner` 相对用户生成 route root 的方式。
- 旧的 `Waiting` / `GettingFarther` / `Lost` 负面反馈逻辑。
- 任何会让小狗主动走到用户身后、原地坐等、因为用户自由移动而“生气”的行为。

V2 应新增 wrapper / runtime：

- `DogVenueFollower`：负责小狗在真实场地内保持前方、跟随 HMD 速度、避开不可行走区域、接近宝藏时开心反馈。
- `VenueNavigationRuntime`：负责使用 `VenuePathfinder` 结果替代旧 path prefab，并向小狗提供推荐方向。

## 必须遵守的文档同步规则

当任何现有脚本被重新用途化、替换、删除或为 V2 大幅修改时，必须更新本文档，让后续开发者知道项目仍然依赖哪些旧内容。
