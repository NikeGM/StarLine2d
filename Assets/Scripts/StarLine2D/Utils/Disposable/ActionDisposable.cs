using System;

namespace StarLine2D.Utils.Disposable
{
    public class ActionDisposable : IDisposable
    {
        private Action _onDispose;

        public ActionDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public ActionDisposable(Action onDispose, ActionDisposable parent)
        {
            _onDispose = () =>
            {
                onDispose?.Invoke();
                parent.Dispose();
            };
        }
        
        public void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }

        public static ActionDisposable Merge(ActionDisposable disposable1, ActionDisposable disposable2)
        {
            return new ActionDisposable(disposable2.Dispose, disposable1);
        }
    }
}