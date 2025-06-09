using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using LinearOptimizationFoodApp.Data;
using LinearOptimizationFoodApp.Models;
using LinearOptimizationFoodApp.Repositories;

namespace LinearOptimizationFoodApp.Tests;

public class RecipeRepositoryTests : IDisposable
{
    private readonly FoodOptimizerContext _context;
    private readonly RecipeRepository _repository;

    public RecipeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<FoodOptimizerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FoodOptimizerContext(options);
        _repository = new RecipeRepository(_context);

        SeedTestData();
    }

    #region GetAllRecipesAsync Tests

    [Fact]
    public async Task GetAllRecipesAsync_ReturnsAllRecipes()
    {
        // Act
        var result = await _repository.GetAllRecipesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3); // Based on seeded data
        result.Should().BeInAscendingOrder(r => r.Name); // Ordered by name
    }

    [Fact]
    public async Task GetAllRecipesAsync_IncludesRecipeIngredientsAndIngredients()
    {
        // Act
        var result = await _repository.GetAllRecipesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(r => r.RecipeIngredients != null);

        // Verify specific recipe has ingredients loaded
        var pancakes = result.FirstOrDefault(r => r.Name == "Test Pancakes");
        pancakes.Should().NotBeNull();
        pancakes!.RecipeIngredients.Should().NotBeEmpty();

        // Verify ThenInclude loaded ingredients
        pancakes.RecipeIngredients.Should().OnlyContain(ri => ri.Ingredient != null);
        pancakes.RecipeIngredients.Should().OnlyContain(ri => !string.IsNullOrEmpty(ri.Ingredient.Name));
    }

    [Fact]
    public async Task GetAllRecipesAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - Clear the database
        _context.Recipes.RemoveRange(_context.Recipes);
        _context.RecipeIngredients.RemoveRange(_context.RecipeIngredients);
        _context.Ingredients.RemoveRange(_context.Ingredients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllRecipesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRecipesAsync_OrdersByName()
    {
        // Act
        var result = await _repository.GetAllRecipesAsync();

        // Assert
        result.Should().NotBeNull();
        var names = result.Select(r => r.Name).ToList();
        names.Should().BeInAscendingOrder();

        // Verify specific order based on our test data
        names.Should().Contain("Test Bread");
        names.Should().Contain("Test Fried Rice");
        names.Should().Contain("Test Pancakes");
    }

    [Fact]
    public async Task GetAllRecipesAsync_VerifiesRequiredIngredientsProperty()
    {
        // Act
        var result = await _repository.GetAllRecipesAsync();

        // Assert
        var pancakes = result.FirstOrDefault(r => r.Name == "Test Pancakes");
        pancakes.Should().NotBeNull();

        // Test the computed RequiredIngredients property
        var requiredIngredients = pancakes!.RequiredIngredients;
        requiredIngredients.Should().NotBeEmpty();
        requiredIngredients.Should().ContainKey("Flour");
        requiredIngredients.Should().ContainKey("Eggs");
        requiredIngredients["Flour"].Should().Be(2);
        requiredIngredients["Eggs"].Should().Be(2);
    }

    #endregion

    #region GetRecipeByIdAsync Tests

    [Fact]
    public async Task GetRecipeByIdAsync_WithValidId_ReturnsRecipe()
    {
        // Act
        var result = await _repository.GetRecipeByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Pancakes");
        result.Description.Should().Be("Test pancakes recipe");
        result.Feeds.Should().Be(4);
    }

    [Fact]
    public async Task GetRecipeByIdAsync_WithValidId_IncludesRecipeIngredientsAndIngredients()
    {
        // Act
        var result = await _repository.GetRecipeByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.RecipeIngredients.Should().NotBeNull();
        result.RecipeIngredients.Should().NotBeEmpty();
        result.RecipeIngredients.Should().HaveCount(2); // Flour and Eggs

        // Verify ThenInclude loaded ingredients
        result.RecipeIngredients.Should().OnlyContain(ri => ri.Ingredient != null);

        var flourIngredient = result.RecipeIngredients.FirstOrDefault(ri => ri.Ingredient.Name == "Flour");
        flourIngredient.Should().NotBeNull();
        flourIngredient!.Quantity.Should().Be(2);
        flourIngredient.Ingredient.Name.Should().Be("Flour");
        flourIngredient.Ingredient.Unit.Should().Be("cups");
    }

    [Fact]
    public async Task GetRecipeByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetRecipeByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecipeByIdAsync_WithZeroId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetRecipeByIdAsync(0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecipeByIdAsync_WithNegativeId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetRecipeByIdAsync(-1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddRecipeAsync Tests

    [Fact]
    public async Task AddRecipeAsync_WithValidRecipe_AddsToDatabase()
    {
        // Arrange
        var newRecipe = new Recipe
        {
            Name = "New Test Recipe",
            Description = "Test description",
            Feeds = 3,
            RecipeIngredients = new List<RecipeIngredient>()
        };
        var initialCount = await _context.Recipes.CountAsync();

        // Act
        var result = await _repository.AddRecipeAsync(newRecipe);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Test Recipe");
        result.Description.Should().Be("Test description");
        result.Feeds.Should().Be(3);

        var currentCount = await _context.Recipes.CountAsync();
        currentCount.Should().Be(initialCount + 1);

        var dbRecipe = await _context.Recipes.FindAsync(result.Id);
        dbRecipe.Should().NotBeNull();
        dbRecipe!.Name.Should().Be("New Test Recipe");
    }

    [Fact]
    public async Task AddRecipeAsync_WithRecipeIngredients_AddsCompleteRecipe()
    {
        // Arrange
        var newRecipe = new Recipe
        {
            Name = "Complex Test Recipe",
            Description = "Recipe with ingredients",
            Feeds = 5,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = 1, Quantity = 3 }, // Flour
                new() { IngredientId = 2, Quantity = 4 }  // Eggs
            }
        };

        // Act
        var result = await _repository.AddRecipeAsync(newRecipe);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);

        // Verify recipe ingredients were added
        var dbRecipeIngredients = await _context.RecipeIngredients
            .Where(ri => ri.RecipeId == result.Id)
            .ToListAsync();

        dbRecipeIngredients.Should().HaveCount(2);
        dbRecipeIngredients.Should().Contain(ri => ri.IngredientId == 1 && ri.Quantity == 3);
        dbRecipeIngredients.Should().Contain(ri => ri.IngredientId == 2 && ri.Quantity == 4);
    }

    

    #endregion

    #region UpdateRecipeAsync Tests

    [Fact]
    public async Task UpdateRecipeAsync_WithValidRecipe_UpdatesInDatabase()
    {
        // Arrange
        var existingRecipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == 1);
        existingRecipe.Should().NotBeNull();

        existingRecipe!.Name = "Updated Pancakes";
        existingRecipe.Description = "Updated description";
        existingRecipe.Feeds = 6;

        // Act
        var result = await _repository.UpdateRecipeAsync(existingRecipe);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Updated Pancakes");
        result.Description.Should().Be("Updated description");
        result.Feeds.Should().Be(6);

        // Verify in database
        var dbRecipe = await _context.Recipes.FindAsync(1);
        dbRecipe.Should().NotBeNull();
        dbRecipe!.Name.Should().Be("Updated Pancakes");
        dbRecipe.Description.Should().Be("Updated description");
        dbRecipe.Feeds.Should().Be(6);
    }

    [Fact]
    public async Task UpdateRecipeAsync_RemovesAndAddsRecipeIngredients()
    {
        // Arrange
        var existingRecipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == 1);
        existingRecipe.Should().NotBeNull();

        // Verify initial state
        var initialIngredients = await _context.RecipeIngredients
            .Where(ri => ri.RecipeId == 1)
            .ToListAsync();
        initialIngredients.Should().HaveCount(2);

        // Modify recipe ingredients
        existingRecipe!.RecipeIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = 1, IngredientId = 3, Quantity = 5 }, // Rice instead of flour/eggs
            new() { RecipeId = 1, IngredientId = 4, Quantity = 2 }  // Milk
        };

        // Act
        var result = await _repository.UpdateRecipeAsync(existingRecipe);

        // Assert
        result.Should().NotBeNull();

        // Verify old ingredients were removed and new ones added
        var updatedIngredients = await _context.RecipeIngredients
            .Where(ri => ri.RecipeId == 1)
            .ToListAsync();

        updatedIngredients.Should().HaveCount(2);
        updatedIngredients.Should().Contain(ri => ri.IngredientId == 3 && ri.Quantity == 5);
        updatedIngredients.Should().Contain(ri => ri.IngredientId == 4 && ri.Quantity == 2);
        updatedIngredients.Should().NotContain(ri => ri.IngredientId == 1); // No more flour
        updatedIngredients.Should().NotContain(ri => ri.IngredientId == 2); // No more eggs
    }

    [Fact]
    public async Task UpdateRecipeAsync_WithEmptyRecipeIngredients_RemovesAllIngredients()
    {
        // Arrange
        var existingRecipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == 1);
        existingRecipe.Should().NotBeNull();

        // Clear recipe ingredients
        existingRecipe!.RecipeIngredients = new List<RecipeIngredient>();

        // Act
        var result = await _repository.UpdateRecipeAsync(existingRecipe);

        // Assert
        result.Should().NotBeNull();

        // Verify all ingredients were removed
        var remainingIngredients = await _context.RecipeIngredients
            .Where(ri => ri.RecipeId == 1)
            .ToListAsync();

        remainingIngredients.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateRecipeAsync_WithNewRecipe_ThrowsException()
    {
        // Arrange
        var newRecipe = new Recipe
        {
            Id = 999, // Non-existent ID
            Name = "Non-existent Recipe",
            Description = "This doesn't exist",
            Feeds = 4,
            RecipeIngredients = new List<RecipeIngredient>()
        };

        // Act & Assert
        var act = async () => await _repository.UpdateRecipeAsync(newRecipe);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateRecipeAsync_DoesNotAffectOtherRecipes()
    {
        // Arrange
        var recipe1 = await _context.Recipes.FindAsync(1);
        var recipe2 = await _context.Recipes.FindAsync(2);
        recipe1.Should().NotBeNull();
        recipe2.Should().NotBeNull();

        var originalRecipe2Name = recipe2!.Name;

        // Update only recipe 1
        recipe1!.Name = "Updated Recipe 1";

        // Act
        await _repository.UpdateRecipeAsync(recipe1);

        // Assert
        var updatedRecipe2 = await _context.Recipes.FindAsync(2);
        updatedRecipe2.Should().NotBeNull();
        updatedRecipe2!.Name.Should().Be(originalRecipe2Name); // Should remain unchanged
    }

    #endregion

    #region DeleteRecipeAsync Tests

    [Fact]
    public async Task DeleteRecipeAsync_WithValidId_RemovesFromDatabase()
    {
        // Arrange
        var initialCount = await _context.Recipes.CountAsync();
        var recipeToDelete = await _context.Recipes.FindAsync(3); // Test Bread
        recipeToDelete.Should().NotBeNull();

        // Act
        await _repository.DeleteRecipeAsync(3);

        // Assert
        var currentCount = await _context.Recipes.CountAsync();
        currentCount.Should().Be(initialCount - 1);

        var deletedRecipe = await _context.Recipes.FindAsync(3);
        deletedRecipe.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRecipeAsync_RemovesRecipeIngredientsAsCascade()
    {
        // Arrange
        var initialRecipeIngredientCount = await _context.RecipeIngredients.CountAsync();
        var recipeIngredientsCount = await _context.RecipeIngredients
            .Where(ri => ri.RecipeId == 1)
            .CountAsync();
        recipeIngredientsCount.Should().BeGreaterThan(0);

        // Act
        await _repository.DeleteRecipeAsync(1);

        // Assert
        var currentRecipeIngredientCount = await _context.RecipeIngredients.CountAsync();
        currentRecipeIngredientCount.Should().Be(initialRecipeIngredientCount - recipeIngredientsCount);

        var remainingRecipeIngredients = await _context.RecipeIngredients
            .Where(ri => ri.RecipeId == 1)
            .ToListAsync();
        remainingRecipeIngredients.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRecipeAsync_WithInvalidId_DoesNothing()
    {
        // Arrange
        var initialCount = await _context.Recipes.CountAsync();

        // Act
        await _repository.DeleteRecipeAsync(999);

        // Assert
        var currentCount = await _context.Recipes.CountAsync();
        currentCount.Should().Be(initialCount); // Count should remain the same
    }

    [Fact]
    public async Task DeleteRecipeAsync_WithZeroId_DoesNothing()
    {
        // Arrange
        var initialCount = await _context.Recipes.CountAsync();

        // Act
        await _repository.DeleteRecipeAsync(0);

        // Assert
        var currentCount = await _context.Recipes.CountAsync();
        currentCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task DeleteRecipeAsync_DoesNotAffectOtherRecipes()
    {
        // Arrange
        var allRecipesBefore = await _context.Recipes.ToListAsync();
        var recipeToKeep = allRecipesBefore.FirstOrDefault(r => r.Id == 2);
        recipeToKeep.Should().NotBeNull();

        // Act
        await _repository.DeleteRecipeAsync(1);

        // Assert
        var remainingRecipe = await _context.Recipes.FindAsync(2);
        remainingRecipe.Should().NotBeNull();
        remainingRecipe!.Name.Should().Be(recipeToKeep!.Name);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task AddUpdateDeleteRecipe_FullLifecycleTest()
    {
        // Add
        var newRecipe = new Recipe
        {
            Name = "Lifecycle Test Recipe",
            Description = "Testing full lifecycle",
            Feeds = 4,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = 1, Quantity = 2 }
            }
        };
        var added = await _repository.AddRecipeAsync(newRecipe);
        added.Id.Should().BeGreaterThan(0);

        // Update
        added.Name = "Updated Lifecycle Recipe";
        added.Feeds = 8;
        added.RecipeIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = added.Id, IngredientId = 2, Quantity = 4 }
        };
        var updated = await _repository.UpdateRecipeAsync(added);
        updated.Name.Should().Be("Updated Lifecycle Recipe");
        updated.Feeds.Should().Be(8);

        // Verify update persisted
        var retrieved = await _repository.GetRecipeByIdAsync(added.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Updated Lifecycle Recipe");
        retrieved.RecipeIngredients.Should().HaveCount(1);
        retrieved.RecipeIngredients.First().IngredientId.Should().Be(2);

        // Delete
        await _repository.DeleteRecipeAsync(added.Id);
        var deletedCheck = await _repository.GetRecipeByIdAsync(added.Id);
        deletedCheck.Should().BeNull();
    }

    [Fact]
    public async Task ComplexRecipeWithMultipleIngredients_IntegrationTest()
    {
        // Arrange
        var complexRecipe = new Recipe
        {
            Name = "Complex Recipe",
            Description = "Recipe with many ingredients",
            Feeds = 10,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = 1, Quantity = 5 }, // Flour
                new() { IngredientId = 2, Quantity = 8 }, // Eggs
                new() { IngredientId = 3, Quantity = 3 }, // Rice
                new() { IngredientId = 4, Quantity = 2 }  // Milk
            }
        };

        // Act
        var added = await _repository.AddRecipeAsync(complexRecipe);
        var retrieved = await _repository.GetRecipeByIdAsync(added.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.RecipeIngredients.Should().HaveCount(4);
        retrieved.RequiredIngredients.Should().HaveCount(4);
        retrieved.RequiredIngredients["Flour"].Should().Be(5);
        retrieved.RequiredIngredients["Eggs"].Should().Be(8);
        retrieved.RequiredIngredients["Rice"].Should().Be(3);
        retrieved.RequiredIngredients["Milk"].Should().Be(2);
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
            },
            new()
            {
                Id = 3,
                Name = "Test Bread",
                Description = "Simple bread recipe",
                Feeds = 8,
                RecipeIngredients = new List<RecipeIngredient>
                {
                    new() { RecipeId = 3, IngredientId = 1, Quantity = 5 }, // Flour
                    new() { RecipeId = 3, IngredientId = 4, Quantity = 1 }  // Milk
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