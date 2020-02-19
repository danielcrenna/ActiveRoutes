// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ActiveRoutes.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using TypeKitchen;

namespace ActiveRoutes
{
	public sealed class DynamicControllerAttribute : Attribute, IControllerModelConvention, IAuthorizeData,
		IDynamicAttribute
	{
		private readonly Type _policyProviderType;
		private readonly string[] _segments;

		public DynamicControllerAttribute(Type featureToggleType, string[] featureToggleTypeSegments = null,
			Type policyProviderType = null, string[] policyProviderTypeSegments = null)
		{
			featureToggleTypeSegments ??= new string[0];

			FeatureToggleType = featureToggleType;
			FeatureToggleTypeSegments = featureToggleTypeSegments;

			_policyProviderType = policyProviderType ?? featureToggleType;
			_segments = policyProviderTypeSegments ?? featureToggleTypeSegments;
		}

		public Type FeatureToggleType { get; }
		public string[] FeatureToggleTypeSegments { get; }

		public void Apply(ControllerModel controller)
		{
			controller.ControllerName = controller.ControllerType.NormalizeControllerName();
		}

		#region IAuthorizeData

		public string Roles { get; set; }
		public string AuthenticationSchemes { get; set; }

		public string Policy
		{
			get => Resolve(ServiceProvider);
			set => throw new NotSupportedException("Dynamic authorization does not support directly setting policy.");
		}

		public IServiceProvider ServiceProvider { get; set; }

		public string Resolve(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
				return null; // don't attempt to resolve if opted-out/disabled

			var currentPolicyOptions = serviceProvider.GetOptionsMonitorValueForType(_policyProviderType);

			object policy = null;
			var policyProperty = _policyProviderType.GetProperty("Policy");
			if (policyProperty == null)
			{
				var reads = ReadAccessor.Create(_policyProviderType, out var members);
				currentPolicyOptions = WalkPoliciesRecursive(0, currentPolicyOptions, reads, members, ref policy);
			}
			else
			{
				var schemeProperty = _policyProviderType.GetProperty("Scheme");
				if (schemeProperty != null)
				{
					var scheme = schemeProperty.GetValue(currentPolicyOptions);
					AuthenticationSchemes = scheme as string ?? Constants.Security.Schemes.NoScheme;
				}
			}

			GuardAgainstUnregisteredSchemes();

			policy ??= policyProperty?.GetValue(currentPolicyOptions);
			return policy as string ?? Constants.Security.Policies.NoPolicy;
		}

		private void GuardAgainstUnregisteredSchemes()
		{
			if (AuthenticationSchemes == null)
				return;

			// IAuthenticationSchemeProvider is never updated once built, so we need to look in configuration
			var schemeProvider = ServiceProvider.GetService<IAuthenticationSchemeProvider>();
			if (schemeProvider == null)
				return;

			var declared = AuthenticationSchemes.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
			foreach (var scheme in declared)
			{
				var s = schemeProvider.GetSchemeAsync(scheme).GetAwaiter().GetResult();
				if (s != null)
					continue;

				schemeProvider.AddScheme(scheme == Constants.Security.Schemes.NoScheme
					? new AuthenticationScheme(scheme, "No scheme proxy.", typeof(NoAuthenticationHandler))
					: new AuthenticationScheme(scheme, "Missing scheme proxy.", typeof(MissingAuthenticationHandler)));
			}
		}

		private object WalkPoliciesRecursive(int segmentIndex, object currentValue, IReadAccessor reads,
			AccessorMembers members, ref object policy)
		{
			foreach (var member in members)
			{
				var key = member.Name;

				if (_segments.Length < segmentIndex + 1 ||
				    _segments[segmentIndex] != key ||
				    !member.CanRead ||
				    !reads.TryGetValue(currentValue, key, out var segment))
					continue;

				if (segment is IFeatureScheme featureScheme)
					AuthenticationSchemes = featureScheme.Scheme;

				if (segment is IFeaturePolicy featurePolicy)
					policy = featurePolicy.Policy;

				currentValue = segment;
				segmentIndex++;
				var segmentReads = ReadAccessor.Create(segment, out var segmentMembers);
				WalkPoliciesRecursive(segmentIndex, segment, segmentReads, segmentMembers, ref policy);
			}

			return currentValue;
		}

		/// <summary>
		///     Fills in for a missing authentication scheme handler.
		///     Prevents a runtime exception when schemes may be missing since they are declared as optional.
		/// </summary>
		internal sealed class NoAuthenticationHandler : IAuthenticationHandler
		{
			private HttpContext _context;
			private AuthenticationScheme _scheme;

			public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
			{
				_scheme = scheme;
				_context = context;

				return Task.CompletedTask;
			}

			public Task<AuthenticateResult> AuthenticateAsync()
			{
				var principal = ClaimsPrincipal.Current ?? new ClaimsPrincipal();
				var ticket = new AuthenticationTicket(principal, _scheme.Name);
				return Task.FromResult(AuthenticateResult.Success(ticket));
			}

			public Task ChallengeAsync(AuthenticationProperties properties)
			{
				return Task.CompletedTask;
			}

			public Task ForbidAsync(AuthenticationProperties properties)
			{
				_context.Response.StatusCode = 403;
				return Task.CompletedTask;
			}
		}

		/// <summary>
		///     Fills in for a declared, but not registered, authentication scheme handler.
		///     Prevents a startup exception when schemes are incorrectly stated.
		/// </summary>
		internal sealed class MissingAuthenticationHandler : IAuthenticationHandler
		{
			private HttpContext _context;
			private AuthenticationScheme _scheme;

			public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
			{
				_scheme = scheme;
				_context = context;
				return Task.CompletedTask;
			}

			public Task<AuthenticateResult> AuthenticateAsync()
			{
				return Task.FromResult(
					AuthenticateResult.Fail($"Scheme '{_scheme.Name}' was declared, but is not registered."));
			}

			public Task ChallengeAsync(AuthenticationProperties properties)
			{
				_context.Response.StatusCode = 401;
				_context.Response.Headers.TryAdd(HeaderNames.WWWAuthenticate, _scheme.Name);
				return Task.CompletedTask;
			}

			public Task ForbidAsync(AuthenticationProperties properties)
			{
				_context.Response.StatusCode = 403;
				return Task.CompletedTask;
			}
		}

		#endregion
	}
}