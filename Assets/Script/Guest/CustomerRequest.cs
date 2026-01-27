using System;
using UnityEngine;

// 定義顧客需求的精確程度
public enum RequestType
{
    SpecificItem,   // 指定單一物品 (例：sword)
    ItemType,       // 只限定物品種類 (ItemType對上都可以)
    ItemTag,        // 只限定物品標籤 (Tag對上都可以)
    ItemTypeWithTag // 限定種類並附帶標籤 (例：ItemType對上且帶有Dragon標籤)
}

// [資料類別] 封裝顧客需求資訊結構
[System.Serializable]
public class CustomerRequest
{
    //需求型態
    public RequestType Type;
    // 針對種類或種類+標籤時會用到
    public ItemType TargetItemType;
    // 如為模糊需求時的標籤 (例："Dragon")
    public string TargetTag;
    // 指定物品時的 ID (例："sword")
    public string TargetItemId;
    // 顧客需求敘述 (用於 UI 對話)
    public string DialogText;
}
