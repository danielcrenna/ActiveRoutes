// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TypeKitchen;

namespace ActiveRoutes
{
	public static class FeatureExtensions
	{
		public static bool FeatureEnabled<TFeature>(this HttpContext context, out TFeature feature)
			where TFeature : class, IFeatureToggle
		{
			return context.RequestServices.FeatureEnabled(out feature);
		}

		public static bool FeatureEnabled<TFeature, TOptions>(this HttpContext context, out TFeature feature)
			where TFeature : class, IFeatureToggle
			where TOptions : class, new()
		{
			return context.RequestServices.FeatureEnabled<TFeature, TOptions>(out feature);
		}

		public static bool FeatureEnabled<TFeature, TOptions>(this IApplicationBuilder appBuilder, out TFeature feature)
			where TFeature : class, IFeatureToggle
			where TOptions : class, new()
		{
			return appBuilder.ApplicationServices.FeatureEnabled<TFeature, TOptions>(out feature);
		}

		public static bool FeatureEnabled<TFeature>(this IServiceProvider serviceProvider, out TFeature feature)
			where TFeature : class, IFeatureToggle
		{
			var options = serviceProvider.GetService(typeof(IOptionsMonitor<TFeature>));
			if (!(options is IOptionsMonitor<TFeature> o))
			{
				feature = default;
				return false;
			}

			feature = o.CurrentValue;
			return feature != null && feature.Enabled;
		}

		public static bool FeatureEnabled<TFeature, TOptions>(this IServiceProvider serviceProvider,
			out TFeature feature)
			where TFeature : class, IFeatureToggle
			where TOptions : class, new()
		{
			var options = serviceProvider.GetService(typeof(IOptionsMonitor<TOptions>));
			if (!(options is IOptionsMonitor<TOptions> o))
			{
				feature = default;
				return false;
			}

			var type = o.CurrentValue.GetType();
			var members = AccessorMembers.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public);
			var featureType = members.SingleOrDefault(x => x.Type == typeof(TFeature));
			if (featureType == null)
			{
				feature = default;
				return false;
			}

			var accessor = ReadAccessor.Create(type);
			feature = accessor[o.CurrentValue, featureType.Name] as TFeature;
			return feature != null && feature.Enabled;
		}
	}
}