// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ActiveRoutes.Internal;
using Microsoft.AspNetCore.Builder;

namespace ActiveRoutes
{
    public static class Use
    {
        public static IApplicationBuilder UseActiveRouting(this IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapDynamicControllerRoute<ActiveRouter>("{**route}"); });
            return app;
        }
    }
}