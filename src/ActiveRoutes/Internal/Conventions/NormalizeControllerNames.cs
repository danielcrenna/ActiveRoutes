// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ActiveRoutes.Internal.Conventions
{
	internal sealed class NormalizeControllerNames : IControllerModelConvention
	{
		public void Apply(ControllerModel controller)
		{
			controller.ControllerName = controller.ControllerType.NormalizeControllerName();
		}
	}
}
