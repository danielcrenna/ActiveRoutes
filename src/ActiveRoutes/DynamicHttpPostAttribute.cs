// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ActiveRoutes
{
	public sealed class DynamicHttpPostAttribute : DynamicHttpMethodAttribute
	{
		private static readonly string[] Methods = {global::HttpMethods.Post};
		public DynamicHttpPostAttribute(): base(Methods) { }
		public DynamicHttpPostAttribute(string template) : base(Methods, template) { }
	}
}