// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Hub.Core.Test.Twin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Edge.Hub.Core.Twin;
    using Microsoft.Azure.Devices.Edge.Storage;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Azure.Devices.Edge.Util.Test.Common;
    using Microsoft.Azure.Devices.Shared;
    using Moq;
    using Xunit;

    [Unit]
    public class ReportedPropertiesStoreTest
    {
        [Fact]
        public async Task UpdateTest()
        {
            // Arrange
            string id = "d1";
            IEntityStore<string, TwinStoreEntity> rpEntityStore = GetReportedPropertiesEntityStore();

            TwinCollection receivedReportedProperties = null;
            var cloudSync = new Mock<ICloudSync>();
            cloudSync.Setup(c => c.UpdateReportedProperties(id, It.IsAny<TwinCollection>()))
                .Callback<string, TwinCollection>((s, collection) => receivedReportedProperties = collection)
                .ReturnsAsync(true);

            var reportedPropertiesStore = new ReportedPropertiesStore(rpEntityStore, cloudSync.Object, Option.None<TimeSpan>());

            var rbase = new TwinCollection
            {
                ["p1"] = "v1",
                ["p2"] = "v2"
            };

            // Act
            await reportedPropertiesStore.Update(id, rbase);
            await reportedPropertiesStore.SyncToCloud(id);

            // Assert
            Assert.NotNull(receivedReportedProperties);
            Assert.Equal(receivedReportedProperties.ToJson(), rbase.ToJson());
        }

        [Fact]
        public async Task SyncToCloudTest()
        {
            // Arrange
            string id = "d1";
            IEntityStore<string, TwinStoreEntity> rpEntityStore = GetReportedPropertiesEntityStore();

            var receivedReportedProperties = new List<TwinCollection>();
            var cloudSync = new Mock<ICloudSync>();
            cloudSync.Setup(c => c.UpdateReportedProperties(id, It.IsAny<TwinCollection>()))
                .Callback<string, TwinCollection>((s, collection) => receivedReportedProperties.Add(collection))
                .ReturnsAsync(true);

            var reportedPropertiesStore = new ReportedPropertiesStore(rpEntityStore, cloudSync.Object, Option.None<TimeSpan>());

            var rp1 = new TwinCollection
            {
                ["p1"] = "v1",
                ["p2"] = "v2"
            };

            var rp2 = new TwinCollection
            {
                ["p1"] = "v12",
                ["p3"] = "v3"
            };

            var rp3 = new TwinCollection
            {
                ["p1"] = "v13",
                ["p3"] = "v32"
            };

            var rp4 = new TwinCollection
            {
                ["p1"] = "v14",
                ["p4"] = "v4"
            };

            // Act
            await reportedPropertiesStore.Update(id, rp1);
            reportedPropertiesStore.InitSyncToCloud(id);

            await reportedPropertiesStore.Update(id, rp2);
            reportedPropertiesStore.InitSyncToCloud(id);

            await reportedPropertiesStore.Update(id, rp3);
            reportedPropertiesStore.InitSyncToCloud(id);

            await reportedPropertiesStore.Update(id, rp4);
            reportedPropertiesStore.InitSyncToCloud(id);

            // Assert
            await Task.Delay(TimeSpan.FromSeconds(7));

            cloudSync.Verify(c => c.UpdateReportedProperties(id, It.IsAny<TwinCollection>()), Times.Once);
            Assert.Equal(1, receivedReportedProperties.Count);
            Assert.Equal(receivedReportedProperties[0].ToJson(), "{\"p1\":\"v14\",\"p2\":\"v2\",\"p3\":\"v32\",\"p4\":\"v4\"}");
        }

        static IEntityStore<string, TwinStoreEntity> GetReportedPropertiesEntityStore()
        {
            var dbStoreProvider = new InMemoryDbStoreProvider();
            var entityStoreProvider = new StoreProvider(dbStoreProvider);
            IEntityStore<string, TwinStoreEntity> entityStore = entityStoreProvider.GetEntityStore<string, TwinStoreEntity>($"rp{Guid.NewGuid()}");
            return entityStore;
        }
    }
}
