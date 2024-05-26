using System;
using System.Collections.Generic;

namespace StarLine.Utils.Disposable
{
    public class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Retain(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }
        
        public void Dispose()
        {
            foreach (var item in _disposables)
            {
                item.Dispose();
            }
            
            _disposables.Clear();
        }
    }
}