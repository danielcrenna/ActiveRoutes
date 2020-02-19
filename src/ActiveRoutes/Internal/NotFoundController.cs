// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ActiveRoutes.Internal
{
	internal sealed class NotFoundController : Controller
	{
		public IActionResult RouteNotFound()
		{
			return NotFound();
		}
	}
}