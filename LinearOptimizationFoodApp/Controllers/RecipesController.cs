using Microsoft.AspNetCore.Mvc;
using LinearOptimizationFoodApp.Services;
using Microsoft.Extensions.Logging;

using LinearOptimizationFoodApp.Models;

namespace LinearOptimizationFoodApp.Controllers
{
    public class RecipesController : Controller
    {
        private readonly IOptimizerService _optimizerService;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(IOptimizerService optimizerService, ILogger<RecipesController> logger)
        {
            _optimizerService = optimizerService ?? throw new ArgumentNullException(nameof(optimizerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        
        /// Display all available recipes
        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)] // 10 minutes
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading all recipes");

                var recipes = await _optimizerService.GetAllRecipesAsync();

                if (recipes == null)
                {
                    _logger.LogWarning("GetAllRecipesAsync returned null");
                    recipes = new List<Recipe>();
                }

                _logger.LogInformation("Successfully loaded {RecipeCount} recipes", recipes.Count);
                return View(recipes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recipes");
                TempData["Error"] = "Unable to load recipes. Please try again later.";
                return View(new List<Recipe>());
            }
        }

        
        /// Display detailed view of a specific recipe
        /// <param name="id">Recipe ID</param>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation("Loading recipe details for ID: {RecipeId}", id);

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid recipe ID: {RecipeId}", id);
                    TempData["Error"] = "Invalid recipe ID.";
                    return RedirectToAction(nameof(Index));
                }

                var recipes = await _optimizerService.GetAllRecipesAsync();
                var recipe = recipes?.FirstOrDefault(r => r.Id == id);

                if (recipe == null)
                {
                    _logger.LogWarning("Recipe not found with ID: {RecipeId}", id);
                    TempData["Error"] = "Recipe not found.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Successfully loaded recipe: {RecipeName}", recipe.Name);
                return View(recipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recipe details for ID: {RecipeId}", id);
                TempData["Error"] = "Unable to load recipe details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        
        /// Search recipes by name or ingredient
        /// <param name="searchTerm">Search term</param>
        [HttpGet]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "searchTerm" })] // 5 minutes, vary by search term
        public async Task<IActionResult> Search(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Searching recipes with term: {SearchTerm}", searchTerm);

                var allRecipes = await _optimizerService.GetAllRecipesAsync();

                var searchResults = allRecipes.Where(r =>
                    r.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.RecipeIngredients.Any(ri => ri.Ingredient.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                ViewData["SearchTerm"] = searchTerm;
                ViewData["ResultCount"] = searchResults.Count;

                _logger.LogInformation("Found {ResultCount} recipes matching search term: {SearchTerm}",
                    searchResults.Count, searchTerm);

                return View("Index", searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching recipes with term: {SearchTerm}", searchTerm);
                TempData["Error"] = "Error occurred while searching. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Get recipes that can be made with available ingredients
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Available()
        {
            try
            {
                _logger.LogInformation("Loading recipes that can be made with available ingredients");

                var allRecipes = await _optimizerService.GetAllRecipesAsync();
                var availableIngredients = await _optimizerService.GetAvailableIngredientsAsync();

                var makeableRecipes = allRecipes.Where(recipe =>
                {
                    return recipe.RequiredIngredients.All(required =>
                        availableIngredients.ContainsKey(required.Key) &&
                        availableIngredients[required.Key] >= required.Value
                    );
                }).ToList();

                ViewData["FilterType"] = "Available";
                ViewData["AvailableCount"] = makeableRecipes.Count;

                _logger.LogInformation("Found {AvailableCount} recipes that can be made with current ingredients",
                    makeableRecipes.Count);

                if (!makeableRecipes.Any() && !availableIngredients.Any())
                {
                    TempData["Info"] = "Set your available ingredients first to see which recipes you can make.";
                }
                else if (!makeableRecipes.Any())
                {
                    TempData["Warning"] = "No recipes can be made with your current ingredients. Try updating your ingredient quantities.";
                }

                return View("Index", makeableRecipes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available recipes");
                TempData["Error"] = "Error occurred while loading available recipes.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}