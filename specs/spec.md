# Spec: Map Theme Selection and Synchronized Destructible Terrain

## Summary
Before an active multiplayer match starts, players choose a predefined map theme. The game loads the matching terrain preset for that theme. During the match, when a projectile collides with the terrain, the game removes a small circular portion of the terrain and synchronizes that terrain change to all connected clients in the same match.

## Actors

- Player client A
- Player client B
- Game service (WebSocket match authority)
- Terrain system in Unity
- Match/session setup flow

## Map Themes
Allowed map themes are predefined values. Example set:

- `desert`
- `snow`
- `grassland`
- `canyon`
- `volcanic`

Each theme must map to one predefined terrain preset. The same `mapType` must always load the same base layout.

## Match Configuration Requirement
Before combat starts, the match must contain a `mapType` value.

That value must be:

- selected before the session becomes active
- shared by all players in the match
- used by all clients to load the same terrain preset

## Trigger
This feature has two triggers:

1. map theme selection before the match
2. projectile impact on terrain during the active match

## Required Data
The synchronized terrain destruction event must contain only the minimum data required to reproduce the same terrain change:

- `type`: event name
- `gameId`: match identifier
- `mapType`: selected map theme if needed during initial game state transfer
- `impactX`: x position of the impact in shared terrain space
- `impactY`: y position of the impact in shared terrain space
- `radius`: destruction radius
- optional: `projectileId` or `shotId` if needed to prevent duplicate application

## Pre-Match Map Selection Behavior

### 1. Selection
Before the match starts, one valid `mapType` must be chosen for the session.

### 2. Validation
The selected `mapType` must belong to the allowed predefined list.

If validation fails:

- the match cannot start with that map type
- the invalid selection must be rejected

### 3. Loading
When clients enter combat:

- they must know the selected `mapType`
- they must load the corresponding predefined terrain preset
- both clients must start from the same base terrain

## WebSocket Contract

### Client to Server
`terrain_hit_candidate`

Purpose:
- report that a projectile impacted terrain

Payload:
```json
{
  "type": "terrain_hit_candidate",
  "gameId": 12,
  "playerId": 3,
  "impactX": 41.5,
  "impactY": 18.2,
  "radius": 2.5
}
```

### Server to Clients
`terrain_destroyed`

Purpose:
- announce the authoritative terrain destruction to all match participants

Payload:
```json
{
  "type": "terrain_destroyed",
  "gameId": 12,
  "impactX": 41.5,
  "impactY": 18.2,
  "radius": 2.5
}
```

### Optional Initial Match State
`game_start`

This event may also include the chosen `mapType` so all clients load the correct preset.

Example:
```json
{
  "type": "game_start",
  "gameId": 12,
  "mapType": "desert"
}
```

### Error Event
`error`

Purpose:
- reject invalid or out-of-scope destruction events

Possible reasons:

- match not active
- player not in match
- invalid coordinates
- invalid radius
- duplicate event

## Functional Behavior

### 0. Map Preset Agreement
Before any terrain destruction occurs:

- the match must have one valid selected `mapType`
- both clients must load the same predefined terrain preset

Terrain destruction must always be applied on top of that shared preset.

### 1. Terrain Impact Detection
When a projectile collides with terrain:

- the impact point must be detected
- the terrain destruction radius must be determined
- the event data must be prepared in shared terrain space

### 2. Validation
Before destruction is broadcast:

- the match must exist
- the match must be active
- the sender must belong to the match
- the coordinates must be inside the valid terrain bounds
- the radius must be within an allowed range

If validation fails:

- no terrain change is applied
- an `error` event may be sent to the sender

### 3. Authoritative Destruction
After validation:

- the match authority emits one `terrain_destroyed` event
- all connected match clients receive the same impact position and radius

### 4. Client Application
When a client receives `terrain_destroyed`:

- it applies a circular subtraction to the terrain
- it refreshes the terrain collider
- it refreshes the visual terrain representation

The client must apply the same deterministic operation for the same payload.

### 5. Tank Ground Correction
After terrain modification:

- tanks must still be grounded on valid terrain
- if a tank is no longer grounded, the system must move it downward until it reaches stable ground, or apply a simple defined fallback rule

The behavior does not need to simulate complex collapse physics. A simple, consistent correction is acceptable.

## Non-Functional Requirements

- the payload must stay small
- the feature must be deterministic
- the same event must not produce different crater shapes on different clients
- performance must remain acceptable for a student project
- the implementation must be understandable and easy to demo

## Accepted Simplifications

- a fixed crater radius is acceptable
- a single terrain layer is acceptable
- only one terrain object per match is acceptable
- immediate local correction of tank position is acceptable
- only one impact processed at a time is acceptable

## Rejected Behaviors

- random crater shape that differs between clients
- client-only terrain destruction with no synchronization
- large terrain destruction with no bounds validation
- terrain updates that do not refresh colliders
- terrain destruction that affects clients outside the match

## Acceptance Scenarios

### Scenario A: Valid impact
Given an active match with two connected players on the same selected map theme,
when a projectile hits the terrain,
then one `terrain_destroyed` event is emitted,
and both clients show the same crater in the same place.

### Scenario A2: Valid map selection
Given a match that has not started yet,
when a player selects a valid predefined map theme,
then that `mapType` becomes the terrain preset for the match,
and both clients load the same base map.

### Scenario B: Invalid radius
Given an active match,
when a client reports a terrain hit with an out-of-range radius,
then the event is rejected
and no terrain change is applied.

### Scenario C: Tank support after destruction
Given a crater removes terrain under or near a tank,
when the terrain update is applied,
then the tank is corrected to a valid grounded position according to the defined fallback rule.

### Scenario D: Match isolation
Given two matches are active at the same time,
when terrain is destroyed in one match,
then only the players in that match receive the `terrain_destroyed` event.

## Definition of Done

- players can select one valid predefined map theme for the match
- both clients load the same preset for that theme
- terrain impact can produce a synchronized crater
- both clients apply the same change
- collider updates after terrain deformation
- tank placement remains valid after terrain change
- invalid terrain events are rejected safely
