using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace GameSystem
{
    /// <summary>
    /// Addressables-based scene loader with progress tracking and proper resource management.
    /// </summary>
    public class AddressableSceneLoader : Singleton<AddressableSceneLoader>
    {
        // Events
        public event Action<float> OnLoadingProgress;
        public event Action<string> OnSceneLoaded;
        public event Action<string> OnSceneLoadFailed;

        // Current loading state
        private AsyncOperationHandle<SceneInstance> _currentHandle;
        private bool _isLoading;
        private float _loadingProgress;

        /// <summary>
        /// Current loading progress (0.0 to 1.0)
        /// </summary>
        public float LoadingProgress => _loadingProgress;

        /// <summary>
        /// Whether a scene is currently being loaded
        /// </summary>
        public bool IsLoading => _isLoading;

        protected override void Awake()
        {
            base.Awake();
            _isLoading = false;
            _loadingProgress = 0f;
        }

        /// <summary>
        /// Load a scene using Addressables (Single mode - unloads current scene)
        /// </summary>
        /// <param name="sceneAddress">Addressables scene address or key</param>
        /// <param name="onComplete">Optional callback when loading completes</param>
        public void LoadScene(string sceneAddress, Action<bool> onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[AddressableSceneLoader] Already loading a scene. Cannot load {sceneAddress}");
                onComplete?.Invoke(false);
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneAddress, LoadSceneMode.Single, onComplete));
        }

        /// <summary>
        /// Load a scene additively using Addressables
        /// </summary>
        /// <param name="sceneAddress">Addressables scene address or key</param>
        /// <param name="onComplete">Optional callback when loading completes</param>
        public void LoadSceneAdditive(string sceneAddress, Action<bool> onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[AddressableSceneLoader] Already loading a scene. Cannot load {sceneAddress}");
                onComplete?.Invoke(false);
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneAddress, LoadSceneMode.Additive, onComplete));
        }

        /// <summary>
        /// Unload a scene by address
        /// </summary>
        /// <param name="sceneAddress">Scene address to unload</param>
        public void UnloadScene(string sceneAddress, Action<bool> onComplete = null)
        {
            StartCoroutine(UnloadSceneAsync(sceneAddress, onComplete));
        }

        private IEnumerator LoadSceneAsync(string sceneAddress, LoadSceneMode loadMode, Action<bool> onComplete)
        {
            _isLoading = true;
            _loadingProgress = 0f;

            Debug.Log($"[AddressableSceneLoader] Loading scene: {sceneAddress} (Mode: {loadMode})");

            // Release previous handle if exists
            ReleaseCurrentHandle();

            // Start loading
            var handle = Addressables.LoadSceneAsync(sceneAddress, loadMode);
            _currentHandle = handle;

            // Track progress
            while (!handle.IsDone)
            {
                _loadingProgress = handle.PercentComplete;
                OnLoadingProgress?.Invoke(_loadingProgress);
                yield return null;
            }

            // Final progress
            _loadingProgress = 1f;
            OnLoadingProgress?.Invoke(_loadingProgress);

            // Check result
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"[AddressableSceneLoader] Successfully loaded scene: {sceneAddress}");
                OnSceneLoaded?.Invoke(sceneAddress);
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"[AddressableSceneLoader] Failed to load scene: {sceneAddress}. Error: {handle.OperationException}");
                OnSceneLoadFailed?.Invoke(sceneAddress);
                onComplete?.Invoke(false);
            }

            _isLoading = false;
        }

        private IEnumerator UnloadSceneAsync(string sceneAddress, Action<bool> onComplete)
        {
            Debug.Log($"[AddressableSceneLoader] Unloading scene: {sceneAddress}");

            var handle = Addressables.UnloadSceneAsync(_currentHandle);

            while (!handle.IsDone)
            {
                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"[AddressableSceneLoader] Successfully unloaded scene: {sceneAddress}");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"[AddressableSceneLoader] Failed to unload scene: {sceneAddress}");
                onComplete?.Invoke(false);
            }

            Addressables.Release(handle);
        }

        private void ReleaseCurrentHandle()
        {
            if (_currentHandle.IsValid())
            {
                Addressables.Release(_currentHandle);
            }
        }

        private void OnDestroy()
        {
            ReleaseCurrentHandle();
        }

        /// <summary>
        /// Load scene with a simple coroutine for convenience
        /// </summary>
        public Coroutine LoadSceneWithProgress(string sceneAddress, Action<float> progressCallback, Action<bool> completeCallback = null)
        {
            return StartCoroutine(LoadSceneWithProgressCoroutine(sceneAddress, progressCallback, completeCallback));
        }

        private IEnumerator LoadSceneWithProgressCoroutine(string sceneAddress, Action<float> progressCallback, Action<bool> completeCallback)
        {
            bool loadSuccess = false;
            bool loadComplete = false;

            // Subscribe to progress
            void OnProgress(float progress)
            {
                progressCallback?.Invoke(progress);
            }

            OnLoadingProgress += OnProgress;

            // Load scene
            LoadScene(sceneAddress, (success) =>
            {
                loadSuccess = success;
                loadComplete = true;
            });

            // Wait for completion
            while (!loadComplete)
            {
                yield return null;
            }

            // Unsubscribe
            OnLoadingProgress -= OnProgress;

            // Invoke completion callback
            completeCallback?.Invoke(loadSuccess);
        }
    }
}
