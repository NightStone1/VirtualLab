using UnityEngine;

public static class GeneratorEfficiencyHelper
{
    public static float GetEfficiency(float generatorArmatureCurrent)
    {
        float i = Mathf.Max(0f, generatorArmatureCurrent);

        if (i <= 0.0f) return 0.20f;
        if (i <= 0.25f) return Mathf.Lerp(0.20f, 0.45f, i / 0.25f);
        if (i <= 0.50f) return Mathf.Lerp(0.45f, 0.58f, (i - 0.25f) / 0.25f);
        if (i <= 1.00f) return Mathf.Lerp(0.58f, 0.67f, (i - 0.50f) / 0.50f);
        if (i <= 1.50f) return Mathf.Lerp(0.67f, 0.68f, (i - 1.00f) / 0.50f);
        if (i <= 2.00f) return Mathf.Lerp(0.68f, 0.66f, (i - 1.50f) / 0.50f);
        if (i <= 2.50f) return Mathf.Lerp(0.66f, 0.58f, (i - 2.00f) / 0.50f);

        return 0.58f;
    }
}