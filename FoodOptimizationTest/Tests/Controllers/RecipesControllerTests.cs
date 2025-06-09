using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LinearOptimizationFoodApp.Controllers;
using LinearOptimizationFoodApp.Services;
using LinearOptimizationFoodApp.Models;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;

namespace LinearOptimizationFoodApp.Tests.Controllers
{
    public class RecipesControllerTests
    {
        private readonly Mock<IOptimizerService> _mockOptimizerService;
        private readonly Mock<ILogger<RecipesController>> _mockLogger;
        private readonly RecipesController _controller;

        public RecipesControllerTests()
        {
            _mockOptimizerService = new Mock<IOptimizerService>();
            _mockLogger = new Mock<ILogger<RecipesController>>();
            _controller = new RecipesController(_mockOptimizerService.Object, _mockLogger.Object);

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
                new RecipesController(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RecipesController(_mockOptimizerService.Object, null));
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
                new Recipe { Id = 2, Name = "Recipe2", Description = "Description2", Feeds = 6 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("Recipe1", model[0].Name);
            Assert.Equal("Recipe2", model[1].Name);
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
        public async Task Index_WithException_ReturnsViewWithEmptyListAndErrorMessage()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Empty(model);
            Assert.Equal("Unable to load recipes. Please try again later.", _controller.TempData["Error"]);
        }

        #endregion

        #region Details Action Tests

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithRecipe()
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe { Id = 1, Name = "Recipe1", Description = "Description1", Feeds = 4 },
                new Recipe { Id = 2, Name = "Recipe2", Description = "Description2", Feeds = 6 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Recipe>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Recipe1", model.Name);
        }

        [Fact]
        public async Task Details_WithInvalidId_RedirectsToIndexWithError()
        {
            // Act
            var result = await _controller.Details(0);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Invalid recipe ID.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Details_WithNegativeId_RedirectsToIndexWithError()
        {
            // Act
            var result = await _controller.Details(-1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Invalid recipe ID.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Details_WithNonExistentId_RedirectsToIndexWithError()
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe { Id = 1, Name = "Recipe1", Description = "Description1", Feeds = 4 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Details(999);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Recipe not found.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Details_WithNullRecipes_RedirectsToIndexWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync((List<Recipe>)null);

            // Act
            var result = await _controller.Details(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Recipe not found.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Details_WithException_RedirectsToIndexWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Details(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Unable to load recipe details. Please try again.", _controller.TempData["Error"]);
        }

        #endregion

        #region Search Action Tests

        [Fact]
        public async Task Search_WithEmptySearchTerm_RedirectsToIndex()
        {
            // Act
            var result = await _controller.Search("");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Search_WithNullSearchTerm_RedirectsToIndex()
        {
            // Act
            var result = await _controller.Search(null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Search_WithWhitespaceSearchTerm_RedirectsToIndex()
        {
            // Act
            var result = await _controller.Search("   ");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Search_WithValidSearchTermMatchingName_ReturnsFilteredResults()
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe { Id = 1, Name = "Chicken Pasta", Description = "Delicious pasta", Feeds = 4 },
                new Recipe { Id = 2, Name = "Beef Stew", Description = "Hearty stew", Feeds = 6 },
                new Recipe { Id = 3, Name = "Chicken Curry", Description = "Spicy curry", Feeds = 4 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Search("Chicken");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.All(model, r => Assert.Contains("Chicken", r.Name, StringComparison.OrdinalIgnoreCase));
            Assert.Equal("Chicken", _controller.ViewData["SearchTerm"]);
            Assert.Equal(2, _controller.ViewData["ResultCount"]);
        }

        [Fact]
        public async Task Search_WithValidSearchTermMatchingDescription_ReturnsFilteredResults()
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe { Id = 1, Name = "Recipe1", Description = "Spicy and delicious", Feeds = 4 },
                new Recipe { Id = 2, Name = "Recipe2", Description = "Mild and tasty", Feeds = 6 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Search("spicy");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Recipe1", model[0].Name);
        }

        [Fact]
        public async Task Search_WithValidSearchTermMatchingIngredient_ReturnsFilteredResults()
        {
            // Arrange
            var chickenIngredient = new Ingredient { Name = "Chicken" };
            var beefIngredient = new Ingredient { Name = "Beef" };

            var recipe1 = new Recipe
            {
                Id = 1,
                Name = "Recipe1",
                Description = "Description1",
                Feeds = 4
            };

            var recipe2 = new Recipe
            {
                Id = 2,
                Name = "Recipe2",
                Description = "Description2",
                Feeds = 6
            };

            recipe1.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 1,
                    Recipe = recipe1,
                    IngredientId = 1,
                    Ingredient = chickenIngredient,
                    Quantity = 2
                }
            };

            recipe2.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 2,
                    Recipe = recipe2,
                    IngredientId = 2,
                    Ingredient = beefIngredient,
                    Quantity = 1
                }
            };

            var recipes = new List<Recipe> { recipe1, recipe2 };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Search("chicken");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Recipe1", model[0].Name);
        }

        [Fact]
        public async Task Search_WithNoMatchingResults_ReturnsEmptyList()
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe { Id = 1, Name = "Recipe1", Description = "Description1", Feeds = 4 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);

            // Act
            var result = await _controller.Search("NonExistentTerm");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Empty(model);
            Assert.Equal("NonExistentTerm", _controller.ViewData["SearchTerm"]);
            Assert.Equal(0, _controller.ViewData["ResultCount"]);
        }

        [Fact]
        public async Task Search_WithException_RedirectsToIndexWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Search("test");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Error occurred while searching. Please try again.", _controller.TempData["Error"]);
        }

        #endregion

        #region Available Action Tests

        [Fact]
        public async Task Available_WithMakeableRecipes_ReturnsFilteredResults()
        {
            // Arrange
            var chickenIngredient = new Ingredient { Name = "Chicken" };
            var beefIngredient = new Ingredient { Name = "Beef" };

            var recipe1 = new Recipe
            {
                Id = 1,
                Name = "Recipe1",
                Feeds = 4
            };

            var recipe2 = new Recipe
            {
                Id = 2,
                Name = "Recipe2",
                Feeds = 6
            };

            recipe1.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 1,
                    Recipe = recipe1,
                    IngredientId = 1,
                    Ingredient = chickenIngredient,
                    Quantity = 2
                }
            };

            recipe2.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 2,
                    Recipe = recipe2,
                    IngredientId = 2,
                    Ingredient = beefIngredient,
                    Quantity = 5
                }
            };

            var recipes = new List<Recipe> { recipe1, recipe2 };

            var availableIngredients = new Dictionary<string, int>
            {
                { "Chicken", 3 },
                { "Beef", 2 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(availableIngredients);

            // Act
            var result = await _controller.Available();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Single(model); // Only Recipe1 can be made (needs 2 Chicken, we have 3)
            Assert.Equal("Recipe1", model[0].Name);
            Assert.Equal("Available", _controller.ViewData["FilterType"]);
            Assert.Equal(1, _controller.ViewData["AvailableCount"]);
        }

        [Fact]
        public async Task Available_WithNoMakeableRecipesAndNoIngredients_ReturnsEmptyListWithInfoMessage()
        {
            // Arrange
            var chickenIngredient = new Ingredient { Name = "Chicken" };

            var recipe1 = new Recipe
            {
                Id = 1,
                Name = "Recipe1",
                Feeds = 4
            };

            recipe1.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 1,
                    Recipe = recipe1,
                    IngredientId = 1,
                    Ingredient = chickenIngredient,
                    Quantity = 2
                }
            };

            var recipes = new List<Recipe> { recipe1 };
            var availableIngredients = new Dictionary<string, int>();

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(availableIngredients);

            // Act
            var result = await _controller.Available();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Empty(model);
            Assert.Equal("Set your available ingredients first to see which recipes you can make.",
                _controller.TempData["Info"]);
        }

        [Fact]
        public async Task Available_WithNoMakeableRecipesButHasIngredients_ReturnsEmptyListWithWarningMessage()
        {
            // Arrange
            var chickenIngredient = new Ingredient { Name = "Chicken" };

            var recipe1 = new Recipe
            {
                Id = 1,
                Name = "Recipe1",
                Feeds = 4
            };

            recipe1.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 1,
                    Recipe = recipe1,
                    IngredientId = 1,
                    Ingredient = chickenIngredient,
                    Quantity = 5
                }
            };

            var recipes = new List<Recipe> { recipe1 };

            var availableIngredients = new Dictionary<string, int>
            {
                { "Chicken", 2 } // Not enough for any recipe
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(availableIngredients);

            // Act
            var result = await _controller.Available();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Empty(model);
            Assert.Equal("No recipes can be made with your current ingredients. Try updating your ingredient quantities.",
                _controller.TempData["Warning"]);
        }

        [Fact]
        public async Task Available_WithComplexRecipeRequirements_ReturnsCorrectResults()
        {
            // Arrange
            var chickenIngredient = new Ingredient { Name = "Chicken" };
            var riceIngredient = new Ingredient { Name = "Rice" };
            var lobsterIngredient = new Ingredient { Name = "Lobster" };

            var simpleRecipe = new Recipe
            {
                Id = 1,
                Name = "Simple Recipe",
                Feeds = 2
            };

            var complexRecipe = new Recipe
            {
                Id = 2,
                Name = "Complex Recipe",
                Feeds = 4
            };

            var impossibleRecipe = new Recipe
            {
                Id = 3,
                Name = "Impossible Recipe",
                Feeds = 6
            };

            simpleRecipe.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 1,
                    Recipe = simpleRecipe,
                    IngredientId = 1,
                    Ingredient = chickenIngredient,
                    Quantity = 1
                }
            };

            complexRecipe.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 2,
                    Recipe = complexRecipe,
                    IngredientId = 1,
                    Ingredient = chickenIngredient,
                    Quantity = 2
                },
                new RecipeIngredient
                {
                    RecipeId = 2,
                    Recipe = complexRecipe,
                    IngredientId = 2,
                    Ingredient = riceIngredient,
                    Quantity = 1
                }
            };

            impossibleRecipe.RecipeIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient
                {
                    RecipeId = 3,
                    Recipe = impossibleRecipe,
                    IngredientId = 3,
                    Ingredient = lobsterIngredient,
                    Quantity = 1
                }
            };

            var recipes = new List<Recipe> { simpleRecipe, complexRecipe, impossibleRecipe };

            var availableIngredients = new Dictionary<string, int>
            {
                { "Chicken", 3 },
                { "Rice", 1 }
            };

            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ReturnsAsync(recipes);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(availableIngredients);

            // Act
            var result = await _controller.Available();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Recipe>>(viewResult.Model);
            Assert.Equal(2, model.Count); // Simple and Complex recipes can be made
            Assert.Contains(model, r => r.Name == "Simple Recipe");
            Assert.Contains(model, r => r.Name == "Complex Recipe");
            Assert.DoesNotContain(model, r => r.Name == "Impossible Recipe");
        }

        [Fact]
        public async Task Available_WithException_RedirectsToIndexWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllRecipesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Available();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Error occurred while loading available recipes.", _controller.TempData["Error"]);
        }

        #endregion
    }
}