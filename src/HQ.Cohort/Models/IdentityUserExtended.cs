using System;
using Microsoft.AspNetCore.Identity;

namespace HQ.Cohort.Models
{
    public class IdentityUserExtended<TKey> : IdentityUser<TKey> where TKey : IEquatable<TKey>
    {
        public int TenantId { get; set; }
    }

    public class IdentityUserExtended : IdentityUserExtended<string> { }
}
