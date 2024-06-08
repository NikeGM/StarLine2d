using System;

namespace StarLine2D.Utils.Observables
{
    [Serializable]
    public class IntObservableProperty : ObservableProperty<int>
    {
        public void Clamp(int min, int max)
        {
            Value = Math.Clamp(Value, min, max);
        }
    }
}