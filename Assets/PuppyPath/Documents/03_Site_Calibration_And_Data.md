# 场地标定和数据规范

最后更新：2026-07-02

## 核心要求

真实活动场地和 Unity 活动场景必须一比一匹配。Unity 距离使用常规约定：

- 1 Unity unit = 1 m 真实世界距离。

用户地图中红色标记的墙体是当前比例尺：

- 真实世界长度：3.45 m。

地图中的黄色区域是人和小狗可以行走的区域。

## 当前已确认的 V2 标定决定

- `VenueOrigin`：Photo Wall 右上角的红点位置。
- Unity `+Z`：地图北方，也就是地图向上方向。
- 地图像素坐标约定：以图片左上角为 `(0, 0)`，`+X` 向右，`+Y` 向下。
- Unity 世界坐标约定：`+X` 对应地图东方 / 右方，`+Z` 对应地图北方 / 上方。
- 当前地图图片尺寸：`2468 x 2160` 像素。
- 比例尺：红色竖向墙体线段真实长度为 `3.45 m`。
- 当前实现入口：`Assets/PuppyPath/Scripts/V2/VenueMapDefinition.cs` 保存地图、原点、比例尺和景点数据；`VenueCalibrationDebugView.cs` 在 Scene 视图中绘制原点、比例尺、地图边界和景点调试点。

仍需在 Unity Inspector 中精确填写：

- `mapOriginPixel`：Photo Wall 右上角红点在原图中的像素坐标。
- `scalePointAPixel` / `scalePointBPixel`：3.45 m 红色竖向比例线两端在原图中的像素坐标。
- 每个景点 / collectible spawn point 的像素坐标。

当前第一批已填写值：

```text
mapOriginPixel = (716, 820)
scalePointAPixel = (1003, 715)
scalePointBPixel = (1003, 833)
scaleSegmentMeters = 3.45
```

由以上值计算：

```text
pixelDistanceOfRedWall = 118 px
metersPerPixel = 3.45 / 118 = 0.029237288 m/px
```

这表示如果地图像素坐标相差 100 px，对应 Unity 世界距离约为 2.92 m。

## 标定流程

## Meta Quest 场地对齐建议

为了让 Meta Quest 中的 Unity 场景与真实世界的朝向、方位和位置尽量一致，推荐使用“固定实体校准点 + Spatial Anchor + 启动校准检查”的方案。

推荐方案：

1. 在真实场地中选择一个固定、不会移动、容易重新找到的位置作为主校准点，例如墙角、固定柱子、固定家具边角或入口附近的稳定结构。
2. 在 Unity 地图中把这个点定义为 `UnityOrigin` 或 `VenueOrigin`。
3. 再选择第二个固定点来确定朝向，例如沿某面长墙的另一个点，用来定义 Unity `+Z` 或场地 forward。
4. 第一次现场部署时，让开发者/工作人员站在主校准点，按校准按钮创建或保存 Meta Quest Spatial Anchor。
5. 使用主锚点决定世界原点，使用第二参考点或已知墙体方向决定旋转朝向。
6. 每次启动 app 时尝试加载已保存的 anchor；加载成功后把整个场地根节点对齐到 anchor。
7. 启动后显示一个隐藏式或开发者可见的校准检查：例如在真实 3.45 m 红墙两端显示两个虚拟点，让工作人员确认是否贴合。

当前第一版实现：

- 使用 `VenueContentRoot` 作为所有场地固定内容的父物体。
- 使用 `VenueAlignmentManager` 做临时现场校准：站在真实 `VenueOrigin`，也就是 Photo Wall 右上角原点，面朝地图北方 / Unity `+Z`，启动时自动把 `VenueContentRoot` 对齐到当前 HMD。
- 使用 `VenueSpatialAnchorBootstrap` 在 `VenueOrigin` 创建 Meta `OVRSpatialAnchor`，并把 `VenueContentRoot` 挂到 anchor 下。正式保存 / 加载 anchor 前，必须在 `OVRManager` 中开启 `Anchor Support`。
- 使用 `VenueWalkableGridVisualizer` 在 Quest 中显示黄色可行走格子，作为地图对齐、比例和方向的第一层可视化。
- 当前 anchor bootstrap 已保存 UUID 到 PlayerPrefs，但后续仍需要补完整的“按 UUID 加载已保存 anchor 并自动恢复场地”的用户流程。

关于二维码：

- 二维码可以作为辅助工具，例如贴在主校准点附近，帮助工作人员确认“这是哪个校准点”。
- 如果后续接入图像识别，二维码也可以用于快速选择对应场地配置。
- 不建议只依赖二维码作为唯一空间定位依据，因为识别角度、光照、遮挡、打印位置误差都会影响稳定性。
- 更可靠的方式是使用 Quest 的空间锚点 / 场地锚点能力，把二维码当作辅助标识，而不是唯一坐标系统。

现场对齐最低要求：

- 至少 1 个主校准点决定位置。
- 至少 1 个方向参考决定朝向。
- 3.45 m 红墙用于比例尺验证。
- 每次地图更新后必须重新验证原点、朝向、比例尺和景点位置。

### 步骤 1：选择地图参考点

在红色墙体上选择两个点：

- `ScalePointA`
- `ScalePointB`

这两个点必须代表真实 3.45 m 墙体线段的两个端点。

### 步骤 2：在地图空间中测量同一线段

在导入后的地图坐标系统中，测量 `ScalePointA` 与 `ScalePointB` 的距离。

可能使用的地图空间：

- 如果使用原始地图图片，则使用像素坐标。
- 如果地图作为 `RectTransform` 使用，则使用 UI local 坐标。
- 如果地图作为 Unity 平面放置，则使用 plane local 坐标。

### 步骤 3：计算比例

公式：

```text
metersPerMapUnit = 3.45 / distance(ScalePointA, ScalePointB)
```

如果使用像素：

```text
metersPerPixel = 3.45 / pixelDistanceOfRedWall
```

### 步骤 4：选择 Unity 原点

选择一个在真实场地和地图中都能稳定找到的位置。候选点：

- 靠近主入口的角点。
- 永久墙体角点。
- 靠近场地中心的固定建筑特征。

选定后记录：

```text
UnityOriginName = VenueOrigin_PhotoWallUpperRight
MapOriginPoint = Photo Wall 右上角红点，像素坐标待 Inspector 精确填写
RealWorldOriginDescription = Photo Wall 右上角对应的现场固定点
```

### 步骤 5：选择 Unity 前方方向

选择地图上的哪个方向对应 Unity `+Z`。

选定后记录：

```text
UnityForward = +Z
MapDirectionForForward = 地图北方 / 图片向上方向
RotationDegrees = 0，除非现场 Spatial Anchor 对齐时需要整体 yaw offset
```

### 步骤 6：创建坐标转换

每个地图点都应转换成 Unity 世界坐标：

```text
mapDelta = mapPoint - mapOriginPoint
scaledDeltaMeters = mapDelta * metersPerMapUnit
unityPosition = rotation * scaledDeltaMeters + unityOriginWorldPosition
```

Y 通常使用地面高度：

```text
unityPosition.y = 0
```

只有悬浮物品、UI 提示、小狗饰品 anchor、奖励特效需要调整 Y。

## 可行走区域数据

黄色区域应被转换成一个或多个 polygon。

推荐数据结构：

```text
WalkableArea
- id
- displayName
- polygonPointsInMapSpace
- polygonPointsInWorldSpace
```

规则：

- 用户和小狗的目标点必须在可行走 polygon 内。
- 路线 line 必须保持在可行走 polygon 内。
- 景点应位于可行走区域内，或非常靠近可行走区域。
- 如果某个点在黄色区域外，只有在不会穿墙的情况下，才能把它吸附到最近的合法点。

当前第一版实现说明：

- `VenueMapDefinition.walkableAreas` 保存一个或多个 `WalkableAreaDefinition`。
- 每个 polygon 点使用原始地图像素坐标，约定仍是左上角 `(0, 0)`、`+X` 向右、`+Y` 向下。
- polygon 点按顺时针或逆时针顺序填写，不需要在末尾重复第一个点。
- 第一版寻路会沿路线段采样多个点，并检查采样点是否在任意可行走 polygon 内。
- 如果 `walkableAreas` 为空，当前代码会临时把所有地图点当作可行走，方便早期调试；正式路线测试前必须填入 polygon。

需要人工提供的坐标：

- 是的，需要提供黄色可行走区域关键拐角的二维像素坐标。
- 不必一开始就把每个微小凹凸都描出来；第一版可以用较粗的外轮廓覆盖主通道。
- 对狭窄走廊、墙角、岔路口附近要更准确，因为这些地方最容易导致路线或小狗穿墙。

## 手工导航图数据

第一版不直接从 polygon 自动生成完整导航网格，而是使用手工 waypoint graph。

推荐数据结构：

```text
VenueNavGraph
- nodes

VenueNavNode
- id
- mapPixel
- neighborNodeIds
```

填写原则：

- waypoint 放在黄色区域中心线附近，而不是贴墙。
- 每个转弯处至少一个 waypoint。
- 每个景点附近至少一个 arrival waypoint。
- 只连接能直线走过去且不穿墙的相邻 waypoint。
- 邻居关系建议双向填写，除非未来有单向动线要求。

调试方式：

- `VenueCalibrationDebugView` 中打开 `Draw Walkable Areas` 和 `Draw Nav Graph`。
- 绿色线表示可行走 polygon。
- 红色线表示 obstacle polygon。
- 蓝色线表示导航 graph。
- 红橙色 nav edge 表示该连接段当前不被认为可通行，通常是穿过 obstacle 或离开 walkable polygon。
- `Edit Nav Graph In Scene` 开启后，Scene 左上角的 `Nav Graph Editing` 面板可手动修改蓝线。点击两个蓝点旁边的小青色选择点后，使用 `Connect` 添加双向邻居连接，使用 `Disconnect` 删除双向邻居连接。
- 在 `Test Path` 中填写测试起点像素和目标景点 id，可显示橙色测试路线。

Scene 手工校准方式：

- 在 `VenueCalibrationDebugView` 右键菜单执行 `Create Map Reference Plane`，将地图图像铺在标定坐标系下方。
- 可用 `Show Map Reference Plane` / `Hide Map Reference Plane` 显示或隐藏地图底图。
- 启用 `Scene Editing` 中对应开关后，可在 Scene 视图直接拖动：
  - 地图原点 `mapOriginPixel`。
  - 3.45 m 比例尺两端 `scalePointAPixel` / `scalePointBPixel`。
  - 景点 / collectible spawn point。
  - 可行走 polygon 顶点。
  - obstacle polygon 顶点。
- nav graph waypoint。
- 拖动后坐标会自动从 Unity world position 转回地图像素坐标并保存到 `VenueMapDefinition`。
- `Populate Detected Nav Graph Draft` 生成的是较密集的中心线 waypoint 草稿，并只保留当前可行走检测认为合法的连接。
- 拖动 `mapOriginPixel` 会保持其他已设置点和线的世界布局不动。
- 拖动 `scalePointAPixel` / `scalePointBPixel` 会保持当前 meters-per-pixel 不变，避免地图和路线被重新缩放。
- 手动连接或断开 nav graph 蓝线会直接写回 `VenueMapDefinition.navGraph.nodes[*].neighborNodeIds`，并支持 Unity Undo。
- 如果需要真正重新计算比例尺，后续应使用单独的显式校准操作。

## 墙体和障碍物数据

黑色墙体以及非黄色内部区域应转换成 blocked geometry 或 obstacle polygon。

推荐数据结构：

```text
Obstacle
- id
- displayName
- polygonPointsInMapSpace
- polygonPointsInWorldSpace
```

规则：

- 小狗不能穿过这些区域。
- 路径段不能穿过这些区域。
- 如果用户正前方的小狗目标点会穿过障碍物，则选择左前方或右前方 fallback。

## 景点和虚拟物品出现点数据

当前新版地图中，黄色圆点表示虚拟物品出现位置。景点文字 label 只用于命名和语义说明；真正的 collectible spawn point 应以后续标定出的黄色圆点坐标为准。

当前已记录 10 个虚拟物品出现点：

| ID | 地图原标记 | 推荐英文显示名 | 中文说明名 | 地图位置 | 世界位置 | 透明度规则 | 物品 | 饰品 | 奖励 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `chess` | Chess | Checkmate Corner | 棋遇小屋 | 左上活动区，Chess 标记附近的上方黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 棋子 / TBD | TBD | TBD |
| `couch` | Couch | Cozy Couch Cove | 软乎乎沙发湾 | 左上活动区，Couch 标记附近的下方黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 抱枕 / TBD | TBD | TBD |
| `photo_wall` | Photo Wall | Snapshot Studio | 咔嚓照相馆 | 左中区域，Photo Wall 标记附近的黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 相机或相框 / TBD | TBD | TBD |
| `goodies` | Goodies | Treat Trove | 甜甜补给站 | 左下内凹区域，Goodies 标记附近的黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 零食袋 / TBD | TBD | TBD |
| `book_wall` | Book Wall | Storybook Wall | 故事书墙 | 右上中部走廊，Book Wall 标记附近的黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 书签、贴纸或徽章 / TBD | TBD | TBD |
| `tap_water` | Tap Water | Splash Stop | 汪汪补水站 | 底部中间偏右，Tap Water 标记附近的黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 水滴、杯子或小水壶 / TBD | TBD | TBD |
| `ice_cream_shop` | Ice Cream Shop | Scoop Station | 冰淇淋小站 | Tap Water 与 Drink Shop 之间、灰色横条右侧附近的黄色点；准备作为冰淇淋店 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 冰淇淋球或甜筒 / TBD | TBD | TBD |
| `drink_shop` | Drink Shop | Fizzy Fridge | 气泡饮料铺 | 右下区域，右侧 Fridge / 饮料店标记附近的黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 可乐罐或饮料杯 | TBD | 免费可乐 / TBD |
| `piano` | Piano | Melody Corner | 音符小舞台 | 右侧中下区域，Piano 标记附近的黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 音符 / TBD | TBD | TBD |
| `plants` | Plants | Garden Patch | 小狗花园 | 右下角 Plants 虚线区域内的黄色点 | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | 小植物或叶子 / TBD | TBD | TBD |

后续标定要求：

- 把每个黄色圆点转换为 `mapPosition` 和 `worldPosition`。
- 如果景点文字位置与黄色点位置不同，导航目的地和透明度计算应使用黄色点或该点附近的可行走目标点。
- 如果黄色点距离墙体太近，应额外定义一个用户可到达点 `arrivalPoint`，但物品本身仍从黄色点出现。

### 自动检测出的第一版 collectible spawn 坐标

来源图片：`Assets/PuppyPath/Maps/map_with_spawn_points.jpg`，原图尺寸 `2468 x 2160`。

检测方法：按橙色圆点颜色阈值提取 connected components，取每个圆点 component 的中心点。该部分置信度较高。

| ID | 自动检测中心像素坐标 |
| --- | --- |
| `chess` | `(262, 470)` |
| `couch` | `(198, 663)` |
| `photo_wall` | `(624, 859)` |
| `book_wall` | `(1710, 857)` |
| `goodies` | `(623, 1388)` |
| `piano` | `(2181, 1642)` |
| `tap_water` | `(1400, 1752)` |
| `ice_cream_shop` | `(1660, 1784)` |
| `drink_shop` | `(1930, 1804)` |
| `plants` | `(2339, 1956)` |

注意：如果后续地图图片更新或圆点位置移动，必须重新检测或手动更新这些坐标。

### 自动检测出的第一版可行走 / 导航草稿

当前 `VenueMapDefinition` 提供 `Populate Detected Draft Map Data` 菜单，用于一键写入第一版草稿：

- `main_walkable_auto_draft`：紫色可行走区域外轮廓的粗略 polygon。
- `central_block_auto_draft`：中间大灰色障碍区域的粗略 obstacle polygon。
- `navGraph`：14 个手工筛选后的中心线 waypoint。

这部分置信度中等，需要人工在 Scene 视图中检查：

- 绿色外轮廓是否大致贴住紫色区域边界。
- 红色障碍轮廓是否覆盖中间不可穿越区域。
- 蓝色 waypoint 是否都落在紫色可行走区域中心附近。
- 蓝色连接线是否有穿墙；如果穿墙，删除该邻居连接或移动节点。
- 橙色测试路线是否避开灰色不可走区域。

## 小地图数据

小地图应该使用与场地世界坐标相同的数据来源。根据 storyboard，`MiniMap` 指游戏 HUD 式局部小地图，只显示用户附近不远处的地理信息；完整场地选择由 `BigMap` 负责。

必须显示的 marker / line：

- 每个景点的狗爪 marker。
- 可选：当前选中目标 marker。
- 地图道路 line。
- 可选：选中目标后的路线预览 line。当前默认关闭，不在地图 UI 上显示导航路线。

世界坐标到小地图 UI 的转换：

```text
worldPosition -> mapPosition -> normalizedMapPosition -> RectTransform anchoredPosition
```

这个转换必须使用与世界场景相同的原点、比例和旋转，避免小地图和真实位置漂移。

2026-07-01 第一版实现：

- `VenueMapUiController` 负责完整大地图 UI 的坐标映射。
- 当前版本取消局部小地图，只使用完整大地图。地图 UI 使用普通 `Image` 显示场地图片，不再需要 `RawImage` 或 `RawImage.uvRect`。
- `VenueMapDefinition.mapTexture` 是校准参考和坐标数据来源，不再默认覆盖 UI 上的美化地图。若需要显示 definition 贴图，才手动开启 `Use Map Definition Texture For Display`。
- `XR Camera` 当前世界位置会先通过 `VenueContentRoot.InverseTransformPoint` 转成场地本地坐标，再调用 `VenueMapDefinition.WorldToMapPixel` 得到用户地图像素坐标。
- 景点 marker 使用 `AttractionDefinition.GetArrivalPixel()` 放置；如果景点有自定义 arrival pixel，则优先显示 arrival，否则显示 collectible spawn pixel。
- 完整大地图显示全部景点 marker，当前不显示用户位置 marker。
- 大地图景点 marker 点击后调用 `VenueNavigationRuntime.StartNavigationToAttraction(attractionId)`。朋友列表导航已取消，朋友列表区域只作为 intro / flow 文案区域。
- `CancelNavigation()` 调用 `VenueNavigationRuntime.StopNavigation()`，并清除当前选中 marker。
- `VenueMapOpenButton` 可挂在打开地图按钮物体上，用于从 `NavigationHudPanel` 重新打开地图。
- `MapImage` 本身不应吃射线；运行时会关闭旧地图图片的 `Image.raycastTarget`，道路和路线 overlay 也不会挡住景点 marker。`Markers` overlay 会强制创建在 `MapImage` 下，保证 marker 坐标和点击层级正确。
- 到达过的景点由 `VenueMapUiController.MarkAttractionVisited(attractionId)` 标记，并使用 Inspector 中的 `Visited Attraction Marker Color` 保持变色。

## 小狗位置规则

自由行走：

- 优先小狗目标点：用户前方 1.5-2 m。
- 允许距离范围：1-3 m。
- 不主动把小狗放到用户身后。
- 当前随便逛逛模式会调用 `VenueNavigationRuntime.StartFreeRoamGuiding()`，复用 `DogGuideController` 生成小狗并持续跟随。
- 如果正前方被阻挡：
  - 尝试左前方。
  - 尝试右前方。
  - 尝试更靠近用户的正前方点。
  - 最后选择前半球附近最近且可见的可行走点。

导航：

- 小狗应该在路线前方，而不是贴在用户脚边。
- 小狗带路距离需要现场调试。
- 小狗必须保持在可行走区域内。
- 小狗不应该为了去下一个路线点而穿墙。

## 运行时路线验证

地图数据处理完成后，使用 `VenueNavigationRuntime` 做第一轮运行时验证。

推荐场景接线：

- 创建空物体 `VenueContentRoot`，把场地固定内容放到它下面。
- `VenueCalibrationDebug`、`VenueRouteLine`、`VenueWalkableGrid`、后续景点物品都应作为 `VenueContentRoot` 的子物体。
- 在场景中创建 `VenueNavigationRuntime` 空物体，挂载 `VenueNavigationRuntime`。
- `Map Definition` 指向当前 `VenueMapDefinition`。
- `XR Camera` 指向 `OVRCameraRig` 下的 CenterEye / HMD transform。
- `Venue Content Root` 指向 `VenueContentRoot`。
- `Route Line Controller` 指向带 `VenueRouteLineController` 的路线物体；当前推荐同时把 `Walkable Grid Visualizer` 字段指向 `VenueWalkableGridVisualizer`，让路线通过 grid 变色显示。
- 可选：`Dog Guide Controller` 指向现有小狗控制器，用于临时验证小狗是否能收到推荐方向。

真机可视化接线：

- 在 `VenueContentRoot` 下创建 `VenueWalkableGrid`。
- 给 `VenueWalkableGrid` 添加 `MeshFilter`、`MeshRenderer`、`VenueWalkableGridVisualizer`。
- `Map Definition` 指向当前 `VenueMapDefinition`。
- `Cell Size Meters` 建议先用 `0.5`，`Cell Fill Ratio` 建议 `0.82`，颜色使用半透明黄色。

现场临时校准方式：

- 在场景中创建 `VenueAlignmentManager`。
- `Map Definition` 指向当前 `VenueMapDefinition`。
- `XR Camera` 指向 CenterEye / HMD transform。
- `Venue Content Root` 指向 `VenueContentRoot`。
- 勾选 `Align On Start` 和 `Align Yaw To Head Forward`。
- 真机启动前，人站在真实 Photo Wall 右上角原点，面朝地图北方 / Unity `+Z`。
- 启动后黄色格子应铺在真实可行走区域附近；如果整体旋转偏差，调整 `Additional Yaw Degrees` 后重新 build / Play。

Spatial Anchor 前置设置：

- 选中 `OVRCameraRig`。
- 在 `OVRManager > Quest Features > General` 开启 `Anchor Support`。
- 不需要多人共享时，不必开启 `Anchor Sharing Support`。
- 在 `VenueContentRoot` 附近创建 `VenueSpatialAnchorBootstrap`，设置 `Map Definition` 和 `Venue Content Root`；需要测试时勾选 `Create Anchor On Start`。

测试方式：

- 在 `Test Destination Attraction Id` 中填写景点 id，例如 `photo_wall`、`drink_shop`、`plants`。
- 组件右键执行 `Start Test Navigation`。
- 地面路线应从当前 HMD 附近生成到目标景点，并保持在绿色可行走区域内。
- 走动时 runtime 会按间隔重新规划路线；如果临时找不到新路线，会保留上一条有效路线。
- 组件右键执行 `Stop Navigation` 可清除路线并停止小狗临时导航。

Route / Grid 注意事项：

- 当前不再默认使用 `LineRenderer` 显示导航路线。`VenueRouteLineController.Draw Line Renderer` 默认关闭。
- `VenueRouteLineController.ShowWorldRoute(...)` 成功后，会调用 `VenueWalkableGridVisualizer.ShowWorldRoute(...)`，把路线附近的可行走 grid 切到 `Route Grid Color`，例如紫色。
- 如果仍看不到路线高亮，优先确认：`VenueNavigationRuntime.Route Line Controller` 是否已拖入路线物体；`VenueRouteLineController.Walkable Grid Visualizer` 是否已拖入 grid；`VenueWalkableGridVisualizer.Route Grid Color` alpha 是否足够高；`StartNavigationToAttraction` 是否成功。
- 如需调试旧线条，可临时开启 `VenueRouteLineController.Draw Line Renderer`。

注意：当前 `VenueNavigationRuntime` 只负责路线和推荐方向，不等于最终小狗行为。最终仍需要 `DogVenueFollower` 来负责小狗始终保持在用户前方、避障、自由行走和宝藏接近反馈。

## 大地图道路

2026-07-02 更新：大地图 UI 使用 `VenueMapDefinition.navGraph` 显示道路网络。景点 marker 使用 `AttractionDefinition.GetArrivalPixel()`，用户只能通过景点 marker 选择导航目标。

当前规则：

- 地图任意点击不再生成目的地 marker。
- 目的地必须来自景点 marker。
- 原朋友列表 UI 不参与导航；旧 `Path_Kevin`、`Path_Ying` 等 prefab 不再用于当前 flow。

## 测试景点 / Placeholder 物品

2026-07-02 更新：新增两个测试工具。

- `VenueAttractionPlaceholderSpawner`：按 `VenueMapDefinition.attractions` 在每个景点的 `collectibleSpawnPixel` 生成一个圆球 placeholder。之后拿到正式模型后，可替换 `Placeholder Prefab` 或扩展为按景点 id 映射不同 prefab。
- `VenueOriginTestAttractionGenerator`：用于现场调试。组件菜单执行 `Generate Origin Test Attractions` 后，会在当前地图原点附近的可行走区域自动挑选 3 个不太贴近的点，生成测试景点数据，并自动建立对应 navgraph 节点和连接。它不生成额外 3D 球；虚拟物品 placeholder 统一由 `VenueAttractionPlaceholderSpawner` 按 `collectibleSpawnPixel` 生成。由于原点后续可能移动，原点移动后可以重新执行该菜单刷新测试数据。

2026-07-02 更新：当前 `VenueMapDefinition.asset` 中三个 origin test 景点已命名为：

| id | Display Name | 中文名 | 当前 reward |
| --- | --- | --- | --- |
| `origin_test_1` | Biscuit Bounce Booth | 饼干蹦蹦站 | `hat_reward` |
| `origin_test_2` | Wagging Wonder Stop | 摇尾惊喜站 | `shirt_reward` |
| `origin_test_3` | Sniffle Spark Station | 嗅嗅闪光站 | `socks_reward` |

`UIScene` 的 `VenueCollectibleSpawner.explicitPlacements` 已把以上三个测试景点分别绑定到对应 reward。当前测试版在景点处生成约小狗高度的橙色测试球，不直接显示真实 3D accessory 模型；用户进入测试球 1 m 内后，小狗自动走到球旁，并由对应 reward 把真实 accessory 穿到狗身上。`DogAccessoryManager` 和 `RewardRevealController` 挂在同一个 `VenueRoot` 上：`DogAccessoryManager.dogGuideController` 指向当前场景 `DogGuideController`，`RewardRevealController.accessoryManager` 指向这个 manager。

抓物品：

- 小狗坐在用户 / 景点附近。
- 小狗应朝向用户或物品。
- 小狗位置必须方便它自动走到虚拟物品旁。
- 当前临时交互不是最终手势 grab，也不需要用户 ray / pinch。`VenueCollectibleSpawner` 检测用户进入测试球 1 m 内后，HUD 显示 `Thank you for helping Puppy find the {place} treasure!`，小狗自动走到球旁；约 3 秒后隐藏球、穿戴对应 accessory、播放开心动画、播放烟花和 `Assets/PuppyPath/Audio/puppy_treasure_sparkle_pop.wav`，并显示 4 秒 `SURPRISE!` reward panel。`RaycastPinchCollectibleDragger` 当前保留但在 `UIScene` 中禁用。
- `RewardRevealController` 会优先使用 Inspector 里绑定的 `Reward Panel Root`、`Title Text`、`Body Text`、`Reward Icon Image`；如果这些字段为空，会在运行时按名字寻找场景中的 `RewardPanel`，并自动绑定其中的 TMP 文本和 Image。Reward icon 播放期间会自动添加 `RewardIconWiggle`，做左右小幅快速摇晃。
- 烟花音效有两个入口：宝藏发现流程填 `RewardRevealController.Firework Sound`；导航到达流程填 `NavigationController.Firework Sound`。如果希望两种烟花都响，同一个下载好的 AudioClip 两处都拖进去。
- `VenueAttractionPlaceholderSpawner.rebuildOnStart` 和 `VenueOriginTestAttractionGenerator.createPlaceholderSpheres` 当前关闭；场景中旧的 `origin_test_*_placeholder_sphere` 也保持 inactive，避免与可交互测试球混淆。

## 现场验证清单

## Quest 内长按校准

2026-07-01 更新：新增 `VenueControllerCalibrationInput`，用于在真机运行时重新校准 `VenueOrigin` 的位置和朝向。

使用方式：

- 在 Scene 中创建 `VenueControllerCalibrationInput` 物体，或把组件挂到现有的校准管理物体上。
- `Alignment Manager` 指向场景中的 `VenueAlignmentManager`。
- `Spatial Anchor Bootstrap` 可选，指向 `VenueSpatialAnchorBootstrap`。如果勾选 `Recreate Spatial Anchor After Calibration`，每次长按校准后会替换运行时 anchor。
- 默认 `Calibration Button = OVRInput.RawButton.Start`，通常对应 Quest 左手柄菜单键；同时启用 `Alternate Calibration Button = OVRInput.RawButton.Back` 作为备用输入。
- `Hold Seconds` 默认建议 1.75 秒，避免误触。
- 用户站在真实 `VenueOrigin`，也就是 Photo Wall 右上角原点，面朝地图北方 / Unity `+Z`，长按校准按钮后，`VenueContentRoot` 会重新对齐到当前 HMD 位置和朝向。

注意：Quest 的系统 Meta / Oculus 键可能被系统保留，应用不一定能稳定捕获。如果 `Start` / `Back` 都没有触发，先在 Inspector 中把 `Calibration Button` 改成 `A`、`B`、`X` 或 `Y` 测试。正式现场版建议使用一个不容易误触的管理员按钮组合或隐藏校准菜单。

### Editor 标定验证清单

- `VenueMapDefinition.Map Pixel Size` 应保持为 `2468 x 2160`。
- `Sync Map Pixel Size From Imported Texture` 应保持关闭，避免 Unity 导入压缩尺寸覆盖原图坐标。
- 在 `VenueMapDefinition` 右键菜单执行 `Log Calibration Summary`，Console 中 `Scale world distance` 应为 `3.45 m` 左右。
- Scene 视图打开 `Gizmos` 后，应能看到红色 `VenueOrigin` 和红色比例线。
- 如果比例线方向或地图边界看起来反了，优先检查是否把图片坐标当成了 Unity 坐标；当前约定是图片 `+Y` 向下，Unity `+Z` 向地图北方 / 图片向上。

- 测量 Unity 中的红色参考墙：应为 3.45 m。
- 站在每个黄色虚拟物品出现点附近，验证小地图 marker 是否对齐。
- 从 Couch 走到 Photo Wall，验证路线方向。
- 在 Drink Shop 附近走动，验证物品透明度距离规则：3 m 内清楚显示，6 m 半透明，10 m 以上不可见。
- 在 `origin_test_1`、`origin_test_2`、`origin_test_3` 附近走动，验证对应测试球按距离渐显，高度约小狗高度；用户进入 1 m 内后，小狗自动走到球旁，HUD 显示感谢文案，然后触发 `hat_reward`、`shirt_reward`、`socks_reward` 的穿戴、烟花音效和 reward panel。
- 确认小狗不会出现在非黄色区域内。
- 确认小狗在狭窄通道中仍然保持可见。
- 确认大地图朝向与用户真实移动一致。

## 必须遵守的文档同步规则

当地图变化、比例尺变化、景点位置变化、黄色可行走区域变化，或真实现场测试发现偏移时，必须在同一次场景 / 数据修改中同步更新本文档。
