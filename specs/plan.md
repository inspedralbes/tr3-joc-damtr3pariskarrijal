# Plan: Map Theme Selection and Synchronized Destructible Terrain

## Implementation Goal
Add a narrow, demonstrable feature where players choose a predefined map theme for the match, all clients load the same terrain preset for that theme, and terrain destruction is synchronized across all clients in the same active match.

## Proposed Architecture

### Unity Client
Responsibilities:

- expose map theme selection in the pre-match flow
- load the terrain preset for the selected `mapType`
- detect projectile collision with terrain
- compute or capture impact position
- send terrain impact candidate through WebSocket
- apply `terrain_destroyed` events received from the match authority
- refresh visual terrain and collider
- correct tank grounding after terrain deformation

### Game Service
Responsibilities:

- validate the selected `mapType` if it is part of the match state
- include the selected `mapType` in the initial game state if needed
- validate terrain destruction events
- ensure the sender belongs to the active match
- broadcast one authoritative `terrain_destroyed` event to all players in that match

The game service should not generate complex terrain geometry itself. It only validates and distributes the authoritative event payload.

## Proposed File-Level Work

### Unity
Possible files or systems:

- menu or lobby selection UI for `mapType`
- `Projectile` or shot handling script
- terrain manager script, for example `TerrainManager.cs`
- combat/gameplay manager that already owns the match WebSocket
- tank grounding helper logic

### Backend
Possible files or systems:

- match/session configuration handling for `mapType`
- WebSocket message handler in the game service
- match session state validation

## Step-by-Step Plan

### Step 1: Define predefined map presets
Create a small fixed set of map themes and one preset terrain layout for each.

Recommended example set:

- desert
- snow
- grassland
- canyon

Requirement:

- each theme always maps to the same base terrain preset
- no procedural generation is needed for this feature

### Step 2: Define the terrain representation
Choose one editable terrain representation in Unity.

Recommended for student scope:

- a destructible mask/texture approach
- or a simple editable 2D collider shape approach

Requirement:
- the same input data must always create the same crater

### Step 3: Add pre-match map selection
Before the match starts:

- allow the player or host to choose a valid predefined `mapType`
- store the selected `mapType` in match/session state
- make sure the combat scene knows which preset to load

### Step 4: Detect projectile impact
Add collision detection between projectile and terrain.

On collision:

- capture impact point
- stop the projectile
- generate a fixed or validated radius
- convert the impact to shared terrain coordinates if necessary

### Step 5: Send a WebSocket terrain event
From the active gameplay client flow, send `terrain_hit_candidate` with:

- `gameId`
- `playerId`
- `impactX`
- `impactY`
- `radius`

### Step 6: Validate in the game service
In the WebSocket server:

- verify the match has a valid `mapType`
- verify active match
- verify sender belongs to match
- verify coordinates are inside allowed bounds
- verify radius is inside allowed bounds

If valid:

- emit `terrain_destroyed` to the match players only

If invalid:

- reject and optionally return an `error` event

### Step 7: Load the selected map preset on clients
When the combat session starts:

- read the selected `mapType`
- load the corresponding predefined terrain preset
- ensure both clients start from the same base terrain

### Step 8: Apply terrain destruction on clients
When `terrain_destroyed` is received:

- modify the terrain data using a deterministic circular subtraction
- rebuild or refresh the collider
- update the visual terrain representation

Important:
- do not use local randomness
- do not let each client decide a different crater

### Step 9: Correct tank placement
After the terrain update:

- check whether each tank is still supported by terrain
- if not, move the tank downward until it reaches stable ground
- use a simple and consistent fallback rule

### Step 10: Test the feature
Minimum tests:

1. one valid map theme selection before the match
2. both clients load the same preset for the same `mapType`
3. one valid terrain impact in a two-player match
4. both clients show the same crater
5. invalid payload is rejected
6. terrain collider still works after several hits
7. tank remains grounded after nearby destruction

## Suggested Constants
These can be tuned later, but should start simple:

- one fixed crater radius for MVP
- one valid terrain bounds rectangle
- one maximum allowed radius

## Simplest Viable Version
If implementation time is short, the MVP should be:

1. a small fixed set of map themes
2. one predefined preset per theme
3. fixed crater radius
4. impact point from projectile collision
5. one `terrain_destroyed` event
6. deterministic crater application on both clients

This is enough for a valid SDD demonstration.

## Risks and Mitigations

### Risk: Clients produce different crater shapes
Mitigation:
- use identical deterministic crater logic and identical coordinate space

### Risk: Clients load different maps
Mitigation:
- use one shared `mapType` value and one fixed preset per theme

### Risk: Collider does not update
Mitigation:
- rebuild or refresh the collider immediately after terrain change

### Risk: Tanks float after terrain deletion
Mitigation:
- run a simple grounding correction after each terrain update

### Risk: Scope grows too much
Mitigation:
- keep only one terrain object, one crater shape, one match type, and one synchronization event

## Completion Criteria
The implementation is complete when:

- the match can store and communicate one valid map theme
- both clients load the same terrain preset for that theme
- a projectile can trigger terrain destruction
- the destruction is synchronized through WebSocket
- both players see the same updated terrain
- collider and grounding still work
- the feature is small enough to explain clearly in a teacher presentation
