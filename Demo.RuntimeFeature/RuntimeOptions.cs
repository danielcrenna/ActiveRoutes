// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ActiveRoutes;

namespace Demo.RuntimeFeature
{
    public class RuntimeOptions :
        IFeatureToggle,
        IFeatureNamespace,
        IFeatureScheme,
        IFeaturePolicy
    {
        public string RootPath { get; set; } = "/api";
        public string Policy { get; set; } = Constants.Security.Policies.NoPolicy;
        public string Scheme { get; set; } = Constants.Security.Schemes.NoScheme;
        public bool Enabled { get; set; } = true;
    }
}