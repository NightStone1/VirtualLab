using System;
using UnityEngine;

public class Lab2WireView : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float wireWidth = 0.003f;
    [SerializeField] private float arcHeight = 0.025f;
    [SerializeField] private float outwardOffset = 0.16f;
    [SerializeField] private int pointsCount = 16;
    [SerializeField] private bool useCameraFacingOutward = true;
    [SerializeField] private Vector3 outwardDirection = Vector3.back;
    [SerializeField] private float roleOffset;
    [SerializeField] private Vector3 roleOffsetDirection = Vector3.right;
    [SerializeField] private float middleBendOffset;
    [SerializeField] private float maxOutwardDistanceFactor = 0.35f;
    [SerializeField] private float maxRoleOffsetDistanceFactor = 0.08f;

    private Transform startAnchor;
    private Transform endAnchor;
    private Func<Vector3> getStartPosition;
    private Func<Vector3> getEndPosition;

    public void Initialize(Transform start, Transform end, Color color, float visualRoleOffset = 0f)
    {
        startAnchor = start;
        endAnchor = end;
        getStartPosition = start != null ? () => start.position : null;
        getEndPosition = end != null ? () => end.position : null;
        roleOffset = visualRoleOffset;

        InitializeRenderer(color);
    }

    public void Initialize(Func<Vector3> startPositionProvider, Func<Vector3> endPositionProvider, Color color, float visualRoleOffset = 0f)
    {
        getStartPosition = startPositionProvider;
        getEndPosition = endPositionProvider;
        roleOffset = visualRoleOffset;

        InitializeRenderer(color);
    }

    public void SetVisualProfile(float newOutwardOffset, float newArcHeight, bool newUseCameraFacingOutward, float newMaxOutwardDistanceFactor, float newMiddleBendOffset = 0f)
    {
        outwardOffset = newOutwardOffset;
        arcHeight = newArcHeight;
        useCameraFacingOutward = newUseCameraFacingOutward;
        maxOutwardDistanceFactor = newMaxOutwardDistanceFactor;
        middleBendOffset = newMiddleBendOffset;
        UpdateWire();
    }

    private void InitializeRenderer(Color color)
    {
        EnsureLineRenderer();
        lineRenderer.widthMultiplier = wireWidth;
        lineRenderer.startWidth = wireWidth;
        lineRenderer.endWidth = wireWidth;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material material = new(shader);
        material.color = color;
        lineRenderer.material = material;

        UpdateWire();
    }

    private void LateUpdate()
    {
        UpdateWire();
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 6;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    private void UpdateWire()
    {
        if (lineRenderer == null || getStartPosition == null || getEndPosition == null)
            return;

        Vector3 start = getStartPosition();
        Vector3 end = getEndPosition();
        float distance = Vector3.Distance(start, end);
        Vector3 outward = GetOutwardDirection(start, end);
        Vector3 roleDirection = roleOffsetDirection.sqrMagnitude > 0.0001f
            ? roleOffsetDirection.normalized
            : Vector3.right;
        float actualOutwardOffset = Mathf.Min(outwardOffset, Mathf.Max(0.015f, distance * maxOutwardDistanceFactor));
        float actualRoleOffset = Mathf.Clamp(roleOffset, -distance * maxRoleOffsetDistanceFactor, distance * maxRoleOffsetDistanceFactor);
        Vector3 outwardVisualOffset = outward * actualOutwardOffset;
        Vector3 firstControl = start + outwardVisualOffset;
        Vector3 secondControl = end + outwardVisualOffset;
        Vector3 middleBendDirection = GetMiddleBendDirection(start, end, outward);
        int actualPointsCount = Mathf.Max(2, pointsCount);

        if (lineRenderer.positionCount != actualPointsCount)
            lineRenderer.positionCount = actualPointsCount;

        for (int i = 0; i < actualPointsCount; i++)
        {
            float t = actualPointsCount == 1 ? 0f : i / (float)(actualPointsCount - 1);
            float middleInfluence = Mathf.Sin(t * Mathf.PI);
            Vector3 point = CalculateBezierPoint(start, firstControl, secondControl, end, t);
            point += Vector3.up * middleInfluence * arcHeight;
            point += roleDirection * middleInfluence * actualRoleOffset;
            point += middleBendDirection * middleInfluence * middleBendOffset;
            lineRenderer.SetPosition(i, point);
        }
    }

    private Vector3 GetMiddleBendDirection(Vector3 start, Vector3 end, Vector3 outward)
    {
        Vector3 connectionDirection = end - start;

        if (connectionDirection.sqrMagnitude <= 0.0001f)
            return Vector3.right;

        Vector3 bendDirection = Vector3.Cross(outward, connectionDirection.normalized);

        return bendDirection.sqrMagnitude > 0.0001f
            ? bendDirection.normalized
            : Vector3.right;
    }

    private Vector3 GetOutwardDirection(Vector3 start, Vector3 end)
    {
        if (useCameraFacingOutward && Camera.main != null)
        {
            Vector3 middle = (start + end) * 0.5f;
            Vector3 cameraDirection = Camera.main.transform.position - middle;

            if (cameraDirection.sqrMagnitude > 0.0001f)
                return cameraDirection.normalized;
        }

        return outwardDirection.sqrMagnitude > 0.0001f
            ? outwardDirection.normalized
            : Vector3.back;
    }

    private static Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * oneMinusT * p0
            + 3f * oneMinusT * oneMinusT * t * p1
            + 3f * oneMinusT * t * t * p2
            + t * t * t * p3;
    }
}
