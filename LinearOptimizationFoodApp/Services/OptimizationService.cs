using LinearOptimizationFoodApp.Models;
using LinearOptimizationFoodApp.Repositories;
using LinearOptimizationFoodApp.ViewModels;
using LinearOptimizationFoodApp.Core;
using Microsoft.Extensions.Caching.Memory;

namespace LinearOptimizationFoodApp.Services
{
    public class OptimizerService : IOptimizerService
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly IMemoryCache _cache;

        // Cache keys as constants
        private const string ALL_RECIPES_CACHE_KEY = "all_recipes";
        private const string ALL_INGREDIENTS_CACHE_KEY = "all_ingredients";
        private const string AVAILABLE_INGREDIENTS_CACHE_KEY = "available_ingredients";

        public OptimizerService(IRecipeRepository recipeRepository,
                               IIngredientRepository ingredientRepository,
                               IMemoryCache cache)
        {
            _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
            _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<OptimizationResultViewModel> FindOptimalCombinationAsync()
        {
            try
            {
                var recipes = await GetAllRecipesAsync();
                var availableIngredients = await GetAvailableIngredientsAsync();

                if (!availableIngredients.Any())
                {
                    return new OptimizationResultViewModel();
                }

                var optimizer = new Optimizer(recipes, availableIngredients);
                var (bestCombination, maxPeopleFed) = optimizer.FindOptimalCombination();

                var usedIngredients = new Dictionary<string, int>();
                var remainingIngredients = new Dictionary<string, int>(availableIngredients);

                foreach (var recipe in bestCombination)
                {
                    foreach (var ingredient in recipe.RequiredIngredients)
                    {
                        usedIngredients[ingredient.Key] = usedIngredients.GetValueOrDefault(ingredient.Key) + ingredient.Value;
                        remainingIngredients[ingredient.Key] -= ingredient.Value;
                    }
                }

                return new OptimizationResultViewModel
                {
                    BestCombination = bestCombination,
                    MaxPeopleFed = maxPeopleFed,
                    UsedIngredients = usedIngredients,
                    RemainingIngredients = remainingIngredients
                };
            }
            catch (Exception)
            {
                // Return empty result on error
                return new OptimizationResultViewModel();
            }
        }

        public async Task<List<Recipe>> GetAllRecipesAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(ALL_RECIPES_CACHE_KEY, out List<Recipe>? cachedRecipes))
            {
                return cachedRecipes ?? new List<Recipe>();
            }

            try
            {
                var recipes = await _recipeRepository.GetAllRecipesAsync() ?? new List<Recipe>();

                // Cache for 30 minutes - recipes don't change often
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(ALL_RECIPES_CACHE_KEY, recipes, cacheOptions);

                return recipes;
            }
            catch (Exception)
            {
                return new List<Recipe>();
            }
        }

        public async Task<List<Ingredient>> GetAllIngredientsAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(ALL_INGREDIENTS_CACHE_KEY, out List<Ingredient>? cachedIngredients))
            {
                return cachedIngredients ?? new List<Ingredient>();
            }

            try
            {
                var ingredients = await _ingredientRepository.GetAllIngredientsAsync() ?? new List<Ingredient>();

                // Cache for 1 hour - ingredients change even less frequently
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(ALL_INGREDIENTS_CACHE_KEY, ingredients, cacheOptions);

                return ingredients;
            }
            catch (Exception)
            {
                return new List<Ingredient>();
            }
        }

        public async Task<Dictionary<string, int>> GetAvailableIngredientsAsync()
        {
            // Available ingredients change frequently, so shorter cache time
            if (_cache.TryGetValue(AVAILABLE_INGREDIENTS_CACHE_KEY, out Dictionary<string, int>? cachedAvailable))
            {
                return cachedAvailable ?? new Dictionary<string, int>();
            }

            try
            {
                var ingredients = await _ingredientRepository.GetAvailableIngredientsAsync() ?? new Dictionary<string, int>();

                // Cache for 5 minutes - available ingredients change more frequently
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.High // High priority since this is frequently accessed
                };

                _cache.Set(AVAILABLE_INGREDIENTS_CACHE_KEY, ingredients, cacheOptions);

                return ingredients;
            }
            catch (Exception)
            {
                return new Dictionary<string, int>();
            }
        }

        public async Task SetAvailableIngredientsAsync(Dictionary<string, int> ingredients)
        {
            if (ingredients == null) return;

            try
            {
                await _ingredientRepository.SetAvailableIngredientsAsync(ingredients);

                // Clear the available ingredients cache since we just updated them
                _cache.Remove(AVAILABLE_INGREDIENTS_CACHE_KEY);
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw
            }
        }

        // Method to clear all caches (useful for admin operations)
        public void ClearAllCaches()
        {
            _cache.Remove(ALL_RECIPES_CACHE_KEY);
            _cache.Remove(ALL_INGREDIENTS_CACHE_KEY);
            _cache.Remove(AVAILABLE_INGREDIENTS_CACHE_KEY);
        }
    }
}