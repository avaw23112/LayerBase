using System.Runtime.CompilerServices;

namespace LayerBase.Async;

/// <summary>Async method builder for ArchTask.</summary>
[AsyncMethodBuilder(typeof(LBTaskMethodBuilder))]
public struct LBTaskMethodBuilder
{
    private LBTaskSource? _source;
    private bool _earlyCompleted;

    public static LBTaskMethodBuilder Create()
    {
        return default;
    }

    public LBTask Task
    {
        get
        {
            if (_earlyCompleted) return LBTask.CompletedTask;
            if (_source == null) _source = LBTaskSource.RentForAsyncMethodBuilder();
            return new LBTask(_source);
        }
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }

    public void SetResult()
    {
        if (_source == null) _earlyCompleted = true;
        else _source.SetResult();
    }

    public void SetException(Exception exception)
    {
        if (_source == null) _source = LBTaskSource.RentForAsyncMethodBuilder();
        _source.SetException(exception ?? throw new ArgumentNullException(nameof(exception)));
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (_source == null) _source = LBTaskSource.RentForAsyncMethodBuilder();
        awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (_source == null) _source = LBTaskSource.RentForAsyncMethodBuilder();
        awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
    }
}

/// <summary>Async method builder for ArchTask{T}.</summary>
[AsyncMethodBuilder(typeof(LBTaskMethodBuilder<>))]
public struct LBTaskMethodBuilder<T>
{
    private LBTaskSource<T>? _source;
    private T _result;
    private bool _earlyCompleted;

    public static LBTaskMethodBuilder<T> Create()
    {
        return default;
    }

    public LBTask<T> Task
    {
        get
        {
            if (_earlyCompleted) return new LBTask<T>(_result);
            if (_source == null) _source = LBTaskSource<T>.RentForAsyncMethodBuilder();
            return new LBTask<T>(_source);
        }
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }

    public void SetResult(T result)
    {
        if (_source == null)
        {
            _result = result;
            _earlyCompleted = true;
        }
        else
        {
            _source.SetResult(result);
        }
    }

    public void SetException(Exception exception)
    {
        if (_source == null) _source = LBTaskSource<T>.RentForAsyncMethodBuilder();
        _source.SetException(exception ?? throw new ArgumentNullException(nameof(exception)));
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (_source == null) _source = LBTaskSource<T>.RentForAsyncMethodBuilder();
        awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (_source == null) _source = LBTaskSource<T>.RentForAsyncMethodBuilder();
        awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
    }
}
