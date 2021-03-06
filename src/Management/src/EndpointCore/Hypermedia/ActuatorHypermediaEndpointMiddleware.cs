﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.EndpointCore.ContentNegotiation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public class ActuatorHypermediaEndpointMiddleware : EndpointMiddleware<Links, string>
    {
        private readonly RequestDelegate _next;

        public ActuatorHypermediaEndpointMiddleware(RequestDelegate next, ActuatorEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ActuatorHypermediaEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions.OfType<ActuatorManagementOptions>(), logger: logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger?.LogDebug("Invoke({0} {1})", context.Request.Method, context.Request.Path.Value);

            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await HandleCloudFoundryRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        protected internal async Task HandleCloudFoundryRequestAsync(HttpContext context)
        {
            var serialInfo = HandleRequest(GetRequestUri(context.Request));
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
        }

        protected internal string GetRequestUri(HttpRequest request)
        {
            string scheme = request.Scheme;

            if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
            {
                scheme = headerScheme.ToString();
            }

            return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
        }
    }
}
