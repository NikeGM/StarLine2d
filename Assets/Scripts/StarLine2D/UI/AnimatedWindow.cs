using System;
using System.Collections.Generic;
using StarLine2D.Singletons;
using StarLine2D.Utils;
using StarLine2D.Utils.Disposable;
using UnityEngine;

namespace StarLine2D.UI
{
    [RequireComponent(typeof(Animator))]
    public class AnimatedWindow : MonoBehaviour
    {
        private static readonly int ShowTrigger = Animator.StringToHash("show");
        private static readonly int HideTrigger = Animator.StringToHash("hide");

        private static readonly Dictionary<string, List<AnimatedWindow>> CurrentWindows = new Dictionary<string, List<AnimatedWindow>>();

        private Action _onClose;
        
        private Animator _animator;

        protected virtual void Start()
        {
            _animator = GetComponent<Animator>();
            _animator.SetTrigger(ShowTrigger);
        }

        public void Close()
        {
            _animator.SetTrigger(HideTrigger);
        }

        public virtual void AnimationEventClose()
        {
            _onClose?.Invoke();
            Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            foreach (var pair in CurrentWindows)
            {
                pair.Value.Remove(GetComponent<AnimatedWindow>());
            }
        }
        
        public ActionDisposable Subscribe(Action call)
        {
            _onClose += call;
            return new ActionDisposable(() => _onClose -= call);
        }

        public static AnimatedWindow Open(string path)
        {
            var window = Resources.Load<GameObject>(path);
            if (window == null) return null;

            var canvas = SingletonMonoBehaviour.GetInstance<CanvasSingleton>().GetComponent<Canvas>();
            if (canvas == null) return null;

            var go = Instantiate(window, canvas.transform);
            
            if (!CurrentWindows.ContainsKey(path)) CurrentWindows.Add(path, new List<AnimatedWindow>());
            var newWindow = go.GetComponent<AnimatedWindow>();
            
            CurrentWindows[path].Add(newWindow);
            return newWindow;
        }

        public static bool IsOpen(string path)
        {
            return CurrentWindows.ContainsKey(path) && CurrentWindows[path].Count > 0;
        }

        public static int CountOpen(string path)
        {
            return !CurrentWindows.ContainsKey(path) ? 0 : CurrentWindows[path].Count;
        }

        public static AnimatedWindow OpenUnique(string path)
        {
            if (IsOpen(path)) return CurrentWindows[path][0];

            return Open(path);
        }

        public static void Switch(string path)
        {
            if (IsOpen(path)) CurrentWindows[path][0].Close();
            else OpenUnique(path);
        }

        public static List<AnimatedWindow> GetOpen(string path)
        {
            return !CurrentWindows.ContainsKey(path) ? new List<AnimatedWindow>() : CurrentWindows[path];
        }

        public static void Close(string path, int index)
        {
            if (!CurrentWindows.ContainsKey(path)) return;
            if (index >= CurrentWindows[path].Count) return;

            CurrentWindows[path].RemoveAt(index);
        }

        public static void CloseLast(string path)
        {
            if (!CurrentWindows.ContainsKey(path)) return;
            Close(path, CurrentWindows[path].Count - 1);
        }

        public static void CloseFirst(string path)
        {
            Close(path, 0);
        }

        public static void CloseAll()
        {
            var windows = FindObjectsOfType<AnimatedWindow>();
            foreach (var window in windows)
            {
                window.Close();
            }
        }
    }
}