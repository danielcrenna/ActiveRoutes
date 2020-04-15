// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ActiveRoutes.Internal.Conventions
{
	internal sealed class NormalizeControllerNames : IApplicationModelConvention, IControllerModelConvention
	{
		public void Apply(ControllerModel controller)
		{
			controller.ControllerName = controller.ControllerType.NormalizeControllerName();
		}

		public void Apply(ApplicationModel application)
		{
			var controllers = application.Controllers.ToArray();
			foreach (var controller in controllers)
				Apply(controller);
		}
	}
}
