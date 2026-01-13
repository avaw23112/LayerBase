using System;
using System.Threading;

namespace LayerBase.Async
{
    /// <summary>Manual completion for ArchTask.</summary>
    public sealed class LBTaskCompletionSource
    {
        private readonly ArchTaskSource _source;

        public LBTaskCompletionSource()
        {
            _source = ArchTaskSource.Rent();
        }

        public LBTask Task => new LBTask(_source);

        public void SetResult() => _source.SetResult();

        public void SetException(Exception ex) => _source.SetException(ex);

        public void SetCanceled(CancellationToken token = default) => _source.SetCanceled(token);

        public bool TrySetResult()
        {
            try { _source.SetResult(); return true; }
            catch { return false; }
        }

        public bool TrySetException(Exception ex)
        {
            try { _source.SetException(ex); return true; }
            catch { return false; }
        }

        public bool TrySetCanceled(CancellationToken token = default)
        {
            try { _source.SetCanceled(token); return true; }
            catch { return false; }
        }
    }

    /// <summary>Manual completion for ArchTask{T}.</summary>
    public sealed class LBTaskCompletionSource<T>
    {
        private readonly ArchTaskSource<T> _source;

        public LBTaskCompletionSource()
        {
            _source = ArchTaskSource<T>.Rent();
        }

        public LBTask<T> Task => new LBTask<T>(_source);

        public void SetResult(T value) => _source.SetResult(value);

        public void SetException(Exception ex) => _source.SetException(ex);

        public void SetCanceled(CancellationToken token = default) => _source.SetCanceled(token);

        public bool TrySetResult(T value)
        {
            try { _source.SetResult(value); return true; }
            catch { return false; }
        }

        public bool TrySetException(Exception ex)
        {
            try { _source.SetException(ex); return true; }
            catch { return false; }
        }

        public bool TrySetCanceled(CancellationToken token = default)
        {
            try { _source.SetCanceled(token); return true; }
            catch { return false; }
        }
    }
}
