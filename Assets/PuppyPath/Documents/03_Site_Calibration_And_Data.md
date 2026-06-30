# 场地标定和数据规范

最后更新：2026-06-30

## 核心要求

真实活动场地和 Unity 活动场景必须一比一匹配。Unity 距离使用常规约定：

- 1 Unity unit = 1 m 真实世界距离。

用户地图中红色标记的墙体是当前比例尺：

- 真实世界长度：3.45 m。

地图中的黄色区域是人和小狗可以行走的区域。

## 标定流程

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
UnityOriginName = TBD
MapOriginPoint = TBD
RealWorldOriginDescription = TBD
```

### 步骤 5：选择 Unity 前方方向

选择地图上的哪个方向对应 Unity `+Z`。

选定后记录：

```text
UnityForward = TBD
MapDirectionForForward = TBD
RotationDegrees = TBD
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

## 景点数据草案

当前草图地图中的景点：

| ID | 显示名称 | 地图位置 | 世界位置 | 显示半径 | 物品 | 饰品 | 奖励 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `chess` | Chess | TBD | TBD | 5 m | TBD | TBD | TBD |
| `sofa` | Sofa | TBD | TBD | 5 m | TBD | TBD | TBD |
| `photo_wall` | Photo Wall | TBD | TBD | 5 m | TBD | TBD | TBD |
| `goodies` | Goodies | TBD | TBD | 5 m | TBD | TBD | TBD |
| `cool_wall` | Cool Wall | TBD | TBD | 5 m | TBD | TBD | TBD |
| `wc` | WC | TBD | TBD | 5 m | TBD | TBD | TBD |
| `fridge` | Fridge | TBD | TBD | 5 m | 可乐罐 | TBD | 免费可乐 / TBD |
| `piano` | Piano | TBD | TBD | 5 m | TBD | TBD | TBD |

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

- 测量 Unity 中的红色参考墙：应为 3.45 m。
- 站在每个景点位置，验证小地图 marker 是否对齐。
- 从 Sofa 走到 Photo Wall，验证路线方向。
- 在 Fridge 附近走动，验证物品显示半径。
- 确认小狗不会出现在非黄色区域内。
- 确认小狗在狭窄通道中仍然保持可见。
- 确认大地图朝向与用户真实移动一致。

## 必须遵守的文档同步规则

当地图变化、比例尺变化、景点位置变化、黄色可行走区域变化，或真实现场测试发现偏移时，必须在同一次场景 / 数据修改中同步更新本文档。
