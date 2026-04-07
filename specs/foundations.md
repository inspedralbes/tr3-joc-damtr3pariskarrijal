# Foundations: Map Theme Selection and Synchronized Destructible Terrain

## Feature
Players choose a predefined map theme before the match starts, and projectile impacts cause synchronized terrain destruction during the match.

## Project Context
This project is a 2D multiplayer tank game built with Unity on the client side and Node.js microservices on the backend. The game already uses:

- HTTP for login, register, create game, and join game
- WebSockets for active in-match communication
- a shared match state between two connected players

The selected SDD feature is a focused gameplay slice with two connected parts:

- players choose a predefined map theme for the match
- when a projectile collides with the terrain, a small part of that selected map is destroyed and the same destruction appears on every connected client in the same match

## Why This Feature
This feature is a good SDD candidate because it is:

- clearly bounded
- visually demonstrable
- relevant to the core gameplay of a Tank Stars / Worms-like game
- connected to multiplayer synchronization, not only local Unity rendering

It also fits the course requirements because it combines Unity, WebSockets, shared game state, and a manageable real-time event.

## Objective
Implement a multiplayer-safe map and terrain flow where:

1. players select a map theme before the match starts
2. the match stores that map theme
3. all clients load the same predefined map preset for that theme
4. a projectile hits the terrain
5. the impact point and destruction radius are computed
6. the terrain change is sent through the active match channel
7. all clients apply the same terrain modification
8. the terrain remains playable after the update

## Scope
Included in scope:

- predefined map themes such as desert, snow, grassland, canyon, or volcanic
- one selected map theme per match
- one shared 2D terrain preset for a match
- projectile-to-terrain collision detection
- destruction of a small circular area of terrain
- synchronization of destruction events over WebSocket
- collider refresh after terrain deformation
- keeping tanks aligned to valid ground after the terrain changes

Out of scope:

- procedural terrain generation
- advanced physics simulation
- particle effects polish
- terrain material variation
- saving terrain state to the database
- replay system
- rollback / reconciliation netcode
- AI / ML-Agent behavior

## Functional Boundary
This feature starts before the match begins, when players choose the map theme for the session.

This feature ends when:

- all clients have loaded the same selected map preset
- the destruction has been applied on all connected clients
- the terrain collider has been refreshed
- tanks are still placed on valid ground, or a defined fallback rule has been applied

## Assumptions

- matches are 1v1
- the game already has a working match/session identity such as `gameId`
- the game can store a simple `mapType` value in the session or match state
- projectile firing already exists or will exist separately
- each map theme uses a predefined terrain preset created in advance
- the terrain can be represented in a modifiable form, such as a texture mask, bitmap, polygon data, or another editable 2D representation
- both clients use the same terrain dimensions and coordinate reference

## Constraints

- The synchronized payload must be minimal. Only the data needed to reproduce the same terrain destruction should be transmitted.
- The selected `mapType` must resolve to the same preset on every client.
- Terrain destruction must be deterministic across clients.
- The terrain update must not depend on local-only randomness.
- The destruction must remain small and controlled to keep the scope realistic.
- The solution must be understandable and maintainable within a student project.

## Technical Direction

- Unity is responsible for visual terrain rendering and local collider refresh.
- Unity is responsible for loading the correct predefined terrain preset for the selected `mapType`.
- The multiplayer channel uses WebSockets.
- The match session system identifies which players receive the terrain update.
- The terrain destruction event should be authoritative from one source. In this project, that should be the active match authority, preferably the game service.

## Risks

- inconsistent terrain deformation between clients
- clients loading different presets for the same map type
- collider not updating correctly after terrain changes
- tanks floating or clipping after ground removal
- payload using world coordinates incorrectly
- too much destruction causing unplayable terrain

## Success Criteria
The feature is considered successful if:

1. players can choose a predefined map theme before the match
2. both clients load the same preset for the selected map theme
3. a projectile hits the terrain during a multiplayer match
4. a single synchronized destruction event is emitted
5. both clients show the same crater in the same location
6. the terrain collider updates correctly on both clients
7. tanks remain on valid terrain or follow a defined fallback behavior
8. the feature works without introducing obvious desynchronization
