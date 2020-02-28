// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
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

			var components = httpContext.RequestServices.GetServices<IDynamicComponent>();

			foreach (var component in components)
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

						if (!httpMethod.HttpMethods.Contains(httpContext.Request.Method, StringComparer.OrdinalIgnoreCase))
							continue;

						var template = GetHttpTemplate(httpMethod, method);
						if (!IsMatch(template, $"/{action}", out var extraValues))
							continue;

						var lastIndex = method.Name.LastIndexOf("Async", StringComparison.OrdinalIgnoreCase);

						// ASP.NET Core MVC conventionally removes "Controller" from the name of the class
						var controllerName = controllerType.NormalizeControllerName()?.ToLowerInvariant();

						// ASP.NET Core MVC conventionally removes "Async" from the end of class methods
						var actionName = lastIndex == -1 ? method.Name : method.Name.Substring(0, lastIndex);

						values["controller"] = controllerName;
						values["action"] = actionName;

						if (extraValues != null && extraValues.Count > 0)
						{
							foreach (var (k, v) in extraValues)
								values[k] = v;
						}

						return new ValueTask<RouteValueDictionary>(values);
					}
				}
			}

			return NotFound(values);
		}

		private static readonly IDictionary<string, RouteTemplate> Templates = new Dictionary<string, RouteTemplate>();
		private static readonly IDictionary<string, RouteValueDictionary> Defaults = new Dictionary<string, RouteValueDictionary>();
		private static readonly IDictionary<string, TemplateMatcher> Matchers = new Dictionary<string, TemplateMatcher>();

		public bool IsMatch(string template, PathString action, out RouteValueDictionary values)
		{
			if (template == null)
			{
				values = default;
				return string.IsNullOrWhiteSpace(action);
			}

			if (template.Equals(action, StringComparison.OrdinalIgnoreCase))
			{
				values = default;
				return true;
			}

			if (!Templates.TryGetValue(template, out var parsed))
				Templates.Add(template, parsed = TemplateParser.Parse(template));

			if(!Defaults.TryGetValue(template, out var defaults))
				Defaults.Add(template, defaults = GetDefaultParameters(parsed));

			if(!Matchers.TryGetValue(template, out var matcher))
				Matchers.Add(template, matcher = new TemplateMatcher(parsed, defaults));

			values = new RouteValueDictionary();
			var match = matcher.TryMatch(action, values);
			return match;
		}

		private static RouteValueDictionary GetDefaultParameters(RouteTemplate parsedTemplate)
		{
			var result = new RouteValueDictionary();
			foreach (var parameter in parsedTemplate.Parameters)
				if (parameter.DefaultValue != null)
					result.Add(parameter.Name, parameter.DefaultValue);
			return result;
		}
		
		private static string GetHttpTemplate(DynamicHttpMethodAttribute httpMethod, AccessorMember member)
		{
			if (member.DeclaringType == null || !member.DeclaringType.TryGetAttribute<RouteAttribute>(true, out var routeAttribute))
				return !string.IsNullOrWhiteSpace(httpMethod.Template) ? httpMethod.Template : default;

			var baseTemplate = routeAttribute.Template;
			return !string.IsNullOrWhiteSpace(httpMethod.Template)
				? $"{baseTemplate}/{httpMethod.Template}"
				: baseTemplate;
		}

		private static bool IsValidForRequest(ICustomAttributeProvider controllerType, IServiceProvider serviceProvider)
		{
			var attributes = TypeDescriptor.GetAttributes(controllerType).OfType<DynamicControllerAttribute>().AsList();
			if (attributes.FirstOrDefault() == null)
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
			AccessorMembers members, IReadOnlyList<string> segments)
		{
			foreach (var member in members)
			{
				var key = member.Name;

				if (segments == null ||
				    segments.Count < segmentIndex + 1 ||
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