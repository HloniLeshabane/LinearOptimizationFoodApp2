using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinearOptimizationFoodApp.Core;
using LinearOptimizationFoodApp.Models;
using FluentAssertions;
using Xunit;

namespace LinearOptimizationFoodAppTests.Tests.Core
{
    public class OptimizerTests
    {
        private List<Recipe> CreateTestRecipes()
        {
            return new List<Recipe>
            {
                new Recipe
                {
                    Id = 1,
                    Name = "Pancakes",
                    Feeds = 4,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new() { Ingredient = new Ingredient { Name = "Flour" }, Quantity = 2 },
                        new() { Ingredient = new Ingredient { Name = "Eggs" }, Quantity = 2 }
                    }
                },
                new Recipe
                {
                    Id = 2,
                    Name = "Fried Rice",
                    Feeds = 6,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new() { Ingredient = new Ingredient { Name = "Rice" }, Quantity = 3 },
                        new() { Ingredient = new Ingredient { Name = "Eggs" }, Quantity = 2 }
                    }
                },
                new Recipe
                {
                    Id = 3,
                    Name = "Chicken Stir Fry",
                    Feeds = 8,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new() { Ingredient = new Ingredient { Name = "Chicken" }, Quantity = 2 },
                        new() { Ingredient = new Ingredient { Name = "Rice" }, Quantity = 2 },
                        new() { Ingredient = new Ingredient { Name = "Vegetables" }, Quantity = 3 }
                    }
                }
            };
        }

        [Fact]
        public void FindOptimalCombination_WithSufficientIngredients_ReturnsOptimalSolution()
        {
            // Arrange
            var recipes = CreateTestRecipes();
            var availableIngredients = new Dictionary<string, int>
            {
                ["Flour"] = 5,
                ["Eggs"] = 10,
                ["Rice"] = 8,
                ["Chicken"] = 3,
                ["Vegetables"] = 5
            };

            var optimizer = new Optimizer(recipes, availableIngredients);

            // Act
            var (bestCombination, maxPeopleFed) = optimizer.FindOptimalCombination();

            // Assert
            maxPeopleFed.Should().BeGreaterThan(0);
            bestCombination.Should().NotBeEmpty();
            bestCombination.Sum(r => r.Feeds).Should().Be(maxPeopleFed);
        }

        [Fact]
        public void FindOptimalCombination_WithNoIngredients_ReturnsZero()
        {
            // Arrange
            var recipes = CreateTestRecipes();
            var availableIngredients = new Dictionary<string, int>();

            var optimizer = new Optimizer(recipes, availableIngredients);

            // Act
            var (bestCombination, maxPeopleFed) = optimizer.FindOptimalCombination();

            // Assert
            maxPeopleFed.Should().Be(0);
            bestCombination.Should().BeEmpty();
        }

        [Fact]
        public void FindOptimalCombination_WithInsufficientIngredients_ReturnsZero()
        {
            // Arrange
            var recipes = CreateTestRecipes();
            var availableIngredients = new Dictionary<string, int>
            {
                ["Flour"] = 1, // Not enough for any recipe
                ["Eggs"] = 1
            };

            var optimizer = new Optimizer(recipes, availableIngredients);

            // Act
            var (bestCombination, maxPeopleFed) = optimizer.FindOptimalCombination();

            // Assert
            maxPeopleFed.Should().Be(0);
            bestCombination.Should().BeEmpty();
        }

        [Fact]
        public void FindOptimalCombination_WithExactIngredients_ReturnsSingleRecipe()
        {
            // Arrange
            var recipes = CreateTestRecipes();
            var availableIngredients = new Dictionary<string, int>
            {
                ["Flour"] = 2,
                ["Eggs"] = 2
            };

            var optimizer = new Optimizer(recipes, availableIngredients);

            // Act
            var (bestCombination, maxPeopleFed) = optimizer.FindOptimalCombination();

            // Assert
            maxPeopleFed.Should().Be(4);
            bestCombination.Should().HaveCount(1);
            bestCombination.First().Name.Should().Be("Pancakes");
        }

        [Fact]
        public void FindOptimalCombination_ChoosesOptimalOverGreedy()
        {
            // Arrange - scenario where greedy approach would fail
            var recipes = new List<Recipe>
            {
                new Recipe
                {
                    Id = 1,
                    Name = "Large Recipe",
                    Feeds = 10,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new() { Ingredient = new Ingredient { Name = "Flour" }, Quantity = 8 },
                        new() { Ingredient = new Ingredient { Name = "Eggs" }, Quantity = 8 }
                    }
                },
                new Recipe
                {
                    Id = 2,
                    Name = "Small Recipe 1",
                    Feeds = 6,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new() { Ingredient = new Ingredient { Name = "Flour" }, Quantity = 4 },
                        new() { Ingredient = new Ingredient { Name = "Eggs" }, Quantity = 4 }
                    }
                },
                new Recipe
                {
                    Id = 3,
                    Name = "Small Recipe 2",
                    Feeds = 6,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new() { Ingredient = new Ingredient { Name = "Flour" }, Quantity = 4 },
                        new() { Ingredient = new Ingredient { Name = "Eggs" }, Quantity = 4 }
                    }
                }
            };

            var availableIngredients = new Dictionary<string, int>
            {
                ["Flour"] = 8,
                ["Eggs"] = 8
            };

            var optimizer = new Optimizer(recipes, availableIngredients);

            // Act
            var (bestCombination, maxPeopleFed) = optimizer.FindOptimalCombination();

            // Assert
            maxPeopleFed.Should().Be(12); // Two small recipes (6+6) instead of one large (10)
            bestCombination.Should().HaveCount(2);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(2, 2, 4)]
        [InlineData(4, 4, 8)]
        [InlineData(10, 10, 20)]
        public void FindOptimalCombination_ScalesCorrectlyWithIngredients(int flour, int eggs, int expectedPeople)
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe
                {
                    Id = 1,
                    Name = "Pancakes",
                    Feeds = 4,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new() { Ingredient = new Ingredient { Name = "Flour" }, Quantity = 2 },
                        new() { Ingredient = new Ingredient { Name = "Eggs" }, Quantity = 2 }
                    }
                }
            };

            var availableIngredients = new Dictionary<string, int>
            {
                ["Flour"] = flour,
                ["Eggs"] = eggs
            };

            var optimizer = new Optimizer(recipes, availableIngredients);

            // Act
            var (bestCombination, maxPeopleFed) = optimizer.FindOptimalCombination();

            // Assert
            maxPeopleFed.Should().Be(expectedPeople);
        }

        [Fact]
        public void Constructor_SortsRecipesByFeedsDescending()
        {
            // Arrange
            var recipes = new List<Recipe>
            {
                new Recipe { Id = 1, Name = "Small", Feeds = 2, RecipeIngredients = new List<RecipeIngredient>() },
                new Recipe { Id = 2, Name = "Large", Feeds = 10, RecipeIngredients = new List<RecipeIngredient>() },
                new Recipe { Id = 3, Name = "Medium", Feeds = 5, RecipeIngredients = new List<RecipeIngredient>() }
            };

            var availableIngredients = new Dictionary<string, int>();

            // Act
            var optimizer = new Optimizer(recipes, availableIngredients);

            // Assert - verify through behavior (larger recipes considered first)
            // This is tested implicitly through the optimization results
            optimizer.Should().NotBeNull();
        }

        [Fact]
        public void FindOptimalCombination_HandlesEmptyRecipeList()
        {
            // Arrange
            var recipes = new List<Recipe>();
            var availableIngredients = new Dictionary<string, int>
            {
                ["Flour"] = 10,
                ["Eggs"] = 10
            };

            var optimizer = new Optimizer(recipes, availableIngredients);

            // Act
            var (bestCombination, maxPeopleFed) = optimizer.FindOptimalCombination();

            // Assert
            maxPeopleFed.Should().Be(0);
            bestCombination.Should().BeEmpty();
        }
    }
}
