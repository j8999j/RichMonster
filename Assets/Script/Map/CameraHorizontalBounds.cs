using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraHorizontalBounds : CinemachineExtension
{
    [SerializeField]
    private bool useBounds;

    [SerializeField]
    private float leftBoundary;

    [SerializeField]
    private float rightBoundary;

    [SerializeField]
    private bool keepViewInsideBounds = true;

    public void SetBounds(float left, float right, bool keepViewInside)
    {
        leftBoundary = Mathf.Min(left, right);
        rightBoundary = Mathf.Max(left, right);
        keepViewInsideBounds = keepViewInside;
        useBounds = true;
    }

    public void ClearBounds()
    {
        useBounds = false;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (!useBounds || stage != CinemachineCore.Stage.Finalize)
            return;

        Vector3 position = state.RawPosition;
        float minX = leftBoundary;
        float maxX = rightBoundary;

        if (keepViewInsideBounds && state.Lens.Orthographic)
        {
            float halfWidth = state.Lens.OrthographicSize * state.Lens.Aspect;
            minX += halfWidth;
            maxX -= halfWidth;
        }

        position.x = minX > maxX
            ? (leftBoundary + rightBoundary) * 0.5f
            : Mathf.Clamp(position.x, minX, maxX);

        state.RawPosition = position;
    }
}
