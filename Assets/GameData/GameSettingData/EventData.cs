using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


[System.Serializable]
public class GameEventDefinition
{
    public string Id;
    public string Name;
    public string EventDescription;
    public List<EventTime> EventTimes = new List<EventTime>();
    public EventRareity eventRareity;
    //特殊觸發效果腳本
    public string TriggerEffect;
}
public class EventDatabase
{
    public List<GameEventDefinition> Events;
}
// 注意：這裡加上 StringEnumConverter，讓 JSON 的 "Early" 字串能自動轉成 Enum
[JsonConverter(typeof(StringEnumConverter))]
public enum EventTime
{
    Early,
    Mid,   // 修改：配合 JSON 輸出的 "Mid"，這裡建議改成 Mid，或需寫特殊轉換器
    Late
}
public enum EventRareity
{
    Common,
    Uncommon,
    Rare
}