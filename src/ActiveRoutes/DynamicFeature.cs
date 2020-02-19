// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace ActiveRoutes
{
	public abstract class DynamicFeature : IDynamicComponent
	{
		public abstract IList<Type> ControllerTypes { get; }
		public Func<string> GetRouteTemplate { get; set; }
	}
}