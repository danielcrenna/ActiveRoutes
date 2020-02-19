// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using TypeKitchen;

namespace ActiveRoutes.Internal
{
    internal static class ControllerTypeExtensions
    {
        public static string NormalizeControllerName(this Type controllerType)
        {
            return controllerType.Name.Contains('`')
                ? GetGenericControllerName(controllerType)
                : controllerType.Name.Replace(nameof(Controller), string.Empty);
        }

        public static string GetGenericControllerName(this Type controllerType)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                if (!controllerType.IsGenericType)
                {
                    sb.Append(controllerType.Name);
                    return;
                }

                var types = controllerType.GetGenericArguments();
                if (types.Length == 0)
                {
                    sb.Append(controllerType.Name);
                    return;
                }

                sb.Append(controllerType.Name.Replace($"{nameof(Controller)}`{types.Length}", string.Empty));
                foreach (var type in types)
                    sb.Append($"_{type.Name}");
            });
        }
    }
}