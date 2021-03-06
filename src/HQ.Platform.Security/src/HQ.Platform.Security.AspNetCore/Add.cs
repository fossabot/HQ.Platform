#region LICENSE

// Unless explicitly acquired and licensed from Licensor under another
// license, the contents of this file are subject to the Reciprocal Public
// License ("RPL") Version 1.5, or subsequent versions as allowed by the RPL,
// and You may not copy or use this file in either source code or executable
// form, except in compliance with the terms and conditions of the RPL.
// 
// All software distributed under the RPL is provided strictly on an "AS
// IS" basis, WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, AND
// LICENSOR HEREBY DISCLAIMS ALL SUCH WARRANTIES, INCLUDING WITHOUT
// LIMITATION, ANY WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE, QUIET ENJOYMENT, OR NON-INFRINGEMENT. See the RPL for specific
// language governing rights and limitations under the RPL.

#endregion

using System;
using HQ.Common;
using HQ.Platform.Security.AspNetCore.Extensions;
using HQ.Platform.Security.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HQ.Platform.Security.AspNetCore
{
    public static class Add
    {
        public static IServiceCollection AddSecurityPolicies(this IServiceCollection services, IConfiguration config)
        {
            return AddSecurityPolicies(services, config.Bind);
        }

        public static IServiceCollection AddSecurityPolicies(this IServiceCollection services, Action<SecurityOptions> configureAction = null)
        {
            var options = new SecurityOptions();
            configureAction?.Invoke(options);
            
            services.Configure<SecurityOptions>(o => { configureAction?.Invoke(o); });

            if (options.Tokens.Enabled)
            {
                services.AddAuthentication(options);
            }

            services.AddAuthorization(x =>
            {
                x.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUserExtended(services, options)
                    .Build();

                if (options.SuperUser.Enabled)
                {
                    x.AddPolicy(Constants.Security.Policies.SuperUserOnly,
                        builder => { builder.RequireRoleExtended(services, options, ClaimValues.SuperUser); });
                }
            });

            if (options.Https.Enabled)
            {
                services.AddHttpsRedirection(o =>
                {
                    o.HttpsPort = null;
                    o.RedirectStatusCode = options.Https.Hsts.Enabled ? 307 : 301;
                });

                if (options.Https.Hsts.Enabled)
                {
                    services.AddHsts(o =>
                    {
                        o.MaxAge = options.Https.Hsts.HstsMaxAge;
                        o.IncludeSubDomains = options.Https.Hsts.IncludeSubdomains;
                        o.Preload = options.Https.Hsts.Preload;
                    });
                }
            }
            

            return services;
        }
    }
}
