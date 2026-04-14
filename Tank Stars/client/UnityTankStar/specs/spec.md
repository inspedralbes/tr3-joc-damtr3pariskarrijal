# SDD Especificació — Terreny de Tank Stars

## 1. GenerateTerrain(int seed, string mapType)

### Entrada
- `seed`: enter per inicialitzar el generador de nombres aleatoris (reproducibilitat determinista).
- `mapType`: cadena de text, un de: "desert", "snow", "grassland", "canyon", "volcanic".

### Sortida
- Mesh visible amb turons des de x=-11 fins a x=+11 unitats mundials.
- PolygonCollider2D coincidint exactament amb la superfície del mesh.

### Comportament
- El bioma afecta el color dels vèrtexs, l'alçada màxima (maxHeight) i l'escala de soroll (noiseScale).
- El mateix seed + mapType sempre produeix exactament el mateix terreny.
- L'alçada del terreny mai baixa de `baseHeight + 0.3` ni supera `baseHeight + maxHeight`.
- S'utilitza FBM (Fractional Brownian Motion) amb 5 octaves, persistència 0.55 i lacunaritat 2.1.
- El mesh té 120 columnes amb 2 vèrtexs per columna (superfície i base).
- Els colors dels vèrtexs inferiors són el 40% del color del bioma per donar profunditat visual.

## 2. DestroyTerrain(Vector2 impactWorldPos, float radius)

### Entrada
- `impactWorldPos`: posició mundial de l'impacte (coordenades x, y).
- `radius`: radi del cràter en unitats mundials.

### Sortida
- Cràter circular esculpit al mesh.
- Collider actualitzat sense buits ni forats.

### Comportament
- Les columnes dins del radi tenen la seva alçada reduïda per una funció de profunditat: `depth = 1 - (dx / radius)`.
- L'alçada esculpida es calcula com: `carved = localY - radius * 1.4 * depth`.
- S'aplica `Mathf.Min(heights[i], carved)`: només es pot eliminar terreny, mai afegir-ne.
- L'alçada mínima és sempre `baseHeight + 0.2` (el terreny mai desapareix completament).
- Ambdós tancs es re-col·loquen a la superfície després de la destrucció (PlaceOnTerrain).

## 3. GetHeightAtX(float worldX)

### Entrada
- `worldX`: coordenada X mundial.

### Sortida
- Coordenada Y mundial de la superfície del terreny a aquella X.

### Comportament
- Converteix la coordenada mundial a índex de columna local.
- Retorna l'alçada de la columna més propera més la posició Y del transform del terreny.

## 4. Casos límit

- **Destrucció a la vora del terreny**: les columnes fora del rang es limiten amb Mathf.Clamp.
- **Cràters superposats**: s'aplica el mínim entre l'alçada existent i la nova alçada esculpida.
- **Tanc cau després de destrucció**: PlaceOnTerrain() es crida després de cada DestroyTerrain().
- **Terreny amb seed = 0**: funciona normalment, Random.InitState(0) és un seed vàlid.
- **MapType no reconegut**: utilitza els valors per defecte del desert.
