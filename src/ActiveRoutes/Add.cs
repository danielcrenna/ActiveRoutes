// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using ActiveRoutes.Internal;
using ActiveRoutes.Internal.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ActiveRoutes
{
	public static class Add
	{
		public static IServiceCollection AddActiveRouting(this IServiceCollection services,
			Action<IMvcCoreBuilder> builderAction)
		{
			services.AddAuthenticationCore();

			var mvcBuilder = services.AddMvcCore();

			mvcBuilder.Services.TryAddEnumerable(ServiceDescriptor
				.Transient<IApplicationModelProvider, DynamicApplicationModelProvider>());
			mvcBuilder.Services.Replace(ServiceDescriptor
				.Singleton<IAuthorizationPolicyProvider, DynamicAuthorizationPolicyProvider>());

			services.AddSingleton<ActiveRouter>();

			builderAction?.Invoke(mvcBuilder);
			return services;
		}
	}
}