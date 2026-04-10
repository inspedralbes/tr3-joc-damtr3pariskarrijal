// Controla un tanc: HP, posició sobre el terreny i visualització
using UnityEngine;
using UnityEngine.UIElements;

public class TankController : MonoBehaviour
{
    [Header("Identificació")]
    public int  playerId;
    public bool isLocalPlayer;

    [Header("Components")]
    public Transform       barrel;
    public TerrainGenerator terrain;

    [Header("Stats")]
    public int currentHp = 100;
    public int maxHp     = 100;

    // Col·loca el tanc sobre el terreny fent un raig cap avall
    public void PlaceOnTerrain()
    {
        if (terrain == null) return;
        float h = terrain.GetHeightAtX(transform.position.x);
        transform.position = new Vector3(transform.position.x, h + 0.5f, 0f);
    }

    // Aplica dany al tanc
    public void TakeDamage(int amount)
    {
        currentHp = Mathf.Max(0, currentHp - amount);
    }

    // Rota el canó segons l'angle donat (en graus)
    public void SetBarrelAngle(float angleDegrees, bool facingRight)
    {
        if (barrel == null) return;
        float sign = facingRight ? 1f : -1f;
        barrel.localRotation = Quaternion.Euler(0f, 0f, angleDegrees * sign);
    }
}
