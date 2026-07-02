# PuppyPath V2 Documentation Readme

Last updated: 2026-07-01

## Document Purpose

This folder records the design and development plan for PuppyPath version 2 before and during implementation. V2 transforms the current project from a "map selection / path preview" prototype into a mixed-reality treasure hunt experience that precisely matches the real event venue:

- The Unity scene must match the real venue in scale, walkable area, and attraction positions.
- The puppy must stay in the user's field of view at all times; it leads the way when navigation is needed and sits patiently while the user grabs virtual items.
- Floating collectible virtual items appear near each attraction. When the user drags an item onto the puppy, it becomes a wearable accessory and unlocks the corresponding surprise.
- The UI flow starts from the existing `logoCanvas`, then moves through the mini-map button, free roam, navigation, and attraction collection.

## Document List

- `01_Concept.md`: Product concept, user flow, interaction design, experience tone, and first-version copy.
- `02_Implementation_Plan.md`: Development phases, systems to implement, reusable scripts, and test steps.
- `03_Site_Calibration_And_Data.md`: Venue scale, coordinate mapping, yellow walkable area, attractions, and data conventions.
- `04_Existing_Project_Audit.md`: Inventory of current assets, scripts, and scenes, and how they migrate to V2.

## Required Documentation Sync Rules

When the user introduces a new requirement, or when attraction names, the real map, reward content, UI copy, interaction rules, or implementation plans change, the corresponding documents in this folder must be updated in the same development step.

This rule is part of the V2 workflow. Code, Unity scenes, and documentation must not drift apart, or on-site debugging will quickly become unmanageable.

## Current Input Materials

- Event venue map sketch provided by the user on 2026-06-30.
- Red wall markers on the map are at real scale; the real-world length is 3.45 m.
- The yellow areas on the map are where people and the dog can walk.
- Attractions / yellow virtual item spawn points visible on the current new map:
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

## Current Implementation Status

V2 implementation has begun. The first version of venue coordinate / map data, Scene calibration debug tools, walkable area / nav graph editing tools, `VenuePathfinder`, and `VenueRouteLineController` are complete. `VenueNavigationRuntime` has been added to generate real-venue routes from the HMD's current world position to an attraction.

For device testing, `VenueAlignmentManager`, `VenueSpatialAnchorBootstrap`, and `VenueWalkableGridVisualizer` have been added. On Quest, the yellow grid can currently visualize the walkable area, and the venue can be temporarily aligned by standing at the Photo Wall top-right origin facing map north.

Next priorities: complete the Spatial Anchor save / load restore flow, wire `VenueNavigationRuntime` into the actual UI selection flow, and continue developing the dedicated `DogVenueFollower` so the puppy stays ahead of the user in both free roam and navigation modes without passing through walls.
