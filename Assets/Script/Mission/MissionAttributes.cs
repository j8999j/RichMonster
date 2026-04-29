using UnityEngine;

// 用於標記 Item ID 的欄位
public class ItemIDSelectAttribute : PropertyAttribute { }

// 用於標記 Tag 的欄位
public class ItemTagSelectAttribute : PropertyAttribute { }

// 用於標記 NPC ID (含妖怪) 的欄位
public class NpcIDSelectAttribute : PropertyAttribute { }

// 用於標記對話 ID 的欄位
public class DialogueIdSelectAttribute : PropertyAttribute { }
