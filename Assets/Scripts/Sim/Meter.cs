using UnityEngine;

public class Meter : MonoBehaviour, IInfoProvider
{
    public float current;
    public string GetInfoText()
    {
        return $"{current:F2}";
    }
}
