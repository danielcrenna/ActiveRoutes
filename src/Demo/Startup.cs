// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using ActiveRoutes;
using ActiveRoutes.Meta;
using Demo.RuntimeFeature;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Demo
{
	public class Startup
	{
		private readonly IConfiguration _configuration;

		public Startup(IConfiguration configuration) => _configuration = configuration;

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			var config = _configuration.GetSection("Token");
			services.Configure<TokenOptions>(config);
			services.AddAuthentication()
				.AddJwtBearer(o =>
				{
					var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Key"]));

					o.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateIssuerSigningKey = true,
						ValidAudience = config["Audience"],
						ValidIssuer = config["Issuer"],
						IssuerSigningKey = signingKey
					};
				});

			services.AddActiveRouting(mvcBuilder =>
			{
				mvcBuilder.AddAuthorization(options =>
					options.AddPolicy("AuthenticatedUser", b => { b.RequireAuthenticatedUser(); }));

				mvcBuilder.AddRuntimeApi<Startup>(_configuration.GetSection("RuntimeFeature"));
			});

			services.AddMetaApi(_configuration.GetSection("MetaFeature"));
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
				app.UseDeveloperExceptionPage();

			app.UseActiveRouting();
		}
	}
}