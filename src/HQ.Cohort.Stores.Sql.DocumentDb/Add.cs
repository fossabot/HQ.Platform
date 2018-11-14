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
using System.Data;
using System.Data.DocumentDb;
using System.Threading;
using HQ.Cohort.Configuration;
using HQ.Common.Models;
using HQ.Connect;
using HQ.Connect.DocumentDb;
using HQ.Lingo.Descriptor;
using HQ.Lingo.Dialects;
using HQ.Lingo.DocumentDb;
using HQ.Rosetta.Queryable;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HQ.Cohort.Stores.Sql.DocumentDb
{
    public static class Add
    {
        public static IdentityBuilder AddDocumentDbIdentityStore<TUser, TRole>(this IdentityBuilder identityBuilder,
            IConfiguration config, string connectionString, ConnectionScope scope = ConnectionScope.ByRequest)
            where TUser : IdentityUser<string>
            where TRole : IdentityRole<string>
        {
            return AddDocumentDbIdentityStore<string, TUser, TRole>(identityBuilder, connectionString, scope);
        }

        public static IdentityBuilder AddDocumentDbIdentityStore<TKey, TUser, TRole>(
            this IdentityBuilder identityBuilder, IConfiguration config, string connectionString,
            ConnectionScope scope = ConnectionScope.ByRequest)
            where TKey : IEquatable<TKey>
            where TUser : IdentityUser<TKey>
            where TRole : IdentityRole<TKey>
        {
            return AddDocumentDbIdentityStore<TKey, TUser, TRole>(identityBuilder, connectionString, scope);
        }

        public static IdentityBuilder AddDocumentDbIdentityStore<TKey, TUser, TRole>(IdentityBuilder identityBuilder,
            string connectionString, ConnectionScope scope,
            IConfiguration identityConfig, IConfiguration documentDbConfig)
            where TKey : IEquatable<TKey>
            where TUser : IdentityUser<TKey>
            where TRole : IdentityRole<TKey>
        {
            identityBuilder.Services.Configure<IdentityOptionsExtended>(identityConfig);
            identityBuilder.Services.Configure<DocumentDbOptions>(documentDbConfig);

            return AddDocumentDbIdentityStore<TKey, TUser, TRole>(identityBuilder, connectionString, scope,
                identityConfig.Bind, documentDbConfig.Bind);
        }

        public static IdentityBuilder AddDocumentDbIdentityStore<TKey, TUser, TRole>(
            this IdentityBuilder identityBuilder, string connectionString,
            ConnectionScope scope = ConnectionScope.ByRequest,
            Action<IdentityOptionsExtended> configureIdentity = null,
            Action<DocumentDbOptions> configureDocumentDb = null)
            where TKey : IEquatable<TKey>
            where TUser : IdentityUser<TKey>
            where TRole : IdentityRole<TKey>
        {
            identityBuilder.Services.AddSingleton<ITypeRegistry, TypeRegistry>();

            var dialect = new DocumentDbDialect();

            var identityOptions = new IdentityOptionsExtended();
            configureIdentity?.Invoke(identityOptions);

            var documentDbOptions = new DocumentDbOptions();
            configureDocumentDb?.Invoke(documentDbOptions);

            MigrateToLatest<TKey>(connectionString, identityOptions, documentDbOptions);

            identityBuilder.AddSqlStores<DocumentDbConnectionFactory, TKey, TUser, TRole>(connectionString, scope,
                OnCommand<TKey>(), OnConnection);

            identityBuilder.Services.AddSingleton<ISqlDialect>(dialect);
            identityBuilder.Services.AddSingleton<IQueryableProvider<TUser>, DocumentDbQueryableProvider<TUser>>();
            identityBuilder.Services.AddSingleton<IQueryableProvider<TRole>, DocumentDbQueryableProvider<TRole>>();

            return identityBuilder;
        }

        private static void OnConnection(IDbConnection c, IServiceProvider r)
        {
            if (c is DocumentDbConnection connection)
            {
            }
        }

        private static Action<IDbCommand, Type, IServiceProvider> OnCommand<TKey>()
            where TKey : IEquatable<TKey>
        {
            return (c, t, r) =>
            {
                if (c is DocumentDbCommand command)
                {
                    var options = r.GetRequiredService<IOptions<DocumentDbOptions>>();
                    var registry = r.GetRequiredService<ITypeRegistry>();

                    var descriptor = SimpleDataDescriptor.Create(t);
                    registry.RegisterIfNotRegistered(t);

                    command.Id = descriptor.Id?.Property?.Name;
                    command.Type = t;
                    command.Collection = options.Value.CollectionId;
                }
            };
        }

        private static void MigrateToLatest<TKey>(string connectionString, IdentityOptionsExtended identityOptions,
            DocumentDbOptions options) where TKey : IEquatable<TKey>
        {
            var runner = new MigrationRunner(connectionString, options);

            if (identityOptions.Stores.CreateIfNotExists)
                runner.CreateDatabaseIfNotExistsAsync(CancellationToken.None).Wait();

            if (identityOptions.Stores.MigrateOnStartup)
                runner.MigrateUp(CancellationToken.None);
        }
    }
}