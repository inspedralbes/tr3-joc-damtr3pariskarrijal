# Registre de Prompts — Tank Stars

## Prompt 1 — 2026-04-01
**Prompt:** Crear l'estructura base del projecte Unity amb carpetes, escenes i scripts buits.
**Resultat:** Estructura de carpetes creada correctament amb totes les escenes i scripts necessaris.
**Problema detectat:** Cap.
**Correcció:** Cap necessària.

## Prompt 2 — 2026-04-02
**Prompt:** Implementar GameManager singleton amb DontDestroyOnLoad i AuthManager amb login/register.
**Resultat:** GameManager i AuthManager funcionant, connexió amb backend via UnityWebRequest.
**Problema detectat:** OnEnable() en AuthManager accedia a rootVisualElement que podia ser null.
**Correcció:** Afegit comprovació de null abans d'accedir als elements UXML.

## Prompt 3 — 2026-04-03
**Prompt:** Crear MenuManager amb selecció de mapa, crear/unir-se a partida i mode VS IA.
**Resultat:** MenuManager implementat amb ciclatge de mapes i connexió API.
**Problema detectat:** Els textos estaven en anglès, haurien d'estar en català.
**Correcció:** Traduïts tots els textos de la interfície al català.

## Prompt 4 — 2026-04-05
**Prompt:** Implementar TerrainGenerator amb FBM Perlin noise i destrucció de terreny.
**Resultat:** Terreny procedural funcionant amb 5 biomes i destrucció circular.
**Problema detectat:** El material del terreny utilitzava Sprites/Default que ignora vertex colors.
**Correcció:** Canviat a Universal Render Pipeline/Unlit amb color base blanc.

## Prompt 5 — 2026-04-07
**Prompt:** Crear CombatManager amb WebSocket per al combat multijugador.
**Resultat:** Combat funcional amb WebSocket, HP bars, sliders i animació de projectil.
**Problema detectat:** PlaceTanksFromPercent intercanviava posicions quan eres player2.
**Correcció:** Canviat per col·locar sempre player1Tank a player1X i player2Tank a player2X.

## Prompt 6 — 2026-04-08
**Prompt:** Implementar VsAIManager amb ML-Agent per al mode VS IA.
**Resultat:** Mode VS IA funcionant amb TankAgent en mode InferenceOnly.
**Problema detectat:** OnMoveLeft/OnMoveRight utilitzaven Time.deltaTime dins d'un callback de botó.
**Correcció:** Canviat a valor fix de 0.2f en lloc de Time.deltaTime.

## Prompt 7 — 2026-04-09
**Prompt:** Crear TankAgent compatible amb el model TankBehavior.onnx entrenat.
**Resultat:** TankAgent amb 5 observacions i 2 accions contínues.
**Problema detectat:** BrainParameters.VectorObservationSize configurat a Initialize() era ignorat.
**Correcció:** Configurat directament al component BehaviorParameters a l'Inspector (SpaceSize=5).

## Prompt 8 — 2026-04-10
**Prompt:** Implementar ProjectSetupTool per construir totes les escenes automàticament.
**Resultat:** Eina d'editor funcional al menú Tools/Setup Project.
**Problema detectat:** No configurava LoginScene, MenuScene ni WaitingScene. Shader incorrecte.
**Correcció:** Afegit setup per totes les escenes. Canviat shader a URP/Unlit. Afegit fallback per sprites.

## Prompt 9 — 2026-04-12
**Prompt:** Crear els fitxers UXML i USS per a totes les pantalles.
**Resultat:** UI completa amb LoginScreen, MenuScreen, WaitingScreen i CombatScreen.
**Problema detectat:** Textos en anglès, haurien d'estar en català segons les especificacions.
**Correcció:** Traduïts tots els textos: "FIRE" → "FOCA", "Your turn" → "El teu torn", etc.

## Prompt 10 — 2026-04-14
**Prompt:** Revisió completa del projecte contra la guia per assegurar compatibilitat.
**Resultat:** Tots els fitxers revisats i actualitzats.
**Problema detectat:** Múltiples problemes: textos en anglès, bug a PlaceTanksFromPercent, Time.deltaTime en callbacks de botons, shader incorrecte al terreny, escenes no configurades al ProjectSetupTool.
**Correcció:** Corregits tots els problemes identificats. Documentació SDD completada en català.
