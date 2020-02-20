// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using TypeKitchen;

namespace ActiveRoutes.Internal.Providers
{
	internal sealed class DynamicApplicationModelProvider : IApplicationModelProvider
	{
		private readonly IServiceProvider _serviceProvider;

		public DynamicApplicationModelProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

		public void OnProvidersExecuting(ApplicationModelProviderContext context)
		{
			SetServiceProviders(context);
		}

		public void OnProvidersExecuted(ApplicationModelProviderContext context)
		{
		}

		public int Order { get; set; }

		private void SetServiceProviders(ApplicationModelProviderContext context)
		{
			var application = context.Result;
			
			for (var i = application.Controllers.Count - 1; i >= 0; i--)
			{
				var controllerModel = application.Controllers[i];
				
				foreach (var o in controllerModel.Attributes)
					if (o is IDynamicAttribute attribute)
						attribute.ServiceProvider = _serviceProvider;

				foreach (var a in controllerModel.Actions)
				foreach (var o in a.Attributes)
					if (o is IDynamicAttribute attribute)
						attribute.ServiceProvider = _serviceProvider;

				AnnotateMissingDynamicAttributes(application, i);
			}
		}

		private void AnnotateMissingDynamicAttributes(ApplicationModel application, int i)
		{
			var controller = application.Controllers[i];

			var found = false;
			foreach (var attribute in controller.Attributes)
				if (attribute is DynamicControllerAttribute)
				{
					found = true;
					break;
				}

			if (found)
				return;

			var dynamicallyAdded = TypeDescriptor.GetAttributes(controller.ControllerType)
				.OfType<DynamicControllerAttribute>().AsList();

			if (dynamicallyAdded.Count == 0)
				return;

			foreach (var attribute in dynamicallyAdded)
				attribute.ServiceProvider = _serviceProvider;

			var attributes = new List<object>(controller.Attributes);
			attributes.InsertRange(0, dynamicallyAdded);

			var clone = CloneControllerWithAttributes(controller, attributes);
			clone.Filters.Insert(0, new AuthorizeFilter(dynamicallyAdded));

			application.Controllers.RemoveAt(i);
			application.Controllers.Insert(i, clone);
		}

		private static ControllerModel CloneControllerWithAttributes(ControllerModel controller, IReadOnlyList<object> attributes)
		{
			var clone = new ControllerModel(controller.ControllerType, attributes)
			{
				ControllerName = controller.ControllerName,
				Application = controller.Application,
				ApiExplorer = new ApiExplorerModel(controller.ApiExplorer)
			};
			foreach (var routeValue in controller.RouteValues)
				clone.RouteValues.Add(routeValue);
			foreach (var property in controller.Properties)
				clone.Properties.Add(property);
			foreach (var filter in controller.Filters)
				clone.Filters.Add(filter);
			foreach (var property in controller.ControllerProperties)
				clone.ControllerProperties.Add(new PropertyModel(property));
			foreach (var action in controller.Actions)
			{
				var actionModel = new ActionModel(action);
				clone.Actions.Add(actionModel);
			}
			foreach (var selector in controller.Selectors)
			{
				var selectorModel = new SelectorModel(selector);
				for (var i = attributes.Count - 1; i >= 0; i--)
				{
					var attribute = attributes[i];
					if (selectorModel.EndpointMetadata.Contains(attribute))
						continue;
					selectorModel.EndpointMetadata.Insert(0, attribute);
				}

				clone.Selectors.Add(selectorModel);
			}

			return clone;
		}
	}
}