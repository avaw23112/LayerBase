using System.Collections.Concurrent;
using System.Reflection;
using LayerBase.Async;
using LayerBase.Layers;

namespace LayerBase.Call;

internal static class CallMethodBinder
{
    private static readonly ConcurrentDictionary<Type, CallMethodMetadata[]> MetadataCache = new();

    public static void Bind(object owner, Layer layer)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (layer == null) throw new ArgumentNullException(nameof(layer));

        var methods = MetadataCache.GetOrAdd(owner.GetType(), static type => Discover(type));
        foreach (var method in methods)
            method.Register(owner, layer);
    }

    public static bool HasCallMethods(Type ownerType)
    {
        if (ownerType == null) throw new ArgumentNullException(nameof(ownerType));
        return MetadataCache.GetOrAdd(ownerType, static type => Discover(type)).Length > 0;
    }

    private static CallMethodMetadata[] Discover(Type ownerType)
    {
        return ownerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static method => method.GetCustomAttribute<CallAttribute>() != null)
            .Select(method => CreateMetadata(ownerType, method))
            .ToArray();
    }

    private static CallMethodMetadata CreateMetadata(Type ownerType, MethodInfo method)
    {
        if (method.IsStatic)
            throw CreateSignatureException(ownerType, method, "static methods are not supported.");

        if (method.ContainsGenericParameters)
            throw CreateSignatureException(ownerType, method, "generic methods are not supported.");

        var parameters = method.GetParameters();
        var takesCancellationToken = false;
        Type? requestType;
        switch (parameters.Length)
        {
            case 1:
                requestType = parameters[0].ParameterType;
                break;
            case 2 when parameters[1].ParameterType == typeof(CancellationToken):
                requestType = parameters[0].ParameterType;
                takesCancellationToken = true;
                break;
            default:
                throw CreateSignatureException(
                    ownerType,
                    method,
                    "signature must be LBTask<TResponse> Method(TRequest request) or LBTask<TResponse> Method(TRequest request, CancellationToken cancellationToken).");
        }

        if (parameters[0].ParameterType.IsByRef || parameters[0].IsOut || parameters[0].ParameterType.IsPointer)
            throw CreateSignatureException(ownerType, method, "request parameter must be passed by value.");

        if (!requestType!.IsValueType)
            throw CreateSignatureException(ownerType, method, "request type must be a struct.");

        if (!TryGetResponseType(method.ReturnType, out var responseType))
            throw CreateSignatureException(ownerType, method, "return type must be LBTask<TResponse>.");

        if (!responseType!.IsValueType)
            throw CreateSignatureException(ownerType, method, "response type must be a struct.");

        var registerMethod = typeof(CallMethodBinder)
            .GetMethod(nameof(CreateMetadataCore), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(ownerType, requestType, responseType);

        return (CallMethodMetadata)registerMethod.Invoke(null, new object[] { method, takesCancellationToken })!;
    }

    private static CallMethodMetadata CreateMetadataCore<TOwner, TRequest, TResponse>(MethodInfo method, bool takesCancellationToken)
        where TRequest : struct
        where TResponse : struct
    {
        return new CallMethodMetadata((owner, layer) =>
        {
            var handler = new ReflectedMethodCallHandler<TOwner, TRequest, TResponse>(
                (TOwner)owner,
                method,
                takesCancellationToken);
            layer.RegisterCallHandler(handler);
        });
    }

    private static bool TryGetResponseType(Type returnType, out Type? responseType)
    {
        responseType = null;
        if (!returnType.IsGenericType) return false;
        if (returnType.GetGenericTypeDefinition() != typeof(LBTask<>)) return false;

        responseType = returnType.GetGenericArguments()[0];
        return true;
    }

    private static InvalidOperationException CreateSignatureException(Type ownerType, MethodInfo method, string detail)
    {
        return new InvalidOperationException(
            $"[Call] method '{ownerType.FullName}.{method.Name}' is invalid: {detail}");
    }

    private readonly struct CallMethodMetadata
    {
        public CallMethodMetadata(Action<object, Layer> register)
        {
            Register = register;
        }

        public Action<object, Layer> Register { get; }
    }

    private sealed class ReflectedMethodCallHandler<TOwner, TRequest, TResponse> : ILayerCallHandler<TRequest, TResponse>
        where TRequest : struct
        where TResponse : struct
    {
        private readonly TOwner _owner;
        private readonly MethodInfo _method;
        private readonly bool _takesCancellationToken;

        public ReflectedMethodCallHandler(TOwner owner, MethodInfo method, bool takesCancellationToken)
        {
            _owner = owner;
            _method = method;
            _takesCancellationToken = takesCancellationToken;
        }

        public LBTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = _takesCancellationToken
                    ? _method.Invoke(_owner, new object?[] { request, cancellationToken })
                    : _method.Invoke(_owner, new object?[] { request });

                if (result is LBTask<TResponse> task) return task;

                return LBTask<TResponse>.FromException(
                    new InvalidOperationException(
                        $"[Call] method '{_method.DeclaringType?.FullName}.{_method.Name}' returned an unexpected result."));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                return LBTask<TResponse>.FromException(ex.InnerException);
            }
        }
    }
}
