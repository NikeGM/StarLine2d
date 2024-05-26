using System;
using StarLine.Utils.Disposable;
using UnityEngine;

namespace StarLine.Utils.Observables
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
    }
}