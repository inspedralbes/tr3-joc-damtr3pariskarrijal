// HumanTankInput — Entrada del jugador humà en mode VS IA.
// Llegeix angle/potència dels mateixos sliders UXML que CombatManager.
// Els botons de moure esquerra/dreta els gestiona VsAIManager directament;
// aquest component manté l'angle del canó sincronitzat amb el slider cada frame
// i permet disparar amb la barra espaiadora.
using UnityEngine;
using UnityEngine.UIElements;

public class HumanTankInput : MonoBehaviour
{
    [Header("References")]
    public TankController tank;
    public VsAIManager    manager;

    // Cache sliders once
    private Slider angleSlider;
    private Slider powerSlider;
    private bool   slidersFound = false;

    void Start()
    {
        TryFindSliders();
    }

    private void TryFindSliders()
    {
        if (slidersFound || manager == null) return;
        var doc = manager.GetComponent<UIDocument>();
        if (doc == null) return;
        var root   = doc.rootVisualElement;
        angleSlider = root.Q<Slider>("angle-slider");
        powerSlider = root.Q<Slider>("power-slider");
        slidersFound = (angleSlider != null && powerSlider != null);
    }

    void Update()
    {
        if (tank == null || manager == null) return;
        if (!manager.IsPlayerTurn()) return;

        if (!slidersFound) TryFindSliders();

        // ── Llegir angle/potència actual dels sliders UI ──────────────────
        float currentAngle = angleSlider != null ? angleSlider.value : 45f;
        float currentPower = powerSlider != null ? powerSlider.value : 75f;

        // ── Mantenir el canó sincronitzat amb el slider ────────────────────
        if (tank != null && manager.aiTank != null)
        {
            bool facingRight = tank.transform.position.x < manager.aiTank.transform.position.x;
            tank.SetBarrelAngle(currentAngle, facingRight);
        }

        // ── Dreceres de teclat (opcional, mateix resultat que els controls UI) ──

        // A/D o Esquerra/Dreta → moure el tanc
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            if (tank.CanStillMove())
            {
                tank.Move(-1f, Time.deltaTime);
                tank.PlaceOnTerrain();
            }
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            if (tank.CanStillMove())
            {
                tank.Move(1f, Time.deltaTime);
                tank.PlaceOnTerrain();
            }
        }

        // W/S o Amunt/Avall → ajustar slider d'angle
        float angleDir = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))   angleDir =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) angleDir = -1f;
        if (Mathf.Abs(angleDir) > 0.01f && angleSlider != null)
            angleSlider.value = Mathf.Clamp(angleSlider.value + angleDir * 45f * Time.deltaTime, 0f, 90f);

        // Q/E → ajustar slider de potència
        float powerDir = 0f;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.Equals)) powerDir =  1f;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Minus)) powerDir = -1f;
        if (Mathf.Abs(powerDir) > 0.01f && powerSlider != null)
            powerSlider.value = Mathf.Clamp(powerSlider.value + powerDir * 50f * Time.deltaTime, 0f, 100f);

        // Espai → disparar (igual que prémer el botó de foc)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            manager.PlayerFires(
                angleSlider != null ? angleSlider.value : 45f,
                powerSlider != null ? powerSlider.value : 75f);
        }
    }
}