// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Demo.Configuration;
using Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Demo.Controllers
{
    public class TokenController : Controller
    {
        private readonly JwtSecurityTokenHandler _handler;
        private readonly IOptionsSnapshot<TokenOptions> _options;

        public TokenController(IOptionsSnapshot<TokenOptions> options)
        {
            _options = options;
            _handler = new JwtSecurityTokenHandler();
        }

        [HttpPost("token")]
        public IActionResult GenerateToken([FromBody] TokenRequestModel model)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, model.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Value.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_options.Value.Issuer, _options.Value.Audience, claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            return Ok(new {token = _handler.WriteToken(token)});
        }
    }
}