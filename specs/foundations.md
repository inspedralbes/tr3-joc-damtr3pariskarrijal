# SDD Foundations — Terreny Destructible Procedural

## 1. Context

Tank Stars és un joc multijugador de tancs desenvolupat amb Unity 6 (6000.3.6f1) + Universal Render Pipeline (URP). El sistema de terreny és responsable de generar terreny procedural amb turons i destruir-lo quan els projectils impacten. Aquesta és la funcionalitat SDD (Spec-Driven Development).

El joc utilitza GameObjects per als tancs, terreny i projectils, amb UI Toolkit (UIDocument + UXML + USS) per al HUD overlay. El terreny es genera amb un mesh personalitzat (MeshFilter + MeshRenderer + PolygonCollider2D).

## 2. Objectius

- **Generació procedural**: Generar terreny únic per partida utilitzant FBM (Fractional Brownian Motion) amb soroll Perlin multi-octava.
- **Reproducibilitat**: El mateix seed + mapType sempre produeix el mateix terreny, permetent sincronització entre clients multijugador.
- **Destrucció en temps real**: Destruir el terreny en forma de cràter circular quan un projectil impacta. Reconstruir el mesh i el collider després de cada destrucció.
- **Biomes**: Suportar 5 tipus de mapa (desert, snow, grassland, canyon, volcanic), cadascun amb colors, alçada màxima i escala de soroll diferents.
- **Interacció amb tancs**: Els tancs cauen a la nova superfície després que el terreny sota ells sigui destruït (PlaceOnTerrain).

## 3. Restriccions

- Ha d'utilitzar Unity 6 amb URP.
- Ha d'utilitzar MeshFilter + MeshRenderer + PolygonCollider2D (no Terrain component).
- La destrucció ha de ser circular amb radi configurable.
- La reconstrucció del mesh ha de completar-se en menys de 16ms (un frame a 60fps).
- Ha de funcionar amb material URP Unlit amb vertex colors.
- El terreny ha d'abastar l'amplada completa de la càmera (de -11 a +11 unitats mundials).
- L'alçada del tanc no pot baixar per sota de baseHeight.
- El collider ha d'actualitzar-se immediatament després de cada destrucció per garantir col·lisions precises.

## 4. Enfocament tècnic

- **Generació del mesh**: Arrays personalitzats de vèrtexs i índexs per construir un strip mesh 2D amb dues files de vèrtexs (superfície superior i base inferior).
- **Col·lisió**: PolygonCollider2D que actualitza el seu path per coincidir exactament amb la superfície visual del mesh.
- **Interacció física**: Callbacks estàndard OnCollisionEnter2D per activar la destrucció al punt d'impacte.
- **Vertex Colors**: Colors assignats directament als vèrtexs del mesh per visualitzar el bioma sense textures addicionals.
- **FBM**: Funció de soroll Fractal Brownian Motion amb 5 octaves, persistència 0.55 i lacunaritat 2.1.

## 5. Temes de mapa

| Mapa       | Color                        | Alçada màx. | Escala soroll |
|------------|------------------------------|-------------|---------------|
| Desert     | rgb(0.9, 0.7, 0.3)          | 5.5         | 0.35          |
| Snow       | rgb(0.9, 0.95, 1.0)         | 6.5         | 0.45          |
| Grassland  | rgb(0.2, 0.8, 0.3)          | 4.0         | 0.25          |
| Canyon     | rgb(0.8, 0.4, 0.2)          | 7.0         | 0.55          |
| Volcanic   | rgb(0.3, 0.1, 0.1)          | 5.0         | 0.4           |
