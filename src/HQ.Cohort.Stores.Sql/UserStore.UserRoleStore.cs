﻿#region LICENSE

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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using HQ.Cohort.Stores.Sql.Models;
using HQ.Lingo.Queries;
using Microsoft.AspNetCore.Identity;

namespace HQ.Cohort.Stores.Sql
{
    partial class UserStore<TUser, TKey, TRole> : IUserRoleStore<TUser>
    {
        public async Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var roleId = await GetRoleIdByNameAsync(roleName);

            if (roleId != null)
            {
                var query = SqlBuilder.Insert(new AspNetUserRoles<TKey>
                {
                    UserId = user.Id,
                    RoleId = roleId
                });

                _connection.SetTypeInfo(typeof(AspNetUserRoles<TKey>));
                var inserted = await _connection.Current.ExecuteAsync(query.Sql, query.Parameters);
                Debug.Assert(inserted == 1);
            }
        }

        public async Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var roleId = await GetRoleIdByNameAsync(roleName);

            if (roleId != null)
            {
                var query = SqlBuilder.Delete<AspNetUserRoles<TUser>>(new {UserId = user.Id, RoleId = roleId});

                _connection.SetTypeInfo(typeof(AspNetUserRoles<TKey>));
                var deleted = await _connection.Current.ExecuteAsync(query.Sql, query.Parameters);
                Debug.Assert(deleted == 1);
            }
        }

        public async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user.NormalizedUserName == _security?.Value.SuperUser?.Username?.ToUpperInvariant())
                return SuperUserRoles;

            var mappingQuery = SqlBuilder.Select<AspNetUserRoles<TKey>>(new
            {
                UserId = user.Id
            });

            // Mapping:
            mappingQuery.Sql += " AND AspNetUserRoles.DocumentType = @DocumentType";
            _connection.SetTypeInfo(typeof(AspNetUserRoles<TKey>));
            var roleMapping =
                (await _connection.Current.QueryAsync<AspNetUserRoles<TKey>>(mappingQuery.Sql, mappingQuery.Parameters))
                .AsList();

            // Roles:
            var roleQuery = SqlBuilder.Select<TRole>();
            if (roleMapping.Any())
            {
                roleQuery.Sql += $" WHERE {typeof(TRole).Name}.Id IN @RoleIds";
                roleQuery.Parameters.Add("@RoleIds", roleMapping.Select(x => x.RoleId));
                _connection.SetTypeInfo(typeof(TRole));

                var roles = await _connection.Current.QueryAsync<TRole>(roleQuery.Sql, roleQuery.Parameters);
                return roles.Select(x => x.Name).ToList();
            }

            return new List<string>(0);
        }

        public async Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            var userRoles = await GetRolesAsync(user, cancellationToken);
            return userRoles.Contains(roleName);
        }

        public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            const string sql = "SELECT u.* " +
                               "FROM AspNetRoles r " +
                               "INNER JOIN AspNetUserRoles AS ur ON ur.RoleId = r.Id " +
                               "INNER JOIN AspNetUsers u ON u.Id = ur.UserId " +
                               "WHERE r.NormalizedName = @NormalizedName";

            var users = await _connection.Current.QueryAsync<TUser>(sql, new
            {
                NormalizedName = roleName
            });

            return users.AsList();
        }

        private async Task<TKey> GetRoleIdByNameAsync(string roleName)
        {
            var query = SqlBuilder.Select<TRole>(new {NormalizedName = roleName?.ToUpperInvariant()});
            _connection.SetTypeInfo(typeof(TRole));
            var role = await _connection.Current.QuerySingleOrDefaultAsync<TRole>(query.Sql, query.Parameters);
            return role.Id;
        }
    }
}