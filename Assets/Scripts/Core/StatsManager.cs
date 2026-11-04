using System;
using UnityEngine;

namespace StreamerGame.Core
{
    public class StatsManager : MonoBehaviour
    {
        public static StatsManager Instance { get; private set; }

        [Header("Player Vitals")]
        [Range(0, 100)] public float energy = 85f;
        [Range(0, 100)] public float mood = 80f;

        [Header("Career & Streaming Stats")]
        public int fame = 1250;
        public int followers = 128400;
        public long revenue = 15500000; // VND
        public int currentViewers = 0;
        public int currentLikes = 0;
        public int sessionGiftsCount = 0;
        public long sessionRevenue = 0;

        [Header("Idol Apparel / Appearance")]
        public string currentOutfit = "Đồ Mặc Nhà Năng Động (Casual)";
        public bool isGroomed = false;
        public bool hasEaten = false;
        public bool hasCoffee = false;

        public event Action OnStatsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Natural slight energy decay over time
            if (energy > 10f)
            {
                energy -= Time.deltaTime * 0.15f;
            }
        }

        public void ModifyEnergy(float delta)
        {
            energy = Mathf.Clamp(energy + delta, 0f, 100f);
            OnStatsUpdated?.Invoke();
        }

        public void ModifyMood(float delta)
        {
            mood = Mathf.Clamp(mood + delta, 0f, 100f);
            OnStatsUpdated?.Invoke();
        }

        public void AddFame(int amount)
        {
            fame = Mathf.Max(0, fame + amount);
            OnStatsUpdated?.Invoke();
        }

        public void AddFollowers(int amount)
        {
            followers = Mathf.Max(0, followers + amount);
            OnStatsUpdated?.Invoke();
        }

        public void AddRevenue(long amount)
        {
            revenue += amount;
            sessionRevenue += amount;
            sessionGiftsCount++;
            OnStatsUpdated?.Invoke();
        }

        public void SetViewers(int viewers)
        {
            currentViewers = Mathf.Max(0, viewers);
            OnStatsUpdated?.Invoke();
        }

        public void AddLikes(int count)
        {
            currentLikes += count;
            OnStatsUpdated?.Invoke();
        }

        public void SetOutfit(string outfitName)
        {
            currentOutfit = outfitName;
            ModifyMood(10f);
            OnStatsUpdated?.Invoke();
        }
    }
}
