// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ActiveRoutes
{
	public sealed class DynamicHttpOptionsAttribute : DynamicHttpMethodAttribute
	{
		private static readonly string[] Methods = {global::HttpMethods.Options};
		public DynamicHttpOptionsAttribute(): base(Methods) { }
		public DynamicHttpOptionsAttribute(string template) : base(Methods, template) { }
	}
}