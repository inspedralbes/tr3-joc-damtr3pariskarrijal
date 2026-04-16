// TankAgent — Agent de tanc ML-Agents per a Tank Stars
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class TankAgent : Agent
{
    [Header("Referències")]
    public TerrainGenerator terrain;
    public TankController   localTank;
    public TankController   enemyTank;
    public GameObject       projectilePrefab;
    public GameObject       explosionPrefab;

    [Header("Control d'estat")]
    public bool isVsAIMode        = false;
    public bool isWaitingForShot  = false;
    public bool canCaptureActions = false;

    // NOTA: NO sobreescriure Initialize() per canviar BrainParameters en temps d'execució.
    // ML-Agents llegeix BrainParameters del component BehaviorParameters serialitzat
    // ABANS que Initialize() s'executi. Configurar els valors correctes directament
    // al component BehaviorParameters a l'Inspector:
    //   Vector Observation > Space Size = 14
    //   Actions > Continuous Actions    = 2
    //   Actions > Discrete Branches     = 0

    public override void OnEpisodeBegin()
    {
        isWaitingForShot = false;

        // Always generate terrain in training mode
        if (terrain != null && !isVsAIMode)
        {
            terrain.GenerateTerrain(Random.Range(1, 99999), "desert");
        }

        if (!isVsAIMode)
        {
            if (localTank != null)
            {
                localTank.transform.position = new Vector3(Random.Range(-8f, -2f), 0, 0);
                localTank.PlaceOnTerrain();
                localTank.currentHp = localTank.maxHp;
            }
            if (enemyTank != null)
            {
                enemyTank.transform.position = new Vector3(Random.Range(2f, 8f), 0, 0);
                enemyTank.PlaceOnTerrain();
                enemyTank.currentHp = enemyTank.maxHp;
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (localTank == null || enemyTank == null)
        {
            for (int i = 0; i < 14; i++) sensor.AddObservation(0f);
            return;
        }

        float dx = enemyTank.transform.position.x - localTank.transform.position.x;
        float dy = enemyTank.transform.position.y - localTank.transform.position.y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        float angleToEnemy = Mathf.Atan2(dy, Mathf.Abs(dx)) * Mathf.Rad2Deg;

        sensor.AddObservation(localTank.transform.position.x / 10f);
        sensor.AddObservation(localTank.transform.position.y / 5f);
        sensor.AddObservation(enemyTank.transform.position.x / 10f);
        sensor.AddObservation(enemyTank.transform.position.y / 5f);
        sensor.AddObservation(dx / 20f);
        sensor.AddObservation(dy / 10f);
        sensor.AddObservation(dist / 20f);
        sensor.AddObservation(angleToEnemy / 90f);
        sensor.AddObservation(localTank.currentHp / localTank.maxHp);
        sensor.AddObservation(enemyTank.currentHp / enemyTank.maxHp);
        sensor.AddObservation((localTank.currentHp - enemyTank.currentHp) / 100f);
        sensor.AddObservation(localTank.barrel != null ? localTank.barrel.localEulerAngles.z / 90f : 0f);
        sensor.AddObservation(terrain != null ? terrain.GetHeightAtX(localTank.transform.position.x) / 5f : 0f);
        sensor.AddObservation(terrain != null ? terrain.GetHeightAtX(enemyTank.transform.position.x) / 5f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Speed up training 10x
        if (!isVsAIMode) Time.timeScale = 10;
        
        // In training mode, always allow actions. In VsAI mode, check turn.
        if (isVsAIMode)
        {
            if (!canCaptureActions) return;
            if (isWaitingForShot) return;
        }
        else
        {
            // Training mode - block if projectile in air
            if (isWaitingForShot) return;
        }

        float angle = 45f;
        float power = 75f;

        // If no trained model loaded, use heuristic
        if (actions.ContinuousActions.Length == 0 && actions.DiscreteActions.Length == 0)
        {
            UseHeuristicFallback();
            return;
        }

        // Support BOTH Continuous and Discrete models
        if (actions.ContinuousActions.Length >= 2)
        {
            // ML-Agents outputs [-1, 1], map to game values
            angle = (actions.ContinuousActions[0] + 1f) / 2f * 90f;
            power = (actions.ContinuousActions[1] + 1f) / 2f * 100f;
            
            angle = Mathf.Clamp(angle, 0f, 90f);
            power = Mathf.Clamp(power, 10f, 100f);
        }
        else if (actions.DiscreteActions.Length >= 2)
        {
            angle = actions.DiscreteActions[0];
            power = actions.DiscreteActions[1];
        }
        else
        {
            UseHeuristicFallback();
            return;
        }

        FireActualShot(angle, power);

        if (!isVsAIMode) AddReward(-0.001f);
    }

    private void UseHeuristicFallback()
    {
        if (localTank == null || enemyTank == null) return;
        
        float dx = enemyTank.transform.position.x - localTank.transform.position.x;
        float dy = enemyTank.transform.position.y - localTank.transform.position.y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        float angleToEnemy = Mathf.Atan2(dy, Mathf.Abs(dx)) * Mathf.Rad2Deg;

        float angle = Mathf.Clamp(angleToEnemy * 0.7f + 15f, 10f, 85f);
        float power = Mathf.Clamp(dist * 5f + 40f, 30f, 95f);

        FireActualShot(angle, power);
        
        if (!isVsAIMode) AddReward(-0.001f);
    }

    public void FireActualShot(float angle, float power)
    {
        if (projectilePrefab == null || localTank == null) return;

        isWaitingForShot = true;

        Vector3 spawnPos = localTank.barrel != null
            ? localTank.barrel.position
            : localTank.transform.position + Vector3.up * 0.5f;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        Collider2D pCol = proj.GetComponent<Collider2D>();
        Collider2D tCol = localTank.GetComponent<Collider2D>();
        if (pCol != null && tCol != null) Physics2D.IgnoreCollision(pCol, tCol);

        if (enemyTank != null)
            localTank.SetBarrelAngle(angle, localTank.transform.position.x < enemyTank.transform.position.x);

        var pc = proj.GetComponent<ProjectileController>();
        if (pc == null)
        {
            Destroy(proj);
            isWaitingForShot = false;
            return;
        }

        pc.SetImpactCallback(OnProjectileImpact);
        pc.Launch(angle, power, enemyTank != null && localTank.transform.position.x < enemyTank.transform.position.x);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        if (actions.Length < 2 || localTank == null || enemyTank == null)
        {
            if (actions.Length >= 2) { actions[0] = 0f; actions[1] = 0f; }
            return;
        }

        float dx = enemyTank.transform.position.x - localTank.transform.position.x;
        float dy = enemyTank.transform.position.y - localTank.transform.position.y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        float angleToEnemy = Mathf.Atan2(dy, Mathf.Abs(dx)) * Mathf.Rad2Deg;

        float optimalAngle = Mathf.Clamp(angleToEnemy * 0.7f + 15f, 10f, 85f);
        float optimalPower = Mathf.Clamp(dist * 5f + 40f, 30f, 95f);

        // Convert [0,90] and [0,100] to [-1,1] for ML-Agents continuous action space
        actions[0] = (optimalAngle / 90f) * 2f - 1f;
        actions[1] = (optimalPower / 100f) * 2f - 1f;
    }

    private void OnProjectileImpact(Vector2 impactWorld, bool hitTank)
    {
        if (isVsAIMode)
        {
            if (hitTank && enemyTank != null)
            {
                enemyTank.TakeDamage(35);
            }
            else if (enemyTank != null)
            {
                float d = Vector2.Distance(impactWorld, enemyTank.transform.position);
                if (d < 1.5f) enemyTank.TakeDamage(15);
            }

            if (terrain != null)
            {
                terrain.DestroyTerrain(impactWorld, 0.5f);
                localTank?.PlaceOnTerrain();
                enemyTank?.PlaceOnTerrain();
            }

            if (explosionPrefab != null)
            {
                GameObject exp = Instantiate(explosionPrefab,
                    new Vector3(impactWorld.x, impactWorld.y, 0f), Quaternion.identity);
                Destroy(exp, 2f);
            }

            isWaitingForShot = false;
            VsAIManager.Instance?.OnProjectileResolved();
        }
        else
        {
            // Recompenses d'entrenament
            if (hitTank)
            {
                AddReward(1.0f);
                Debug.Log("[TankAgent] HIT! Ending episode");
            }
            else
            {
                float d = enemyTank != null
                    ? Vector2.Distance(impactWorld, enemyTank.transform.position)
                    : 10f;
                AddReward(-0.01f * d);
                Debug.Log($"[TankAgent] Miss - dist: {d:F1}, ending episode");
            }
            isWaitingForShot = false;
            
            // Force end episode immediately
            EndEpisode();
            
            // Request immediate decision for next episode
            RequestDecision();
        }
    }
}