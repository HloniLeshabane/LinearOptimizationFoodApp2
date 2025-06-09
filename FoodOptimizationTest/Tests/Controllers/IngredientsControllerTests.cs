using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LinearOptimizationFoodApp.Controllers;
using LinearOptimizationFoodApp.Services;
using LinearOptimizationFoodApp.ViewModels;
using LinearOptimizationFoodApp.Models;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LinearOptimizationFoodApp.Tests.Controllers
{
    public class IngredientsControllerTests
    {
        private readonly Mock<IOptimizerService> _mockOptimizerService;
        private readonly Mock<ILogger<IngredientsController>> _mockLogger;
        private readonly IngredientsController _controller;

        public IngredientsControllerTests()
        {
            _mockOptimizerService = new Mock<IOptimizerService>();
            _mockLogger = new Mock<ILogger<IngredientsController>>();
            _controller = new IngredientsController(_mockOptimizerService.Object, _mockLogger.Object);

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
                new IngredientsController(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new IngredientsController(_mockOptimizerService.Object, null));
        }

        #endregion

        #region Index Action Tests

        [Fact]
        public async Task Index_WithValidData_ReturnsViewWithCorrectViewModel()
        {
            // Arrange
            var allIngredients = new List<Ingredient>
            {
                new Ingredient { Id = 1, Name = "Flour", Unit = "cups" },
                new Ingredient { Id = 2, Name = "Eggs", Unit = "pieces" },
                new Ingredient { Id = 3, Name = "Milk", Unit = "liters" }
            };

            var availableIngredients = new Dictionary<string, int>
            {
                { "Flour", 5 },
                { "Eggs", 12 },
                { "Milk", 0 }
            };

            _mockOptimizerService.Setup(x => x.GetAllIngredientsAsync())
                .ReturnsAsync(allIngredients);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(availableIngredients);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<IngredientsViewModel>(viewResult.Model);

            Assert.Equal(3, model.Ingredients.Count);

            var flourIngredient = model.Ingredients.First(i => i.Name == "Flour");
            Assert.Equal(1, flourIngredient.Id);
            Assert.Equal("Flour", flourIngredient.Name);
            Assert.Equal("cups", flourIngredient.Unit);
            Assert.Equal(5, flourIngredient.Quantity);

            var eggsIngredient = model.Ingredients.First(i => i.Name == "Eggs");
            Assert.Equal(12, eggsIngredient.Quantity);

            var milkIngredient = model.Ingredients.First(i => i.Name == "Milk");
            Assert.Equal(0, milkIngredient.Quantity);
        }

        [Fact]
        public async Task Index_WithNullAllIngredients_ReturnsViewWithEmptyViewModel()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllIngredientsAsync())
                .ReturnsAsync((List<Ingredient>)null);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(new Dictionary<string, int>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<IngredientsViewModel>(viewResult.Model);
            Assert.Empty(model.Ingredients);
        }

        [Fact]
        public async Task Index_WithNullAvailableIngredients_ReturnsViewWithZeroQuantities()
        {
            // Arrange
            var allIngredients = new List<Ingredient>
            {
                new Ingredient { Id = 1, Name = "Flour", Unit = "cups" }
            };

            _mockOptimizerService.Setup(x => x.GetAllIngredientsAsync())
                .ReturnsAsync(allIngredients);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync((Dictionary<string, int>)null);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<IngredientsViewModel>(viewResult.Model);
            Assert.Single(model.Ingredients);
            Assert.Equal(0, model.Ingredients[0].Quantity);
        }

        [Fact]
        public async Task Index_WithAllZeroQuantities_SetsInfoMessage()
        {
            // Arrange
            var allIngredients = new List<Ingredient>
            {
                new Ingredient { Id = 1, Name = "Flour", Unit = "cups" }
            };
            var availableIngredients = new Dictionary<string, int> { { "Flour", 0 } };

            _mockOptimizerService.Setup(x => x.GetAllIngredientsAsync())
                .ReturnsAsync(allIngredients);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(availableIngredients);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Set your ingredient quantities to start optimizing your meals!",
                _controller.TempData["Info"]);
        }

        [Fact]
        public async Task Index_WithSomeQuantities_DoesNotSetInfoMessage()
        {
            // Arrange
            var allIngredients = new List<Ingredient>
            {
                new Ingredient { Id = 1, Name = "Flour", Unit = "cups" }
            };
            var availableIngredients = new Dictionary<string, int> { { "Flour", 5 } };

            _mockOptimizerService.Setup(x => x.GetAllIngredientsAsync())
                .ReturnsAsync(allIngredients);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(availableIngredients);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.TempData.ContainsKey("Info"));
        }

        [Fact]
        public async Task Index_WithException_ReturnsViewWithErrorMessage()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAllIngredientsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<IngredientsViewModel>(viewResult.Model);
            Assert.Empty(model.Ingredients);
            Assert.Equal("Unable to load ingredients. Please try again later.",
                _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Index_IngredientsAreOrderedByName()
        {
            // Arrange
            var allIngredients = new List<Ingredient>
            {
                new Ingredient { Id = 1, Name = "Zucchini", Unit = "pieces" },
                new Ingredient { Id = 2, Name = "Apple", Unit = "pieces" },
                new Ingredient { Id = 3, Name = "Milk", Unit = "liters" }
            };

            _mockOptimizerService.Setup(x => x.GetAllIngredientsAsync())
                .ReturnsAsync(allIngredients);
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(new Dictionary<string, int>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<IngredientsViewModel>(viewResult.Model);

            var names = model.Ingredients.Select(i => i.Name).ToList();
            Assert.Equal(new[] { "Apple", "Milk", "Zucchini" }, names);
        }

        #endregion

        #region Update Action Tests

        [Fact]
        public async Task Update_WithValidModel_UpdatesIngredientsAndRedirects()
        {
            // Arrange
            var model = new IngredientsViewModel
            {
                Ingredients = new List<IngredientQuantityViewModel>
                {
                    new IngredientQuantityViewModel { Name = "Flour", Quantity = 5 },
                    new IngredientQuantityViewModel { Name = "Eggs", Quantity = 12 },
                    new IngredientQuantityViewModel { Name = "Milk", Quantity = 0 }
                }
            };

            Dictionary<string, int> capturedQuantities = null;
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Callback<Dictionary<string, int>>(q => capturedQuantities = q)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.NotNull(capturedQuantities);
            Assert.Equal(3, capturedQuantities.Count);
            Assert.Equal(5, capturedQuantities["Flour"]);
            Assert.Equal(12, capturedQuantities["Eggs"]);
            Assert.Equal(0, capturedQuantities["Milk"]);

            Assert.Equal("Successfully updated ingredients! You have 2 ingredients available.",
                _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Update_WithNullModel_RedirectsWithError()
        {
            // Act
            var result = await _controller.Update(null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Invalid data received. Please try again.",
                _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Update_WithNullIngredients_RedirectsWithError()
        {
            // Arrange
            var model = new IngredientsViewModel { Ingredients = null };

            // Act
            var result = await _controller.Update(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Invalid data received. Please try again.",
                _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Update_WithInvalidModelState_ReturnsViewWithError()
        {
            // Arrange
            var model = new IngredientsViewModel
            {
                Ingredients = new List<IngredientQuantityViewModel>
                {
                    new IngredientQuantityViewModel { Name = "Flour", Quantity = 5 }
                }
            };

            _controller.ModelState.AddModelError("SomeField", "Some error");

            // Act
            var result = await _controller.Update(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Same(model, viewResult.Model);
            Assert.Contains("Please correct the following errors: Some error",
                _controller.TempData["Error"].ToString());
        }

        [Fact]
        public async Task Update_FiltersOutNegativeQuantities()
        {
            // Arrange
            var model = new IngredientsViewModel
            {
                Ingredients = new List<IngredientQuantityViewModel>
                {
                    new IngredientQuantityViewModel { Name = "Flour", Quantity = 5 },
                    new IngredientQuantityViewModel { Name = "BadIngredient", Quantity = -1 },
                    new IngredientQuantityViewModel { Name = "Eggs", Quantity = 0 }
                }
            };

            Dictionary<string, int> capturedQuantities = null;
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Callback<Dictionary<string, int>>(q => capturedQuantities = q)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(model);

            // Assert
            Assert.NotNull(capturedQuantities);
            Assert.Equal(2, capturedQuantities.Count); // Only positive quantities and zero
            Assert.True(capturedQuantities.ContainsKey("Flour"));
            Assert.True(capturedQuantities.ContainsKey("Eggs"));
            Assert.False(capturedQuantities.ContainsKey("BadIngredient"));
        }

        [Fact]
        public async Task Update_FiltersOutNullOrEmptyNames()
        {
            // Arrange
            var model = new IngredientsViewModel
            {
                Ingredients = new List<IngredientQuantityViewModel>
                {
                    new IngredientQuantityViewModel { Name = "Flour", Quantity = 5 },
                    new IngredientQuantityViewModel { Name = "", Quantity = 3 },
                    new IngredientQuantityViewModel { Name = null, Quantity = 2 },
                    new IngredientQuantityViewModel { Name = "   ", Quantity = 1 }
                }
            };

            Dictionary<string, int> capturedQuantities = null;
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Callback<Dictionary<string, int>>(q => capturedQuantities = q)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(model);

            // Assert
            Assert.NotNull(capturedQuantities);
            Assert.Single(capturedQuantities); // Only "Flour"
            Assert.True(capturedQuantities.ContainsKey("Flour"));
        }

        [Fact]
        public async Task Update_WithAllZeroQuantities_SetsWarningMessage()
        {
            // Arrange
            var model = new IngredientsViewModel
            {
                Ingredients = new List<IngredientQuantityViewModel>
                {
                    new IngredientQuantityViewModel { Name = "Flour", Quantity = 0 },
                    new IngredientQuantityViewModel { Name = "Eggs", Quantity = 0 }
                }
            };

            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(model);

            // Assert
            Assert.Equal("All ingredient quantities are set to 0. Set some quantities to start optimizing!",
                _controller.TempData["Warning"]);
        }

        [Fact]
        public async Task Update_WithOneNonZeroQuantity_SetsSingularSuccessMessage()
        {
            // Arrange
            var model = new IngredientsViewModel
            {
                Ingredients = new List<IngredientQuantityViewModel>
                {
                    new IngredientQuantityViewModel { Name = "Flour", Quantity = 5 },
                    new IngredientQuantityViewModel { Name = "Eggs", Quantity = 0 }
                }
            };

            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(model);

            // Assert
            Assert.Equal("Successfully updated ingredients! You have 1 ingredient available.",
                _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Update_WithException_RedirectsWithError()
        {
            // Arrange
            var model = new IngredientsViewModel
            {
                Ingredients = new List<IngredientQuantityViewModel>
                {
                    new IngredientQuantityViewModel { Name = "Flour", Quantity = 5 }
                }
            };

            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Update(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Unable to save ingredient quantities. Please try again.",
                _controller.TempData["Error"]);
        }

        #endregion

        #region Clear Action Tests

        [Fact]
        public async Task Clear_SuccessfullyClearsIngredients()
        {
            // Arrange
            Dictionary<string, int> capturedQuantities = null;
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Callback<Dictionary<string, int>>(q => capturedQuantities = q)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Clear();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.NotNull(capturedQuantities);
            Assert.Empty(capturedQuantities);
            Assert.Equal("All ingredient quantities have been cleared.",
                _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Clear_WithException_RedirectsWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Clear();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Unable to clear ingredient quantities. Please try again.",
                _controller.TempData["Error"]);
        }

        #endregion

        #region GetQuantities Action Tests

        [Fact]
        public async Task GetQuantities_ReturnsJsonWithIngredientQuantities()
        {
            // Arrange
            var availableIngredients = new Dictionary<string, int>
            {
                { "Flour", 5 },
                { "Eggs", 12 }
            };

            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync(availableIngredients);

            // Act
            var result = await _controller.GetQuantities();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = Assert.IsType<Dictionary<string, int>>(jsonResult.Value);
            Assert.Equal(2, data.Count);
            Assert.Equal(5, data["Flour"]);
            Assert.Equal(12, data["Eggs"]);
        }

        [Fact]
        public async Task GetQuantities_WithNullFromService_ReturnsEmptyDictionary()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ReturnsAsync((Dictionary<string, int>)null);

            // Act
            var result = await _controller.GetQuantities();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = Assert.IsType<Dictionary<string, int>>(jsonResult.Value);
            Assert.Empty(data);
        }

        [Fact]
        public async Task GetQuantities_WithException_ReturnsEmptyDictionary()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.GetAvailableIngredientsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetQuantities();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = Assert.IsType<Dictionary<string, int>>(jsonResult.Value);
            Assert.Empty(data);
        }

        #endregion

        #region QuickSet Action Tests

        [Fact]
        public async Task QuickSet_WithBasicPreset_SetsCorrectIngredients()
        {
            // Arrange
            Dictionary<string, int> capturedQuantities = null;
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Callback<Dictionary<string, int>>(q => capturedQuantities = q)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.QuickSet("basic");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.NotNull(capturedQuantities);
            Assert.Equal(5, capturedQuantities["Flour"]);
            Assert.Equal(12, capturedQuantities["Eggs"]);
            Assert.Equal(3, capturedQuantities["Milk"]);
            Assert.Equal(10, capturedQuantities["Butter"]);
            Assert.Equal(2, capturedQuantities["Sugar"]);

            Assert.Equal("Successfully applied basic ingredient preset!",
                _controller.TempData["Success"]);
        }

        [Fact]
        public async Task QuickSet_WithAsianPreset_SetsCorrectIngredients()
        {
            // Arrange
            Dictionary<string, int> capturedQuantities = null;
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Callback<Dictionary<string, int>>(q => capturedQuantities = q)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.QuickSet("asian");

            // Assert
            Assert.NotNull(capturedQuantities);
            Assert.Equal(8, capturedQuantities["Rice"]);
            Assert.Equal(6, capturedQuantities["Eggs"]);
            Assert.Equal(4, capturedQuantities["Vegetables"]);
            Assert.Equal(2, capturedQuantities["Chicken"]);

            Assert.Equal("Successfully applied asian ingredient preset!",
                _controller.TempData["Success"]);
        }

        [Fact]
        public async Task QuickSet_WithItalianPreset_SetsCorrectIngredients()
        {
            // Arrange
            Dictionary<string, int> capturedQuantities = null;
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Callback<Dictionary<string, int>>(q => capturedQuantities = q)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.QuickSet("italian");

            // Assert
            Assert.NotNull(capturedQuantities);
            Assert.Equal(4, capturedQuantities["Pasta"]);
            Assert.Equal(2, capturedQuantities["Cheese"]);
            Assert.Equal(2, capturedQuantities["Milk"]);
            Assert.Equal(5, capturedQuantities["Butter"]);

            Assert.Equal("Successfully applied italian ingredient preset!",
                _controller.TempData["Success"]);
        }

        [Fact]
        public async Task QuickSet_WithCaseInsensitivePreset_Works()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.QuickSet("BASIC");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Successfully applied BASIC ingredient preset!",
                _controller.TempData["Success"]);
        }

        [Fact]
        public async Task QuickSet_WithUnknownPreset_SetsErrorMessage()
        {
            // Act
            var result = await _controller.QuickSet("unknown");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Unknown preset selected.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task QuickSet_WithNullPreset_SetsErrorMessage()
        {
            // Act
            var result = await _controller.QuickSet(null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Unknown preset selected.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task QuickSet_WithException_RedirectsWithError()
        {
            // Arrange
            _mockOptimizerService.Setup(x => x.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.QuickSet("basic");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Unable to apply preset. Please try again.",
                _controller.TempData["Error"]);
        }

        #endregion
    }
}