using System.Collections;
using System.Threading.Tasks;
using GameSystem;
using Talksystem;
using UnityEngine;
using UnityEngine.UI;

public class StoryPlaybackPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image storyImage;
    [SerializeField] private Sprite defaultStorySprite;

    [Header("Fade")]
    [SerializeField] private float defaultFadeInDuration = 0.25f;
    [SerializeField] private float defaultFadeOutDuration = 0.25f;

    [Header("Dialogue")]
    [SerializeField] private TalkSystem talkSystem;

    private int _playVersion;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        EnsureCanvasGroup();

        if (talkSystem == null && GameManager.Instance != null)
            talkSystem = GameManager.Instance.talkSystem;

        ClosePanel();
    }

    public void Configure(GameObject root, Image image, TalkSystem talk)
    {
        panelRoot = root != null ? root : gameObject;
        storyImage = image;
        talkSystem = talk;
        ClosePanel();
    }

    public void Show()
    {
        _playVersion++;
        _ = ShowAsync(defaultFadeInDuration);
    }

    public void Hide()
    {
        _playVersion++;
        _ = HideAsync(defaultFadeOutDuration);
    }

    public void CloseImmediate()
    {
        _playVersion++;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        ClosePanel();
    }

    public Task ShowAsync(float duration)
    {
        return FadePanelAsync(true, duration < 0f ? defaultFadeInDuration : duration);
    }

    public Task HideAsync(float duration)
    {
        return FadePanelAsync(false, duration < 0f ? defaultFadeOutDuration : duration);
    }

    public Task LoadImageAsync(string storyImageId)
    {
        _playVersion++;
        return LoadStoryImageAsync(storyImageId, _playVersion);
    }

    public async Task<bool> PlayStoryAsync(MonsterStoryDatabase storyData)
    {
        if (storyData == null || string.IsNullOrWhiteSpace(storyData.MonsterStoryID))
        {
            Debug.LogError("[StoryPlaybackPanel] Story data or MonsterStoryID is empty.");
            return false;
        }

        _playVersion++;
        int version = _playVersion;

        await ShowAsync(defaultFadeInDuration);
        await LoadStoryImageAsync(storyData.MonsterStoryID, version);

        string dialogueText = await ResolveDialogueTextAsync(storyData);
        if (string.IsNullOrWhiteSpace(dialogueText))
        {
            Debug.LogError($"[StoryPlaybackPanel] Story has no dialogue text: {storyData.MonsterStoryID}");
            ClosePanel();
            return false;
        }

        TalkSystem player = ResolveTalkSystem();
        if (player == null)
        {
            Debug.LogError("[StoryPlaybackPanel] TalkSystem not found.");
            ClosePanel();
            return false;
        }

        bool completed = await player.PlayDialogueAsync(dialogueText);

        if (version == _playVersion)
            await HideAsync(defaultFadeOutDuration);

        return completed;
    }

    public void StopStory()
    {
        _playVersion++;
        ResolveTalkSystem()?.StopDialogue();
        ClosePanel();
    }

    private async Task<string> ResolveDialogueTextAsync(MonsterStoryDatabase storyData)
    {
        await GameDataLoader.PreloadDialoguesByLabelAsync();

        if (GameDataLoader.CachedDialogueTexts.TryGetValue(storyData.MonsterStoryID, out string dialogueText)
            && !string.IsNullOrWhiteSpace(dialogueText))
        {
            return dialogueText;
        }

        return storyData.MonsterStory;
    }

    private Task LoadStoryImageAsync(string storyImageId, int version)
    {
        if (storyImage == null || string.IsNullOrWhiteSpace(storyImageId))
            return Task.CompletedTask;

        storyImage.sprite = defaultStorySprite;
        storyImage.enabled = defaultStorySprite != null;

        var taskSource = new TaskCompletionSource<bool>();
        SpriteLoader.LoadSpriteAsync(storyImageId, sprite =>
        {
            if (version != _playVersion || storyImage == null)
            {
                taskSource.TrySetResult(false);
                return;
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[StoryPlaybackPanel] SpriteLoader could not load story image ID: {storyImageId}");
                storyImage.sprite = defaultStorySprite;
                storyImage.enabled = defaultStorySprite != null;
                taskSource.TrySetResult(false);
                return;
            }

            storyImage.sprite = sprite;
            storyImage.enabled = true;
            storyImage.preserveAspect = true;
            taskSource.TrySetResult(true);
        });

        return taskSource.Task;
    }

    private TalkSystem ResolveTalkSystem()
    {
        if (talkSystem != null)
            return talkSystem;

        if (GameManager.Instance != null)
            talkSystem = GameManager.Instance.talkSystem;

        if (talkSystem == null)
            talkSystem = FindObjectOfType<TalkSystem>();

        return talkSystem;
    }

    private void OpenPanel()
    {
        GameObject root = panelRoot != null ? panelRoot : gameObject;
        root.SetActive(true);
        root.transform.SetAsLastSibling();

        CanvasGroup canvasGroup = EnsureCanvasGroup();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void ClosePanel()
    {
        GameObject root = panelRoot != null ? panelRoot : gameObject;
        CanvasGroup canvasGroup = EnsureCanvasGroup();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        root.SetActive(false);
    }

    private Task FadePanelAsync(bool show, float duration)
    {
        var taskSource = new TaskCompletionSource<bool>();
        GameObject root = panelRoot != null ? panelRoot : gameObject;

        if (show)
            root.SetActive(true);
        else if (!root.activeInHierarchy)
        {
            ClosePanel();
            taskSource.TrySetResult(true);
            return taskSource.Task;
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        _fadeCoroutine = StartCoroutine(FadePanelCoroutine(show, duration, taskSource));
        return taskSource.Task;
    }

    private IEnumerator FadePanelCoroutine(bool show, float duration, TaskCompletionSource<bool> taskSource)
    {
        GameObject root = panelRoot != null ? panelRoot : gameObject;
        CanvasGroup canvasGroup = EnsureCanvasGroup();

        if (show)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = true;
        }

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }

        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
        if (!show)
            root.SetActive(false);

        _fadeCoroutine = null;
        taskSource.TrySetResult(true);
    }

    private CanvasGroup EnsureCanvasGroup()
    {
        if (panelCanvasGroup != null)
            return panelCanvasGroup;

        GameObject root = panelRoot != null ? panelRoot : gameObject;
        panelCanvasGroup = root.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = root.AddComponent<CanvasGroup>();

        return panelCanvasGroup;
    }
}
