using UnityEngine;
using Cinemachine;
using DG.Tweening;
using Player;
using UnityEngine.UI;
using GameSystem;
using UnityEngine.Serialization;

public class TelePoint : MonoBehaviour, IInteractable
{
    public Vector3 TelePosition;
    public GameObject interactPrompt;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Image BlackImage;
    private float fadeDuration = 0.3f;
    [Header("Camera Bounds")]
    [FormerlySerializedAs("setCameraHorizontalBounds")]
    [SerializeField] private bool lockCameraHorizontalBounds;
    [SerializeField] private CinemachineVirtualCamera targetCamera;
    [SerializeField] private float cameraLeftBoundary;
    [SerializeField] private float cameraRightBoundary;
    [SerializeField] private bool keepCameraViewInsideBounds = false;

    private Sequence _teleportSequence;
    private bool _isTeleporting;

    protected virtual void OnDisable()
    {
        if (_teleportSequence != null && _teleportSequence.IsActive())
        {
            _teleportSequence.Kill();
        }

        ReleaseTeleportLock();
    }

    public virtual void Interact()
    {
        NextMap();
    }

    public void ShowPrompt()
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    private void NextMap()
    {
        LoadMapPos(fadeDuration);
    }

    public void LoadMapPos(float duration)
    {
        if (_isTeleporting)
        {
            return;
        }

        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            return;
        }

        _isTeleporting = true;
        manager.LockPlayerMove(PlayerLockSources.TelePoint);
        manager.LockPlayerInteract(PlayerLockSources.TelePoint);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
        }

        if (BlackImage == null)
        {
            MovePlayerAndApplyCameraBounds(manager);
            ReleaseTeleportLock();
            return;
        }

        BlackImage.DOKill();
        _teleportSequence = DOTween.Sequence();
        _teleportSequence.Append(BlackImage.DOFade(1f, duration));
        _teleportSequence.AppendCallback(() => MovePlayerAndApplyCameraBounds(manager));
        _teleportSequence.AppendInterval(0.3f);
        _teleportSequence.Append(BlackImage.DOFade(0f, duration));
        _teleportSequence.OnComplete(ReleaseTeleportLock);
    }

    private void MovePlayerAndApplyCameraBounds(GameManager manager)
    {
        manager.SwitchPlayerPos(TelePosition);
        ApplyCameraBounds();
    }

    private void ApplyCameraBounds()
    {
        CinemachineVirtualCamera virtualCamera = targetCamera != null
            ? targetCamera
            : FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera == null)
        {
            Debug.LogWarning($"[{nameof(TelePoint)}] 找不到 CinemachineVirtualCamera，無法設定攝影機邊界。");
            return;
        }

        CameraHorizontalBounds bounds = virtualCamera.GetComponent<CameraHorizontalBounds>();
        if (!lockCameraHorizontalBounds)
        {
            if (bounds != null)
                bounds.ClearBounds();

            virtualCamera.PreviousStateIsValid = false;
            return;
        }

        if (bounds == null)
        {
            bounds = virtualCamera.gameObject.AddComponent<CameraHorizontalBounds>();
        }

        bounds.SetBounds(cameraLeftBoundary, cameraRightBoundary, keepCameraViewInsideBounds);
        virtualCamera.PreviousStateIsValid = false;
    }

    private void ReleaseTeleportLock()
    {
        if (!_isTeleporting)
        {
            return;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }

        GameManager manager = GameManager.Instance;
        if (manager != null)
        {
            manager.UnlockPlayerMove(PlayerLockSources.TelePoint);
            manager.UnlockPlayerInteract(PlayerLockSources.TelePoint);
        }

        _isTeleporting = false;
    }
}
