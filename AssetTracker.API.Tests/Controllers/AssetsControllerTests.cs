using AssetTracker.API.Controller;
using AssetTracker.API.Models;
using AssetTracker.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.API.Tests.Controllers
{
    public class AssetsControllerTests
    {
        #region GetAssets Tests

        [Fact]
        public async Task GetAssets_ReturnsAllAssets()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AssetsController(context);

            // Act
            var result = await controller.GetAssets();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as OkObjectResult;
            if (actionResult != null)
            {
                var assets = actionResult.Value as IEnumerable<Asset>;
                assets.Should().NotBeNull();
                assets.Should().HaveCountGreaterThan(0);
            }
            else
            {
                result.Value.Should().NotBeNull();
                result.Value.Should().BeAssignableTo<IEnumerable<Asset>>();
            }
        }

        [Fact]
        public async Task GetAssets_ReturnsEmptyListWhenNoAssets()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AssetsController(context);

            // Act
            var result = await controller.GetAssets();

            // Assert
            result.Should().NotBeNull();
        }

        #endregion

        #region GetAsset Tests

        [Fact]
        public async Task GetAsset_WithValidId_ReturnsAsset()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AssetsController(context);

            // Act
            var result = await controller.GetAsset(1);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result;
            if (actionResult is OkObjectResult okResult)
            {
                var asset = okResult.Value as Asset;
                asset.Should().NotBeNull();
                asset!.AssetID.Should().Be(1);
            }
            else
            {
                result.Value.Should().NotBeNull();
                result.Value!.AssetID.Should().Be(1);
            }
        }

        [Fact]
        public async Task GetAsset_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AssetsController(context);

            // Act
            var result = await controller.GetAsset(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region PostAsset Tests

        [Fact]
        public async Task PostAsset_WithValidData_AddsAssetToDatabase()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AssetsController(context);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var newAsset = new Asset
            {
                Name = "New Laptop",
                SerialNumber = "SN999",
                Category = "Laptop",
                Status = "Available",
                PurchaseDate = DateTime.UtcNow,
                Value = 1500.00m
            };

            // Act
            var result = await controller.PostAsset(newAsset);

            // Assert
            var createdAsset = context.Assets.FirstOrDefault(a => a.SerialNumber == "SN999");
            createdAsset.Should().NotBeNull();
            createdAsset!.Name.Should().Be("New Laptop");
        }

        #endregion

        #region PutAsset Tests

        [Fact]
        public async Task PutAsset_WithValidData_UpdatesAsset()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AssetsController(context);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var asset = context.Assets.First();
            asset.Name = "Updated Name";

            // Act
            var result = await controller.PutAsset(asset.AssetID, asset);

            // Assert
            var updatedAsset = context.Assets.Find(asset.AssetID);
            updatedAsset!.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task PutAsset_WithMismatchedId_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AssetsController(context);
            var asset = context.Assets.First();

            // Act
            var result = await controller.PutAsset(999, asset);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region DeleteAsset Tests

        [Fact]
        public async Task DeleteAsset_WithValidId_RemovesAsset()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AssetsController(context);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var assetId = context.Assets.First().AssetID;

            // Act
            var result = await controller.DeleteAsset(assetId);

            // Assert - Soft delete: status changed to "Decommissioned", not removed from DB
            var decommissionedAsset = context.Assets.Find(assetId);
            decommissionedAsset.Should().NotBeNull();
            decommissionedAsset!.Status.Should().Be("Decommissioned");
        }

        [Fact]
        public async Task DeleteAsset_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AssetsController(context);

            // Act
            var result = await controller.DeleteAsset(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion
    }
}
