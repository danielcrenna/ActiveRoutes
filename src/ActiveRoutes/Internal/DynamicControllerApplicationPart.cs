// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace ActiveRoutes.Internal
{
	internal sealed class DynamicControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
	{
		public override string Name => nameof(DynamicControllerApplicationPart);
		public IEnumerable<TypeInfo> Types { get; }
		public DynamicControllerApplicationPart(IEnumerable<TypeInfo> types) => Types = types;
	}
}