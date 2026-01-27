using System;
namespace GameSystem
{
    public static class GameRng
    {
        // 全域隨機
        private static System.Random _dailyRandom;
        //區域雜湊隨機
        private static int? _dailySeedHash;
        // 初始化：每天開始時呼叫一次
        public static void InitDailySeed(int masterSeed, int currentDay)
        {
            // 簡單的雜湊算法，確保每天的種子都不一樣
            // 9973 是一個大質數，用來打散數值分佈
            int dailySeedHash = masterSeed + (currentDay * 9973);//65537
            _dailySeedHash = dailySeedHash;
            _dailyRandom = new System.Random(dailySeedHash);
            UnityEngine.Debug.Log($"[GameRng] Day {currentDay} Seed initialized: {dailySeedHash}");
        }
        #region Global Rng
        // 取得整數 (min 包含, max 不包含) -> 對應 Random.Range(int, int)
        public static int Range(int min, int max)
        {
            if (_dailyRandom == null) return UnityEngine.Random.Range(min, max);
            return _dailyRandom.Next(min, max);
        }

        // 取得浮點數 (0.0 ~ 1.0) -> 對應 Random.value
        public static float Value()
        {
            if (_dailyRandom == null) return UnityEngine.Random.value;
            return (float)_dailyRandom.NextDouble();
        }
        #endregion

        #region Local Rng
        private static uint StableHash32(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;

            const uint offset = 2166136261u;
            const uint prime = 16777619u;

            uint hash = offset;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= prime;
            }
            return hash;
        }

        // -----------------------------
        // 2) 整數混洗（Avalanche/Mix）
        // -----------------------------
        private static uint Mix(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return x;
        }

        // -----------------------------
        // 3) 合併 dailySeed 與 keyHash
        // -----------------------------
        private static uint CombineSeed(uint dailySeed, uint keyHash)
        {
            // 這裡用 XOR + constant 讓分佈更好
            return Mix(dailySeed ^ (keyHash + 0x9e3779b9u));
        }

        // -----------------------------
        // 4) 固定模式（Keyed）
        // -----------------------------
        public static int RangeKeyed(int min, int max, string key)
        {
            if (!_dailySeedHash.HasValue)
                return UnityEngine.Random.Range(min, max);

            int range = max - min;
            if (range <= 0) return min;

            uint daily = (uint)_dailySeedHash.Value;
            uint keyHash = StableHash32(key);
            uint x = CombineSeed(daily, keyHash);

            return min + (int)(x % (uint)range);
        }
        public static float ValueKeyed(string key)
        {
            if (!_dailySeedHash.HasValue)
                return UnityEngine.Random.value;

            uint daily = (uint)_dailySeedHash.Value;
            uint keyHash = StableHash32(key);
            uint x = CombineSeed(daily, keyHash);

            // 取 24-bit 映射到 0~1
            return (x & 0xFFFFFF) / 16777216f;
        }
        #endregion
    }
}