using System;
using StarLine2D.Utils.Disposable;
using UnityEngine;

namespace StarLine2D.Utils.Observables
{
    [Serializable]
    public class ObservableProperty<TPropertyType>
    {
        [SerializeField] private TPropertyType value;

        private TPropertyType _storedValue;
        
        private Action<TPropertyType, TPropertyType> _onChanged;
        
        public TPropertyType Value
        {
            get => _storedValue;
            set
            {
                var isSame = _storedValue.Equals(value);
                if (isSame) return;
                var oldValue = _storedValue;

                _storedValue = value;
                this.value = _storedValue;
                
                _onChanged?.Invoke(oldValue, _storedValue);
            }
        }

        public void Validate()
        {
            Value = value;
        }

        public ActionDisposable Subscribe(Action<TPropertyType, TPropertyType> call)
        {
            _onChanged += call;
            return new ActionDisposable(() => _onChanged -= call);
        }

        public ActionDisposable SubscribeAndInvoke(Action<TPropertyType, TPropertyType> call)
        {
            call?.Invoke(_storedValue, _storedValue);
            return Subscribe(call);
        }

        public ActionDisposable Watch(ObservableProperty<TPropertyType> other)
        {
            Value = other.Value;

            var disposable1 = other.Subscribe((a, b) =>
            {
                if (a.Equals(b)) return;
                Value = other.Value;
            });

            var disposable2 = Subscribe((a, b) =>
            {
                if (a.Equals(b)) return;
                other.Value = Value;
            });

            return ActionDisposable.Merge(disposable1, disposable2);
        }
    }
}