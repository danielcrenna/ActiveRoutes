// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using ActiveRoutes;

namespace Demo.RuntimeFeature
{
	public class RuntimeFeature : DynamicFeature
	{
		public RuntimeFeature() => ControllerTypes = new[] {typeof(RuntimeController<>)};

		public override IList<Type> ControllerTypes { get; }
	}
}