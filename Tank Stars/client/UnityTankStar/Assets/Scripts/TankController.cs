// TankController — Controla un tanc: HP, moviment sobre el terreny i rotació del canó
using UnityEngine;

public class TankController : MonoBehaviour
{
    [Header("Identificació")]
    public int  playerId;
    public bool isLocalPlayer;

    [Header("Components")]
    public Transform        barrel;
    public TerrainGenerator terrain;

    [Header("Estadístiques")]
    public int   currentHp    = 100;
    public int   maxHp        = 100;

    [Header("Moviment")]
    public float moveSpeed        = 4f;   // unitats mundials per segon
    public float maxMovePerTurn   = 2.5f; // distància total permesa per torn
    public float worldBoundsX     = 9.5f; // no pot anar més enllà de ±aquest valor

    // Seguiment de distància moguda
    private float _distanceMovedThisTurn = 0f;
    public  float DistanceMovedThisTurn => _distanceMovedThisTurn;

    // ─── Col·locació al terreny ───────────────────────────────────────────

    /// <summary>Col·loca el tanc exactament sobre la superfície del terreny.</summary>
    public void PlaceOnTerrain()
    {
        if (terrain == null) return;
        float worldY = terrain.GetHeightAtX(transform.position.x);
        transform.position = new Vector3(transform.position.x, worldY + 0.35f, 0f);
    }

    // ─── Gestió de torns ─────────────────────────────────────────────────

    /// <summary>Crida a l'inici del torn per reiniciar el pressupost de moviment.</summary>
    public void StartTurn() => _distanceMovedThisTurn = 0f;

    // ─── Moviment ─────────────────────────────────────────────────────────

    /// <summary>
    /// Mou el tanc horitzontalment seguint la superfície del terreny.
    /// Retorna la distància real moguda.
    /// direction: -1 (esquerra) a +1 (dreta).
    /// </summary>
    public float Move(float direction, float deltaTime)
    {
        float remaining = maxMovePerTurn - _distanceMovedThisTurn;
        if (remaining <= 0f) return 0f;

        float step   = direction * moveSpeed * deltaTime;
        float capped = Mathf.Clamp(step, -remaining, remaining);

        float newX = Mathf.Clamp(transform.position.x + capped, -worldBoundsX, worldBoundsX);
        float actualStep = newX - transform.position.x;

        if (Mathf.Abs(actualStep) < 0.0001f) return 0f;

        // Moure i enganxar al terreny
        transform.position = new Vector3(newX, transform.position.y, 0f);
        PlaceOnTerrain();

        _distanceMovedThisTurn += Mathf.Abs(actualStep);
        return actualStep;
    }

    /// <summary>Comprova si el tanc encara pot moure's aquest torn.</summary>
    public bool CanStillMove() => _distanceMovedThisTurn < maxMovePerTurn;

    // ─── Combat ──────────────────────────────────────────────────────────

    /// <summary>Aplica dany al tanc (no pot baixar de 0).</summary>
    public void TakeDamage(int amount)
    {
        currentHp = Mathf.Max(0, currentHp - amount);
        if (currentHp <= 0)
        {
            PlaceOnTerrain();
        }
    }

    public bool IsAlive => currentHp > 0;

    // ─── Canó ────────────────────────────────────────────────────────────

    /// <summary>Rota el canó a l'angle donat en graus.</summary>
    public void SetBarrelAngle(float angleDegrees, bool facingRight)
    {
        if (barrel == null) return;
        float sign = facingRight ? 1f : -1f;
        barrel.localRotation = Quaternion.Euler(0f, 0f, angleDegrees * sign);
    }
}
