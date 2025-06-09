using Xunit;
using FluentAssertions;
using Moq;
using LinearOptimizationFoodApp.Models;
using LinearOptimizationFoodApp.Repositories;
using LinearOptimizationFoodApp.Services;
using LinearOptimizationFoodApp.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace LinearOptimizationFoodApp.Tests;

public class OptimizerServiceTests
{
    private readonly Mock<IRecipeRepository> _recipeRepositoryMock;
    private readonly Mock<IIngredientRepository> _ingredientRepositoryMock;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly OptimizerService _service;

    // Use the same constants as the service
    private const string ALL_RECIPES_CACHE_KEY = "all_recipes";
    private const string ALL_INGREDIENTS_CACHE_KEY = "all_ingredients";
    private const string AVAILABLE_INGREDIENTS_CACHE_KEY = "available_ingredients";

    public OptimizerServiceTests()
    {
        _recipeRepositoryMock = new Mock<IRecipeRepository>();
        _ingredientRepositoryMock = new Mock<IIngredientRepository>();
        _memoryCacheMock = new Mock<IMemoryCache>();

        _service = new OptimizerService(_recipeRepositoryMock.Object, _ingredientRepositoryMock.Object, _memoryCacheMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRecipeRepository_ThrowsException()
    {
        // Act & Assert
        var act = () => new OptimizerService(null!, _ingredientRepositoryMock.Object, _memoryCacheMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("recipeRepository");
    }

    [Fact]
    public void Constructor_WithNullIngredientRepository_ThrowsException()
    {
        // Act & Assert
        var act = () => new OptimizerService(_recipeRepositoryMock.Object, null!, _memoryCacheMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("ingredientRepository");
    }

    [Fact]
    public void Constructor_WithNullMemoryCache_ThrowsException()
    {
        // Act & Assert
        var act = () => new OptimizerService(_recipeRepositoryMock.Object, _ingredientRepositoryMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    #endregion

    #region FindOptimalCombinationAsync Tests

    [Fact]
    public async Task FindOptimalCombinationAsync_WithNoIngredients_ReturnsEmptyResult()
    {
        // Arrange
        var recipes = CreateTestRecipes();
        var emptyIngredients = new Dictionary<string, int>();

        SetupCacheMiss();
        _recipeRepositoryMock.Setup(r => r.GetAllRecipesAsync()).ReturnsAsync(recipes);
        _ingredientRepositoryMock.Setup(i => i.GetAvailableIngredientsAsync()).ReturnsAsync(emptyIngredients);

        // Act
        var result = await _service.FindOptimalCombinationAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasResults.Should().BeFalse();
        result.MaxPeopleFed.Should().Be(0);
        result.BestCombination.Should().BeEmpty();
        result.UsedIngredients.Should().BeEmpty();
        result.RemainingIngredients.Should().BeEmpty();
    }

    [Fact]
    public async Task FindOptimalCombinationAsync_WithInsufficientIngredients_ReturnsEmptyResult()
    {
        // Arrange
        var recipes = CreateTestRecipes();
        var insufficientIngredients = new Dictionary<string, int> { ["Flour"] = 1, ["Eggs"] = 1 }; // Not enough for any recipe

        SetupCacheMiss();
        _recipeRepositoryMock.Setup(r => r.GetAllRecipesAsync()).ReturnsAsync(recipes);
        _ingredientRepositoryMock.Setup(i => i.GetAvailableIngredientsAsync()).ReturnsAsync(insufficientIngredients);

        // Act
        var result = await _service.FindOptimalCombinationAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasResults.Should().BeFalse();
        result.MaxPeopleFed.Should().Be(0);
        result.BestCombination.Should().BeEmpty();
    }
    

    [Fact]
    public async Task FindOptimalCombinationAsync_WhenRepositoryThrows_ReturnsEmptyResult()
    {
        // Arrange
        SetupCacheMiss();
        _recipeRepositoryMock.Setup(r => r.GetAllRecipesAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.FindOptimalCombinationAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasResults.Should().BeFalse();
        result.MaxPeopleFed.Should().Be(0);
        result.BestCombination.Should().BeEmpty();
        result.UsedIngredients.Should().BeEmpty();
        result.RemainingIngredients.Should().BeEmpty();
    }

    [Fact]
    public async Task FindOptimalCombinationAsync_WhenIngredientRepositoryThrows_ReturnsEmptyResult()
    {
        // Arrange
        var recipes = CreateTestRecipes();
        SetupCacheMiss();
        _recipeRepositoryMock.Setup(r => r.GetAllRecipesAsync()).ReturnsAsync(recipes);
        _ingredientRepositoryMock.Setup(i => i.GetAvailableIngredientsAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.FindOptimalCombinationAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasResults.Should().BeFalse();
    }

    #endregion

    #region GetAllRecipesAsync Tests

    

    [Fact]
    public async Task GetAllRecipesAsync_WhenRepositoryReturnsNull_ReturnsEmptyList()
    {
        // Arrange
        SetupCacheMiss();
        _recipeRepositoryMock.Setup(r => r.GetAllRecipesAsync()).ReturnsAsync((List<Recipe>?)null);

        // Act
        var result = await _service.GetAllRecipesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRecipesAsync_WhenRepositoryThrows_ReturnsEmptyList()
    {
        // Arrange
        SetupCacheMiss();
        _recipeRepositoryMock.Setup(r => r.GetAllRecipesAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetAllRecipesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRecipesAsync_UsesCaching_WhenCacheHit()
    {
        // Arrange
        var cachedRecipes = CreateTestRecipes();
        object cacheValue = cachedRecipes;

        _memoryCacheMock.Setup(x => x.TryGetValue(ALL_RECIPES_CACHE_KEY, out cacheValue))
            .Returns(true);

        // Act
        var result = await _service.GetAllRecipesAsync();

        // Assert
        result.Should().BeEquivalentTo(cachedRecipes);
        _recipeRepositoryMock.Verify(r => r.GetAllRecipesAsync(), Times.Never); // Should not call repository when cache hit
    }

    

    #endregion

    #region GetAllIngredientsAsync Tests

    [Fact]
    public async Task GetAllIngredientsAsync_WhenRepositoryReturnsNull_ReturnsEmptyList()
    {
        // Arrange
        SetupCacheMiss();
        _ingredientRepositoryMock.Setup(i => i.GetAllIngredientsAsync()).ReturnsAsync((List<Ingredient>?)null);

        // Act
        var result = await _service.GetAllIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllIngredientsAsync_WhenRepositoryThrows_ReturnsEmptyList()
    {
        // Arrange
        SetupCacheMiss();
        _ingredientRepositoryMock.Setup(i => i.GetAllIngredientsAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetAllIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllIngredientsAsync_UsesCaching_WhenCacheHit()
    {
        // Arrange
        var cachedIngredients = new List<Ingredient> { new() { Id = 1, Name = "Flour", Unit = "cups" } };
        object cacheValue = cachedIngredients;

        _memoryCacheMock.Setup(x => x.TryGetValue(ALL_INGREDIENTS_CACHE_KEY, out cacheValue))
            .Returns(true);

        // Act
        var result = await _service.GetAllIngredientsAsync();

        // Assert
        result.Should().BeEquivalentTo(cachedIngredients);
        _ingredientRepositoryMock.Verify(i => i.GetAllIngredientsAsync(), Times.Never);
    }

    #endregion

    #region GetAvailableIngredientsAsync Tests

   

    [Fact]
    public async Task GetAvailableIngredientsAsync_WhenRepositoryReturnsNull_ReturnsEmptyDictionary()
    {
        // Arrange
        SetupCacheMiss();
        _ingredientRepositoryMock.Setup(i => i.GetAvailableIngredientsAsync()).ReturnsAsync((Dictionary<string, int>?)null);

        // Act
        var result = await _service.GetAvailableIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableIngredientsAsync_WhenRepositoryThrows_ReturnsEmptyDictionary()
    {
        // Arrange
        SetupCacheMiss();
        _ingredientRepositoryMock.Setup(i => i.GetAvailableIngredientsAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetAvailableIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableIngredientsAsync_UsesCaching_WhenCacheHit()
    {
        // Arrange
        var cachedIngredients = new Dictionary<string, int> { ["Flour"] = 5 };
        object cacheValue = cachedIngredients;

        _memoryCacheMock.Setup(x => x.TryGetValue(AVAILABLE_INGREDIENTS_CACHE_KEY, out cacheValue))
            .Returns(true);

        // Act
        var result = await _service.GetAvailableIngredientsAsync();

        // Assert
        result.Should().BeEquivalentTo(cachedIngredients);
        _ingredientRepositoryMock.Verify(i => i.GetAvailableIngredientsAsync(), Times.Never);
    }

    #endregion

    #region SetAvailableIngredientsAsync Tests

    [Fact]
    public async Task SetAvailableIngredientsAsync_CallsRepository()
    {
        // Arrange
        var ingredients = new Dictionary<string, int> { ["Flour"] = 5 };

        // Act
        await _service.SetAvailableIngredientsAsync(ingredients);

        // Assert
        _ingredientRepositoryMock.Verify(i => i.SetAvailableIngredientsAsync(ingredients), Times.Once);
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_WithNullIngredients_DoesNotCallRepository()
    {
        // Act
        await _service.SetAvailableIngredientsAsync(null);

        // Assert
        _ingredientRepositoryMock.Verify(i => i.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()), Times.Never);
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_WhenRepositoryThrows_DoesNotThrow()
    {
        // Arrange
        var ingredients = new Dictionary<string, int> { ["Flour"] = 5 };
        _ingredientRepositoryMock.Setup(i => i.SetAvailableIngredientsAsync(It.IsAny<Dictionary<string, int>>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var act = async () => await _service.SetAvailableIngredientsAsync(ingredients);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_WithEmptyDictionary_CallsRepository()
    {
        // Arrange
        var emptyIngredients = new Dictionary<string, int>();

        // Act
        await _service.SetAvailableIngredientsAsync(emptyIngredients);

        // Assert
        _ingredientRepositoryMock.Verify(i => i.SetAvailableIngredientsAsync(emptyIngredients), Times.Once);
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_ClearsCacheAfterUpdate()
    {
        // Arrange
        var ingredients = new Dictionary<string, int> { ["Flour"] = 5 };

        // Act
        await _service.SetAvailableIngredientsAsync(ingredients);

        // Assert
        _memoryCacheMock.Verify(x => x.Remove(AVAILABLE_INGREDIENTS_CACHE_KEY), Times.Once);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public void ClearAllCaches_RemovesAllCacheKeys()
    {
        // Act
        _service.ClearAllCaches();

        // Assert
        _memoryCacheMock.Verify(x => x.Remove(ALL_RECIPES_CACHE_KEY), Times.Once);
        _memoryCacheMock.Verify(x => x.Remove(ALL_INGREDIENTS_CACHE_KEY), Times.Once);
        _memoryCacheMock.Verify(x => x.Remove(AVAILABLE_INGREDIENTS_CACHE_KEY), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupCacheMiss()
    {
        // Setup cache to always return false (cache miss) for default behavior
        _memoryCacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);
    }

    private static List<Recipe> CreateTestRecipes()
    {
        return new List<Recipe>
        {
            CreateRecipe(1, "Pancakes", 4, new Dictionary<string, int> { ["Flour"] = 2, ["Eggs"] = 2 }),
            CreateRecipe(2, "Scrambled Eggs", 2, new Dictionary<string, int> { ["Eggs"] = 3, ["Milk"] = 1 })
        };
    }

    private static Recipe CreateRecipe(int id, string name, int feeds, Dictionary<string, int> ingredients)
    {
        var recipe = new Recipe
        {
            Id = id,
            Name = name,
            Feeds = feeds,
            RecipeIngredients = new List<RecipeIngredient>()
        };

        foreach (var kvp in ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                RecipeId = id,
                Recipe = recipe,
                IngredientId = 0, // Mock ID
                Ingredient = new Ingredient { Name = kvp.Key },
                Quantity = kvp.Value
            });
        }

        return recipe;
    }

    #endregion
}