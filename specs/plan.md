# SDD Pla d'implementació — Sistema de terreny

## Fase 1: Generació de l'array d'alçades

1. Inicialitzar el generador aleatori amb el seed proporcionat.
2. Generar un offset aleatori per al soroll Perlin.
3. Configurar els paràmetres del bioma (color, maxHeight, noiseScale) segons el mapType.
4. Per a cada columna (0 a 119):
   - Calcular la coordenada X normalitzada.
   - Aplicar la funció FBM (soroll Perlin multi-octava amb 5 octaves).
   - Limitar l'alçada resultant al rang [baseHeight + 0.3, baseHeight + maxHeight].
5. Emmagatzemar les alçades a l'array `heights[]`.

## Fase 2: Construcció del mesh

1. Crear arrays de vèrtexs (2 per columna: superfície i base), triangles, UVs i colors.
2. Per a cada columna:
   - Vèrtex superior: (x, heights[i], 0).
   - Vèrtex inferior: (x, bottom, 0) on bottom = baseHeight - 3.
   - Color superior: color del bioma.
   - Color inferior: color del bioma * 0.4 (més fosc).
3. Construir els triangles connectant columnes adjacents (2 triangles per parell).
4. Assignar el mesh al MeshFilter.

## Fase 3: Construcció del collider

1. Crear un array de Vector2 amb les posicions de superfície de cada columna.
2. Afegir 2 punts extra per tancar la forma per la base (cantonada inferior dreta i esquerra).
3. Assignar el path al PolygonCollider2D.

## Fase 4: Destrucció del terreny

1. Rebre la posició d'impacte mundial i el radi.
2. Convertir la posició d'impacte a coordenades locals del terreny.
3. Per a cada columna dins del radi:
   - Calcular la distància horitzontal (dx) des de l'impacte.
   - Calcular el factor de profunditat: `depth = 1 - (dx / radius)`.
   - Calcular l'alçada esculpida: `carved = localY - radius * 1.4 * depth`.
   - Aplicar: `heights[i] = Min(heights[i], Max(baseHeight + 0.2, carved))`.
4. Reconstruir el mesh amb els nous valors d'alçada.
5. Reconstruir el collider.

## Fase 5: Integració amb el joc

1. Implementar GetHeightAtX per permetre als tancs consultar l'alçada del terreny.
2. Implementar PlaceOnTerrain als TankController per enganxar-se a la superfície.
3. Connectar el ProjectileController per cridar DestroyTerrain a l'impacte.
4. Cridar PlaceOnTerrain en ambdós tancs després de cada destrucció.

## Fase 6: Verificació

1. Provar amb crides manuals des d'un botó a l'Inspector abans de connectar al bucle de joc.
2. Verificar que el mateix seed produeix el mateix terreny en dos clients separats.
3. Verificar que la destrucció no crea buits ni forats al collider.
4. Verificar que els tancs cauen correctament després de la destrucció.
5. Mesurar el temps de reconstrucció del mesh per confirmar que és inferior a 16ms.
