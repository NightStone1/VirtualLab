using UnityEngine;

public class NeedleSynchronoscopeView : MonoBehaviour
{
    [SerializeField] private Transform needle;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    public void SetPhaseDifference(float phaseDeg, float deltaFrequency)
    {
        if (needle == null)
        {
            return;
        }

        float angle = Mathf.Repeat(phaseDeg, 360f);
        needle.localRotation = Quaternion.AngleAxis(-angle, rotationAxis.normalized);
    }
}
