using GameSystem;
using UnityEngine;

namespace Shop
{
    public class WanderingYokaiMerchant : ShelfShopBase
    {
        private string _greetingDialogueId;
        private bool _initialized;
        private bool _hasGreetedToday;
        private bool _inDialogue;

        protected override string LockSource => PlayerLockSources.WanderingYokaiMerchant;
        protected override GameCurrencyType CurrencyType => GameCurrencyType.MonsterGold;
        protected override bool AllowDuplicateItems => false;

        protected override void Start()
        {
            if (_shopUIView == null)
            {
                _shopUIView = GetComponent<ShopViewBase>();
            }
            RegisterViewEvents();
        }

        public void Initialize(WanderingSO config)
        {
            if (config == null)
            {
                Debug.LogError("[WanderingYokaiMerchant] Initialize received a null WanderingSO.");
                return;
            }

            if (string.IsNullOrEmpty(config.ShopID))
            {
                Debug.LogError("[WanderingYokaiMerchant] WanderingSO is missing ShopID.");
                return;
            }

            ShopID = config.ShopID;
            _greetingDialogueId = config.GreetingDialogueId;
            GetShopData();

            if (_shopUIView == null)
            {
                _shopUIView = GetComponent<ShopViewBase>();
            }

            if (_shopUIView != null && !_initialized)
            {
                RegisterViewEvents();
            }

            _initialized = true;
        }

        protected override void OnInteract()
        {
            if (!_initialized || _inDialogue) return;

            if (GameManager.Instance.IsPlayerMoveLocked(LockSource))
            {
                _shopUIView.SetVisible();
                GameManager.Instance.UnlockPlayerMove(LockSource);
                return;
            }

            if (_hasGreetedToday)
            {
                OpenShop();
            }
            else
            {
                StartGreetingDialogue();
            }
        }

        private async void StartGreetingDialogue()
        {
            var talk = GameManager.Instance.talkSystem;
            if (talk == null)
            {
                _hasGreetedToday = true;
                OpenShop();
                return;
            }

            _inDialogue = true;
            string dialogueText = await GameDataLoader.LoadDialogueTextAsync(_greetingDialogueId);
            if (this == null)
            {
                return;
            }

            talk = GameManager.Instance.talkSystem;
            if (talk == null || string.IsNullOrEmpty(dialogueText))
            {
                _inDialogue = false;
                _hasGreetedToday = true;
                OpenShop();
                return;
            }

            bool completed = await talk.PlayDialogueAsync(dialogueText);
            if (this == null)
            {
                return;
            }

            _inDialogue = false;
            if (!completed)
            {
                return;
            }

            _hasGreetedToday = true;
            OpenShop();
        }
    }
}
