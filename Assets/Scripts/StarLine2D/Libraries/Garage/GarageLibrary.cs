using System;
using System.Collections.Generic;
using StarLine2D.Controllers;
using UnityEngine;

namespace StarLine2D.Libraries.Garage
{
    [CreateAssetMenu(menuName = "Custom/GarageLibrary", fileName = "GarageLibrary")]
    public class GarageLibrary : ScriptableObject
    {
        [SerializeField] private List<GarageItem> items = new();
        
        public int Count => items.Count;

        public List<GarageItem> GetAll()
        {
            return new List<GarageItem>(items);
        }

        public List<GarageItem> GetAll(GarageItem.ShipType shipType)
        {
            return new List<GarageItem>(items.FindAll(item => item.Type.Equals(shipType)));
        }

        public GarageItem GetOne(int index)
        {
            if (index < 0 || index >= items.Count) return null;
            return items[index];
        }

        public GarageItem GetOne(int index, GarageItem.ShipType shipType)
        {
            var list = GetAll(shipType);
            if (index < 0 || index >= list.Count) return null;
            return list[index];
        }

        public GarageItem GetRandom()
        {
            if (Count == 0) return null;
            
            var index = UnityEngine.Random.Range(0, items.Count);
            return items[index];
        }

        public GarageItem GetRandom(GarageItem.ShipType shipType)
        {
            var filtered = GetAll(shipType);
            return filtered.Count == 0 ? null : filtered[UnityEngine.Random.Range(0, filtered.Count)];
        }

        public int GetIndex(ShipController ship)
        {
            return items.FindIndex(item => item.Prefab == ship);
        }

        public int GetIndex(ShipController ship, GarageItem.ShipType shipType)
        {
            return GetAll(shipType).FindIndex(item => item.Prefab == ship);
        }

        private void OnValidate()
        {
            foreach (var item in items) item.SetDefaultName();
        }
        
        private static GarageLibrary _instance;
        public static GarageLibrary I => _instance == null ? Load() : _instance;

        private static GarageLibrary Load()
        {
            _instance = Resources.Load<GarageLibrary>("GarageLibrary");
            return _instance;
        }

    }

    [Serializable]
    public class GarageItem
    {
        public enum ShipType
        {
            Player,
            Ally,
            Enemy
        }

        [SerializeField] private string name;
        [SerializeField] private ShipController shipPrefab;
        [SerializeField] private ShipType shipType;

        public string Name => name;
        public ShipController Prefab => shipPrefab;
        public ShipType Type => shipType;
        
        public void SetDefaultName()
        {
            if (name != "") return;
            
            name = shipPrefab != null ? $"{shipPrefab.name}-{shipType}" : "None";
        }
    }
}