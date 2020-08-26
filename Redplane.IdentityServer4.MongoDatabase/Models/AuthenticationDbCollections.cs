﻿using IdentityServer4.Models;
using MongoDB.Driver;
using Redplane.IdentityServer4.MongoDatabase.Interfaces;

namespace Redplane.IdentityServer4.MongoDatabase.Models
{
    internal class AuthenticationDbCollections : IAuthenticationMongoCollections
    {
        #region Constructor

        public AuthenticationDbCollections(IMongoCollection<Client> clients,
            IMongoCollection<PersistedGrant> persistedGrants,
            IMongoCollection<ApiResource> apiResources,
            IMongoCollection<IdentityResource> identityResources,
            IMongoCollection<ApiScope> apiScopes)
        {
            Clients = clients;
            PersistedGrants = persistedGrants;
            ApiResources = apiResources;
            IdentityResources = identityResources;
            ApiScopes = apiScopes;
        }

        #endregion

        #region Properties

        public IMongoCollection<Client> Clients { get; }

        public IMongoCollection<PersistedGrant> PersistedGrants { get; }

        public IMongoCollection<ApiResource> ApiResources { get; }

        public IMongoCollection<IdentityResource> IdentityResources { get; }

        public IMongoCollection<ApiScope> ApiScopes { get; }

        #endregion
    }
}