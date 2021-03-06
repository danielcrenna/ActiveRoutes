﻿// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Reflection;
using ActiveRoutes.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TypeKitchen;
using TypeKitchen.Creation;

namespace ActiveRoutes
{
	public static class MvcBuilderExtensions
	{
		public static TBuilder AddActiveRoute<TBuilder, TController, TFeature, TFeatureOptions>(
			this IMvcCoreBuilder mvcBuilder)
			where TBuilder : IFeatureBuilder
			where TFeature : class, IDynamicFeature
			where TFeatureOptions : class
		{
			AddActiveRouteImpl<TController, TFeature, TFeatureOptions>(mvcBuilder);

			return Instancing.CreateInstance<TBuilder>(mvcBuilder.Services);
		}

		public static IMvcCoreBuilder AddActiveRoute<TController, TFeature, TFeatureOptions>(this IMvcCoreBuilder mvcBuilder)
			where TFeature : class, IDynamicFeature
			where TFeatureOptions : class
		{
			AddActiveRouteImpl<TController, TFeature, TFeatureOptions>(mvcBuilder);

			return mvcBuilder;
		}

		private static void AddActiveRouteImpl<TController, TFeature, TFeatureOptions>(IMvcCoreBuilder mvcBuilder)
			where TFeature : class, IDynamicFeature
			where TFeatureOptions : class
		{
			// Add [DynamicController(typeof(TComponentOptions))] if not present
			if (!typeof(TController).HasAttribute<DynamicControllerAttribute>())
			{
				var attribute = new DynamicControllerAttribute(typeof(TFeatureOptions));
				TypeDescriptor.AddAttributes(typeof(TController), attribute);
				var attributes = TypeDescriptor.GetAttributes(typeof(TController));
				if(!attributes.Contains(attribute))
					throw new InvalidOperationException("Could not add attribute dynamically on this runtime.");
			}

			// See: https://github.com/aspnet/Mvc/issues/5992
			mvcBuilder.AddApplicationPart(typeof(TController).Assembly);
			mvcBuilder.ConfigureApplicationPartManager(x =>
			{
				x.ApplicationParts.Add(new DynamicControllerApplicationPart(new[] {typeof(TController).GetTypeInfo()}));
			});

			var componentDescriptor = ServiceDescriptor.Singleton(r =>
			{
				var component = Instancing.CreateInstance<TFeature>();
				component.GetRouteTemplate = () =>
				{
					var o = r.GetRequiredService<IOptionsMonitor<TFeatureOptions>>();
					return o.CurrentValue is IFeatureNamespace ns ? ns.RootPath ?? string.Empty : string.Empty;
				};
				return component;
			});

			mvcBuilder.Services.Replace(componentDescriptor);
			mvcBuilder.Services.AddTransient<IDynamicFeature>(r =>
			{
				// cached singleton 
				var component = r.GetService<TFeature>();
				
				// each resolution, we could be discovering a different controller that needs hydration into its type
				for (var i = 0; i < component.ControllerTypes.Count; i++)
				{
					var controllerType = component.ControllerTypes[i];
					if (controllerType.IsGenericType && controllerType.Name == typeof(TController).Name)
						component.ControllerTypes[i] = typeof(TController);
				}

				return component;
			});

			mvcBuilder.AddAuthorization(x =>
			{
				if (x.GetPolicy(Constants.Security.Policies.NoPolicy) == null)
					x.AddPolicy(Constants.Security.Policies.NoPolicy, b => { b.RequireAssertion(context => true); });
			});
		}
	}
}