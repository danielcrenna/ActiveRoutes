// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using ActiveRoutes.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TypeKitchen.Creation;

namespace ActiveRoutes
{
    public static class MvcBuilderExtensions
    {
        public static TBuilder AddActiveRoute<TBuilder, TController, TComponent, TComponentOptions>(
            this IMvcCoreBuilder mvcBuilder)
            where TComponent : class, IDynamicComponent
            where TComponentOptions : class, IFeatureNamespace
        {
            // See: https://github.com/aspnet/Mvc/issues/5992
            mvcBuilder.AddApplicationPart(typeof(TController).Assembly);
            mvcBuilder.ConfigureApplicationPartManager(x =>
            {
                x.ApplicationParts.Add(
                    new DynamicControllerApplicationPart(new[] {typeof(TController).GetTypeInfo()}));
            });

            mvcBuilder.Services.Replace(ServiceDescriptor.Singleton(r =>
            {
                var component = Instancing.CreateInstance<TComponent>();

                for (var i = 0; i < component.ControllerTypes.Count; i++)
                {
                    var controllerType = component.ControllerTypes[i];
                    if (controllerType.IsGenericType && controllerType.Name == typeof(TController).Name)
                        component.ControllerTypes[i] = typeof(TController);
                }

                component.GetRouteTemplate = () =>
                {
                    var o = r.GetRequiredService<IOptionsMonitor<TComponentOptions>>();
                    return o.CurrentValue.RootPath ?? string.Empty;
                };

                return component;
            }));

            mvcBuilder.Services.AddSingleton<IDynamicComponent>(r => r.GetService<TComponent>());

            mvcBuilder.AddAuthorization(x =>
            {
                if (x.GetPolicy(Constants.Security.Policies.NoPolicy) == null)
                    x.AddPolicy(Constants.Security.Policies.NoPolicy, b => { b.RequireAssertion(context => true); });
            });

            return Instancing.CreateInstance<TBuilder>(mvcBuilder.Services);
        }
    }
}