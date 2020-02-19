// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

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
			foreach (var controllerModel in context.Result.Controllers)
			{
				foreach (var o in controllerModel.Attributes)
					if (o is IDynamicAttribute attribute)
						attribute.ServiceProvider = _serviceProvider;

				foreach (var a in controllerModel.Actions)
				foreach (var o in a.Attributes)
					if (o is IDynamicAttribute attribute)
						attribute.ServiceProvider = _serviceProvider;
			}
		}
	}
}