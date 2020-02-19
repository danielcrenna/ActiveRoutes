// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace ActiveRoutes
{
    public interface IDynamicComponent
    {
        IList<Type> ControllerTypes { get; }
        Func<string> GetRouteTemplate { get; set; }
    }
}