using Arch.Core;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.ECS.Projection.Flow;

namespace LayerBase;

public static class ServiceECSExtensions
{
    /// <summary>
    /// 查询同时拥有 T1 组件的实体。
    /// </summary>
    /// <typeparam name="T1">第 1 个参与查询的组件类型。</typeparam>
    /// <param name="IService">当前 IService，用于取得其内部绑定的 ECSWorld。</param>
    /// <returns>返回 1 组件查询流程对象，后续可继续 Where、ForEach、Post 等操作。</returns>
    public static ProjectionQueryFlow1<T1> Query<T1>(this IService IService)
    {
        return ECSWorld(IService).Query<T1>();
    }

    /// <summary>
    /// 查询同时拥有 T1、T2 组件的实体。
    /// </summary>
    /// <typeparam name="T1">第 1 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T2">第 2 个参与查询的组件类型。</typeparam>
    /// <param name="IService">当前 IService，用于取得其内部绑定的 ECSWorld。</param>
    /// <returns>返回 2 组件查询流程对象，后续可继续 Where、ForEach、Post 等操作。</returns>
    public static ProjectionQueryFlow2<T1, T2> Query<T1, T2>(this IService IService)
    {
        return ECSWorld(IService).Query<T1, T2>();
    }

    /// <summary>
    /// 查询同时拥有 T1、T2、T3 组件的实体。
    /// </summary>
    /// <typeparam name="T1">第 1 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T2">第 2 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T3">第 3 个参与查询的组件类型。</typeparam>
    /// <param name="IService">当前 IService，用于取得其内部绑定的 ECSWorld。</param>
    /// <returns>返回 3 组件查询流程对象，后续可继续 Where、ForEach、Post 等操作。</returns>
    public static ProjectionQueryFlow3<T1, T2, T3> Query<T1, T2, T3>(this IService IService)
    {
        return ECSWorld(IService).Query<T1, T2, T3>();
    }

    /// <summary>
    /// 查询同时拥有 T1、T2、T3、T4 组件的实体。
    /// </summary>
    /// <typeparam name="T1">第 1 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T2">第 2 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T3">第 3 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T4">第 4 个参与查询的组件类型。</typeparam>
    /// <param name="IService">当前 IService，用于取得其内部绑定的 ECSWorld。</param>
    /// <returns>返回 4 组件查询流程对象，后续可继续 Where、ForEach、Post 等操作。</returns>
    public static ProjectionQueryFlow4<T1, T2, T3, T4> Query<T1, T2, T3, T4>(this IService IService)
    {
        return ECSWorld(IService).Query<T1, T2, T3, T4>();
    }

    /// <summary>
    /// 查询同时拥有 T1、T2、T3、T4、T5 组件的实体。
    /// </summary>
    /// <typeparam name="T1">第 1 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T2">第 2 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T3">第 3 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T4">第 4 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T5">第 5 个参与查询的组件类型。</typeparam>
    /// <param name="IService">当前 IService，用于取得其内部绑定的 ECSWorld。</param>
    /// <returns>返回 5 组件查询流程对象，后续可继续 Where、ForEach、Post 等操作。</returns>
    public static ProjectionQueryFlow5<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>(this IService IService)
    {
        return ECSWorld(IService).Query<T1, T2, T3, T4, T5>();
    }

    /// <summary>
    /// 查询同时拥有 T1、T2、T3、T4、T5、T6 组件的实体。
    /// </summary>
    /// <typeparam name="T1">第 1 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T2">第 2 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T3">第 3 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T4">第 4 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T5">第 5 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T6">第 6 个参与查询的组件类型。</typeparam>
    /// <param name="IService">当前 IService，用于取得其内部绑定的 ECSWorld。</param>
    /// <returns>返回 6 组件查询流程对象，后续可继续 Where、ForEach、Post 等操作。</returns>
    public static ProjectionQueryFlow6<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>(this IService IService)
    {
        return ECSWorld(IService).Query<T1, T2, T3, T4, T5, T6>();
    }

    /// <summary>
    /// 查询同时拥有 T1、T2、T3、T4、T5、T6、T7 组件的实体。
    /// </summary>
    /// <typeparam name="T1">第 1 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T2">第 2 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T3">第 3 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T4">第 4 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T5">第 5 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T6">第 6 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T7">第 7 个参与查询的组件类型。</typeparam>
    /// <param name="IService">当前 IService，用于取得其内部绑定的 ECSWorld。</param>
    /// <returns>返回 7 组件查询流程对象，后续可继续 Where、ForEach、Post 等操作。</returns>
    public static ProjectionQueryFlow7<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>(
        this IService IService)
    {
        return ECSWorld(IService).Query<T1, T2, T3, T4, T5, T6, T7>();
    }

    /// <summary>
    /// 查询同时拥有 T1、T2、T3、T4、T5、T6、T7、T8 组件的实体。
    /// </summary>
    /// <typeparam name="T1">第 1 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T2">第 2 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T3">第 3 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T4">第 4 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T5">第 5 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T6">第 6 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T7">第 7 个参与查询的组件类型。</typeparam>
    /// <typeparam name="T8">第 8 个参与查询的组件类型。</typeparam>
    /// <param name="IService">当前 IService，用于取得其内部绑定的 ECSWorld。</param>
    /// <returns>返回 8 组件查询流程对象，后续可继续 Where、ForEach、Post 等操作。</returns>
    public static ProjectionQueryFlow8<T1, T2, T3, T4, T5, T6, T7, T8> Query<T1, T2, T3, T4, T5, T6, T7, T8>(
        this IService IService)
    {
        return ECSWorld(IService).Query<T1, T2, T3, T4, T5, T6, T7, T8>();
    }

    /// <summary>
    /// Gets the ECS <see cref="World"/> bound to the current service.
    ///
    /// Advanced API:
    /// Prefer <c>Query</c> in normal business code. Access <see cref="World"/> directly only when you need
    /// lower-level ECS capabilities and understand the threading and structural-change rules yourself.
    /// </summary>
    public static World ECSWorld(this IService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return ServiceLayerBinder.RequireBinding(service).OwnerScope.EcsScheduler.World;
    }
}
