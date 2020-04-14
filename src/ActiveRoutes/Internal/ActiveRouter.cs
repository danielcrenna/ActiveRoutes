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
using Microsoft.AspNetCore.Mvc.ActionConstraints;
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
		private readonly IOptions<MvcOptions> _options;
		private static readonly string NotFoundController;
		private static readonly string NotFoundAction;

		static ActiveRouter()
		{
			NotFoundController = typeof(NotFoundController).NormalizeControllerName()?.ToLowerInvariant();
			NotFoundAction = nameof(Internal.NotFoundController.RouteNotFound).ToLowerInvariant();
		}

		public ActiveRouter(IOptions<MvcOptions> options)
		{
			_options = options;
		}

		public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
		{
			const string routeKey = "route";

			if (!values.TryGetValue(routeKey, out var route) || !(route is string routeValue) ||
			    string.IsNullOrWhiteSpace(routeValue))
				return new ValueTask<RouteValueDictionary>(values);

			values.Remove(routeKey);

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

				var action = prefix == string.Empty? routeValue : routeValue.Replace($"{prefix}/", string.Empty);

				foreach (var controllerType in component.ControllerTypes)
				{
					if (!IsValidForRequest(controllerType, httpContext))
						continue;

					var methods = AccessorMembers.Create(controllerType, AccessorMemberTypes.Methods,
						AccessorMemberScope.Public);

					foreach (var method in methods)
					foreach (var attribute in method.Attributes)
					{
						if (attribute is NonActionAttribute)
							continue;

						if (!(attribute is DynamicHttpMethodAttribute httpMethod))
							continue;

						if (!httpMethod.HttpMethods.Contains(httpContext.Request.Method, StringComparer.OrdinalIgnoreCase))
							continue;

						var template = GetHttpTemplate(httpMethod, method);
						if (template == string.Empty)
							template = prefix;

						if (!IsMatch(template, $"/{action}", httpContext, method, out var extraValues))
							continue;
						
						values["controller"] = ResolveControllerName(controllerType);
						values["action"] = ResolveActionName(method);

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

		// ASP.NET Core MVC conventionally removes "Controller" from the name of the class
		private static string ResolveControllerName(Type controllerType)
		{
			var controllerName = controllerType.NormalizeControllerName()?.ToLowerInvariant();
			return controllerName;
		}

		// ASP.NET Core MVC conventionally removes "Async" from the end of class methods
		private string ResolveActionName(AccessorMember method)
		{
			const string suffix = "Async";

			return _options.Value.SuppressAsyncSuffixInActionNames &&
			       method.Name.EndsWith(suffix, StringComparison.Ordinal)
				? method.Name.Substring(0, method.Name.Length - suffix.Length)
				: method.Name;
		}

		private static readonly IDictionary<string, RouteTemplate> Templates = new Dictionary<string, RouteTemplate>();
		private static readonly IDictionary<string, RouteValueDictionary> Defaults = new Dictionary<string, RouteValueDictionary>();
		private static readonly IDictionary<string, TemplateMatcher> Matchers = new Dictionary<string, TemplateMatcher>();

		public bool IsMatch(string template, PathString action, HttpContext httpContext, AccessorMember method, out RouteValueDictionary values)
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
			
			if (!matcher.TryMatch(action, values))
				return false;

			var constraints = method.Attributes.OfType<IActionConstraint>().OrderBy(x => x.Order).AsList();
			if (constraints.Count > 0)
			{
				var context = new ActionConstraintContext {RouteContext = new RouteContext(httpContext)};
				foreach (var constraint in constraints)
				{
					if (!constraint.Accept(context))
						return false;
				}
			}

			return true;
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
			if(member.DeclaringType != null)
			{
				if (member.DeclaringType.TryGetAttribute<RouteAttribute>(true, out var routeAttribute))
				{
					var baseTemplate = routeAttribute.Template;

					return !string.IsNullOrWhiteSpace(httpMethod.Template)
						? $"{baseTemplate}/{httpMethod.Template}"
						: baseTemplate;
				}
			}

			if (httpMethod.Template == string.Empty)
				return string.Empty;

			return !string.IsNullOrWhiteSpace(httpMethod.Template) ? httpMethod.Template : default;
		}

		private static bool IsValidForRequest(ICustomAttributeProvider controllerType, HttpContext httpContext)
		{
			IsFeatureEnabled(controllerType, httpContext.RequestServices, out var isFeatureEnabled);
			return isFeatureEnabled;
		}

		private static bool IsFeatureEnabled(ICustomAttributeProvider controllerType, IServiceProvider serviceProvider,
			out bool isValidForRequest)
		{
			var attributes = TypeDescriptor.GetAttributes(controllerType).OfType<DynamicControllerAttribute>().AsList();
			if (attributes.FirstOrDefault() == null)
			{
				isValidForRequest = true;
				return true;
			}

			foreach (var attribute in attributes)
				if (!IsEnabled(serviceProvider, attribute.FeatureToggleType, attribute.FeatureToggleTypeSegments))
				{
					isValidForRequest = false;
					return true;
				}

			isValidForRequest = false;
			return false;
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