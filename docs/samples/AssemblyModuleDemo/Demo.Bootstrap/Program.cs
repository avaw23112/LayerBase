using Demo.Combat;
using Demo.Foundation;
using LayerBase;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace Demo.Bootstrap;

/// <summary>
/// 完整的 LayerBase 启动流程：
/// 1. AssemblyModule 发现与 ModuleCatalog 载入（自动）
/// 2. LayerRuntime.Build() 时自动合并模块
/// 3. Scope 消息分派（Event Post + Call）
/// 4. 元数据验证
///
/// 开发体验：只需将程序集的 ILayerBaseModule 实例注入到 Runtime 即可。
/// </summary>
internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine("=== LayerBase 完整启动流程 + AssemblyModule 载入 ===\n");

        // ── 阶段1：触发模块发现（GeneratedModuleCatalog 的 static ctor 向注册表注册）──
        Console.WriteLine("▶ 阶段1: 触发模块自动发现");
        var catalogType = typeof(Program).Assembly.GetType("LayerBase.Modules.GeneratedModuleCatalog");
        if (catalogType != null)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(catalogType.TypeHandle);
            Console.WriteLine("  [OK] 模块发现完成");
        }
        else
        {
            Console.WriteLine("  [INFO] GeneratedModuleCatalog 未找到，使用手动注册");
        }

        // 读取已发现的模块
        var modules = ModuleCatalogRegistry.GetAllModules();
        if (modules == null || modules.Length == 0)
        {
            Console.WriteLine("  [WARN] 未发现模块");
            return;
        }

        Console.WriteLine($"  发现 {modules.Length} 个 AssemblyModule:");
        foreach (var mod in modules)
        {
            ModuleManifest m = mod.Manifest;
            Console.WriteLine($"    {mod.GetType().FullName}");
            Console.WriteLine($"      层={m.LayerContracts.Count} 域={m.ScopeDefinitions.Count} 消息={m.MessageContracts.Count}");
            Console.WriteLine($"      服务={m.Services.Count} 上下文={m.Contexts.Count} 处理器={m.Handlers.Count}");
        }

        // 触发各程序集的生成式 dispatcher 注册（GeneratedScopeRuntimeHostFactory 的 static ctor）
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var factoryType = asm.GetType("LayerBase.Scope.GeneratedScopeRuntimeHostFactory");
            if (factoryType != null)
            {
                try { System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(factoryType.TypeHandle); }
                catch { }
            }
        }
        Console.WriteLine();

        // ── 阶段2：LayerRuntime 构建 ──
        // 开发者只需要做：
        //   1. 创建层
        //   2. Push 到 Builder
        //   3. Install 模块
        //   4. Build
        //   其他由 LayerRuntime 内部完成
        Console.WriteLine("▶ 阶段2: LayerRuntime 构建");
        LayerHub.Reset();

        var layer = new CombatLayer();
        using var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Install()  // 自动从 ModuleCatalogRegistry 载入已发现的模块
            .Build();

        Console.WriteLine($"  [OK] LayerRuntime 创建完成");
        Console.WriteLine($"  ScopeHost: {(runtime.ScopeHost != null ? "已创建" : "无")}");
        if (runtime.ScopeHost != null)
        {
            Console.WriteLine($"  域数量: {runtime.ScopeHost.Scopes.Count}");
            foreach (var scope in runtime.ScopeHost.Scopes)
            {
                Console.WriteLine($"    Scope[{scope.ScopeId}]: {scope.Descriptor.Name} 服务={scope.Services.Length}");
                for (int i = 0; i < scope.Services.Length; i++)
                {
                    Console.WriteLine($"      Service[{i}]: {scope.Services[i].GetType().Name}");
                }
            }
        }
        Console.WriteLine();

        // ── 阶段3：Scope 消息分派 ──
        Console.WriteLine("▶ 阶段3: Scope 消息分派");
        if (runtime.ScopeHost != null && runtime.ScopeHost.Scopes.Count > 1)
        {
            ScopeRuntime? combatScope = null;
            for (int i = 0; i < runtime.ScopeHost.Scopes.Count; i++)
            {
                if (runtime.ScopeHost.Scopes[i].Descriptor.Name == "CombatScope")
                {
                    combatScope = runtime.ScopeHost.Scopes[i];
                    break;
                }
            }

            if (combatScope != null)
            {
                Console.WriteLine($"  找到 CombatScope (scopeId={combatScope.ScopeId}, services={combatScope.Services.Length})");

                if (GlobalDispatcherRegistry.PostDispatcher != null && GlobalDispatcherRegistry.CallDispatcher != null)
                {
                    var combatRef = runtime.ScopeHost.Routes.GetScopeRef<CombatScope>(combatScope.ScopeId);

                    Console.WriteLine("  [Post] CombatantDamagedEvent(Health=80)");
                    combatRef.Post(new CombatantDamagedEvent(80));
                    runtime.Pump(0.1f);

                    Console.WriteLine("  [Call] CalculateDamageCall(AttackerId=5, SkillPower=100)");
                    var callTask = combatRef.Call(new CalculateDamageCall(5, 100));
                    runtime.Pump(0.1f);
                    var result = await callTask;

                    Console.WriteLine($"  [OK]  Damage={result.Damage}, RemainingHP={result.RemainingHealth}");
                }
                else
                {
                    Console.WriteLine("  [INFO] 无生成式 dispatcher，跳过消息分派");
                }
            }
        }
        Console.WriteLine();

        // ── 阶段4：元数据验证 ──
        Console.WriteLine("▶ 阶段4: 模块元数据验证");
        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(modules);
        Console.WriteLine($"  层契约 GameplayLayer: {catalog.LayerContracts.ContainsKey(typeof(GameplayLayer).TypeHandle)}");
        Console.WriteLine($"  域定义 CombatScope: {catalog.ScopeDefinitions.ContainsKey(typeof(CombatScope).TypeHandle)}");

        Type[] msgTypes = [typeof(CalculateDamageCall), typeof(CombatantDamagedEvent)];
        foreach (var t in msgTypes)
            Console.WriteLine($"  消息契约 {t.Name}: {catalog.MessageContracts.ContainsKey(t.TypeHandle)}");

        Console.WriteLine($"  服务 CombatService: {catalog.Services.Any(s => Type.GetTypeFromHandle(s.ServiceType) == typeof(CombatService))}");
        if (catalog.ServiceSlots.TryGetValue(typeof(CombatService).TypeHandle, out int slot))
            Console.WriteLine($"  CombatService ScopeSlot: {slot}");

        Console.WriteLine($"  Call路由={catalog.CallRoutes.Count} Event路由={catalog.EventRoutes.Count} EH路由={catalog.EventHandlerRoutes.Count}");
        Console.WriteLine();

        Console.WriteLine("=== 启动流程完成: AssemblyModule 载入 + LayerRuntime 运行验证通过 ===");
    }
}
