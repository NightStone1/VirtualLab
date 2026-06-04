using UnityEngine;

public class LampSynchronoscopeView : MonoBehaviour
{
    [SerializeField] private Renderer[] lamps;
    [SerializeField] private Color darkColor = Color.black;
    [SerializeField] private Color brightColor = Color.yellow;

    public void SetPhaseDifference(float phaseDeg)
    {
        if (lamps == null)
        {
            return;
        }

        float phaseError = Mathf.Abs(Mathf.DeltaAngle(0f, phaseDeg));
        float brightness = 1f - Mathf.Clamp01(phaseError / 180f);
        Color color = Color.Lerp(darkColor, brightColor, brightness);

        for (int i = 0; i < lamps.Length; i++)
        {
            Renderer lamp = lamps[i];
            if (lamp == null)
            {
                continue;
            }

            Material material = lamp.material;
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", brightColor * brightness);
        }
    }
}
