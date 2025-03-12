using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace StarLine2D.UI.Widgets
{
    [RequireComponent(typeof(Text.TextWidget))]
    public class CountdownWidget : MonoBehaviour
    {
        [SerializeField] private int value = 100;
        [SerializeField] private float speed = 1f;
        [SerializeField] private bool showMinutes = false;
        [SerializeField] private UnityEvent onDone;

        private Text.TextWidget _text;
        private bool _started = false;
        private int _currentValue;
        private float _lastTime = 0;

        private void Awake()
        {
            _text = GetComponent<Text.TextWidget>();
            Flush();
        }

        private void Update()
        {
            if (!_started) return;

            if (_lastTime == 0)
            {
                _lastTime = Time.time;
                _currentValue = value;
                return;
            }
            
            var currentTime = Time.time;
            while (_lastTime + speed <= currentTime)
            {
                _currentValue--;
                _lastTime += speed;
            }

            if (_currentValue < 0) _currentValue = 0;
            _text.SetText(!showMinutes
                ? _currentValue.ToString()
                : $"{Math.Floor(_currentValue / 60f)}:{_currentValue % 60}");

            if (_currentValue != 0) return;
            onDone?.Invoke();
            Flush();
        }

        [ContextMenu("Start Countdown")]
        public void StartCountdown()
        {
            _started = true;
        }

        [ContextMenu("Flush Countdown")]
        public void Flush()
        {
            _currentValue = value;
            _lastTime = 0;
            _started = false;
            _text.SetText("");
        }
    }
}