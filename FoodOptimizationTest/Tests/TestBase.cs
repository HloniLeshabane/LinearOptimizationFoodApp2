using LinearOptimizationFoodApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinearOptimizationFoodAppTests.Tests
{
    public abstract class TestBase
    {
        /// <summary>
        /// Sets up a controller with proper TempData for testing.
        /// Prevents NullReferenceExceptions in controller tests.
        /// </summary>
        protected static void SetupControllerContext(Controller controller)
        {
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = tempData;
        }

        /// <summary>
        /// Creates a standard set of test recipes using the new ingredient data.
        /// Based on: Burger, Pie, Sandwich, Pasta, Salad, Pizza
        /// </summary>
        protected static List<Recipe> CreateTestRecipes()
        {
            return new List<Recipe>
            {
                new()
                {
                    Id = 1,
                    Name = "Burger",
                    Description = "Classic burger with meat, lettuce, tomato, cheese and dough",
                    Feeds = 1,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new()
                        {
                            IngredientId = 1,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 1, Name = "Meat", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 2,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 2, Name = "Lettuce", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 3,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 3, Name = "Tomato", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 4,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 4, Name = "Cheese", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 5,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 5, Name = "Dough", Unit = "pieces" }
                        }
                    }
                },
                new()
                {
                    Id = 2,
                    Name = "Pasta",
                    Description = "Delicious pasta with dough, tomato, cheese and meat",
                    Feeds = 2,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new()
                        {
                            IngredientId = 5,
                            Quantity = 2,
                            Ingredient = new Ingredient { Id = 5, Name = "Dough", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 3,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 3, Name = "Tomato", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 4,
                            Quantity = 2,
                            Ingredient = new Ingredient { Id = 4, Name = "Cheese", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 1,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 1, Name = "Meat", Unit = "pieces" }
                        }
                    }
                },
                new()
                {
                    Id = 3,
                    Name = "Salad",
                    Description = "Fresh salad with lettuce, tomato, cucumber, cheese and olives",
                    Feeds = 3,
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new()
                        {
                            IngredientId = 2,
                            Quantity = 2,
                            Ingredient = new Ingredient { Id = 2, Name = "Lettuce", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 3,
                            Quantity = 2,
                            Ingredient = new Ingredient { Id = 3, Name = "Tomato", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 6,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 6, Name = "Cucumber", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 4,
                            Quantity = 2,
                            Ingredient = new Ingredient { Id = 4, Name = "Cheese", Unit = "pieces" }
                        },
                        new()
                        {
                            IngredientId = 7,
                            Quantity = 1,
                            Ingredient = new Ingredient { Id = 7, Name = "Olives", Unit = "pieces" }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates test ingredients using the new ingredient data.
        /// </summary>
        protected static List<Ingredient> CreateTestIngredients()
        {
            return new List<Ingredient>
            {
                new() { Id = 1, Name = "Meat", Unit = "pieces" },
                new() { Id = 2, Name = "Lettuce", Unit = "pieces" },
                new() { Id = 3, Name = "Tomato", Unit = "pieces" },
                new() { Id = 4, Name = "Cheese", Unit = "pieces" },
                new() { Id = 5, Name = "Dough", Unit = "pieces" },
                new() { Id = 6, Name = "Cucumber", Unit = "pieces" },
                new() { Id = 7, Name = "Olives", Unit = "pieces" }
            };
        }

        /// <summary>
        /// Creates test available ingredients dictionary using new ingredients.
        /// </summary>
        protected static Dictionary<string, int> CreateTestAvailableIngredients()
        {
            return new Dictionary<string, int>
            {
                ["Meat"] = 6,
                ["Lettuce"] = 3,
                ["Tomato"] = 6,
                ["Cheese"] = 6,
                ["Dough"] = 10,
                ["Cucumber"] = 2,
                ["Olives"] = 2
            };
        }

        /// <summary>
        /// Creates minimal test available ingredients for basic testing.
        /// </summary>
        protected static Dictionary<string, int> CreateMinimalTestAvailableIngredients()
        {
            return new Dictionary<string, int>
            {
                ["Meat"] = 3,
                ["Dough"] = 5,
                ["Cheese"] = 3
            };
        }

        /// <summary>
        /// Creates a single simple recipe for basic testing.
        /// </summary>
        protected static Recipe CreateSimpleTestRecipe()
        {
            return new Recipe
            {
                Id = 1,
                Name = "Simple Sandwich",
                Description = "Basic sandwich",
                Feeds = 1,
                RecipeIngredients = new List<RecipeIngredient>
                {
                    new()
                    {
                        IngredientId = 5,
                        Quantity = 1,
                        Ingredient = new Ingredient { Id = 5, Name = "Dough", Unit = "pieces" }
                    },
                    new()
                    {
                        IngredientId = 6,
                        Quantity = 1,
                        Ingredient = new Ingredient { Id = 6, Name = "Cucumber", Unit = "pieces" }
                    }
                }
            };
        }

        /// <summary>
        /// Generates many test recipes for performance testing using new ingredients.
        /// </summary>
        protected static List<Recipe> GenerateManyTestRecipes(int count)
        {
            var recipes = new List<Recipe>();
            var ingredientNames = new[] { "Meat", "Lettuce", "Tomato", "Cheese", "Dough", "Cucumber", "Olives" };

            for (int i = 1; i <= count; i++)
            {
                var recipeIngredients = new List<RecipeIngredient>();
                var ingredientCount = Random.Shared.Next(1, 4);

                for (int j = 0; j < ingredientCount; j++)
                {
                    var ingredientName = ingredientNames[Random.Shared.Next(ingredientNames.Length)];
                    recipeIngredients.Add(new RecipeIngredient
                    {
                        IngredientId = j + 1,
                        Quantity = Random.Shared.Next(1, 4),
                        Ingredient = new Ingredient { Id = j + 1, Name = ingredientName, Unit = "pieces" }
                    });
                }

                recipes.Add(new Recipe
                {
                    Id = i,
                    Name = $"Generated Recipe {i}",
                    Description = $"Test recipe {i}",
                    Feeds = Random.Shared.Next(1, 6),
                    RecipeIngredients = recipeIngredients
                });
            }

            return recipes;
        }

        /// <summary>
        /// Generates test ingredients for large datasets using new ingredient names.
        /// </summary>
        protected static Dictionary<string, int> GenerateManyTestIngredients(int uniqueIngredientCount)
        {
            var ingredients = new Dictionary<string, int>();
            var baseNames = new[] { "Meat", "Lettuce", "Tomato", "Cheese", "Dough", "Cucumber", "Olives" };

            for (int i = 0; i < uniqueIngredientCount; i++)
            {
                var name = i < baseNames.Length ? baseNames[i] : $"Ingredient{i}";
                ingredients[name] = Random.Shared.Next(2, 15);
            }

            return ingredients;
        }

        /// <summary>
        /// Helper method to create a recipe with specific ingredients for testing.
        /// </summary>
        protected static Recipe CreateRecipe(int id, string name, int feeds, Dictionary<string, int> ingredients)
        {
            var recipe = new Recipe
            {
                Id = id,
                Name = name,
                Description = $"Test recipe: {name}",
                Feeds = feeds,
                RecipeIngredients = new List<RecipeIngredient>()
            };

            int ingredientId = 1;
            foreach (var kvp in ingredients)
            {
                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = id,
                    Recipe = recipe,
                    IngredientId = ingredientId,
                    Ingredient = new Ingredient { Id = ingredientId, Name = kvp.Key, Unit = "pieces" },
                    Quantity = kvp.Value
                });
                ingredientId++;
            }

            return recipe;
        }

        /// <summary>
        /// Creates test data that matches your actual application seed data.
        /// Use this for integration tests that need to match the real database.
        /// </summary>
        protected static List<Recipe> CreateRealAppTestRecipes()
        {
            return new List<Recipe>
            {
                CreateRecipe(1, "Burger", 1, new Dictionary<string, int>
                {
                    ["Meat"] = 1, ["Lettuce"] = 1, ["Tomato"] = 1, ["Cheese"] = 1, ["Dough"] = 1
                }),
                CreateRecipe(2, "Pie", 1, new Dictionary<string, int>
                {
                    ["Dough"] = 2, ["Meat"] = 2
                }),
                CreateRecipe(3, "Sandwich", 1, new Dictionary<string, int>
                {
                    ["Dough"] = 1, ["Cucumber"] = 1
                }),
                CreateRecipe(4, "Pasta", 2, new Dictionary<string, int>
                {
                    ["Dough"] = 2, ["Tomato"] = 1, ["Cheese"] = 2, ["Meat"] = 1
                }),
                CreateRecipe(5, "Salad", 3, new Dictionary<string, int>
                {
                    ["Lettuce"] = 2, ["Tomato"] = 2, ["Cucumber"] = 1, ["Cheese"] = 2, ["Olives"] = 1
                }),
                CreateRecipe(6, "Pizza", 4, new Dictionary<string, int>
                {
                    ["Dough"] = 3, ["Tomato"] = 2, ["Cheese"] = 3, ["Olives"] = 1
                })
            };
        }
    }
}