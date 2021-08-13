﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Mongo2Go;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NUnit.Framework;
using Redplane.IdentityServer4.MongoDatabase.Constants;
using Redplane.IdentityServer4.MongoDatabase.Stores;
using Redplane.IdentityServer4.MongoDatabase.UnitTest.Constants;

namespace Redplane.IdentityServer4.MongoDatabase.UnitTest.Tests.Stores.ResourceStores
{
    [TestFixture]
    public class GetAllResourcesAsyncTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _mongoDbRunner = MongoDbRunner.Start();

            var mongoClient = new MongoClient(_mongoDbRunner.ConnectionString);
            var database = mongoClient.GetDatabase(DatabaseClientConstant.AuthenticationDatabase);

            if (!BsonClassMap.IsClassMapRegistered(typeof(ApiResource)))
                BsonClassMap.RegisterClassMap<ApiResource>(options =>
                {
                    options.AutoMap();
                    options.SetIgnoreExtraElements(true);
                });

            if (!BsonClassMap.IsClassMapRegistered(typeof(IdentityResource)))
                BsonClassMap.RegisterClassMap<IdentityResource>(options =>
                {
                    options.AutoMap();
                    options.SetIgnoreExtraElements(true);
                });

            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .Register(provider =>
                {
                    var clients = database.GetCollection<Client>("clients");
                    var persistedGrants = database.GetCollection<PersistedGrant>("persistedGrants");
                    var identityResources = database.GetCollection<IdentityResource>("identityResources");
                    var apiResources = database.GetCollection<ApiResource>("apiResources");
                    var apiScopes = database.GetCollection<ApiScope>("apiScopes");

                    return new AuthenticationDatabaseContext(Guid.NewGuid().ToString("D"), clients, persistedGrants,
                        apiResources, identityResources, apiScopes,
                        () => mongoClient.StartSession());
                })
                .As<IAuthenticationDatabaseContext>()
                .InstancePerLifetimeScope();

            containerBuilder
                .Register(x => mongoClient)
                .As<IMongoClient>()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<ClientStore>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            containerBuilder.RegisterType<ResourceStore>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            _container = containerBuilder.Build();
        }

        [SetUp]
        public void Setup()
        {
            var mongoClient = _container.Resolve<IMongoClient>();
            var database = mongoClient.GetDatabase(DatabaseClientConstant.AuthenticationDatabase);

            var apiResources = database.GetCollection<ApiResource>(AuthenticationCollectionNameConstants.ApiResources);
            for (var apiResourceIndex = 0; apiResourceIndex < 10; apiResourceIndex++)
            {
                var name = $"ar-name-{apiResourceIndex}";
                var displayName = $"ar-display-name-{apiResourceIndex}";
                var userClaims = new List<string>();

                for (var userClaimId = 0; userClaimId < 10; userClaimId++)
                    userClaims.Add($"ar-uc-{userClaimId}");

                var apiScopeName = "ars-name-1";
                var apiResource = new ApiResource(name, displayName, userClaims);
                apiResource.Scopes = new List<string> { apiScopeName };

                apiResources.InsertOne(apiResource);
            }
        }

        [TearDown]
        public void TearDown()
        {
            var mongoClient = _container.Resolve<IMongoClient>();
            var database = mongoClient.GetDatabase(DatabaseClientConstant.AuthenticationDatabase);

            var apiResources = database.GetCollection<ApiResource>(AuthenticationCollectionNameConstants.ApiResources);
            apiResources.DeleteMany(FilterDefinition<ApiResource>.Empty);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_mongoDbRunner != null && !_mongoDbRunner.Disposed)
                _mongoDbRunner.Dispose();

            _container?.Dispose();
        }

        private MongoDbRunner _mongoDbRunner;

        private IContainer _container;

        [Test]
        public async Task GetAllResourcesAsync_Returns_AllResources()
        {
            var resourceStore = _container.Resolve<IResourceStore>();

            var resource = await resourceStore.GetAllResourcesAsync();
            Assert.NotNull(resource);

            var mongoClient = _container.Resolve<IMongoClient>();
            var database = mongoClient.GetDatabase(DatabaseClientConstant.AuthenticationDatabase);

            var apiResources = database.GetCollection<ApiResource>(AuthenticationCollectionNameConstants.ApiResources)
                .Find(FilterDefinition<ApiResource>.Empty).ToList();
            var identityResources = database
                .GetCollection<IdentityResource>(AuthenticationCollectionNameConstants.ApiResources)
                .Find(FilterDefinition<IdentityResource>.Empty).ToList();

            Assert.AreEqual(apiResources.Count, resource.ApiResources.Count);
        }
    }
}