# 场地标定和数据规范

最后更新：2026-07-01

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

## 小地图数据

小地图应该使用与场地世界坐标相同的数据来源。

必须显示的 marker：

- 用户位置 marker。
- 每个景点的狗爪 marker。
- 可选：当前选中目标 marker。
- 可选：路线预览 line。

世界坐标到小地图 UI 的转换：

```text
worldPosition -> mapPosition -> normalizedMapPosition -> RectTransform anchoredPosition
```

这个转换必须使用与世界场景相同的原点、比例和旋转，避免小地图和真实位置漂移。

## 小狗位置规则

自由行走：

- 优先小狗目标点：用户前方 1.5-2 m。
- 允许距离范围：1-3 m。
- 不主动把小狗放到用户身后。
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

抓物品：

- 小狗坐在用户 / 景点附近。
- 小狗应朝向用户或物品。
- 小狗位置必须方便用户把虚拟物品拖到它身上。

## 现场验证清单

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
- 确认小狗不会出现在非黄色区域内。
- 确认小狗在狭窄通道中仍然保持可见。
- 确认大地图朝向与用户真实移动一致。

## 必须遵守的文档同步规则

当地图变化、比例尺变化、景点位置变化、黄色可行走区域变化，或真实现场测试发现偏移时，必须在同一次场景 / 数据修改中同步更新本文档。
