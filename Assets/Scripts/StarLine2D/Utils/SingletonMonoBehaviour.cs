using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarLine2D.Utils
{
    public class SingletonMonoBehaviour : MonoBehaviour
    {
        private static readonly Dictionary<Type, SingletonMonoBehaviour> Instances =
            new Dictionary<Type, SingletonMonoBehaviour>();

        private static readonly Dictionary<Type, List<SingletonMonoBehaviour>> Candidates =
            new Dictionary<Type, List<SingletonMonoBehaviour>>();

        public static TType GetInstance<TType>() where TType : SingletonMonoBehaviour
        {
            var t = typeof(TType);

            Load<TType>();
            DestroyCandidates<TType>();

            return (TType)Instances[t];
        }

        public static TType GetOrCreateInstance<TType>() where TType : SingletonMonoBehaviour
        {
            var found = GetInstance<TType>();
            if (found != null) return found;

            var t = typeof(TType);

            var fullName = t.ToString().Split('.');
            var name = fullName[fullName.Length - 1];
            var go = new GameObject(name);
            go.AddComponent<TType>();

            Instances[t] = go.GetComponent<TType>();
            return (TType)Instances[t];
        }

        private static void Load<TType>() where TType : SingletonMonoBehaviour
        {
            var t = typeof(TType);

            if (!Instances.ContainsKey(t)) Instances[t] = null;
            if (!Candidates.ContainsKey(t)) Candidates[t] = new List<SingletonMonoBehaviour>();
            if (Candidates[t].Count == 0) return;

            if (Instances[t] != null) return;

            var foundIndex = Candidates[t].FindIndex(item => item.singletonMain);
            if (foundIndex >= 0) Instances[t] = Candidates[t][foundIndex];
            else Instances[t] = Candidates[t][0];
        }

        private static void DestroyCandidates<TType>() where TType : SingletonMonoBehaviour
        {
            var t = typeof(TType);
            
            if (!Instances.ContainsKey(t)) Instances[t] = null;
            if (!Candidates.ContainsKey(t)) Candidates[t] = new List<SingletonMonoBehaviour>();

            if (Instances[t] == null) return;
            
            Candidates[t]
                .FindAll(item => item != Instances[t])
                .ForEach(item => Destroy(item.gameObject));
            
            Candidates[t].Clear();
        }

        private static void AddCandidate(SingletonMonoBehaviour obj)
        {
            var t = obj.GetType();

            if (Instances.ContainsKey(t) && Instances[t] != null)
            {
                Destroy(obj.gameObject);
                return;
            }
            
            if (!Candidates.ContainsKey(t)) Candidates[t] = new List<SingletonMonoBehaviour>();
            Candidates[t].Add(obj);
        }
        
        [SerializeField] private bool singletonMain = false;

        protected virtual void Awake()
        {
            AddCandidate(this);
        }

        protected virtual void OnDestroy()
        {
            var t = this.GetType();

            if (Instances.ContainsKey(t) && Instances[t] == this) Instances[t] = null;
            if (Candidates.ContainsKey(t)) Candidates[t].Remove(this);
        }
    }
}