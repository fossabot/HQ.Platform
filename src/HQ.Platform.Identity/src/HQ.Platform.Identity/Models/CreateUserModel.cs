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

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HQ.Platform.Identity.Models
{
    public class CreateUserModel
    {
        [Required]
        [ProtectedPersonalData]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [PersonalData] public string UserName { get; set; }

        [ProtectedPersonalData]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        public bool EmailConfirmed { get; set; }

        [ProtectedPersonalData]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        public bool PhoneNumberConfirmed { get; set; }

        public string ConcurrencyStamp { get; set; }
    }
}
