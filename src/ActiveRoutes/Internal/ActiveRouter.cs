// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TypeKitchen;

namespace ActiveRoutes.Internal
{
	internal sealed class ActiveRouter : DynamicRouteValueTransformer
	{
		private static readonly string NotFoundController;
		private static readonly string NotFoundAction;

		static ActiveRouter()
		{
			NotFoundController = typeof(NotFoundController).NormalizeControllerName()?.ToLowerInvariant();
			NotFoundAction = nameof(Internal.NotFoundController.RouteNotFound).ToLowerInvariant();
		}

		public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext,
			RouteValueDictionary values)
		{
			if (!values.TryGetValue("route", out var route) || !(route is string routeValue) ||
			    string.IsNullOrWhiteSpace(routeValue))
				return new ValueTask<RouteValueDictionary>(values);

			values.Remove("route");

			foreach (var component in httpContext.RequestServices.GetServices<IDynamicComponent>())
			{
				if (component.GetRouteTemplate == null)
					continue;

				var prefix = component.GetRouteTemplate();
				if (prefix.StartsWith('/'))
					prefix = prefix.Substring(1);

				if (!routeValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					continue;

				var action = routeValue.Replace($"{prefix}/", string.Empty);

				foreach (var controllerType in component.ControllerTypes)
				{
					if (!IsValidForRequest(controllerType, httpContext.RequestServices))
						continue;

					var methods = AccessorMembers.Create(controllerType, AccessorMemberTypes.Methods,
						AccessorMemberScope.Public);

					foreach (var method in methods)
					foreach (var attribute in method.Attributes)
					{
						if (!(attribute is DynamicHttpMethodAttribute httpMethod))
							continue;
						if (!httpMethod.Template.Equals(action, StringComparison.OrdinalIgnoreCase))
							continue;

						var controller = controllerType.NormalizeControllerName()?.ToLowerInvariant();
						values["controller"] = controller;
						values["action"] = method.Name;
						return new ValueTask<RouteValueDictionary>(values);
					}
				}
			}

			return NotFound(values);
		}

		private static bool IsValidForRequest(ICustomAttributeProvider controllerType, IServiceProvider serviceProvider)
		{
			if (!controllerType.TryGetAttributes<DynamicControllerAttribute>(true, out var attributes))
				return true;
			foreach (var attribute in attributes)
				if (!IsEnabled(serviceProvider, attribute.FeatureToggleType, attribute.FeatureToggleTypeSegments))
					return false;
			return true;
		}

		private static ValueTask<RouteValueDictionary> NotFound(RouteValueDictionary values)
		{
			values["controller"] = NotFoundController;
			values["action"] = NotFoundAction;
			return new ValueTask<RouteValueDictionary>(values);
		}

		#region IFeatureToggle [Enabled]

		public static bool IsEnabled(IServiceProvider serviceProvider, Type featureProviderType, string[] segments)
		{
			if (serviceProvider == null)
				return false; // don't attempt to resolve if opted-out/disabled

			var optionsType = typeof(IOptionsMonitor<>).MakeGenericType(featureProviderType);
			var options = serviceProvider.GetRequiredService(optionsType);

			var currentValueProperty = optionsType.GetProperty(nameof(IOptionsMonitor<object>.CurrentValue));
			var currentValue = currentValueProperty?.GetValue(options);

			var reads = ReadAccessor.Create(featureProviderType, out var members);
			if (!members.TryGetValue(nameof(IFeatureToggle.Enabled), out _))
			{
				currentValue = WalkFeatureRecursive(0, currentValue, reads, members, segments);

				reads = ReadAccessor.Create(currentValue, out members);
				members.TryGetValue(nameof(IFeatureToggle.Enabled), out _);
			}

			if (!reads.TryGetValue(currentValue, nameof(IFeatureToggle.Enabled), out var enabled))
				return false;
			return enabled is bool toggle && toggle;
		}

		private static object WalkFeatureRecursive(int segmentIndex, object currentValue, IReadAccessor reads,
			AccessorMembers members, string[] segments)
		{
			foreach (var member in members)
			{
				var key = member.Name;

				if (segments.Length < segmentIndex + 1 ||
				    segments[segmentIndex] != key ||
				    !member.CanRead ||
				    !reads.TryGetValue(currentValue, key, out var segment))
					continue;

				if (segment is IFeatureToggle featureToggle)
				{
					currentValue = featureToggle;
					return currentValue;
				}

				currentValue = segment;
				segmentIndex++;
				var segmentReads = ReadAccessor.Create(segment, out var segmentMembers);
				WalkFeatureRecursive(segmentIndex, segment, segmentReads, segmentMembers, segments);
			}

			return currentValue;
		}

		#endregion
	}
}