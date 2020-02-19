// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ActiveRoutes.Internal.Providers
{
	internal sealed class DynamicAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
	{
		private readonly AuthorizationOptions _options;

		public DynamicAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) =>
			_options = options.Value;

		public override async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
		{
			AuthorizationPolicy policy;

			if (policyName != Constants.Security.Policies.NoPolicy)
			{
				policy = await base.GetPolicyAsync(policyName);
				if (policy != null)
					return policy;
			}

			policy = await base.GetPolicyAsync(Constants.Security.Policies.NoPolicy);
			if (policy != null)
				return policy;

			policy = new AuthorizationPolicyBuilder()
				.AddAuthenticationSchemes(Constants.Security.Schemes.NoScheme)
				.RequireAssertion(context => true)
				.Build();

			_options.AddPolicy(policyName, policy);

			return policy;
		}
	}
}