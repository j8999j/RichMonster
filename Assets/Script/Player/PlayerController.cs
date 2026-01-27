using UnityEngine;

namespace Player
{
    // [新增] 強制要求 Animator 和 SpriteRenderer，防止 Component Missing 錯誤
    [RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Interaction Settings")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private float interactRadius = 1.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private Transform interactionOrigin;

        // Components
        private Rigidbody _rigidbody;
        private Animator _animator;          // [新增]
        private SpriteRenderer _spriteRenderer; // [新增]

        // State Variables
        private float _moveInput;
        private IInteractable _currentInteractable;
        private bool _CanInteract = true;
        private bool _CanMove = true;
        private bool _IsNight = false;

        // [新增] 動畫參數優化 (Hash ID)
        // 說明: 使用 Hash ID 比直接用 String 快很多，這是資深程式設計師的習慣
        private static readonly int IsWalkingKey = Animator.StringToHash("IsWalking");
        private static readonly int IsNightKey = Animator.StringToHash("IsNight");
        private static readonly int InteractTriggerKey = Animator.StringToHash("Interact"); // 如果你有做互動動作的話

        // [新增] 紀錄原始的 InteractionOrigin 本地座標 (用於翻轉)
        private Vector3 _originalInteractionPos;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();           // [新增]
            _spriteRenderer = GetComponent<SpriteRenderer>(); // [新增]

            if (interactionOrigin == null)
            {
                interactionOrigin = transform;
            }
            
            // 記錄初始位置，以便翻轉計算
            _originalInteractionPos = interactionOrigin.localPosition;
        }

        private void Update()
        {
            UpdateInteractablePrompt();

            if (_CanMove)
            {
                _moveInput = Input.GetAxisRaw("Horizontal");
            }
            else
            {
                _moveInput = 0f; // 如果不能動，強制歸零，避免滑步
            }

            // [新增] 集中處理動畫與轉向邏輯
            HandleAnimationAndOrientation();

            if (_CanInteract)
            {
                if (Input.GetKeyDown(interactKey))
                {
                    TryInteract();
                }
            }
        }

        private void FixedUpdate()
        {
            var velocity = _rigidbody.velocity;
            
            // 保持 Y 軸速度 (重力)，只改變 X 軸
            velocity.x = _moveInput * moveSpeed; 
            
            _rigidbody.velocity = velocity;
        }

        // [新增] 處理動畫與轉向的核心方法
        private void HandleAnimationAndOrientation()
        {
            // 1. 設定動畫狀態
            // Mathf.Abs(_moveInput) > 0.1f 用來避免浮點數誤差造成的微小抖動
            bool isWalking = Mathf.Abs(_moveInput) > 0.01f;
            _animator.SetBool(IsWalkingKey, isWalking);
            // 2. 處理轉向 (HD-2D 必須用 flipX，不能用 Rotation)
            if (_moveInput != 0)
            {
                // 向左(-1)時 flipX = true，向右(1)時 flipX = false
                bool faceLeft = _moveInput < 0;
                
                // 只有狀態改變時才執行賦值，微幅優化
                if (_spriteRenderer.flipX != faceLeft)
                {
                    _spriteRenderer.flipX = faceLeft;
                    
                    // [進階] 同步翻轉互動判定點
                    // 如果 interactionOrigin 不是角色本身，而是子物件，需要同步位移
                    if (interactionOrigin != transform)
                    {
                        float xPos = faceLeft ? -Mathf.Abs(_originalInteractionPos.x) : Mathf.Abs(_originalInteractionPos.x);
                        interactionOrigin.localPosition = new Vector3(xPos, _originalInteractionPos.y, _originalInteractionPos.z);
                    }
                }
            }
        }

        private void TryInteract()
        {
            if (_currentInteractable != null)
            {
                _currentInteractable.Interact();
                
                // [選用] 如果你有做撿東西或對話的動畫，可以在這裡觸發
                // _animator.SetTrigger(InteractTriggerKey); 
            }
        }

        // --- 以下維持原樣 ---

        private void OnDrawGizmosSelected()
        {
            if (interactionOrigin == null)
            {
                interactionOrigin = transform;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(interactionOrigin.position, interactRadius);
        }

        private void UpdateInteractablePrompt()
        {
            Vector3 origin = interactionOrigin != null ? interactionOrigin.position : _rigidbody.position;
            var hits = Physics.OverlapSphere(origin, interactRadius, interactableLayers);

            IInteractable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;

                float sqrDistance = (hit.transform.position - origin).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = interactable;
                }
            }

            if (nearest != _currentInteractable)
            {
                _currentInteractable?.HidePrompt();
                _currentInteractable = nearest;
                _currentInteractable?.ShowPrompt();
            }
        }

        private void OnDisable()
        {
            _currentInteractable?.HidePrompt();
            _currentInteractable = null;
        }

        public void SetCanInteract(bool canInteract)
        {
            _CanInteract = canInteract;
        }

        public void SetCanMove(bool canMove)
        {
            _CanMove = canMove;
            // 當被外部禁止移動時，也要確保動畫變回 Idle
            if (!canMove)
            {
                _moveInput = 0;
                _animator.SetBool(IsWalkingKey, false);
            }
        }

        public void SetIsNight(bool isNight)
        {
            _IsNight = isNight;
            _animator.SetBool(IsNightKey, _IsNight);
        }
    }
}