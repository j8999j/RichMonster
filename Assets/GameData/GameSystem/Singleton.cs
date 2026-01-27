using UnityEngine;

// <T> 代表這是一個泛型類別，where T : Component 限制 T 必須是 Unity 的元件
public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance of {typeof(T).Name} already destroyed on application quit.");
                return null;
            }

            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();

                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name; // 將物件命名為類別名稱 (例如 "GameManager")
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    // 虛擬方法 (Virtual)，讓子類別可以覆寫 (Override) 但保留基礎功能
    protected virtual void Awake()
    {
        // 檢查是否已經有另一個實例存在
        if (_instance == null)
        {
            _instance = this as T;
            // transform.parent == null 確保它是根物件，因為 DontDestroyOnLoad 只能作用於根物件
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _applicationIsQuitting = true;
        }
    }
}