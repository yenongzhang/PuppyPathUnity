# PuppyPath V2 Implementation Plan

Last updated: 2026-07-01

## Current Technical Reading Summary

The existing project already has many reusable parts:

- `UIBootSequence` already handles logo fade-in, hold, fade-out, and safely shows the main UI after the logo finishes.
- `CanvasFollowHead` can keep a canvas positioned in front of the user's head.
- `PuppyPathSelectionUI` supports map clicks and marker placement, but it is currently based on the old grid-based Europa-Park map approach.
- `PathPreviewController` can instantiate path prefabs and draw routes with `LineRenderer`.
- `RouteRootSpawner` spawns route content in front of the user, which was useful for the old prototype, but V2 needs real-venue fixed coordinates, so this must change.
- `NavigationRuntimeController` already handles path progress, arrival, off-route detection, waiting, and dog navigation state.
- `NavigationHUDController` can switch between the main UI and navigation HUD text.
- `NavigationController` handles the old flow: selection, preview, start navigation, arrival fireworks, return to menu, and related steps.
- `DogGuideController` spawns the dog, plays animation states, switches face textures, plays barks, moves along route direction, responds to navigation state, and performs random behaviors.
- Dog models, animation FBX files, face textures, audio, UI images, logo, map images, route prefabs, and fireworks prefabs already exist.

V2 should reuse these solid animation and UI foundations, but the core scene model must change to "real-venue fixed coordinates + attraction system."

## Deprecated: Friend List / Find-a-Friend

The Friend List / Find-a-Friend feature is deprecated and out of scope for V2. Do not implement or extend it unless explicitly revived in a future milestone.

## Recommended Architecture

### New Data Layer

Create data assets or serializable scene data to describe:

- Venue map scale and coordinate transforms.
- One or more walkable polygons extracted from the yellow area.
- Obstacle / wall polygons extracted from non-walkable areas.
- Attraction definitions.
- Attraction virtual items.
- Dog accessory slots.
- Reward content.

Recommended new scripts:

- `VenueMapDefinition`
- `VenueCoordinateMapper`
- `WalkableArea`
- `AttractionDefinition`
- `AttractionRegistry`
- `CollectibleItemDefinition`
- `DogAccessoryDefinition`
- `RewardDefinition`

Prefer `ScriptableObject` for stable content data. Use scene objects for spatial anchors, hand-placed waypoints, and debug points.

Currently in progress:

- `Assets/PuppyPath/Scripts/V2/VenueMapDefinition.cs`: `ScriptableObject` venue map definition storing map dimensions, origin pixel coordinates, 3.45 m scale endpoints, orientation, and attraction data.
- `Assets/PuppyPath/Scripts/V2/VenueCoordinateMapper.cs`: Utility for converting between map pixel coordinates and Unity world coordinates.
- `Assets/PuppyPath/Scripts/V2/VenueCalibrationDebugView.cs`: Scene view debug drawing tool for inspecting origin, map bounds, scale, and attraction markers.
- `Assets/PuppyPath/Scripts/V2/VenuePathfinder.cs`: First-version hand-built nav graph pathfinding tool that generates map pixel routes and Unity world routes from a waypoint graph.
- `Assets/PuppyPath/Scripts/V2/VenueRouteLineController.cs`: First-version route `LineRenderer` drawing component that can draw routes from a test start point to a target attraction.
- `Assets/PuppyPath/Scripts/V2/VenueNavigationRuntime.cs`: First-version V2 venue navigation runtime that builds real-venue routes from the HMD world position to attractions, refreshes the ground route line, and provides recommended direction to the dog controller.
- `Assets/PuppyPath/Scripts/V2/VenueMapUiController.cs`: First-version V2 big-map UI controller that maps the user's current position and attraction positions to full-map UI markers and wires attraction clicks into `VenueNavigationRuntime`.
- `Assets/PuppyPath/Scripts/V2/VenueMapMarker.cs`: Map UI marker component that stores attraction id, shows selected state, and passes click events back to the map UI controller.
- `Assets/PuppyPath/Scripts/V2/VenueMapOpenButton.cs`: Lightweight top-right map button entry that opens the big map; can be attached to a button object.
- `Assets/PuppyPath/Scripts/V2/PuppyPathV2FlowController.cs`: First-version storyboard flow controller that reuses the old Canvas panels and manages UI states such as `Boot`, `Intro`, `FreeRoam`, `BigMap`, `Navigation`, and `RewardPopup`; 3D item grabbing no longer uses a dedicated UI panel.
- `Assets/PuppyPath/Scripts/V2/VenueAlignmentManager.cs`: First-version on-site calibration component that aligns `VenueContentRoot` to the real `VenueOrigin` at the current HMD position.
- `Assets/PuppyPath/Scripts/V2/VenueSpatialAnchorBootstrap.cs`: First-version Meta Spatial Anchor bootstrap that can create an `OVRSpatialAnchor` at `VenueOrigin` and parent venue content under the anchor.
- `Assets/PuppyPath/Scripts/V2/VenueWalkableGridVisualizer.cs`: On-device visualization tool that tiles the current walkable area with yellow cells for validating map alignment on Quest.
- `Assets/PuppyPath/Scripts/V2/VenueControllerCalibrationInput.cs`: Quest on-device long-press calibration input component that resets `VenueContentRoot` position and orientation when the user stands at `VenueOrigin` facing map north via a controller button, and can recreate the runtime Spatial Anchor.
- `Assets/PuppyPath/Scripts/V2/VenueMapReferencePlane.cs`: Lays the map image onto the Scene XZ plane at the current calibration scale for manual alignment.
- `Assets/PuppyPath/Scripts/V2/Editor/VenueCalibrationDebugViewEditor.cs`: Scene view drag-edit tool that can directly move attraction points, polygon vertices, and nav graph nodes.

Confirmed first-version coordinate conventions:

- `VenueOrigin` uses the red dot at the top-right corner of Photo Wall.
- Unity `+Z` corresponds to map north / image up direction.
- Map pixel coordinates use the image top-left as `(0, 0)`, with `+X` to the right and `+Y` downward.

### New Game Flow Layer

Create a high-level state controller:

- `PuppyPathV2GameController`

Suggested states:

- `Boot`
- `Intro`
- `FreeWalk`
- `MapOpen`
- `Navigating`
- `AttractionReveal`
- `ItemGrab`
- `Reward`

This controller coordinates UI, dog behavior, attraction display, collection state, and navigation mode.

### New Venue Navigation Layer

The old navigation used user-relative generated path prefabs. V2 needs world-fixed paths in the real venue:

- Use the 3.45 m red wall as the scale reference to convert map points into Unity world coordinates.
- Build a walkable graph structure on the yellow area.
- Use graph search / pathfinding to generate routes from the user's current position to the target attraction.
- Clamp the dog target position to the walkable area.
- Prevent the dog and ground route from passing through walls or non-walkable areas.

Recommended new scripts:

- `VenueNavGraph`
- `VenuePathfinder`
- `VenueRouteLineController`
- `DogVenueFollower`

The ground route drawing approach in the existing `PathPreviewController` can be used as reference, but V2 should not continue depending on old path prefab ids.

### New Map Button / Big Map Layer

Replace the old grid map with a top-right map button and real-venue big map:

- Top-right map button in the view.
- User current-position marker.
- One paw marker per attraction.
- Tap or touch the map button to open the centered big map.
- Tap a paw marker on the big map to start navigation.
- Navigation mode must provide an exit / cancel navigation button so the user can return to free roaming at any time if they no longer want to go to the current target.

Recommended new scripts:

- `VenueMapUiController`: Unified management of the full big map, user position marker, and attraction markers.
- `VenueMapMarker`: Display and click entry for a single attraction marker.
- `VenueMapOpenButton`: Converts top-right map button clicks into open-big-map button/pointer events.

The old `PuppyPathSelectionUI` can be used as reference for pointer clicks and marker placement, but V2 should use attraction coordinates rather than fixed grid rows and columns.

### New Attraction and Collection Layer

Each attraction needs:

- Name.
- World coordinate position.
- Virtual item spawn position. On the current new map, the yellow dots are authoritative; attraction text labels are naming reference only.
- Transparency distance curve instead of a single display radius.
- Suggested first-version transparency rule: alpha = 1 within 3 m, alpha = 0.5 at 6 m, alpha = 0 beyond 10 m, with smooth interpolation in between.
- Floating item prefab.
- Item distance show/hide logic.
- Hint UI.
- Whether it has been collected.
- Corresponding dog accessory slot.
- Reward content.

Recommended new scripts:

- `AttractionTrigger`
- `FloatingCollectibleItem`
- `CollectibleGrabHandler`
- `DogAccessoryManager`
- `RewardRevealController`

Meta Quest / XR interaction should be built on the XR setup already used in the project. The current version uses hand tracking only, not controllers. The old PuppyPath already uses pointing pinch; V2 needs to add grab gestures on top of that for grabbing and dragging items onto the dog.

### Dog Behavior Layer

`DogGuideController` is the first-priority reuse target, but V2 should wrap it or gradually split it:

- Free roaming: dog stays 1–3 m in front of the user within the yellow walkable area.
- Navigation: dog walks ahead of the user along the generated route.
- Item grab: dog sits and waits.
- Reward: dog happy reaction.
- Accessories: dog shows collected wearables.

Recommended approach:

1. Add a V2 wrapper that sends high-level commands to the existing dog controller.
2. Keep current animation state names and face texture switching logic.
3. Only refactor further if `DogGuideController` is too tightly coupled to old route logic.

Old navigation / dog script reading summary:

- `PathPreviewController` instantiates static path prefabs from old path ids and draws routes with `LineRenderer`.
- `NavigationRuntimeController` reads current path waypoints, judges distance from the route centerline, progress along the route, movement direction, waiting state, and arrival state, and sends `NavState` and recommended direction to `DogGuideController`.
- `DogGuideController` spawns the dog, plays animations, switches expressions, moves to a target point ahead of the user, and responds to old states such as waiting / getting farther / lost / arrived.
- V2 can reuse dog spawning, animation, expression, movement, and bark code ideas, but must not continue depending on old path prefabs and old "angry / waiting / lost" feedback logic.

V2 dog behavior rules:

- Free roam mode: dog follows HMD horizontal movement direction and stays about 1–3 m in front of the user.
- If HMD movement speed is clearly noticeable, use movement direction to choose the dog's forward target; if the user is mostly stationary, fall back to HMD forward.
- Dog movement speed should be close to or slightly faster than HMD horizontal speed; switch from walk to trot / canter when needed.
- While the user is not stopping, the dog should not stop to wait or sit.
- The dog should not run behind the user; if it falls behind, it should prioritize catching up to a walkable point ahead of the user.
- Forward target points must pass `VenueMapDefinition.IsMapPixelWalkable` or an equivalent later API.
- If straight ahead is not walkable, try left-front, right-front, a closer forward point, then the nearest walkable nav node in order.
- Navigation mode: route source changes to the real-venue route generated by `VenuePathfinder` / `VenueRouteLineController`, but dog movement and animation can reuse `DogGuideController` locomotion code.
- Near treasure, the dog barks happily toward the treasure direction; this is positive discovery feedback and does not use the negative feedback from old `GettingFarther` / `Lost`.
- "Just wandering" mode has no off-route concept; the dog does not get angry because the user leaves a route.

Suggested implementation:

1. Add `DogVenueFollower` as a V2 wrapper responsible for HMD speed detection, forward target selection, walkable-area constraints, and treasure-proximity feedback.
2. Add a small set of public methods or a lightweight wrapper API to the existing `DogGuideController` to reuse dog spawning and walk / trot / canter / happy / bark animations.
3. Do not rewrite old `NavigationRuntimeController` for now; first write a new venue navigation runtime for V2 whose input is route points generated by `VenuePathfinder`.
4. Keep old scripts for reference and rollback, but V2 no longer uses old path prefabs as the real navigation data source.

## Development Phases

### Phase 1: Venue Calibration Prototype

Goal: Create a Unity venue scene consistent with the real map scale.

Current status: Basic data structures and debug visualization scripts are in place. Next step is to create a `VenueMapDefinition` asset in Unity and fill in Photo Wall origin, 3.45 m scale line endpoints, and attraction pixel coordinates in the Inspector.

Current test method:

1. In `VenueMapDefinition`, confirm `Map Pixel Size = 2468 x 2160` and leave `Sync Map Pixel Size From Imported Texture` unchecked.
2. Fill in `Map Origin Pixel`, `Scale Point A Pixel`, and `Scale Point B Pixel`.
3. Create or select a `VenueCalibrationDebug` empty object in the scene, attach `VenueCalibrationDebugView`, and reference the current `VenueMapDefinition`.
4. Open Scene view `Gizmos` in the top-right.
5. Select `VenueMapDefinition` in the Project view and run `Log Calibration Summary` from the component context menu.
6. In the Console, `Scale world distance` should show about `3.45 m`.
7. In Scene view you should see Photo Wall origin, 3.45 m scale line, and map bounds; after filling attraction coordinates you should also see 10 attraction markers.

Current first batch of calibration values filled in:

- `Map Origin Pixel = (716, 820)`.
- `Scale Point A Pixel = (1003, 715)`.
- `Scale Point B Pixel = (1003, 833)`.

Steps:

1. Import or place the latest event venue map image as a reference plane or UI overlay.
2. Mark the reference line segment of the red wall in map coordinates.
3. Compute map-to-meters scale from the 3.45 m wall.
4. Choose Unity origin and venue orientation.
5. Create a simple ground / walkable-area visualization from the yellow area.
6. Add debug markers for all currently marked attractions and yellow virtual item spawn points.
7. Verify distances in Unity with measurement tools.
8. Write all chosen origin, orientation, and scale values into `03_Site_Calibration_And_Data.md`.

Acceptance criteria:

- Measuring the red wall in Unity should yield 3.45 Unity units, i.e. 3.45 m.
- All known attractions and yellow virtual item spawn points appear in reasonable relative positions.
- The yellow walkable area has become a venue-fixed region.

### Phase 2: Walkable Area and Pathfinding

Goal: Navigation and dog position must respect the real walkable area.

Current status: `WalkableAreaDefinition`, `VenueNavGraphDefinition`, and `VenueNavNodeDefinition` data structures have been added; `VenueCalibrationDebugView` can draw green walkable polygons, blue waypoint graphs, and orange test routes. `VenuePathfinder` currently uses a hand-built waypoint graph and checks whether straight-segment sample points lie inside walkable polygons.

2026-07-01 update: The map owner has finished hand-editing walkable areas and the nav graph in Scene. Next step moves from data editing to runtime validation: generate venue-fixed routes from the HMD / XR Camera current world position to target attractions and draw ground `LineRenderer` routes.

`VenueNavigationRuntime` has been added:

- Inputs: `VenueMapDefinition`, `xrCamera`, `VenueRouteLineController`, optional `DogGuideController`.
- Public API: `StartNavigationToAttraction(string attractionId)` and `StopNavigation()`.
- Inspector context menu: `Start Test Navigation` / `Stop Navigation` for testing any attraction route without UI hooked up.
- Behavior: converts HMD world position to map pixel coordinates, calls `VenuePathfinder` to generate a route, then draws the line with `VenueRouteLineController.ShowWorldRoute`.
- Re-plans the route at intervals during play; if new planning fails, keeps the last valid route to avoid routes disappearing suddenly during on-site testing.
- If the user's current position or target point is slightly outside the walkable polygon, temporarily snaps to the nearest walkable nav node to reduce failures from small on-site calibration errors.
- Current dog integration is transitional: reuses `DogGuideController.BeginGuiding` / `ApplyNavigationState` and only sends non-negative states such as `Neutral`, `GettingCloser`, and `Arrived`; a dedicated `DogVenueFollower` should still be implemented later.

2026-07-01 on-device visualization / on-site calibration update:

- Added `VenueContentRoot` as the parent for all venue-fixed content. `VenueCalibrationDebug`, `VenueRouteLine`, `VenueWalkableGridVisualizer`, future attraction items, and dog target points should all live under this root.
- Added `VenueAlignmentManager`: for quick testing, when the user stands at the real Photo Wall top-right origin facing map north / Unity `+Z`, startup automatically aligns `VenueContentRoot` to the current HMD.
- Added `VenueSpatialAnchorBootstrap`: creates a Meta `OVRSpatialAnchor` at `VenueOrigin` and can parent `VenueContentRoot` under the anchor as the basis for persistent venue alignment later.
- Added `VenueWalkableGridVisualizer`: generates yellow semi-transparent cells from `VenueMapDefinition.IsMapPixelWalkable` so walkable areas are visible on Quest.
- Added `VenueControllerCalibrationInput`: during on-device runtime, when the user stands at the real `VenueOrigin` facing map north, long-pressing a controller button re-runs position + orientation calibration; if `VenueSpatialAnchorBootstrap` is connected, it can also replace the runtime Spatial Anchor. Defaults to `OVRInput.RawButton.Start` because the system Meta / Oculus button may be reserved by Quest OS and not reliably captured by the app.
- Meta Quest Spatial Anchor prerequisite: on `OVRCameraRig` under `OVRManager > Quest Features > General`, enable `Anchor Support`; enable `Anchor Sharing Support` only when shared anchors are needed.

2026-07-01 update: An auto-detect draft entry has been added. Running `Populate Detected Draft Map Data` from the `VenueMapDefinition` context / gear menu fills from current `map_with_spawn_points.jpg` auto-detection results:

- 10 `collectibleSpawnPixel` values.
- 1 main walkable outer contour `main_walkable_auto_draft`.
- 1 central obstacle region `central_block_auto_draft` to prevent routes through the large central gray block.
- 14 first-version waypoint graph nodes.

This data is draft, not final on-site calibration. Orange dot detection has higher confidence; walkable contours and waypoints need manual review in Scene view.

Blue waypoint graph notes:

- Blue points and blue lines are not walls and not walkable boundaries.
- They are pathfinding centerlines: routes go from one blue point to adjacent blue points.
- If a blue line crosses a red obstacle or gray non-walkable area, that edge shows red-orange in the debug view and the node or neighbor connection must be moved or removed.
- With `Edit Nav Graph In Scene` enabled, a `Nav Graph Editing` panel appears in the top-left of Scene view. Click the small cyan selection points beside blue points to select two waypoints, then use `Connect` to link manually or `Disconnect` to remove impassable connections.

Recommended manual correction workflow:

1. Select `VenueCalibrationDebug` in the scene.
2. Run `Create Map Reference Plane` from the component context menu to lay the current map image under Scene.
3. Open Scene view `Gizmos`.
4. In `Scene Editing`, enable as needed:
   - `Edit Calibration Points In Scene`
   - `Edit Attractions In Scene`
   - `Edit Walkable Areas In Scene`
   - `Edit Obstacle Areas In Scene`
   - `Edit Nav Graph In Scene`
5. Drag red origin / scale endpoints, yellow points, green polygon vertices, red obstacle vertices, or blue nav nodes directly in Scene.
6. To manually change blue lines, click the small cyan selection points beside two blue points in the top-left `Nav Graph Editing` panel, then click `Connect` or `Disconnect`.
7. Changes write back to `VenueMapDefinition` and can be undone with Undo.
8. Prioritize fixing red-orange nav edges because they represent impassable or wall-crossing connections in the current graph.

2026-07-01 update: `Populate Detected Nav Graph Draft` now produces a denser waypoint draft. It first places more corridor centerline nodes, then auto-filters connections that cross non-walkable areas with `IsMapSegmentWalkable`. The goal is to give the dog more free-movement choices later while avoiding a default graph that goes straight through walls.

2026-07-01 calibration editing rule update:

- When dragging `mapOriginPixel` in Scene, keep the world layout of already-set points and lines from drifting; code synchronously adjusts `originWorldPosition`.
- When dragging `scalePointAPixel` / `scalePointBPixel` in Scene, keep current meters-per-pixel unchanged so other points and lines are not rescale implicitly.
- If scale really needs to be recalculated later, add an explicit "recalibrate scale" operation rather than implicitly changing scale during ordinary drags.
- `VenueCalibrationDebugView` provides `Show Map Reference Plane` / `Hide Map Reference Plane` to show or hide the map underlay in Scene.
- `VenueCalibrationDebugViewEditor` provides nav graph manual connect / disconnect tools to repair valid paths broken in auto drafts or remove edges confirmed impassable by hand.

Data needed from the map owner:

- Walkable area polygon: key corner pixel coordinates of the yellow area outer contour. First version does not need extreme precision, but must cover main passages the user and dog can walk.
- If the yellow area splits into multiple disconnected blocks, each block needs its own `WalkableAreaDefinition`.
- Navigation waypoints: key turning-point pixel coordinates along walkable-area centerlines. Waypoint count can be fewer than polygon corners; the important part is having points at each corridor turn, junction, and near attractions.
- Waypoint neighbor relationships: each `VenueNavNodeDefinition.neighborNodeIds` lists directly passable adjacent node ids.

Minimum testable data:

1. A rough `WalkableAreaDefinition` covering the corridor from Photo Wall to near the red scale line.
2. 3–5 waypoints forming a path from Photo Wall to any one test attraction.
3. At least one attraction with `collectibleSpawnPixel` or `arrivalPixel` filled in.
4. In `VenueCalibrationDebugView` `Test Path`, fill `testStartPixel` and `testDestinationAttractionId`; Scene view should show an orange route.

Recommended naming:

- Main corridor waypoints use `main_01`, `main_02`, `main_03`.
- Branch points use attraction abbreviations, e.g. `photo_wall_arrival`, `drink_shop_arrival`.
- Fill neighbor relationships bidirectionally first, e.g. `main_01` connects to `main_02`, and `main_02` also connects to `main_01`.

Steps:

1. Convert yellow area boundaries into polygon data.
2. Add obstacle / wall polygons from non-walkable areas.
3. Hand-build the navigation graph initially; consider automatic sampling from polygons later.
4. Implement route generation from user position to attractions.
5. Render ground route lines.
6. Use `VenueNavigationRuntime` to test routes from HMD position to all 10 attractions.
7. Clamp route points and dog target points to the walkable area.
8. Add debug tools showing nearest valid points and blocked path edges.

Acceptance criteria:

- Generated routes stay inside the yellow area.
- Dog target points do not appear behind walls or through walls.
- Routes can be generated to each attraction from multiple test positions.

### Phase 3: V2 UI Flow

Goal: Replace the old selection flow with boot, intro, map button, big map, and top status text.

2026-07-01 update: Local minimap has been removed in favor of a top-right map button. Added `VenueMapUiController` and `VenueMapMarker` to show attraction markers on the big map, continuously update the user current-position marker, and call `VenueNavigationRuntime.StartNavigationToAttraction(attractionId)` after the user selects an attraction. First version focuses on the main chain: "map button does not block view, full big map can select attractions, map can enter navigation"; refine visuals, paw icons, intro, and the full V2 state machine later.

2026-07-01 Storyboard flow update:

```text
Boot / Logo -> Intro -> FreeRoam -> Tap Map Button -> BigMap -> Tap Attraction
-> Navigation -> Arrived -> 3D item grab to dog -> RewardPopup -> FreeRoam
```

Added `PuppyPathV2FlowController` as a lightweight state switcher on the old Canvas. It does not replace `UIBootSequence`; it enters `Intro` via `UIBootSequence.onBootFinished` and enters `Navigation` through attraction selection on `VenueMapUiController`.

2026-07-01 update: `RewardPanel` and `ItemGrabPanel` are no longer built as separate panels. After arriving at an attraction, the reward flow is handled by the real 3D item: after the user grabs the 3D item onto the dog, call `PuppyPathV2FlowController.ShowRewardPopup()`, show `RewardPopupPanel`, play the shaking animation, auto-close after 10 seconds, and return to `FreeRoam`.

Steps:

1. Reuse `UIBootSequence` for logo playback.
2. Add V2 main HUD:
   - Top status text.
   - Top-right map button in the view.
   - Dog speech bubble.
3. Implement intro timing and flow.
4. Implement big-map user position marker.
5. Implement paw markers for each attraction.
6. Implement big map open / close.
7. Implement attraction marker selection.
8. Connect marker selection to navigation mode.
9. Implement navigation exit button: clear current route, cancel target, hide navigation UI, and return to free roaming.

Acceptance criteria:

- Logo appears first.
- Dog and minimap appear after logo.
- Intro dialogue can play.
- Minimap can expand to big map.
- Tapping a paw marker enters navigation state.
- In navigation state, user can tap exit / cancel navigation and immediately return to free roaming.

### Phase 4: Dog Free Roam and Navigation Behavior

Goal: Make the dog reliable and present in the real venue.

Steps:

1. Spawn the dog after intro starts.
2. Free roam target selection:
   - Prefer 1.5–2 m ahead of the user.
   - Allow distance range 1–3 m.
   - If blocked ahead, test left-front and right-front.
   - Reject target points behind the user.
   - Clamp target points to the walkable area.
3. Navigation target selection:
   - Place the dog ahead on the generated route.
   - Keep an easy-to-watch distance from the user.
   - Use existing walk / trot animations.
4. Item interaction behavior:
   - Dog stops moving.
   - Dog sits.
   - Dog looks at the user or the item.
5. Arrival behavior:
   - Dog briefly celebrates.
   - System returns to free roaming after a few seconds.

Acceptance criteria:

- Dog stays visible during normal walking.
- Dog does not pass through walls.
- Dog does not stand outside the yellow area.
- Dog sits and waits while the user handles collectibles.

### Phase 5: Attraction Display and Collectible Items

Goal: Show floating virtual items when the user approaches attractions.

Steps:

1. Implement per-attraction item distance transparency control instead of a single on/off trigger.
2. First-version transparency curve:
   - 100% visible within 3 m.
   - About 50% visible at 6 m.
   - 0% visible beyond 10 m.
   - Smooth interpolation between 3–10 m.
3. Spawn or activate floating items at spawn points corresponding to yellow dots.
4. Synchronize item material, hint UI, or interactable state with transparency.
5. Show hint UI near items.
6. Support hand-grab gesture to grab and drag; no controllers.
7. Detect whether the item is placed on the dog target collider / zone.
8. Hide hints after successful collection.
9. Mark the attraction as collected.

1. 在小狗 prefab 根节点添加统一的饰品挂载点（2026-07-02 根据设计师意见调整：饰品挂在小狗整体根节点上，跟随小狗整体位置/朝向移动，不挂在某根具体骨骼上、不跟随骨骼自身的动画细节，例如头部动画的点头、尾巴摇动）。
   - `DogAccessorySlot`（Head/Face/Neck/Back/Tail）保留作为资产上的描述性标签，方便区分"这件饰品大致是头部风格还是脖子风格"，但不再对应到具体骨骼查找。
   - 每件饰品通过 `DogAccessoryDefinition` 上的 `localPositionOffset`/`localEulerOffset` 手动微调相对根节点的大致位置。
2. 为每个收集物定义饰品 slot（作为描述性分类，不影响挂载点）。
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

- Items appear only near their corresponding attraction.
- Item transparency changes continuously with user distance: clear up close, fading away at distance.
- Hints are readable and do not clutter the whole scene.
- User can hand-grab an item and drag it onto the dog.
- Same attraction cannot be collected twice unless explicitly reset.

### Phase 6: Dog Accessories and Rewards

Goal: Each collectible changes the dog's appearance and shows the corresponding reward.

Steps:

1. Add accessory anchors on the dog prefab:
   - Head.
   - Eyes / face.
   - Neck.
   - Body / back.
   - Tail or side if needed.
2. Define an accessory slot for each collectible.
3. Attach collected accessory prefabs to the corresponding slot.
4. Save collection state for the current session; no persistence across app restart for now.
5. Play 1–2 second shaking / surprise animation.
6. Show attraction reward UI.
7. Return to free roaming after the user closes the reward or configured time ends.

Acceptance criteria:

- Each collected item continues to show on the dog.
- Multiple accessories can coexist.
- Reward display is clearly tied to the attraction collection action.

### Phase 7: Dog Animation and Expression Polish

Goal: Make the dog more expressive and emotional.

Steps:

1. Review existing animation controller states.
2. Map V2 states to animation clips:
   - Idle / curious.
   - Walk ahead.
   - Guide.
   - Sit wait.
   - Happy reward.
   - Curious reveal.
3. Review face texture groups.
4. Add expression transitions for attraction reveal and reward moments.
5. Add barks or light sound effects when needed.

Acceptance criteria:

- Dog state changes are clear and readable.
- Animations do not flicker frequently.
- Sitting and waiting during item grab feels natural and intentional.

### Phase 8: On-Site Testing and Iteration

Goal: Verify the real event venue matches the Unity scene.

Steps:

1. Place known test markers at venue corners or reference points.
2. Test map scale with the 3.45 m wall.
3. Walk between attractions and compare real distances to Unity distances.
4. Tune attraction trigger radii.
5. Tune dog leading distance and obstacle fallback logic.
6. Test minimap direction with real movement.
7. Test all reward flows with on-site staff.
8. Update all related documents whenever the venue map or rewards change.

Acceptance criteria:

- User position on the minimap feels correct.
- Attractions trigger at expected real-world positions.
- Dog remains believable in narrow passages.
- On-site staff can understand the reward flow without extra technical explanation.

## Recommended Implementation Order

1. Lock venue coordinate system and scale.
2. Establish walkable area and attraction markers.
3. Generate minimap / big map from attraction data.
4. Implement route generation and ground route line.
5. Rework dog free roam and navigation position logic.
6. Add collectible display, grab, and placement.
7. Add accessory attachment and reward display.
8. Polish dog animation and expressions.

## Parallel Workstream Recommendations

Current recommendation splits into two non-blocking development lines:

### Workstream A: Map / Venue Coordinates / Navigation Foundation

Owner: Map owner.

Current tasks:

- Finish filling pixel coordinates for all 10 attractions / collectible spawn points in `VenueMapDefinition`.
- Use `VenueCalibrationDebugView` to verify scale, origin, map orientation, and relative attraction positions.
- Next: build hand-drawn polygon or waypoint drafts for the yellow walkable area.
- Later: implement `VenueNavGraph`, `VenuePathfinder`, `VenueRouteLineController`.
- Code already provides first-version `VenuePathfinder` and `VenueRouteLineController`; next focus is filling polygon and waypoint data and testing routes in Scene view.

Deliverables:

- Venue coordinate data readable by code.
- Trustworthy map markers and scale in Scene view.
- First-version walkable area / navigation graph.

### Workstream B: Dog Accessories / Rewards / Animation Testing

Owner: Second programmer.

This line can proceed in parallel because it mainly depends on the dog prefab, animations, and temporary test buttons, not final map coordinates.

Current tasks:

- Organize accessory anchors on the dog prefab: `Head`, `Face`, `Neck`, `Back`, etc.
- Add draft `DogAccessoryDefinition` and `DogAccessoryManager`; use test keys or Inspector buttons to attach accessory prefabs to specified anchors.
- Test sit, happy, surprise, shake, and related animation states in `DogTestScene` or a copied V2 test scene.
- Organize temporary accessory slots per attraction (specific accessories and reward content are not finalized; use placeholder data to run the flow through, without referencing specific item descriptions from early draft tables).
- Add a simple `RewardRevealController` draft that can show a test reward panel or trigger the existing fireworks prefab.
- Add draft `AttractionTrigger`, `FloatingCollectibleItem`, and `CollectibleGrabHandler` (remaining parts of "New Attraction and Collection Layer", merged into this workstream on 2026-07-01):
  - Attraction virtual items fade in by user distance (3 m / 6 m / 10 m tiers + interpolation in between).
  - Hand-grab to grab items, drag, detect placement on the dog, then trigger attach and collection marking.
  - Use fixed coordinates / radii in the test scene for distance and position first; connect to real attraction positions from `VenueMapDefinition` after Workstream A venue coordinates are ready.
  - Implement `CollectibleItemDefinition` data type together with this block.

2026-07-01 已交付（脚本层，Unity 编辑器内的手动接线步骤见下方"仍需在 Unity 编辑器中完成"）：

- `Assets/PuppyPath/Scripts/V2/Accessory/DogAccessoryAnchors.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/DogAccessoryDefinition.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/DogAccessoryManager.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/DogAccessoryTestKeys.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/RewardDefinition.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/RewardRevealController.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/CollectibleItemDefinition.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/FloatingCollectibleItem.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/AttractionTrigger.cs`
- `Assets/PuppyPath/Scripts/V2/Accessory/CollectibleGrabHandler.cs`
- `DogGuideController.cs` 追加了 `DogSpawned` 事件、`CurrentDog` 只读属性、`PlayOneShotState(string)` 方法（其余逻辑未改动）。

抓取交互建立在 Meta XR Interaction SDK 的 `Oculus.Interaction.Grabbable`（`WhenPointerEventRaised` 事件）之上，`HandGrabInteractable` 等具体 interactable 组件需要在 Inspector 里挂到收藏品 prefab 上，`FloatingCollectibleItem`/`CollectibleGrabHandler` 代码里不假设具体 interactable 类型（用 `Behaviour` 引用），保持解耦。

2026-07-02 根据设计师意见调整：`DogAccessoryAnchors` 改为统一挂载到小狗根节点（不区分 Head/Face/Neck/Back 具体骨骼），饰品跟随小狗整体位置/朝向移动，不跟随骨骼自身动画细节。`GetAnchor(slot)` 现在忽略 slot 参数，统一返回根节点 transform（或手动指定的 `rootAnchor` 覆盖值）。`DogAccessoryManager`/`DogGuideController` 无需改动。

仍需在 Unity 编辑器中完成（纯手动操作，非代码）：

- 在小狗实际使用的 prefab（当前测试场景引用的是 `DogPrefab3.prefab`，注意项目里有 `DogPrefab`/`DogPrefab2`/`DogPrefab3` 三个变体，需要确认 `DogGuideController.dogPrefab` 具体指向哪个）根节点挂 `DogAccessoryAnchors`，`Root Anchor` 字段留空即可（默认使用自身 transform）。
- 创建占位测试数据资产（`DogAccessoryDefinition`、`RewardDefinition`、`CollectibleItemDefinition`）和占位几何体 prefab，注意每个 `DogAccessoryDefinition` 需要不同的 `id` 才能同时共存，不能多个资产共用同一个 `id`。
- 复制 `DogTestScene.unity` 为 `DogAccessoryTestScene.unity`，挂好新脚本、场景引用和测试键位。
- 在收藏品 prefab 上挂 Meta XR Interaction SDK 的 `Grabbable` + `HandGrabInteractable`（或 `DistanceHandGrabInteractable`），并接到 `CollectibleGrabHandler`/`FloatingCollectibleItem` 的对应字段。

边界约束：
Boundary constraints:

- Do not modify coordinate logic in `VenueMapDefinition`, `VenueCoordinateMapper`, or `VenueCalibrationDebugView`.
- Do not hard-wire reward flow to map navigation yet; build independently testable APIs first, e.g. `ShowReward(string attractionId)`.
- If `DogGuideController` must change, prefer adding a wrapper or small public methods rather than rewriting existing navigation behavior.
- Validate distance fade and grab-placement mechanics with test coordinates first; do not block this workstream waiting for real coordinate integration.

Deliverables:

- Usable accessory anchors on the dog prefab.
- Manager that can manually attach / detach accessories in a test scene.
- Standalone demo of reward display and dog animation reactions.
- Standalone testable demo of attraction item fade-in + hand-grab placement onto the dog (test coordinates, not yet depending on real venue data).

### Modules To Be Assigned

The following modules are listed by script name in "Recommended Architecture" but are not yet assigned to Workstream A or B. Record them here to avoid omission:

- `PuppyPathV2GameController` / `PuppyPathV2FlowController` follow-up integration: `PuppyPathV2FlowController` already handles first-version UI flow (`Boot` / `Intro` / `FreeRoam` / `BigMap` / `Navigation` / `RewardPopup`). If dog behavior, 3D item grabbing, and reward data grow more complex later, decide whether to split out a higher-level game controller.
- Map button / big map UI: now `VenueMapUiController` + `VenueMapMarker` + `VenueMapOpenButton`. Script skeleton is done; next step is wiring old Canvas `IntroPanel`, `MapPanel`, `NavigationHudPanel`, and other panels into `PuppyPathV2FlowController` and adjusting visuals per storyboard.
- `AttractionRegistry`: to be assigned. The `attractions` list inside `VenueMapDefinition` already serves a similar data access role; whether to extract a separate type is left for Workstream A to decide later.
- `DogVenueFollower` (dog follow / obstacle avoidance within walkable area): to be assigned. Needs Workstream A walkable-area data; logically closer to `DogGuideController`, so likely Workstream B takes it later, but real integration waits until A's walkable data exists—record only for now, no schedule yet.

## Risks

- The current map is only a draft and will change; spatial data must be easy to update.
- If the real XR tracking origin is not aligned with the map, all attraction triggers will be misaligned.
- The old route system is user-relative generated and cannot directly satisfy real-venue navigation.
- Dog visibility and obstacle avoidance must be validated on-site; Editor simulation alone is not enough.
- Grab interaction uses hand tracking only; pointing pinch vs grab recognition stability needs focused validation.

## Required Documentation Sync Rules

When implementation changes any architecture, phase, task, acceptance criteria, or risk assessment, this document must be updated in the same change set as the code / scene modification.
