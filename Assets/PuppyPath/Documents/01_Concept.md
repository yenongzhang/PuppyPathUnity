# PuppyPath V2 Concept Document

Last updated: 2026-07-01

## Deprecated Legacy Features

The friend list and find-a-friend flow from the V1 prototype are deprecated. V2 is map-only: users select attractions from the big map, not friends from a friends list.

## Legacy Prototype UI Copy (location-only)

The table below preserves the V1 prototype intro and map instruction strings, with friend-related wording removed. These strings are retained for reference only; V2 uses the copy defined in the sections below.

| UI Element | Legacy String (location-only) |
| --- | --- |
| Intro — phase 1, first text | Hi, I'm PuppyPath :) |
| Intro — phase 1, second text | I'll have a little beagle take you wherever you want to go. |
| Intro — phase 1, third text | Please select the location you want to visit on the map. |
| Map selection — phase 2 default | Do you want to go to the location you marked?<br>Click "Show Path" to preview your path. |
| Map selection — phase 2 template (`locationPhase2TextTemplate`) | Do you want to go to {0} in Europa-Park?<br>Click "Show Path" to preview your path. |
| Path preview | Here is the first part of your path.<br><br>Ready to go? |
| Navigation encouragement | Good job! You're on the right path. |

## One-Line Concept

PuppyPath is a cute mixed-reality puppy companion. It knows the real event venue and leads users to treasures hidden near each attraction; every treasure collected adds another cute accessory to the puppy.

## Experience Goals

Users should feel like this puppy has set up a treasure hunt at the venue in advance. It is not a simple navigation arrow but a character with personality: it introduces itself, stays in the user's field of view, leads the way when needed, waits patiently while the user grabs items, celebrates discoveries together, and becomes more distinctive as the user collects accessories.

## Core Loop

1. The user enters the app.
2. `logoCanvas` appears on screen first.
3. After the logo disappears, the puppy appears on the ground.
4. A UI dialog bubble appears above the puppy's head for a self-introduction.
5. A map button appears in the upper-right of the field of view. The mini-map is not shown continuously, to avoid blocking the main view.
6. By default the user is in free roam: wander freely and naturally discover nearby attractions.
7. When the user taps the map button in the upper-right and opens the big map, they can tap a paw-print marker to select a target attraction. Only then does the system enter navigation mode.
8. When the user approaches an attraction, the corresponding virtual item fades in by distance: clearer when closer, more transparent when farther away.
9. The user grabs the virtual item and drags it onto the puppy.
10. While the user is grabbing an item, the puppy sits and waits.
11. The item becomes a wearable accessory on the puppy.
12. A 1–2 second shaking / surprise animation plays on screen.
13. The surprise reward for that attraction is shown.
14. The system returns to free roam; the user can continue collecting more items.

## Puppy Opening Self-Introduction Copy

Recommended English version:

> Hi, I'm PuppyPath! Follow my paw prints to find hidden treasures. Drag anything shiny onto me — there might be a surprise!

Tone direction:

- Cute, energetic, with a touch of playfulness.
- Conveys that the puppy knows this event venue.
- Conveys that paw prints mark attractions worth exploring.
- Conveys that treasures are hidden near attractions.
- Conveys that the user can drag found items onto the puppy.

## UI Modes

### Boot Mode

- Only the logo canvas is shown.
- Main interaction UI is hidden until the logo sequence finishes.
- The existing `UIBootSequence` can be reused and extended.

### Intro Mode

- The puppy appears on the ground.
- A speech bubble is shown above the puppy's head.
- The map button appears in the upper-right of the field of view.
- After the puppy's introduction ends, the system enters free roam mode.

### Free Roam Mode

- Top UI copy can be: `Free roam`
- A cuter variant can be: `Let's wander around and see if we can sniff out some treasures!`
- Only the map button remains in the upper-right; map content is hidden by default to minimize obstruction of the main view.
- The puppy stays roughly 1–3 m ahead of the user.
- The puppy must remain inside the yellow walkable area.
- The puppy must not pass through walls or non-walkable areas.
- If there is an obstacle directly ahead of the user, the puppy can move to the left-front or right-front instead.
- The puppy should avoid running behind the user so it stays visible.
- When the user approaches an attraction, that attraction's virtual item can change opacity automatically based on distance.

### Map Selection Mode

- The user taps or touches the map button in the upper-right.
- The map button opens a centered full-size big map.
- Paw-print markers represent each attraction.
- Navigation begins only after the user taps a paw-print marker; if no marker is tapped, free roam continues.
- Closing or canceling the big map returns to free roam mode.

### Navigation Mode

- A line appears on the ground from near the user's current position to the target attraction.
- The puppy leads the way ahead.
- Top UI copy: `Going to {attraction name}...`
- Navigation mode UI must provide a clear exit / cancel navigation button, for example: `Exit navigation`.
- When the user taps exit navigation, the system clears the ground route, cancels the current target, and the puppy returns to free-roam follow behavior.
- The puppy must still stay within the walkable area and remain visible ahead of the user.
- When the user enters the attraction range, the top UI changes to: `Arrived at {attraction name}`
- After a few seconds the system automatically switches back to free roam mode; if an attraction item interaction is in progress, the item interaction flow takes priority.

### Attraction Item Interaction Mode

- Triggered when the user approaches an attraction, for example within 5 m.
- The attraction's virtual item opacity is controlled continuously by distance to the user, not by a simple on/off toggle.
- Recommended first-version distance-opacity rules:
  - User within 3 m of the item: 100% visible.
  - User around 6 m from the item: 50% visible.
  - User more than 10 m from the item: 0% visible.
  - Use smooth interpolation between 3–10 m to avoid opacity jumps.
- UI hint near the item: `Drag it onto the puppy with your hand — there might be a surprise!`
- The puppy switches to a sitting-and-waiting pose.
- The user grabs the item with hand tracking and drags it onto the puppy; the current version does not use controllers.
- After successfully placing it on the puppy:
  - The item attaches to the puppy's corresponding accessory slot.
  - Within the current session, the puppy keeps wearing that accessory.
  - Play a 1–2 second shaking / surprise animation.
  - Show the reward UI or reward content.

## Attraction Draft

On the current new map, yellow dots mark the actual spawn positions for virtual items at each attraction. Attraction labels and item spawn points may not coincide exactly; implementation should treat the yellow dots as collectible spawn points.

| Attraction / Item Point | Yellow Item Spawn on Map | Virtual Item Draft | Puppy Accessory Draft | Reward Draft |
| --- | --- | --- | --- | --- |
| Chess | Upper-left area, yellow dot above the Chess marker | Chess piece | Small crown or chessboard hat | TBD |
| Couch | Upper-left area, yellow dot below the Couch marker | Throw pillow or small blanket | Scarf | TBD |
| Photo Wall | Mid-left area, yellow dot near the Photo Wall marker | Camera or photo frame | Glasses | TBD |
| Goodies | Lower-left inset area, yellow dot near the Goodies marker | Snack bag | Bandana | TBD |
| Book Wall | Upper-right mid corridor, yellow dot near the Book Wall marker | Bookmark, sticker, or badge | Cool sunglasses | TBD |
| Tap Water | Bottom center-right, yellow dot near the Tap Water marker | Water drop, cup, or small kettle | Small bow tie or collar charm | TBD |
| Ice Cream Shop | Yellow dot between Tap Water and Drink Shop, to the right of the gray bar; intended as the ice cream shop | Ice cream scoop or cone | Cone hat or colorful scarf | TBD |
| Drink Shop | Lower-right area, yellow dot near the Fridge / drink shop marker on the right | Cola can or drink cup | Hat or collar charm | Free cola / drink reward |
| Piano | Mid-lower right area, yellow dot near the Piano marker | Musical note | Bow tie | TBD |
| Plants | Yellow dot inside the Plants dashed area at the lower-right | Small plant or leaf | Flower wreath or back accessory | TBD |

Reward details remain TBD because the user indicated they will be defined later.

## Theme-Park Style Naming Scheme

The names below are the current recommended naming scheme for unifying map markers, navigation UI, puppy dialogue, and reward copy. The goal is for attractions to feel like stations in a theme park rather than plain office/venue labels. If the user confirms final names later, this table and the venue data document must be updated together.

| Map Original Marker / Item Point | Recommended English Display Name | Chinese Reference Name | Notes |
| --- | --- | --- | --- |
| Chess | Checkmate Corner | 棋遇小屋 | Suited to chess pieces, crowns, strategy-themed rewards |
| Couch | Cozy Couch Cove | 软乎乎沙发湾 | Suited to rest, throw pillows, scarves |
| Photo Wall | Snapshot Studio | 咔嚓照相馆 | Suited to cameras, frames, glasses |
| Goodies | Treat Trove | 甜甜补给站 | Suited to snack bags, stickers, small gifts |
| Book Wall | Storybook Wall | 故事书墙 | Suited to bookmarks, badges, paper-style items |
| Tap Water | Splash Stop | 汪汪补水站 | Suited to water drops, cups, small kettles |
| Ice Cream Shop | Scoop Station | 冰淇淋小站 | Suited to cones, ice cream scoops, dessert rewards |
| Drink Shop | Fizzy Fridge | 气泡饮料铺 | Suited to cola, drink vouchers |
| Piano | Melody Corner | 音符小舞台 | Suited to musical notes, bow ties, music rewards |
| Plants | Garden Patch | 小狗花园 | Suited to leaves, flower wreaths, nature-themed rewards |

## Design Principles

- Venue accuracy first: the virtual walkable area must match the real yellow activity zone.
- Puppy visibility first: the puppy should always feel present, easy to read, and not hidden behind the user.
- Gentle guidance: navigation should feel like being led by the puppy, not commanded by a strict GPS arrow.
- Collectible growth: every item collected must produce a visible change on the puppy.
- Short feedback loop: show item, grab, wear, surprise, reward — this sequence must be fast and clear.
- Lightweight UI: show only essential text while walking; put detailed choices in the big map or reward UI.

## 2026-07-01 Puppy Behavior Update

- The puppy must always try to stay ahead of the user's movement direction, not run behind the user.
- In free roam / wander mode, the puppy no longer judges whether the user is off-route and no longer reacts negatively when the user goes the wrong way.
- While the user is moving, the puppy must not sit or wait just because the user has not fully stopped; it should keep walking or running to stay ahead of the user.
- The puppy should detect the HMD's horizontal movement speed and move at roughly the same speed or slightly faster than the user.
- With no navigation target, the puppy uses the user's HMD movement direction as forward; if the user is temporarily still, use head orientation as fallback.
- In navigation mode, the puppy leads ahead along the venue pathfinding route.
- When obstacles or non-walkable areas are ahead, the puppy cannot go straight through; it should choose an alternative target point within the walkable area.
- When approaching a treasure / collectible spawn point, the puppy should bark happily toward the treasure, not give negative feedback toward the user.
- The puppy can still have positive random behaviors such as happiness, curiosity, and sniffing, but these must not override the main rule of staying ahead of and visible to the user.

## 2026-07-01 Storyboard Flow Update

Per the storyboard, the current V2 app flow is:

```text
Boot / Logo
-> Intro
-> FreeRoam
-> Tap Map Button
-> BigMap
-> Tap Attraction
-> Navigation
-> Arrived
-> 3D item grab to dog
-> RewardPopup
-> FreeRoam
```

Hidden developer / on-site calibration entry:

```text
Tap Reset / Adjust Orientation -> Boot or on-site calibration
```

Important clarification: the current version removes `MiniMap`. Only the map button appears in the upper-right; full venue selection appears only in `BigMap`.

## Open Design Questions

- Precise physical origin when aligning Unity to the real venue. Prefer a fixed physical calibration point + Meta Quest Spatial Anchor / venue calibration flow; QR codes can assist recognition or manual confirmation but should not be the sole positioning basis.
- Venue map orientation in Unity — which direction corresponds to Unity `+Z`.
- Final attraction positions after the real map is updated.
- Final virtual item models and puppy accessory attachment slots.
- Final reward content for each attraction.
- Whether rewards are purely visual, vouchers, QR codes, or require on-site staff coordination.
- Confirmed: users use hand tracking, not controllers. The legacy PuppyPath already used pointing pinch; V2 adds grab on top of that.
- Confirmed: collection progress does not need to persist across app restarts for now; only the current session's collection state is kept.

## Required Documentation Sync Rules

When any requirement changes, this document must be updated in the same step as code or scene changes. In particular, update this document when attractions, copy, rewards, interaction ranges, puppy behavior, or user flow change.
