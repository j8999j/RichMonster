using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideOnAnimationEnd : MonoBehaviour
{
    // Animation Event 會呼叫這個方法
    public void HideObject()
    {
        gameObject.SetActive(false);
    }
}
