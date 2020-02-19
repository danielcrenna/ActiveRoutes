// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using ActiveRoutes;
using Microsoft.AspNetCore.Mvc;

namespace Demo.RuntimeFeature
{
	[DynamicController(typeof(RuntimeOptions))]
	public class RuntimeController<T> : Controller
	{
		[DynamicHttpGet("env/name")]
		public IActionResult GetEnvironmentMachineName()
		{
			return Ok($"{Environment.MachineName}.{typeof(T).Name}");
		}
	}
}