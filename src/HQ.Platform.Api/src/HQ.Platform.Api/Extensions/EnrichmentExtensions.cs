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

using System.Collections.Generic;
using System.Net;
using HQ.Common;
using HQ.Common.Helpers;
using HQ.Data.Contracts;
using HQ.Data.Contracts.Configuration;
using HQ.Platform.Api.Configuration;
using HQ.Platform.Api.Models;
using Microsoft.AspNetCore.Http;

namespace HQ.Platform.Api.Extensions
{
    public static class EnrichmentExtensions
    {
        public static void MaybeTrim(this HttpResponse response, HttpRequest request, PublicApiOptions options)
        {
            if (FeatureRequested(request, options.JsonConversion.TrimOperator, options.JsonConversion.TrimEnabled))
            {
                request.HttpContext.Items[Constants.ContextKeys.JsonTrim] = true;
            }
        }

        public static void MaybePrettyPrint(this HttpResponse response, HttpRequest request, PublicApiOptions options)
        {
            if (FeatureRequested(request, options.JsonConversion.PrettyPrintOperator,
                options.JsonConversion.PrettyPrintEnabled))
            {
                request.HttpContext.Items[Constants.ContextKeys.JsonPrettyPrint] = true;
            }
        }

        public static void MaybeEnvelope(this HttpResponse response, HttpRequest request, PublicApiOptions apiOptions,
            QueryOptions queryOptions,
            IPageHeader data, IList<Error> errors, out object body)
        {
            if (FeatureRequested(request, apiOptions.JsonConversion.EnvelopeOperator,
                apiOptions.JsonConversion.EnvelopeEnabled))
            {
                body = new EnvelopeBody
                {
                    Data = data,
                    Status = response.StatusCode,
                    Headers = response.Headers,
                    Paging = new PagingInfo
                    {
                        TotalCount = data.TotalCount,
                        TotalPages = data.TotalPages,
                        NextPage = GetNextPage(request, data, queryOptions),
                        PreviousPage = GetPreviousPage(request, data, queryOptions),
                        LastPage = GetLastPage(request, data, queryOptions)
                    },
                    Errors = errors,
                    HasErrors = errors?.Count > 0
                };

                response.StatusCode = (int) HttpStatusCode.OK;
                return;
            }

            var link = StringBuilderPool.Scoped(sb =>
            {
                if (data.TotalPages > 1)
                {
                    var firstPage = $"<{GetFirstPage(request, data, queryOptions)}>; rel=\"first\"";
                    sb.Append(firstPage);
                }

                if (data.HasNextPage)
                {
                    var nextPage = $"<{GetNextPage(request, data, queryOptions)}>; rel=\"next\"";
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(nextPage);
                }

                if (data.HasPreviousPage)
                {
                    var previousPage = $"<{GetPreviousPage(request, data, queryOptions)}>; rel=\"previous\"";
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(previousPage);
                }

                if (data.TotalPages > 1)
                {
                    var lastPage = $"<{GetLastPage(request, data, queryOptions)}>; rel=\"last\"";
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(lastPage);
                }
            });

            if (link.Length > 0)
            {
                response.Headers.Add(Constants.HttpHeaders.Link, link);
            }

            response.Headers.Add(queryOptions.TotalCountHeader, data.TotalCount.ToString());
            response.Headers.Add(queryOptions.TotalPagesHeader, data.TotalPages.ToString());
            body = new NestedBody
            {
                Data = data,
                Errors = errors,
                HasErrors = errors?.Count > 0
            };
        }

        public static void MaybeEnvelope(this HttpResponse response, HttpRequest request, PublicApiOptions apiOptions,
            QueryOptions queryOptions, object data, IList<Error> errors, out object body)
        {
            if (FeatureRequested(request, apiOptions.JsonConversion.EnvelopeOperator,
                apiOptions.JsonConversion.EnvelopeEnabled))
            {
                body = new EnvelopeBody
                {
                    Data = data,
                    Status = response.StatusCode,
                    Headers = response.Headers,
                    Errors = errors,
                    HasErrors = errors?.Count > 0
                };
            }
            else
            {
                body = new NestedBody
                {
                    Data = data,
                    Errors = errors,
                    HasErrors = errors?.Count > 0
                };
            }

            response.StatusCode = (int) HttpStatusCode.OK;
        }

        internal static string GetPreviousPage(HttpRequest request, IPageHeader header, QueryOptions options)
        {
            return !header.HasPreviousPage
                ? null
                : $"{request.Scheme}://{request.Host}{request.Path}?{options.PageOperator}={header.Index - 1}&{options.PerPageOperator}={header.Size}";
        }

        internal static string GetNextPage(HttpRequest request, IPageHeader header, QueryOptions options)
        {
            return !header.HasNextPage
                ? null
                : $"{request.Scheme}://{request.Host}{request.Path}?{options.PageOperator}={header.Index + 2}&{options.PerPageOperator}={header.Size}";
        }

        internal static string GetFirstPage(HttpRequest request, IPageHeader header, QueryOptions options)
        {
            return header.TotalPages == 0
                ? null
                : $"{request.Scheme}://{request.Host}{request.Path}?{options.PageOperator}=1&{options.PerPageOperator}={header.Size}";
        }

        internal static string GetLastPage(HttpRequest request, IPageHeader header, QueryOptions options)
        {
            return header.TotalPages < 2
                ? null
                : $"{request.Scheme}://{request.Host}{request.Path}?{options.PageOperator}={header.TotalPages}&{options.PerPageOperator}={header.Size}";
        }

        public static bool FeatureRequested(this HttpRequest request, string @operator, bool @default)
        {
            if (string.IsNullOrWhiteSpace(@operator))
            {
                return @default;
            }

            bool useFeature;
            if (request.Query.TryGetValue(@operator, out var values))
            {
                bool.TryParse(values, out useFeature);
            }
            else
            {
                useFeature = @default;
            }

            return useFeature;
        }
    }
}
