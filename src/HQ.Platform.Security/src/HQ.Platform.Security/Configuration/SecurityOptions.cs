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

using System.Net;

namespace HQ.Platform.Security.Configuration
{
    public class SecurityOptions
    {
        public SuperUserOptions SuperUser { get; set; } = new SuperUserOptions();
        public ClaimOptions Claims { get; set; } = new ClaimOptions();
        public TokenOptions Tokens { get; set; } = new TokenOptions();
        public HttpsOptions Https { get; set; } = new HttpsOptions();
        public BlockListOptions BlockLists { get; set; } = new BlockListOptions();
        public HttpStatusCode? ForbidStatusCode { get; set; } = HttpStatusCode.Forbidden;
    }
}
