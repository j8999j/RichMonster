using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowGuide : MonoBehaviour
{


}
[System.Serializable]
public class GudieData
{
    public int Step;
    
}
    //需求建立監聽類(按照需求生成監聽例如對話結束、購買物品、前往妖界、前往妖界某物互動、點擊某按鈕等)
    //任務一：
    //步驟一：強制進入對話模式
    //步驟二：對話結束後提示前往與雜貨店互動(開始監聽步驟四)
    //步驟三：雜貨店互動結束後給予獎勵(剩餘的庫存)
    //步驟四：在商店中購買任一項物品(如果已完成跳過進行步驟五)
    //步驟五：提示與雜貨店互動(準備切換到午後)
    //步驟六：與雜貨店互動後，強制提示"點擊休息一下"(開啟面板)
    //步驟七：強制提示"點擊確定"(切換到午後)
    //任務二：
    //步驟一：提示如何前往妖界