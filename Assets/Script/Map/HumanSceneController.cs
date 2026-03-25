using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//控制人界場景物件
public class HumanSceneController : MonoBehaviour
{
    public GameObject DayLight;
    public GameObject NoonLight;
    public GameObject DayBackGround;
    public GameObject NoonBackGround;
    public GameObject DayCloud;
    public GameObject NoonCloud;
    private void OnEnable()
    {
        GameFlowEvent.OnDayPhaseChanged += SwitchDayPhase;
    }
    private void OnDisable()
    {
        GameFlowEvent.OnDayPhaseChanged -= SwitchDayPhase;
    }
    private void SwitchDayPhase(DayPhase state)
    {
        switch (state)
        {
            case DayPhase.HumanDay:
                SwitchToDay();
                break;
            case DayPhase.AfterNoon:
                SwitchToNoon();
                break;
        }
    }
    public void SwitchToNoon()
    {
        DayLight.SetActive(false);
        NoonLight.SetActive(true);
        DayBackGround.SetActive(false);
        NoonBackGround.SetActive(true);
        DayCloud.SetActive(false);
        NoonCloud.SetActive(true);
    }
    public void SwitchToDay()
    {
        DayLight.SetActive(true);
        NoonLight.SetActive(false);
        DayBackGround.SetActive(true);
        NoonBackGround.SetActive(false);
        DayCloud.SetActive(true);
        NoonCloud.SetActive(false);
    }
}
