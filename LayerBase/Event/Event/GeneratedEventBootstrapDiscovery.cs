using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LayerBase.Core.Event;

internal static class GeneratedEventBootstrapDiscovery
{
    private static readonly string[] s_bootstrapTypeNames =
    {
        "LayerBase.Core.Event.EventPrewarmBootstrapper",
        "LayerBase.Event.EventMetaData.EventMetaDataBootstrapper"
    };

    private static readonly object s_lock = new();
    private static readonly HashSet<Assembly> s_initializedAssemblies = new();

    internal static void EnsureLoadedAssembliesInitialized()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            Assembly assembly = assemblies[i];

            if (assembly.IsDynamic)
                continue;

            lock (s_lock)
            {
                if (!s_initializedAssemblies.Add(assembly))
                    continue;
            }

            try
            {
                InitializeAssembly(assembly);
            }
            catch
            {
                lock (s_lock)
                {
                    s_initializedAssemblies.Remove(assembly);
                }

                throw;
            }
        }
    }

    private static void InitializeAssembly(Assembly assembly)
    {
        for (int i = 0; i < s_bootstrapTypeNames.Length; i++)
        {
            string typeName = s_bootstrapTypeNames[i];
            Type? bootstrapType = assembly.GetType(
                typeName,
                throwOnError: false,
                ignoreCase: false);

            if (bootstrapType == null)
                continue;

            try
            {
                RuntimeHelpers.RunClassConstructor(
                    bootstrapType.TypeHandle);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Failed to initialize generated LayerBase bootstrap " +
                    $"`{typeName}` from assembly " +
                    $"`{assembly.FullName}`.",
                    exception);
            }
        }
    }
}
