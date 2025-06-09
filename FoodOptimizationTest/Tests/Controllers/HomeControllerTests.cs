using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LinearOptimizationFoodApp.Controllers;
using LinearOptimizationFoodApp.Services;
using LinearOptimizationFoodApp.Models;
using Moq;
using Xunit;

namespace LinearOptimizationFoodApp.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<IOptimizerService> _mockOptimizerService;
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockOptimizerService = new Mock<IOptimizerService>();
            _mockLogger = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(_mockOptimizerService.Object, _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullOptimizerService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new HomeController(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new HomeController(_mockOptimizerService.Object, null));
        }

        #endregion

        #region Index Action Tests

        [Fact]
        public async Task Index_WithValidRecipes_ReturnsViewWithRecipes()
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe { Id = 1, Name = "Recipe1", Description = "Description1", Feeds = 4 },
                new Recipe { Id = 2, Name = "Recipe2", Description = "Description2", Feeds = 6 },
                new Recipe { Id = 3, Name = "Recipe3", Description = "Description3", Feeds = 2 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Equal(3, model.Count);
            Assert.Equal("Recipe1", model[0].Name);
            Assert.Equal("Recipe2", model[1].Name);
            Assert.Equal("Recipe3", model[2].Name);
        }

        [Fact]
        public async Task Index_WithEmptyRecipeList_ReturnsViewWithEmptyList()
        {
            // Arrange
            var recipes = new List<Recipe>();

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_WithNullRecipes_ReturnsViewWithEmptyList()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync((List<Recipe>)null);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_WithException_ReturnsViewWithEmptyList()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_WithServiceException_HandlesGracefully()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ThrowsAsync(new InvalidOperationException("Service unavailable"));

            // Act & Assert - Should not throw
            var result = await _controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task Index_CallsOptimizerServiceOnce()
        {
            // Arrange
            var recipes = new List<Recipe>();
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            await _controller.Index();

            // Assert
            _mockOptimizerService.Verify(x => x.GetAllRecipesAsync(), Times.Once);
        }

        #endregion

        
    }
}