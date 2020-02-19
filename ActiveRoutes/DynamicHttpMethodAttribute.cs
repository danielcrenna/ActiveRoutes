// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ActiveRoutes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class DynamicHttpMethodAttribute : Attribute, IActionHttpMethodProvider
    {
        protected DynamicHttpMethodAttribute(IEnumerable<string> httpMethods, string template)
        {
            HttpMethods = httpMethods;
            Template = template;
        }

        public string Name { get; set; }
        public string Template { get; }

        public IEnumerable<string> HttpMethods { get; }
    }
}