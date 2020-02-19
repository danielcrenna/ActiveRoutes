// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ActiveRoutes.Internal
{
    internal static class ServiceProviderExtensions
    {
        public static object GetOptionsMonitorValueForType(this IServiceProvider serviceProvider, Type componentType)
        {
            var optionsMonitorType = typeof(IOptionsMonitor<>).MakeGenericType(componentType);
            var optionsMonitor = serviceProvider?.GetRequiredService(optionsMonitorType);
            if (optionsMonitor == null)
                return null;
            var currentValueProperty = optionsMonitorType.GetProperty(nameof(IOptionsMonitor<object>.CurrentValue));
            var currentValue = currentValueProperty?.GetValue(optionsMonitor);
            return currentValue;
        }
    }
}