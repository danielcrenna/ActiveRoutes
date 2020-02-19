// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ActiveRoutes
{
	public sealed class DynamicHttpPatchAttribute : DynamicHttpMethodAttribute
	{
		private static readonly string[] Methods = {global::HttpMethods.Patch};
		public DynamicHttpPatchAttribute(): base(Methods) { }
		public DynamicHttpPatchAttribute(string template) : base(Methods, template) { }
	}
}