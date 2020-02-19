// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using ActiveRoutes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.RuntimeFeature
{
    public static class Add
    {
        public static RuntimeBuilder AddRuntimeApi<T>(this IMvcCoreBuilder mvcBuilder, IConfiguration config)
        {
            mvcBuilder.Services.Configure<RuntimeOptions>(config, o => { o.BindNonPublicProperties = false; });

            return mvcBuilder.AddActiveRoute<RuntimeBuilder, RuntimeController<T>, RuntimeFeature, RuntimeOptions>();
        }

        public static RuntimeBuilder AddRuntimeApi<T>(this IMvcCoreBuilder mvcBuilder,
            Action<RuntimeOptions> configureAction = null)
        {
            if (configureAction != null)
                mvcBuilder.Services.Configure(configureAction);

            return mvcBuilder.AddActiveRoute<RuntimeBuilder, RuntimeController<T>, RuntimeFeature, RuntimeOptions>();
        }
    }
}