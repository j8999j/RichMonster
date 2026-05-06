using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Souvenir
{
    public class GroceryCardsView : MonoBehaviour
    {
        public const float DefaultRevealInterval = 0.34f;

        [SerializeField] private List<Image> pointImages = new List<Image>();
        [SerializeField] private float revealInterval = DefaultRevealInterval;

        private readonly Dictionary<Image, Animator> _pointAnimators = new Dictionary<Image, Animator>();
        private Coroutine _revealCoroutine;
        private Action _closeRequested;
        private Action _revealCompleted;
        private Button _closeButton;
        private bool _allowManualClose = true;

        private void Awake()
        {
            EnsurePointImages();
            EnsureCloseButton();
            HideAllPoints();
        }

        public void Show(
            int currentPoints,
            Action closeRequested = null,
            Action revealCompleted = null,
            bool allowManualClose = true)
        {
            _closeRequested = closeRequested;
            _revealCompleted = revealCompleted;
            _allowManualClose = allowManualClose;
            EnsurePointImages();
            EnsureCloseButton();

            int pointCount = Mathf.Clamp(currentPoints, 0, pointImages.Count);
            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
            }

            HideAllPoints();
            _revealCoroutine = StartCoroutine(RevealPoints(pointCount));
        }

        public void Close()
        {
            if (!_allowManualClose)
            {
                return;
            }

            if (_closeRequested != null)
            {
                _closeRequested.Invoke();
                return;
            }

            if (!Addressables.ReleaseInstance(gameObject))
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }
        }

        private void EnsurePointImages()
        {
            pointImages.RemoveAll(image => image == null);
            if (pointImages.Count > 0)
            {
                CacheAnimators();
                return;
            }

            Transform pointContainer = transform.Find("PointCotain");
            IEnumerable<Image> candidates = pointContainer != null
                ? pointContainer.GetComponentsInChildren<Image>(true)
                : GetComponentsInChildren<Image>(true).Where(IsPointImage);

            pointImages = candidates
                .Where(IsPointImage)
                .OrderBy(image => image.transform.GetSiblingIndex())
                .ToList();

            Image bigPoint = GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image != null && image.name == "GroceryBigPoint");
            if (bigPoint != null && !pointImages.Contains(bigPoint))
            {
                pointImages.Add(bigPoint);
            }

            CacheAnimators();
        }

        private static bool IsPointImage(Image image)
        {
            if (image == null) return false;
            string objectName = image.gameObject.name;
            return objectName.StartsWith("GroceryPoint", StringComparison.Ordinal)
                || objectName == "GroceryBigPoint";
        }

        private void CacheAnimators()
        {
            _pointAnimators.Clear();
            foreach (Image image in pointImages)
            {
                Animator animator = image != null ? image.GetComponent<Animator>() : null;
                if (animator != null)
                {
                    _pointAnimators[image] = animator;
                }
            }
        }

        private void EnsureCloseButton()
        {
            if (_closeButton != null) return;

            Transform raycast = transform.Find("Raycast");
            if (raycast == null) return;

            _closeButton = raycast.GetComponent<Button>();
            if (_closeButton == null)
            {
                _closeButton = raycast.gameObject.AddComponent<Button>();
                _closeButton.transition = Selectable.Transition.None;
            }

            _closeButton.onClick.RemoveListener(Close);
            _closeButton.onClick.AddListener(Close);
        }

        private void HideAllPoints()
        {
            foreach (Image image in pointImages)
            {
                SetPointVisible(image, false);
            }
        }

        private IEnumerator RevealPoints(int pointCount)
        {
            for (int i = 0; i < pointCount; i++)
            {
                SetPointVisible(pointImages[i], true);

                if (revealInterval > 0f)
                {
                    yield return new WaitForSeconds(revealInterval);
                }
            }

            _revealCoroutine = null;
            _revealCompleted?.Invoke();
        }

        private void SetPointVisible(Image image, bool visible)
        {
            if (image == null) return;

            image.gameObject.SetActive(visible);
            if (!_pointAnimators.TryGetValue(image, out Animator animator) || animator == null) return;

            if (visible)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }
    }
}
