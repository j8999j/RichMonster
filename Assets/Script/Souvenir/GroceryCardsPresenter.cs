using UnityEngine;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Souvenir
{
    public static class GroceryCardsPresenter
    {
        public const string Address = "GroceryCards";

        private static GameObject _currentInstance;
        private static Canvas _runtimeCanvas;
        private static int _showVersion;

        public static void Show(int currentPoints, Transform parent = null)
        {
            ShowInternal(currentPoints, parent, null, true);
        }

        public static void ShowAndCloseAfterReveal(int currentPoints, Action closed = null, Transform parent = null)
        {
            ShowInternal(currentPoints, parent, () =>
            {
                Close();
                closed?.Invoke();
            }, false);
        }

        private static void ShowInternal(
            int currentPoints,
            Transform parent,
            Action revealCompleted,
            bool allowManualClose)
        {
            _showVersion++;
            int version = _showVersion;

            ReleaseCurrentInstance();

            Transform targetParent = parent != null ? parent : ResolveParent();
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(Address, targetParent, false);
            handle.Completed += completedHandle =>
            {
                if (version != _showVersion)
                {
                    ReleaseCompletedInstance(completedHandle);
                    return;
                }

                if (completedHandle.Status != AsyncOperationStatus.Succeeded || completedHandle.Result == null)
                {
                    Debug.LogError($"[GroceryCardsPresenter] Failed to instantiate Addressables prefab: {Address}");
                    revealCompleted?.Invoke();
                    return;
                }

                _currentInstance = completedHandle.Result;
                SetupRectTransform(_currentInstance);

                GroceryCardsView view = _currentInstance.GetComponent<GroceryCardsView>();
                if (view == null)
                {
                    view = _currentInstance.AddComponent<GroceryCardsView>();
                }

                view.Show(currentPoints, Close, revealCompleted, allowManualClose);
            };
        }

        public static void Close()
        {
            _showVersion++;
            ReleaseCurrentInstance();
        }

        private static Transform ResolveParent()
        {
            Canvas[] activeCanvases = UnityEngine.Object.FindObjectsOfType<Canvas>(false);
            Canvas canvas = PickBestCanvas(activeCanvases);
            if (canvas != null)
            {
                return canvas.transform;
            }

            Canvas[] allCanvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
            canvas = PickBestCanvas(allCanvases);
            if (canvas != null)
            {
                return canvas.transform;
            }

            if (_runtimeCanvas == null)
            {
                GameObject canvasObject = new GameObject(
                    "GroceryCardsRuntimeCanvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                _runtimeCanvas = canvasObject.GetComponent<Canvas>();
                _runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _runtimeCanvas.sortingOrder = 5000;
            }

            return _runtimeCanvas.transform;
        }

        private static Canvas PickBestCanvas(Canvas[] canvases)
        {
            Canvas bestCanvas = null;
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.isRootCanvas) continue;
                if (bestCanvas == null || canvas.sortingOrder > bestCanvas.sortingOrder)
                {
                    bestCanvas = canvas;
                }
            }

            return bestCanvas;
        }

        private static void SetupRectTransform(GameObject instance)
        {
            if (instance == null) return;

            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            rectTransform.SetAsLastSibling();
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void ReleaseCurrentInstance()
        {
            if (_currentInstance != null)
            {
                if (!Addressables.ReleaseInstance(_currentInstance))
                {
                    UnityEngine.Object.Destroy(_currentInstance);
                }

                _currentInstance = null;
            }

            if (_runtimeCanvas != null && _runtimeCanvas.transform.childCount == 0)
            {
                UnityEngine.Object.Destroy(_runtimeCanvas.gameObject);
                _runtimeCanvas = null;
            }
        }

        private static void ReleaseCompletedInstance(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                Addressables.ReleaseInstance(handle.Result);
            }
        }
    }
}
