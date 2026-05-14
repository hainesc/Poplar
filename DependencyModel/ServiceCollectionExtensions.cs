// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Poplar.ViewModels;

namespace Poplar.DependencyModel;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransientFromNamespace(
        this IServiceCollection services,
        string namespaceName,
        params Assembly[] assemblies
    )
    {
        foreach (Assembly assembly in assemblies)
        {
            try
            {
                var types = assembly
                    .GetTypes()
                    .Where(x =>
                        x.IsClass
                        && !x.IsAbstract
                        && x.Namespace != null
                        && x.Namespace.StartsWith(namespaceName, StringComparison.InvariantCultureIgnoreCase)
                        && (x.Name.EndsWith("Page") || x.Name.EndsWith("ViewModel"))
                    );

                foreach (Type type in types)
                {
                    if (services.All(x => x.ServiceType != type))
                    {
                        if (type == typeof(ViewModel))
                        {
                            continue;
                        }

                        _ = services.AddTransient(type);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DependencyInjection] Failed to load types from {assembly.FullName}: {ex.Message}");
            }
        }

        return services;
    }
}
