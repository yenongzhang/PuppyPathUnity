# Site Calibration and Data Specification

Last updated: 2026-07-01

## Core Requirements

The real-world event venue and the Unity event scene must match one-to-one. Unity distance uses the standard convention:

- 1 Unity unit = 1 m real-world distance.

The red-marked wall on the user map is the current scale reference:

- Real-world length: 3.45 m.

The yellow areas on the map are where people and the puppy can walk.

## Current Confirmed V2 Calibration Decisions

- `VenueOrigin`: the red dot at the upper-right corner of the Photo Wall.
- Unity `+Z`: map north, i.e. the upward direction on the map.
- Map pixel coordinate convention: image top-left is `(0, 0)`, `+X` to the right, `+Y` downward.
- Unity world coordinate convention: `+X` corresponds to map east / right, `+Z` corresponds to map north / up.
- Current map image size: `2468 x 2160` pixels.
- Scale reference: the real length of the red vertical wall segment is `3.45 m`.
- Current implementation entry points: `Assets/PuppyPath/Scripts/V2/VenueMapDefinition.cs` stores the map, origin, scale, and attraction data; `VenueCalibrationDebugView.cs` draws the origin, scale reference, map bounds, and attraction debug points in the Scene view.

Still required to be filled precisely in the Unity Inspector:

- `mapOriginPixel`: pixel coordinates of the Photo Wall upper-right red dot in the source image.
- `scalePointAPixel` / `scalePointBPixel`: pixel coordinates of the two endpoints of the 3.45 m red vertical scale line in the source image.
- Pixel coordinates for each attraction / collectible spawn point.

Current first-batch filled values:

```text
mapOriginPixel = (716, 820)
scalePointAPixel = (1003, 715)
scalePointBPixel = (1003, 833)
scaleSegmentMeters = 3.45
```

Computed from the above:

```text
pixelDistanceOfRedWall = 118 px
metersPerPixel = 3.45 / 118 = 0.029237288 m/px
```

This means if map pixel coordinates differ by 100 px, the corresponding Unity world distance is about 2.92 m.

## Calibration Workflow

## Meta Quest Venue Alignment Recommendations

To keep the Unity scene on Meta Quest as aligned as possible with real-world orientation, bearing, and position, use a "fixed physical calibration point + Spatial Anchor + startup calibration check" approach.

Recommended approach:

1. Choose a fixed, immovable, easy-to-relocate position in the real venue as the primary calibration point—for example a wall corner, fixed pillar, fixed furniture corner, or stable structure near the entrance.
2. Define that point in the Unity map as `UnityOrigin` or `VenueOrigin`.
3. Choose a second fixed point to determine orientation—for example another point along a long wall, used to define Unity `+Z` or venue forward.
4. On first on-site deployment, have a developer/staff member stand at the primary calibration point and press a calibration button to create or save a Meta Quest Spatial Anchor.
5. Use the primary anchor to set the world origin; use the second reference point or a known wall direction to set rotation.
6. On each app launch, attempt to load the saved anchor; after a successful load, align the entire venue root to the anchor.
7. After startup, show a hidden or developer-visible calibration check—for example display two virtual points at the real 3.45 m red wall endpoints so staff can confirm alignment.

Current first-version implementation:

- Use `VenueContentRoot` as the parent for all fixed venue content.
- Use `VenueAlignmentManager` for temporary on-site calibration: stand at the real `VenueOrigin` (Photo Wall upper-right origin), face map north / Unity `+Z`, and on startup automatically align `VenueContentRoot` to the current HMD.
- Use `VenueSpatialAnchorBootstrap` to create a Meta `OVRSpatialAnchor` at `VenueOrigin` and parent `VenueContentRoot` under the anchor. Before formally saving/loading anchors, enable `Anchor Support` in `OVRManager`.
- Use `VenueWalkableGridVisualizer` on Quest to display yellow walkable grid cells as the first layer of visualization for map alignment, scale, and direction.
- The current anchor bootstrap saves a UUID to PlayerPrefs, but a complete user flow for "load saved anchor by UUID and automatically restore the venue" still needs to be added.

Regarding QR codes:

- QR codes can serve as a helper—for example placed near the primary calibration point so staff can confirm "which calibration point this is."
- If image recognition is added later, QR codes can also be used to quickly select the corresponding venue configuration.
- Do not rely on QR codes alone as the sole spatial positioning basis; recognition angle, lighting, occlusion, and print placement error all affect stability.
- A more reliable approach is to use Quest spatial anchor / venue anchor capabilities and treat QR codes as auxiliary identifiers, not the sole coordinate system.

Minimum on-site alignment requirements:

- At least 1 primary calibration point to determine position.
- At least 1 direction reference to determine orientation.
- The 3.45 m red wall for scale verification.
- After every map update, re-verify origin, orientation, scale, and attraction positions.

### Step 1: Choose Map Reference Points

On the red wall, choose two points:

- `ScalePointA`
- `ScalePointB`

These two points must represent the two endpoints of the real 3.45 m wall segment.

### Step 2: Measure the Same Segment in Map Space

In the imported map coordinate system, measure the distance between `ScalePointA` and `ScalePointB`.

Possible map spaces:

- If using the raw map image, use pixel coordinates.
- If the map is used as a `RectTransform`, use UI local coordinates.
- If the map is placed as a Unity plane, use plane local coordinates.

### Step 3: Compute Scale

Formula:

```text
metersPerMapUnit = 3.45 / distance(ScalePointA, ScalePointB)
```

If using pixels:

```text
metersPerPixel = 3.45 / pixelDistanceOfRedWall
```

### Step 4: Choose Unity Origin

Choose a position that can be reliably found in both the real venue and the map. Candidate points:

- A corner near the main entrance.
- A permanent wall corner.
- A fixed architectural feature near the venue center.

After selection, record:

```text
UnityOriginName = VenueOrigin_PhotoWallUpperRight
MapOriginPoint = Photo Wall upper-right red dot; pixel coordinates to be filled precisely in Inspector
RealWorldOriginDescription = Fixed on-site point corresponding to Photo Wall upper-right corner
```

### Step 5: Choose Unity Forward Direction

Choose which direction on the map corresponds to Unity `+Z`.

After selection, record:

```text
UnityForward = +Z
MapDirectionForForward = map north / image upward direction
RotationDegrees = 0, unless a global yaw offset is needed during on-site Spatial Anchor alignment
```

### Step 6: Create Coordinate Conversion

Every map point should be converted to Unity world coordinates:

```text
mapDelta = mapPoint - mapOriginPoint
scaledDeltaMeters = mapDelta * metersPerMapUnit
unityPosition = rotation * scaledDeltaMeters + unityOriginWorldPosition
```

Y usually uses ground height:

```text
unityPosition.y = 0
```

Only floating items, UI hints, puppy accessory anchors, and reward effects need Y adjusted.

## Walkable Area Data

Yellow areas should be converted into one or more polygons.

Recommended data structure:

```text
WalkableArea
- id
- displayName
- polygonPointsInMapSpace
- polygonPointsInWorldSpace
```

Rules:

- User and puppy target points must be inside walkable polygons.
- Route lines must stay inside walkable polygons.
- Attractions should be inside walkable areas, or very close to them.
- If a point is outside the yellow area, snap it to the nearest valid point only when that does not cause wall penetration.

Current first-version implementation notes:

- `VenueMapDefinition.walkableAreas` stores one or more `WalkableAreaDefinition`.
- Each polygon point uses raw map pixel coordinates; convention remains top-left `(0, 0)`, `+X` right, `+Y` down.
- Polygon points are listed in clockwise or counterclockwise order; do not repeat the first point at the end.
- First-version pathfinding samples multiple points along route segments and checks whether sample points fall inside any walkable polygon.
- If `walkableAreas` is empty, current code temporarily treats all map points as walkable for early debugging; polygons must be filled before formal route testing.

Coordinates that must be provided manually:

- Yes—2D pixel coordinates for key corners of the yellow walkable area are required.
- You do not need to trace every small bump at first; the first version can use a coarse outer contour covering main passages.
- Narrow corridors, wall corners, and junctions need higher accuracy because routes or the puppy are most likely to clip through walls there.

## Hand-Crafted Navigation Graph Data

The first version does not auto-generate a full navigation mesh from polygons; it uses a hand-crafted waypoint graph.

Recommended data structure:

```text
VenueNavGraph
- nodes

VenueNavNode
- id
- mapPixel
- neighborNodeIds
```

Filling principles:

- Place waypoints near the centerline of yellow areas, not against walls.
- At least one waypoint at each turn.
- At least one arrival waypoint near each attraction.
- Only connect adjacent waypoints that can be reached in a straight line without wall penetration.
- Neighbor relationships should be bidirectional unless one-way flow is required later.

Debugging:

- In `VenueCalibrationDebugView`, enable `Draw Walkable Areas` and `Draw Nav Graph`.
- Green lines represent walkable polygons.
- Red lines represent obstacle polygons.
- Blue lines represent the navigation graph.
- Red-orange nav edges indicate connections currently considered non-walkable—usually crossing an obstacle or leaving the walkable polygon.
- With `Edit Nav Graph In Scene` enabled, the `Nav Graph Editing` panel in the top-left of the Scene view allows manual editing of blue lines. Click the small cyan selection points beside two blue nodes, then use `Connect` to add bidirectional neighbor links and `Disconnect` to remove bidirectional neighbor links.
- In `Test Path`, fill in a test start pixel and target attraction id to display an orange test route.

Scene hand calibration:

- In `VenueCalibrationDebugView`, run `Create Map Reference Plane` from the context menu to lay the map image under the calibration coordinate system.
- Use `Show Map Reference Plane` / `Hide Map Reference Plane` to show or hide the map underlay.
- With the corresponding `Scene Editing` toggles enabled, drag directly in the Scene view:
  - Map origin `mapOriginPixel`.
  - 3.45 m scale endpoints `scalePointAPixel` / `scalePointBPixel`.
  - Attraction / collectible spawn points.
  - Walkable polygon vertices.
  - Obstacle polygon vertices.
- Nav graph waypoints.
- After dragging, coordinates are automatically converted from Unity world position back to map pixel coordinates and saved to `VenueMapDefinition`.
- `Populate Detected Nav Graph Draft` generates a denser centerline waypoint draft and keeps only connections considered valid by current walkability checks.
- Dragging `mapOriginPixel` keeps the world layout of other set points and lines unchanged.
- Dragging `scalePointAPixel` / `scalePointBPixel` keeps the current meters-per-pixel unchanged to avoid rescaling the map and routes.
- Manually connecting or disconnecting nav graph blue lines writes directly to `VenueMapDefinition.navGraph.nodes[*].neighborNodeIds` and supports Unity Undo.
- If the scale truly needs recalculation, use a separate explicit calibration operation later.

## Wall and Obstacle Data

Black walls and non-yellow interior areas should be converted into blocked geometry or obstacle polygons.

Recommended data structure:

```text
Obstacle
- id
- displayName
- polygonPointsInMapSpace
- polygonPointsInWorldSpace
```

Rules:

- The puppy cannot pass through these areas.
- Path segments cannot pass through these areas.
- If the puppy target point directly ahead of the user would cross an obstacle, choose a left-forward or right-forward fallback.

## Attraction and Virtual Item Spawn Point Data

On the current new map, yellow dots indicate virtual item spawn positions. Attraction text labels are for naming and semantics only; actual collectible spawn points should follow the yellow dot coordinates determined by subsequent calibration.

Currently recorded 10 virtual item spawn points:

| ID | Map Original Label | Recommended English Display Name | Chinese Reference Name | Map Location | World Position | Transparency Rule | Item | Accessory | Reward |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `chess` | Chess | Checkmate Corner | 棋遇小屋 | Upper-left activity zone, yellow point above the Chess label | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | chess piece / TBD | TBD | TBD |
| `couch` | Couch | Cozy Couch Cove | 软乎乎沙发湾 | Upper-left activity zone, yellow point below the Couch label | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | pillow / TBD | TBD | TBD |
| `photo_wall` | Photo Wall | Snapshot Studio | 咔嚓照相馆 | Left-center area, yellow point near the Photo Wall label | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | camera or frame / TBD | TBD | TBD |
| `goodies` | Goodies | Treat Trove | 甜甜补给站 | Lower-left inset area, yellow point near the Goodies label | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | snack bag / TBD | TBD | TBD |
| `book_wall` | Book Wall | Storybook Wall | 故事书墙 | Upper-right mid corridor, yellow point near the Book Wall label | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | bookmark, sticker, or badge / TBD | TBD | TBD |
| `tap_water` | Tap Water | Splash Stop | 汪汪补水站 | Bottom center-right, yellow point near the Tap Water label | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | water drop, cup, or small kettle / TBD | TBD | TBD |
| `ice_cream_shop` | Ice Cream Shop | Scoop Station | 冰淇淋小站 | Between Tap Water and Drink Shop, yellow point near the right side of the gray horizontal bar; intended as ice cream shop | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | ice cream scoop or cone / TBD | TBD | TBD |
| `drink_shop` | Drink Shop | Fizzy Fridge | 气泡饮料铺 | Lower-right area, yellow point near the right-side Fridge / drink shop label | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | soda can or drink cup | TBD | free cola / TBD |
| `piano` | Piano | Melody Corner | 音符小舞台 | Right-center lower area, yellow point near the Piano label | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | musical note / TBD | TBD | TBD |
| `plants` | Plants | Garden Patch | 小狗花园 | Yellow point inside the dashed Plants area at lower-right | TBD | 3 m=100%, 6 m=50%, 10 m+=0% | small plant or leaf / TBD | TBD | TBD |

Subsequent calibration requirements:

- Convert each yellow dot to `mapPosition` and `worldPosition`.
- If an attraction text position differs from the yellow dot position, navigation destination and transparency calculations should use the yellow dot or a nearby walkable target point.
- If a yellow dot is too close to a wall, additionally define a user-reachable `arrivalPoint`, while the item itself still spawns at the yellow dot.

### Auto-Detected First-Version Collectible Spawn Coordinates

Source image: `Assets/PuppyPath/Maps/map_with_spawn_points.jpg`, original size `2468 x 2160`.

Detection method: extract connected components by orange dot color threshold; use each dot component's center. This part has high confidence.

| ID | Auto-Detected Center Pixel Coordinates |
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

Note: if the map image is updated later or dot positions move, these coordinates must be re-detected or manually updated.

### Auto-Detected First-Version Walkable / Navigation Draft

Current `VenueMapDefinition` provides a `Populate Detected Draft Map Data` menu to write a first-version draft in one click:

- `main_walkable_auto_draft`: coarse polygon outline of the purple walkable area.
- `central_block_auto_draft`: coarse obstacle polygon of the large gray blocked area in the center.
- `navGraph`: 14 hand-filtered centerline waypoints.

This part has medium confidence and needs manual review in the Scene view:

- Whether the green outer contour roughly follows the purple area boundary.
- Whether the red obstacle contour covers the non-walkable center area.
- Whether blue waypoints all sit near the center of the purple walkable area.
- Whether blue connection lines pass through walls; if so, delete that neighbor link or move the node.
- Whether the orange test route avoids gray non-walkable areas.

## Mini-Map Data

The mini-map should use the same data source as venue world coordinates. Per the storyboard, `MiniMap` refers to the in-game HUD-style local mini-map showing geography only near the user; full venue selection is handled by `BigMap`.

Markers that must be shown:

- User position marker.
- Paw marker for each attraction.
- Optional: currently selected target marker.
- Optional: route preview line.

World coordinate to mini-map UI conversion:

```text
worldPosition -> mapPosition -> normalizedMapPosition -> RectTransform anchoredPosition
```

This conversion must use the same origin, scale, and rotation as the world scene to avoid drift between the mini-map and real position.

2026-07-01 first-version implementation:

- `VenueMapUiController` handles coordinate mapping for the local mini-map / full large map UI.
- The mini-map sits in the upper-right of the view and crops to the area near the user via `RawImage.uvRect`; the large map opens as a centered panel showing the full venue.
- The `XR Camera` current world position is first converted to venue-local coordinates via `VenueContentRoot.InverseTransformPoint`, then `VenueMapDefinition.WorldToMapPixel` yields the user map pixel coordinates.
- Attraction markers are placed using `AttractionDefinition.GetArrivalPixel()`; if an attraction has a custom arrival pixel, arrival is shown preferentially; otherwise the collectible spawn pixel is shown.
- The local mini-map shows only attraction markers within the current crop; the full large map shows all attraction markers.
- Clicking an attraction marker on the large map calls `VenueNavigationRuntime.StartNavigationToAttraction(attractionId)`.
- `CancelNavigation()` calls `VenueNavigationRuntime.StopNavigation()` and clears the current selected marker.
- `VenueMapOpenButton` can be attached to the mini-map `RawImage` or a button object to call `VenueMapUiController.OpenLargeMap()` when the mini-map is clicked.

## Puppy Position Rules

Free roaming:

- Preferred puppy target point: 1.5–2 m in front of the user.
- Allowed distance range: 1–3 m.
- Do not actively place the puppy behind the user.
- If directly ahead is blocked:
  - Try left-forward.
  - Try right-forward.
  - Try a point directly ahead but closer to the user.
  - Finally choose the nearest visible walkable point in the forward hemisphere.

Navigation:

- The puppy should lead along the route ahead, not stick to the user's feet.
- Puppy lead distance needs on-site tuning.
- The puppy must stay inside walkable areas.
- The puppy should not clip through walls to reach the next route point.

## Runtime Route Validation

After map data processing is complete, use `VenueNavigationRuntime` for the first round of runtime validation.

Recommended scene wiring:

- Create an empty `VenueContentRoot` and parent all fixed venue content under it.
- `VenueCalibrationDebug`, `VenueRouteLine`, `VenueWalkableGrid`, and future attraction items should all be children of `VenueContentRoot`.
- Create a `VenueNavigationRuntime` empty object in the scene and attach `VenueNavigationRuntime`.
- `Map Definition` points to the current `VenueMapDefinition`.
- `XR Camera` points to CenterEye / HMD transform under `OVRCameraRig`.
- `Venue Content Root` points to `VenueContentRoot`.
- `Route Line Controller` points to the route object with `VenueRouteLineController + LineRenderer`.
- Optional: `Dog Guide Controller` points to the existing puppy controller for temporary validation that the puppy receives recommended direction.

Device visualization wiring:

- Create `VenueWalkableGrid` under `VenueContentRoot`.
- Add `MeshFilter`, `MeshRenderer`, and `VenueWalkableGridVisualizer` to `VenueWalkableGrid`.
- `Map Definition` points to the current `VenueMapDefinition`.
- For `Cell Size Meters`, start with `0.5`; for `Cell Fill Ratio`, use `0.82`; color should be semi-transparent yellow.

Temporary on-site calibration:

- Create `VenueAlignmentManager` in the scene.
- `Map Definition` points to the current `VenueMapDefinition`.
- `XR Camera` points to CenterEye / HMD transform.
- `Venue Content Root` points to `VenueContentRoot`.
- Enable `Align On Start` and `Align Yaw To Head Forward`.
- Before device startup, stand at the real Photo Wall upper-right origin facing map north / Unity `+Z`.
- After startup, yellow grid cells should lie near the real walkable area; if overall rotation is off, adjust `Additional Yaw Degrees` and rebuild / Play again.

Spatial Anchor prerequisites:

- Select `OVRCameraRig`.
- Under `OVRManager > Quest Features > General`, enable `Anchor Support`.
- `Anchor Sharing Support` is not required when multi-user sharing is not needed.
- Create `VenueSpatialAnchorBootstrap` near `VenueContentRoot`, set `Map Definition` and `Venue Content Root`; enable `Create Anchor On Start` when testing.

Testing:

- Fill `Test Destination Attraction Id` with an attraction id, e.g. `photo_wall`, `drink_shop`, `plants`.
- Run `Start Test Navigation` from the component context menu.
- The ground route should generate from near the current HMD to the target attraction and stay inside green walkable areas.
- While moving, runtime replans the route at intervals; if no new route is found temporarily, the last valid route is kept.
- Run `Stop Navigation` from the component context menu to clear the route and stop temporary puppy navigation.

Route Line notes:

- `VenueRouteLine` itself is only an empty object carrying `LineRenderer`; without route points, squares seen in Scene / XR are usually selection boxes or Rect-like gizmos, not the actual route.
- Only when `VenueNavigationRuntime.StartNavigationToAttraction(...)` or `VenueRouteLineController.Show Test Route` successfully generates more than 2 route points does `LineRenderer` show the real ground route.
- Current `VenueRouteLineController` automatically configures `LineRenderer` as world space, thicker line width, shadows off, and defaults to runtime unlit material to avoid Quest visibility issues from Lit materials or ground z-fighting.
- If the line is still invisible, first confirm: whether `VenueNavigationRuntime.Route Line Controller` references `VenueRouteLine`; whether `StartNavigationToAttraction` succeeded; whether `Line Height Offset` is above ground; whether `LineRenderer.Position Count` is greater than 1.

Note: current `VenueNavigationRuntime` only handles routes and recommended direction—it is not final puppy behavior. `DogVenueFollower` is still required for keeping the puppy in front of the user, obstacle avoidance, free roaming, and treasure proximity feedback.

Item pickup:

- The puppy sits near the user / attraction.
- The puppy should face the user or the item.
- Puppy position must make it easy for the user to drag virtual items onto the puppy.

## On-Site Validation Checklist

## Long-Press Calibration in Quest

2026-07-01 update: added `VenueControllerCalibrationInput` to recalibrate `VenueOrigin` position and orientation at runtime on device.

Usage:

- Create a `VenueControllerCalibrationInput` object in the Scene, or attach the component to an existing calibration manager object.
- `Alignment Manager` points to `VenueAlignmentManager` in the scene.
- `Spatial Anchor Bootstrap` is optional and points to `VenueSpatialAnchorBootstrap`. If `Recreate Spatial Anchor After Calibration` is enabled, each long-press calibration replaces the runtime anchor.
- Default `Calibration Button = OVRInput.RawButton.Start`, usually the Quest left controller menu button; `Alternate Calibration Button = OVRInput.RawButton.Back` is also enabled as backup input.
- Default `Hold Seconds` is 1.75 s to reduce accidental triggers.
- Stand at the real `VenueOrigin` (Photo Wall upper-right origin), face map north / Unity `+Z`, long-press the calibration button, and `VenueContentRoot` realigns to the current HMD position and orientation.

Note: Quest system Meta / Oculus buttons may be reserved by the OS and not reliably capturable by the app. If neither `Start` nor `Back` triggers, change `Calibration Button` in the Inspector to `A`, `B`, `X`, or `Y` for testing. For production on-site use, prefer a hard-to-mispress admin button combination or hidden calibration menu.

### Editor Calibration Validation Checklist

- `VenueMapDefinition.Map Pixel Size` should remain `2468 x 2160`.
- Keep `Sync Map Pixel Size From Imported Texture` disabled to avoid Unity import compression size overwriting source image coordinates.
- Run `Log Calibration Summary` from the `VenueMapDefinition` context menu; Console `Scale world distance` should be about `3.45 m`.
- With `Gizmos` enabled in the Scene view, red `VenueOrigin` and the red scale line should be visible.
- If the scale line direction or map bounds look inverted, first check whether image coordinates were mistaken for Unity coordinates; current convention is image `+Y` down, Unity `+Z` toward map north / image up.

- Measure the red reference wall in Unity: should be 3.45 m.
- Stand near each yellow virtual item spawn point and verify mini-map marker alignment.
- Walk from Couch to Photo Wall and verify route direction.
- Move near Drink Shop and verify item transparency distance rules: clear within 3 m, semi-transparent at 6 m, invisible at 10 m+.
- Confirm the puppy does not appear outside non-yellow areas.
- Confirm the puppy stays visible in narrow passages.
- Confirm large-map orientation matches the user's real movement.

## Required Documentation Sync Rules

When the map changes, scale changes, attraction positions change, yellow walkable areas change, or on-site testing reveals offset, this document must be updated in the same scene / data change pass.
