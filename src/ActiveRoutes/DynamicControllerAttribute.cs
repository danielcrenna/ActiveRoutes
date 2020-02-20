// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace ActiveRoutes
{
	public sealed class DynamicControllerAttribute : DynamicAuthorizeAttribute
	{
		public DynamicControllerAttribute(Type featureToggleType, string[] featureToggleTypeSegments = null,
			Type policyProviderType = null, string[] policyProviderTypeSegments = null) : base(policyProviderType ?? featureToggleType, policyProviderTypeSegments ?? featureToggleTypeSegments)
		{
			FeatureToggleType = featureToggleType;
			FeatureToggleTypeSegments = featureToggleTypeSegments ?? new string[0];
		}

		public Type FeatureToggleType { get; }
		public string[] FeatureToggleTypeSegments { get; }
	}
}