using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LinearOptimizationFoodApp.Controllers;
using LinearOptimizationFoodApp.Services;
using LinearOptimizationFoodApp.ViewModels;
using LinearOptimizationFoodApp.Models;
using Moq;
using Xunit;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;

namespace LinearOptimizationFoodApp.Tests.Controllers
{
    public class OptimizerControllerTests
    {
        private readonly Mock<IOptimizerService> _mockOptimizerService;
        private readonly Mock<ILogger<OptimizerController>> _mockLogger;
        private readonly OptimizerController _controller;

        public OptimizerControllerTests()
        {
            _mockOptimizerService = new Mock<IOptimizerService>();
            _mockLogger = new Mock<ILogger<OptimizerController>>();
            _controller = new OptimizerController(_mockOptimizerService.Object, _mockLogger.Object);

            // Setup TempData and ViewData for controller testing
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullOptimizerService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new OptimizerController(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new OptimizerController(_mockOptimizerService.Object, null));
        }

        #endregion

        #region Index Action Tests

        [Fact]
        public async Task Index_WithNoIngredients_ReturnsViewWithWarning()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(new Dictionary<string, int>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<OptimizationResultViewModel>(viewResult.Model);
            Assert.Equal("Please set your available ingredients first to get optimization results.",
                _controller.TempData["Warning"]);
        }

        [Fact]
        public async Task Index_WithIngredientsButZeroQuantities_ReturnsViewWithWarning()
        {
            // Arrange
            var ingredients = new Dictionary<string, int>
            {
                { "ingredient1", 0 },
                { "ingredient2", 0 }
            };
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(ingredients);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<OptimizationResultViewModel>(viewResult.Model);
            Assert.Equal("Please set your available ingredients first to get optimization results.",
                _controller.TempData["Warning"]);
        }

        [Fact]
        public async Task Index_WithOptimizationServiceReturningNull_ReturnsEmptyViewModel()
        {
            // Arrange
            var ingredients = new Dictionary<string, int> { { "ingredient1", 5 } };
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(ingredients);
            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync((OptimizationResultViewModel)null);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<OptimizationResultViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Index_WithSuccessfulOptimization_ReturnsViewWithSuccessMessage()
        {
            // Arrange
            var ingredients = new Dictionary<string, int> { { "ingredient1", 5 } };
            var optimizationResult = new OptimizationResultViewModel
            {
                MaxPeopleFed = 10,
                BestCombination = new List<Recipe>
                {
                    new Recipe { Id = 1, Name = "Recipe1", Feeds = 5 },
                    new Recipe { Id = 2, Name = "Recipe2", Feeds = 5 }
                }
            };

            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(ingredients);
            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync(optimizationResult);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(optimizationResult, viewResult.Model);
            Assert.Equal("Optimization complete! Found a combination that feeds 10 people.",
                _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Index_WithNoResults_ReturnsViewWithInfoMessage()
        {
            // Arrange
            var ingredients = new Dictionary<string, int> { { "ingredient1", 5 } };
            var optimizationResult = new OptimizationResultViewModel
            {
                MaxPeopleFed = 0,
                BestCombination = new List<Recipe>()
            };

            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(ingredients);
            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync(optimizationResult);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(optimizationResult, viewResult.Model);
            Assert.Equal("No recipe combinations possible with your current ingredients. Try adding more ingredients or adjusting quantities.",
                _controller.TempData["Info"]);
        }

        [Fact]
        public async Task Index_WithException_ReturnsViewWithErrorMessage()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<OptimizationResultViewModel>(viewResult.Model);
            Assert.Equal("An error occurred during optimization. Please try again.",
                _controller.TempData["Error"]);
        }

        #endregion

        #region OptimizeJson Action Tests

        [Fact]
        public async Task OptimizeJson_WithSuccessfulOptimization_ReturnsJsonWithResults()
        {
            // Arrange
            var optimizationResult = new OptimizationResultViewModel
            {
                MaxPeopleFed = 15,
                BestCombination = new List<Recipe>
                {
                    new Recipe { Id = 1, Name = "Recipe1", Feeds = 10 },
                    new Recipe { Id = 2, Name = "Recipe2", Feeds = 5 }
                }
            };

            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync(optimizationResult);

            // Act
            var result = await _controller.OptimizeJson();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;

            // Use reflection to check the anonymous object properties
            var successProperty = data.GetType().GetProperty("success");
            var maxPeopleFedProperty = data.GetType().GetProperty("maxPeopleFed");
            var recipeCountProperty = data.GetType().GetProperty("recipeCount");
            var hasResultsProperty = data.GetType().GetProperty("hasResults");
            var recipesProperty = data.GetType().GetProperty("recipes");

            Assert.True((bool)successProperty.GetValue(data));
            Assert.Equal(15, (int)maxPeopleFedProperty.GetValue(data));
            Assert.Equal(2, (int)recipeCountProperty.GetValue(data));
            Assert.True((bool)hasResultsProperty.GetValue(data));

            var recipes = (List<RecipeDto>)recipesProperty.GetValue(data);
            Assert.Equal(2, recipes.Count);
            Assert.Equal("Recipe1", recipes[0].Name);
            Assert.Equal(10, recipes[0].Feeds);
        }

        [Fact]
        public async Task OptimizeJson_WithNullResult_ReturnsJsonWithFailure()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync((OptimizationResultViewModel)null);

            // Act
            var result = await _controller.OptimizeJson();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;

            var successProperty = data.GetType().GetProperty("success");
            var messageProperty = data.GetType().GetProperty("message");

            Assert.False((bool)successProperty.GetValue(data));
            Assert.Equal("Optimization failed", (string)messageProperty.GetValue(data));
        }

        [Fact]
        public async Task OptimizeJson_WithException_ReturnsJsonWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.OptimizeJson();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;

            var successProperty = data.GetType().GetProperty("success");
            var messageProperty = data.GetType().GetProperty("message");

            Assert.False((bool)successProperty.GetValue(data));
            Assert.Equal("An error occurred during optimization", (string)messageProperty.GetValue(data));
        }

        #endregion

        #region Compare Action Tests

        [Fact]
        public async Task Compare_WithNoIngredients_RedirectsToIngredientsWithWarning()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(new Dictionary<string, int>());

            // Act
            var result = await _controller.Compare();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Ingredients", redirectResult.ControllerName);
            Assert.Equal("Please set your available ingredients first.",
                _controller.TempData["Warning"]);
        }

        [Fact]
        public async Task Compare_WithValidIngredients_ReturnsViewWithComparisonMode()
        {
            // Arrange
            var ingredients = new Dictionary<string, int> { { "ingredient1", 5 } };
            var optimizationResult = new OptimizationResultViewModel();

            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(ingredients);
            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync(optimizationResult);

            // Act
            var result = await _controller.Compare();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Same(optimizationResult, viewResult.Model);
            Assert.True((bool)_controller.ViewData["ComparisonMode"]);
        }

        [Fact]
        public async Task Compare_WithException_RedirectsToIndexWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Compare();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Unable to load comparison. Please try again.",
                _controller.TempData["Error"]);
        }

        #endregion

        #region Export Action Tests

        [Fact]
        public async Task Export_WithNoResults_RedirectsToIndexWithWarning()
        {
            // Arrange
            var optimizationResult = new OptimizationResultViewModel
            {
                MaxPeopleFed = 0,
                BestCombination = new List<Recipe>()
            };

            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync(optimizationResult);

            // Act
            var result = await _controller.Export("json");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("No optimization results to export. Run optimization first.",
                _controller.TempData["Warning"]);
        }

        [Fact]
        public async Task Export_WithValidResultsAndJsonFormat_ReturnsFileResult()
        {
            // Arrange
            var optimizationResult = new OptimizationResultViewModel
            {
                MaxPeopleFed = 10,
                BestCombination = new List<Recipe>
                {
                    new Recipe
                    {
                        Id = 1,
                        Name = "Recipe1",
                        Feeds = 10,
                        Description = "Test recipe"
                        // Note: Not setting RecipeIngredients for this test since we're only testing export functionality
                        // The RequiredIngredients property will return an empty dictionary
                    }
                },
                UsedIngredients = new Dictionary<string, int> { { "ingredient1", 2 } },
                RemainingIngredients = new Dictionary<string, int> { { "ingredient1", 3 } }
            };

            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync(optimizationResult);

            // Act
            var result = await _controller.Export("json");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/json", fileResult.ContentType);
            Assert.Contains("optimization-results-", fileResult.FileDownloadName);
            Assert.Contains(".json", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task Export_WithUnsupportedFormat_RedirectsToIndexWithError()
        {
            // Arrange
            var optimizationResult = new OptimizationResultViewModel
            {
                MaxPeopleFed = 10,
                BestCombination = new List<Recipe> { new Recipe() }
            };

            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ReturnsAsync(optimizationResult);

            // Act
            var result = await _controller.Export("xml");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Unsupported export format.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Export_WithException_RedirectsToIndexWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.FindOptimalCombinationAsync())
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Export("json");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Unable to export results. Please try again.",
                _controller.TempData["Error"]);
        }

        #endregion

        #region Stats Action Tests

        [Fact]
        public async Task Stats_WithValidData_ReturnsJsonWithStatistics()
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe { Id = 1, Name = "Recipe1", Feeds = 5 },
                new Recipe { Id = 2, Name = "Recipe2", Feeds = 10 }
            };
            var ingredients = new Dictionary<string, int>
            {
                { "ingredient1", 5 },
                { "ingredient2", 0 },
                { "ingredient3", 3 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(ingredients);

            // Act
            var result = await _controller.Stats();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;

            var totalRecipesProperty = data.GetType().GetProperty("TotalRecipes");
            var availableIngredientsProperty = data.GetType().GetProperty("AvailableIngredients");
            var totalIngredientsProperty = data.GetType().GetProperty("TotalIngredients");
            var maxPossiblePeopleProperty = data.GetType().GetProperty("MaxPossiblePeople");
            var averageRecipeSizeProperty = data.GetType().GetProperty("AverageRecipeSize");

            Assert.Equal(2, (int)totalRecipesProperty.GetValue(data));
            Assert.Equal(2, (int)availableIngredientsProperty.GetValue(data)); // Only non-zero ingredients
            Assert.Equal(3, (int)totalIngredientsProperty.GetValue(data));
            Assert.Equal(15, (int)maxPossiblePeopleProperty.GetValue(data));
            Assert.Equal(7.5, (double)averageRecipeSizeProperty.GetValue(data));
        }

        [Fact]
        public async Task Stats_WithNullData_ReturnsJsonWithZeroValues()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync((List<Recipe>)null);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync((Dictionary<string, int>)null);

            // Act
            var result = await _controller.Stats();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;

            var totalRecipesProperty = data.GetType().GetProperty("TotalRecipes");
            var availableIngredientsProperty = data.GetType().GetProperty("AvailableIngredients");
            var totalIngredientsProperty = data.GetType().GetProperty("TotalIngredients");
            var maxPossiblePeopleProperty = data.GetType().GetProperty("MaxPossiblePeople");
            var averageRecipeSizeProperty = data.GetType().GetProperty("AverageRecipeSize");

            Assert.Equal(0, (int)totalRecipesProperty.GetValue(data));
            Assert.Equal(0, (int)availableIngredientsProperty.GetValue(data));
            Assert.Equal(0, (int)totalIngredientsProperty.GetValue(data));
            Assert.Equal(0, (int)maxPossiblePeopleProperty.GetValue(data));
            Assert.Equal(0.0, (double)averageRecipeSizeProperty.GetValue(data));
        }

        [Fact]
        public async Task Stats_WithException_ReturnsJsonWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Stats();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;

            var errorProperty = data.GetType().GetProperty("error");
            Assert.Equal("Unable to load statistics", (string)errorProperty.GetValue(data));
        }

        #endregion
    }
}