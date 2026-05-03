using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Talksystem
{
    public class DialogueChoicePresenter : MonoBehaviour
    {
        [SerializeField]
        private DialogueView dialogueView;

        private readonly List<Button> spawnedButtons = new List<Button>();
        private TaskCompletionSource<int> currentChoiceTask;

        private void Awake()
        {
            ResolveDialogueView();
            SetOptionContainerVisible(false);
        }

        private void OnDisable()
        {
            CompleteChoice(-1);
        }

        public Task<int> ShowChoicesAsync(string prompt, IReadOnlyList<string> options)
        {
            if (options == null || options.Count == 0)
                return Task.FromResult(-1);

            CompleteChoice(-1);
            RectTransform container = ResolveOptionContainer();
            if (container == null)
            {
                Debug.LogWarning("[TalkSystem] Dialogue choice ButtonGroupContain not found.");
                return Task.FromResult(-1);
            }

            ClearOptions();

            DialogueView view = ResolveDialogueView();
            if (view != null)
            {
                view.ShowPanel();
                view.HideContinueIndicator();
                view.SetText(prompt ?? string.Empty);
                view.ShowAllCharacters();
            }

            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                Button button = CreateOptionButton(container);
                SetButtonText(button, options[i]);
                button.onClick.AddListener(() => CompleteChoice(index));
                spawnedButtons.Add(button);
            }

            currentChoiceTask = new TaskCompletionSource<int>();
            SetOptionContainerVisible(true);
            return currentChoiceTask.Task;
        }

        public void HideChoices()
        {
            SetOptionContainerVisible(false);
            ClearOptions();
        }

        public void Configure(DialogueView view)
        {
            if (view != null)
                dialogueView = view;

            SetOptionContainerVisible(false);
        }

        private void CompleteChoice(int selectedIndex)
        {
            TaskCompletionSource<int> task = currentChoiceTask;
            currentChoiceTask = null;

            HideChoices();
            task?.TrySetResult(selectedIndex);
        }

        private DialogueView ResolveDialogueView()
        {
            if (dialogueView != null)
                return dialogueView;

            dialogueView = GetComponentInParent<DialogueView>(true);
            if (dialogueView == null)
                dialogueView = GetComponent<DialogueView>();

            if (dialogueView == null)
                dialogueView = FindObjectOfType<DialogueView>(true);

            return dialogueView;
        }

        private RectTransform ResolveOptionContainer()
        {
            DialogueView view = ResolveDialogueView();
            return view != null ? view.ChoiceButtonGroupContain : null;
        }

        private Button CreateOptionButton(RectTransform container)
        {
            Button optionButtonPrefab = ResolveDialogueView()?.ChoiceButtonPrefab;
            if (optionButtonPrefab != null)
                return Instantiate(optionButtonPrefab, container);

            GameObject buttonObject = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(container, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420f, 52f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 52f;
            layoutElement.preferredHeight = 52f;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 4f);
            textRect.offsetMax = new Vector2(-16f, -4f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            return buttonObject.GetComponent<Button>();
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text ?? string.Empty;
        }

        private void ClearOptions()
        {
            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                Button button = spawnedButtons[i];
                if (button == null)
                    continue;

                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }

            spawnedButtons.Clear();
        }

        private void SetOptionContainerVisible(bool visible)
        {
            RectTransform container = ResolveOptionContainer();
            if (container == null)
                return;

            container.gameObject.SetActive(visible);
            CanvasGroup canvasGroup = container.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }
    }
}
