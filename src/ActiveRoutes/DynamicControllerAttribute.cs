// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using ActiveRoutes.Internal;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ActiveRoutes
{
	public sealed class DynamicControllerAttribute : DynamicAuthorizeAttribute, IControllerModelConvention, IDynamicAttribute
	{
		public DynamicControllerAttribute(Type featureToggleType, string[] featureToggleTypeSegments = null,
			Type policyProviderType = null, string[] policyProviderTypeSegments = null) : base(policyProviderType ?? featureToggleType, policyProviderTypeSegments ?? featureToggleTypeSegments)
		{
			FeatureToggleType = featureToggleType;
			FeatureToggleTypeSegments = featureToggleTypeSegments ?? new string[0];
		}

		public Type FeatureToggleType { get; }
		public string[] FeatureToggleTypeSegments { get; }

		public void Apply(ControllerModel controller)
		{
			controller.ControllerName = controller.ControllerType.NormalizeControllerName();
		}
	}
}