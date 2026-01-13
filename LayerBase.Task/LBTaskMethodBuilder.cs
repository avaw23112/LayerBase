using System;
using System.Runtime.CompilerServices;

namespace LayerBase.Async
{
    /// <summary>Async method builder for ArchTask.</summary>
    [AsyncMethodBuilder(typeof(LBTaskMethodBuilder))]
    public readonly struct LBTaskMethodBuilder
    {
        private readonly ArchTaskSource _source;

        private LBTaskMethodBuilder(ArchTaskSource source)
        {
            _source = source;
        }

        public static LBTaskMethodBuilder Create()
        {
            return new LBTaskMethodBuilder(ArchTaskSource.Rent());
        }

        public LBTask Task => new LBTask(_source);

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetResult()
        {
            _source.SetResult();
        }

        public void SetException(Exception exception)
        {
            _source.SetException(exception ?? throw new ArgumentNullException(nameof(exception)));
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            // no tracking needed
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }

    /// <summary>Async method builder for ArchTask{T}.</summary>
    [AsyncMethodBuilder(typeof(LBTaskMethodBuilder<>))]
    public readonly struct LBTaskMethodBuilder<T>
    {
        private readonly ArchTaskSource<T> _source;

        private LBTaskMethodBuilder(ArchTaskSource<T> source)
        {
            _source = source;
        }

        public static LBTaskMethodBuilder<T> Create()
        {
            return new LBTaskMethodBuilder<T>(ArchTaskSource<T>.Rent());
        }

        public LBTask<T> Task => new LBTask<T>(_source);

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetResult(T result)
        {
            _source.SetResult(result);
        }

        public void SetException(Exception exception)
        {
            _source.SetException(exception ?? throw new ArgumentNullException(nameof(exception)));
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            // no tracking needed
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }
}
