// Genera el terreny procedural i gestiona la destrucció per impacte
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Configuració del terreny")]
    public int columns      = 100;
    public float width      = 20f;
    public float baseHeight = -4f;
    public float maxHeight  = 3f;
    public float noiseScale = 0.15f;

    private float[]          heights;
    private MeshFilter       meshFilter;
    private PolygonCollider2D polyCollider;

    void Awake()
    {
        meshFilter   = GetComponent<MeshFilter>();
        polyCollider = GetComponent<PolygonCollider2D>();
    }

    // Genera el terreny amb una llavor donada
    public void GenerateTerrain(int seed, string mapType)
    {
        Random.InitState(seed);
        float offset = Random.Range(0f, 1000f);

        heights = new float[columns];
        for (int i = 0; i < columns; i++)
        {
            float xNorm  = i / (float)(columns - 1);
            heights[i]   = baseHeight +
                           Mathf.PerlinNoise(xNorm * noiseScale * 10f + offset, 0f) * maxHeight;
        }

        BuildMesh();
        BuildCollider();
    }

    // Construeix el mesh del terreny a partir dels heights
    void BuildMesh()
    {
        var mesh     = new Mesh();
        int vCount   = columns * 2;
        var verts    = new Vector3[vCount];
        var tris     = new int[(columns - 1) * 6];
        var uvs      = new Vector2[vCount];

        float stepX = width / (columns - 1);
        float startX = -width / 2f;

        for (int i = 0; i < columns; i++)
        {
            float x        = startX + i * stepX;
            verts[i * 2]   = new Vector3(x, heights[i], 0f);      // punt superior
            verts[i * 2 + 1] = new Vector3(x, baseHeight - 2f, 0f); // punt inferior

            uvs[i * 2]     = new Vector2(i / (float)(columns - 1), 1f);
            uvs[i * 2 + 1] = new Vector2(i / (float)(columns - 1), 0f);
        }

        int t = 0;
        for (int i = 0; i < columns - 1; i++)
        {
            int bl = i * 2 + 1;
            int br = (i + 1) * 2 + 1;
            int tl = i * 2;
            int tr = (i + 1) * 2;

            tris[t++] = bl; tris[t++] = tl; tris[t++] = tr;
            tris[t++] = bl; tris[t++] = tr; tris[t++] = br;
        }

        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.uv        = uvs;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    // Construeix el col·lisionador poligonal
    void BuildCollider()
    {
        float stepX  = width / (columns - 1);
        float startX = -width / 2f;

        var points = new Vector2[columns + 2];
        for (int i = 0; i < columns; i++)
        {
            points[i] = new Vector2(startX + i * stepX, heights[i]);
        }
        // Tanca el polígon per sota
        points[columns]     = new Vector2(startX + width, baseHeight - 2f);
        points[columns + 1] = new Vector2(startX,         baseHeight - 2f);

        polyCollider.SetPath(0, points);
    }

    // Destrueix el terreny en un radi circular al punt d'impacte
    public void DestroyTerrain(Vector2 impactWorld, float radius)
    {
        if (heights == null) return;

        float stepX  = width / (columns - 1);
        float startX = -width / 2f;

        for (int i = 0; i < columns; i++)
        {
            float colX  = startX + i * stepX;
            float dx    = Mathf.Abs(colX - impactWorld.x);
            if (dx > radius) continue;

            float depth  = 1f - (dx / radius);
            float carved = impactWorld.y - radius * 1.2f * depth;
            heights[i]   = Mathf.Min(heights[i], Mathf.Max(baseHeight + 0.2f, carved));
        }

        BuildMesh();
        BuildCollider();
    }

    // Retorna l'alçada del terreny en una posició X del món
    public float GetHeightAtX(float worldX)
    {
        if (heights == null) return baseHeight;

        float stepX  = width / (columns - 1);
        float startX = -width / 2f;
        float relX   = worldX - startX;
        int   idx    = Mathf.RoundToInt(relX / stepX);
        idx = Mathf.Clamp(idx, 0, columns - 1);
        return heights[idx];
    }
}
