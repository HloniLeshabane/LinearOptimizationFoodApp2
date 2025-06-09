using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using LinearOptimizationFoodApp.Data;
using LinearOptimizationFoodApp.Models;
using LinearOptimizationFoodApp.Repositories;

namespace LinearOptimizationFoodApp.Tests;

public class IngredientRepositoryTests : IDisposable
{
    private readonly FoodOptimizerContext _context;
    private readonly IngredientRepository _repository;

    public IngredientRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<FoodOptimizerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FoodOptimizerContext(options);
        _repository = new IngredientRepository(_context);

        SeedTestData();
    }

    #region GetAllIngredientsAsync Tests

    [Fact]
    public async Task GetAllIngredientsAsync_ReturnsAllIngredients()
    {
        // Act
        var result = await _repository.GetAllIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4); // Based on seeded data
        result.Should().BeInAscendingOrder(i => i.Name); // Ordered by name
    }

    [Fact]
    public async Task GetAllIngredientsAsync_IncludesRecipeIngredients()
    {
        // Act
        var result = await _repository.GetAllIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(i => i.RecipeIngredients != null);

        // Verify specific ingredient has recipe ingredients loaded
        var flour = result.FirstOrDefault(i => i.Name == "Flour");
        flour.Should().NotBeNull();
        flour!.RecipeIngredients.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllIngredientsAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - Clear the database
        _context.Ingredients.RemoveRange(_context.Ingredients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllIngredientsAsync_OrdersByName()
    {
        // Act
        var result = await _repository.GetAllIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        var names = result.Select(i => i.Name).ToList();
        names.Should().BeInAscendingOrder();

        // Verify specific order based on our test data
        names[0].Should().Be("Eggs");
        names[1].Should().Be("Flour");
        names[2].Should().Be("Milk");
        names[3].Should().Be("Rice");
    }

    #endregion

    #region GetIngredientByIdAsync Tests

    [Fact]
    public async Task GetIngredientByIdAsync_WithValidId_ReturnsIngredient()
    {
        // Act
        var result = await _repository.GetIngredientByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Flour");
        result.Unit.Should().Be("cups");
    }

    [Fact]
    public async Task GetIngredientByIdAsync_WithValidId_IncludesRecipeIngredientsAndRecipes()
    {
        // Act
        var result = await _repository.GetIngredientByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.RecipeIngredients.Should().NotBeNull();
        result.RecipeIngredients.Should().NotBeEmpty();

        // Verify that recipes are included through ThenInclude
        result.RecipeIngredients.Should().OnlyContain(ri => ri.Recipe != null);

        var firstRecipeIngredient = result.RecipeIngredients.First();
        firstRecipeIngredient.Recipe.Should().NotBeNull();
        firstRecipeIngredient.Recipe.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetIngredientByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetIngredientByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetIngredientByIdAsync_WithZeroId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetIngredientByIdAsync(0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetIngredientByIdAsync_WithNegativeId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetIngredientByIdAsync(-1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddIngredientAsync Tests

    [Fact]
    public async Task AddIngredientAsync_WithValidIngredient_AddsToDatabase()
    {
        // Arrange
        var newIngredient = new Ingredient
        {
            Name = "New Test Ingredient",
            Unit = "units"
        };
        var initialCount = await _context.Ingredients.CountAsync();

        // Act
        var result = await _repository.AddIngredientAsync(newIngredient);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Test Ingredient");
        result.Unit.Should().Be("units");

        var currentCount = await _context.Ingredients.CountAsync();
        currentCount.Should().Be(initialCount + 1);

        var dbIngredient = await _context.Ingredients.FindAsync(result.Id);
        dbIngredient.Should().NotBeNull();
        dbIngredient!.Name.Should().Be("New Test Ingredient");
    }

    [Fact]
    public async Task AddIngredientAsync_SetsIdAfterSaving()
    {
        // Arrange
        var newIngredient = new Ingredient
        {
            Name = "Another Test Ingredient",
            Unit = "grams"
        };

        // Verify ID is not set before adding
        newIngredient.Id.Should().Be(0);

        // Act
        var result = await _repository.AddIngredientAsync(newIngredient);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        newIngredient.Id.Should().BeGreaterThan(0); // Original object should also have ID set
        result.Id.Should().Be(newIngredient.Id);
    }

    #endregion

    #region UpdateIngredientAsync Tests

    [Fact]
    public async Task UpdateIngredientAsync_WithValidIngredient_UpdatesInDatabase()
    {
        // Arrange
        var existingIngredient = await _context.Ingredients.FindAsync(1);
        existingIngredient.Should().NotBeNull();

        existingIngredient!.Name = "Updated Flour";
        existingIngredient.Unit = "kilograms";

        // Act
        var result = await _repository.UpdateIngredientAsync(existingIngredient);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Updated Flour");
        result.Unit.Should().Be("kilograms");

        // Verify in database
        var dbIngredient = await _context.Ingredients.FindAsync(1);
        dbIngredient.Should().NotBeNull();
        dbIngredient!.Name.Should().Be("Updated Flour");
        dbIngredient.Unit.Should().Be("kilograms");
    }

    [Fact]
    public async Task UpdateIngredientAsync_WithNewIngredient_ThrowsException()
    {
        // Arrange
        var newIngredient = new Ingredient
        {
            Id = 999, // Non-existent ID
            Name = "Non-existent Ingredient",
            Unit = "units"
        };

        // Act & Assert
        var act = async () => await _repository.UpdateIngredientAsync(newIngredient);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    #endregion

    #region DeleteIngredientAsync Tests

    [Fact]
    public async Task DeleteIngredientAsync_WithValidId_RemovesFromDatabase()
    {
        // Arrange
        var initialCount = await _context.Ingredients.CountAsync();
        var ingredientToDelete = await _context.Ingredients.FindAsync(4); // Milk
        ingredientToDelete.Should().NotBeNull();

        // Act
        await _repository.DeleteIngredientAsync(4);

        // Assert
        var currentCount = await _context.Ingredients.CountAsync();
        currentCount.Should().Be(initialCount - 1);

        var deletedIngredient = await _context.Ingredients.FindAsync(4);
        deletedIngredient.Should().BeNull();
    }

    [Fact]
    public async Task DeleteIngredientAsync_WithInvalidId_DoesNothing()
    {
        // Arrange
        var initialCount = await _context.Ingredients.CountAsync();

        // Act
        await _repository.DeleteIngredientAsync(999);

        // Assert
        var currentCount = await _context.Ingredients.CountAsync();
        currentCount.Should().Be(initialCount); // Count should remain the same
    }

    [Fact]
    public async Task DeleteIngredientAsync_WithZeroId_DoesNothing()
    {
        // Arrange
        var initialCount = await _context.Ingredients.CountAsync();

        // Act
        await _repository.DeleteIngredientAsync(0);

        // Assert
        var currentCount = await _context.Ingredients.CountAsync();
        currentCount.Should().Be(initialCount);
    }

    #endregion

    #region GetAvailableIngredientsAsync Tests

    [Fact]
    public async Task GetAvailableIngredientsAsync_ReturnsCorrectIngredients()
    {
        // Arrange - Add some available ingredients
        var availableIngredients = new List<AvailableIngredient>
        {
            new() { IngredientId = 1, Quantity = 5 }, // Flour
            new() { IngredientId = 2, Quantity = 12 } // Eggs
        };
        _context.AvailableIngredients.AddRange(availableIngredients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAvailableIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("Flour");
        result.Should().ContainKey("Eggs");
        result["Flour"].Should().Be(5);
        result["Eggs"].Should().Be(12);
    }

    [Fact]
    public async Task GetAvailableIngredientsAsync_WithNoAvailableIngredients_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _repository.GetAvailableIngredientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableIngredientsAsync_IncludesIngredientNames()
    {
        // Arrange
        var availableIngredients = new List<AvailableIngredient>
        {
            new() { IngredientId = 1, Quantity = 3 }, // Flour
            new() { IngredientId = 3, Quantity = 8 }  // Rice
        };
        _context.AvailableIngredients.AddRange(availableIngredients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAvailableIngredientsAsync();

        // Assert
        result.Keys.Should().OnlyContain(key => !string.IsNullOrEmpty(key));
        result.Keys.Should().Contain("Flour");
        result.Keys.Should().Contain("Rice");
    }

    #endregion

    #region SetAvailableIngredientsAsync Tests

    [Fact]
    public async Task SetAvailableIngredientsAsync_WithValidIngredients_SetsCorrectly()
    {
        // Arrange
        var ingredients = new Dictionary<string, int>
        {
            ["Flour"] = 15,
            ["Eggs"] = 25,
            ["Rice"] = 10
        };

        // Act
        await _repository.SetAvailableIngredientsAsync(ingredients);

        // Assert
        var result = await _repository.GetAvailableIngredientsAsync();
        result.Should().HaveCount(3);
        result["Flour"].Should().Be(15);
        result["Eggs"].Should().Be(25);
        result["Rice"].Should().Be(10);

        // Verify in database
        var dbAvailable = await _context.AvailableIngredients.Include(ai => ai.Ingredient).ToListAsync();
        dbAvailable.Should().HaveCount(3);
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_ReplacesExistingIngredients()
    {
        // Arrange - Add initial available ingredients
        var initialIngredients = new List<AvailableIngredient>
        {
            new() { IngredientId = 1, Quantity = 5 },
            new() { IngredientId = 2, Quantity = 10 }
        };
        _context.AvailableIngredients.AddRange(initialIngredients);
        await _context.SaveChangesAsync();

        var newIngredients = new Dictionary<string, int>
        {
            ["Rice"] = 20,
            ["Milk"] = 3
        };

        // Act
        await _repository.SetAvailableIngredientsAsync(newIngredients);

        // Assert
        var result = await _repository.GetAvailableIngredientsAsync();
        result.Should().HaveCount(2);
        result.Should().ContainKey("Rice");
        result.Should().ContainKey("Milk");
        result.Should().NotContainKey("Flour");
        result.Should().NotContainKey("Eggs");
        result["Rice"].Should().Be(20);
        result["Milk"].Should().Be(3);
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_WithZeroQuantities_DoesNotAddThem()
    {
        // Arrange
        var ingredients = new Dictionary<string, int>
        {
            ["Flour"] = 15,
            ["Eggs"] = 0,    // Zero quantity
            ["Rice"] = 10
        };

        // Act
        await _repository.SetAvailableIngredientsAsync(ingredients);

        // Assert
        var result = await _repository.GetAvailableIngredientsAsync();
        result.Should().HaveCount(2); // Only Flour and Rice
        result.Should().ContainKey("Flour");
        result.Should().ContainKey("Rice");
        result.Should().NotContainKey("Eggs");
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_WithNegativeQuantities_DoesNotAddThem()
    {
        // Arrange
        var ingredients = new Dictionary<string, int>
        {
            ["Flour"] = 15,
            ["Eggs"] = -5,   // Negative quantity
            ["Rice"] = 10
        };

        // Act
        await _repository.SetAvailableIngredientsAsync(ingredients);

        // Assert
        var result = await _repository.GetAvailableIngredientsAsync();
        result.Should().HaveCount(2); // Only Flour and Rice
        result.Should().NotContainKey("Eggs");
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_WithNonExistentIngredient_IgnoresIt()
    {
        // Arrange
        var ingredients = new Dictionary<string, int>
        {
            ["Flour"] = 15,
            ["NonExistentIngredient"] = 10, // This ingredient doesn't exist
            ["Rice"] = 8
        };

        // Act
        await _repository.SetAvailableIngredientsAsync(ingredients);

        // Assert
        var result = await _repository.GetAvailableIngredientsAsync();
        result.Should().HaveCount(2); // Only Flour and Rice
        result.Should().ContainKey("Flour");
        result.Should().ContainKey("Rice");
        result.Should().NotContainKey("NonExistentIngredient");
    }

    [Fact]
    public async Task SetAvailableIngredientsAsync_WithEmptyDictionary_ClearsAllAvailableIngredients()
    {
        // Arrange - Add some initial available ingredients
        var initialIngredients = new List<AvailableIngredient>
        {
            new() { IngredientId = 1, Quantity = 5 },
            new() { IngredientId = 2, Quantity = 10 }
        };
        _context.AvailableIngredients.AddRange(initialIngredients);
        await _context.SaveChangesAsync();

        var emptyIngredients = new Dictionary<string, int>();

        // Act
        await _repository.SetAvailableIngredientsAsync(emptyIngredients);

        // Assert
        var result = await _repository.GetAvailableIngredientsAsync();
        result.Should().BeEmpty();

        var dbAvailable = await _context.AvailableIngredients.ToListAsync();
        dbAvailable.Should().BeEmpty();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SetAndGetAvailableIngredients_IntegrationTest()
    {
        // Arrange
        var testIngredients = new Dictionary<string, int>
        {
            ["Flour"] = 15,
            ["Eggs"] = 25,
            ["Rice"] = 12
        };

        // Act
        await _repository.SetAvailableIngredientsAsync(testIngredients);
        var result = await _repository.GetAvailableIngredientsAsync();

        // Assert
        result.Should().BeEquivalentTo(testIngredients);
    }

    [Fact]
    public async Task AddUpdateDeleteIngredient_FullLifecycleTest()
    {
        // Add
        var newIngredient = new Ingredient { Name = "Test Ingredient", Unit = "units" };
        var added = await _repository.AddIngredientAsync(newIngredient);
        added.Id.Should().BeGreaterThan(0);

        // Update
        added.Name = "Updated Test Ingredient";
        added.Unit = "pieces";
        var updated = await _repository.UpdateIngredientAsync(added);
        updated.Name.Should().Be("Updated Test Ingredient");
        updated.Unit.Should().Be("pieces");

        // Verify update persisted
        var retrieved = await _repository.GetIngredientByIdAsync(added.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Updated Test Ingredient");

        // Delete
        await _repository.DeleteIngredientAsync(added.Id);
        var deletedCheck = await _repository.GetIngredientByIdAsync(added.Id);
        deletedCheck.Should().BeNull();
    }

    #endregion

    #region Helper Methods and Cleanup

    private void SeedTestData()
    {
        var ingredients = new List<Ingredient>
        {
            new() { Id = 1, Name = "Flour", Unit = "cups" },
            new() { Id = 2, Name = "Eggs", Unit = "pieces" },
            new() { Id = 3, Name = "Rice", Unit = "cups" },
            new() { Id = 4, Name = "Milk", Unit = "liters" }
        };

        _context.Ingredients.AddRange(ingredients);
        _context.SaveChanges();

        var recipes = new List<Recipe>
        {
            new()
            {
                Id = 1,
                Name = "Test Pancakes",
                Description = "Test pancakes recipe",
                Feeds = 4,
                RecipeIngredients = new List<RecipeIngredient>
                {
                    new() { RecipeId = 1, IngredientId = 1, Quantity = 2 }, // Flour
                    new() { RecipeId = 1, IngredientId = 2, Quantity = 2 }  // Eggs
                }
            },
            new()
            {
                Id = 2,
                Name = "Test Fried Rice",
                Description = "Test fried rice recipe",
                Feeds = 6,
                RecipeIngredients = new List<RecipeIngredient>
                {
                    new() { RecipeId = 2, IngredientId = 3, Quantity = 3 }, // Rice
                    new() { RecipeId = 2, IngredientId = 2, Quantity = 2 }  // Eggs
                }
            }
        };

        _context.Recipes.AddRange(recipes);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #endregion
}