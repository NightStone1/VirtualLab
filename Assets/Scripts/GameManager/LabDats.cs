using UnityEngine;

[CreateAssetMenu(fileName = "NewLabData", menuName = "Labs/Lab Data")]
public class LabData : ScriptableObject
{
    public string sceneName;
    public string title;

    [TextArea(2, 5)]
    public string theme;

    public int order;
}