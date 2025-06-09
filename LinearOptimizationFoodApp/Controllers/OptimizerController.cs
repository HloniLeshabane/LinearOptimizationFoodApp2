using Microsoft.AspNetCore.Mvc;
using LinearOptimizationFoodApp.Services;
using LinearOptimizationFoodApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace LinearOptimizationFoodApp.Controllers
{
    // DTO for JSON responses
    public class RecipeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Feeds { get; set; }
    }

    public class OptimizerController : Controller
    {
        private readonly IOptimizerService _optimizerService;
        private readonly ILogger<OptimizerController> _logger;

        public OptimizerController(IOptimizerService optimizerService, ILogger<OptimizerController> logger)
        {
            _optimizerService = optimizerService ?? throw new ArgumentNullException(nameof(optimizerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        /// Display optimization results
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)] // 30 seconds
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Starting meal optimization");

                var availableIngredients = await _optimizerService.GetAvailableIngredientsAsync();

                if (availableIngredients == null || !availableIngredients.Any() || availableIngredients.All(kv => kv.Value <= 0))
                {
                    _logger.LogInformation("No ingredients available for optimization");
                    TempData["Warning"] = "Please set your available ingredients first to get optimization results.";
                    return View(new OptimizationResultViewModel());
                }

                var result = await _optimizerService.FindOptimalCombinationAsync();

                if (result == null)
                {
                    _logger.LogWarning("Optimization service returned null result");
                    result = new OptimizationResultViewModel();
                }

                _logger.LogInformation("Optimization completed. Max people fed: {MaxPeopleFed}, Recipes used: {RecipeCount}",
                    result.MaxPeopleFed, result.BestCombination?.Count ?? 0);

                if (result.HasResults)
                {
                    TempData["Success"] = $"Optimization complete! Found a combination that feeds {result.MaxPeopleFed} people.";

                    _logger.LogInformation("Optimal recipe combination found:");
                    foreach (var recipe in result.BestCombination)
                    {
                        _logger.LogInformation("- {RecipeName} (feeds {Feeds} people)", recipe.Name, recipe.Feeds);
                    }
                }
                else
                {
                    TempData["Info"] = "No recipe combinations possible with your current ingredients. Try adding more ingredients or adjusting quantities.";
                }

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during meal optimization");
                TempData["Error"] = "An error occurred during optimization. Please try again.";
                return View(new OptimizationResultViewModel());
            }
        }

        /// <summary>
        /// Run optimization and return results as JSON (for AJAX requests)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> OptimizeJson()
        {
            try
            {
                _logger.LogInformation("Running optimization via AJAX request");

                var result = await _optimizerService.FindOptimalCombinationAsync();

                if (result == null)
                {
                    return Json(new { success = false, message = "Optimization failed" });
                }

                var recipes = result.BestCombination?.Select(r => new RecipeDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Feeds = r.Feeds
                }).ToList() ?? new List<RecipeDto>();

                return Json(new
                {
                    success = true,
                    maxPeopleFed = result.MaxPeopleFed,
                    recipeCount = result.BestCombination?.Count ?? 0,
                    hasResults = result.HasResults,
                    recipes = recipes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AJAX optimization");
                return Json(new { success = false, message = "An error occurred during optimization" });
            }
        }

        /// <summary>
        /// Compare different optimization strategies (future enhancement)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Compare()
        {
            try
            {
                _logger.LogInformation("Loading optimization comparison page");

                var availableIngredients = await _optimizerService.GetAvailableIngredientsAsync();

                if (!availableIngredients.Any() || availableIngredients.All(kv => kv.Value <= 0))
                {
                    TempData["Warning"] = "Please set your available ingredients first.";
                    return RedirectToAction("Index", "Ingredients");
                }

                var result = await _optimizerService.FindOptimalCombinationAsync();

                ViewData["ComparisonMode"] = true;
                return View("Index", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in optimization comparison");
                TempData["Error"] = "Unable to load comparison. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Export optimization results (future enhancement)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Export(string format = "json")
        {
            try
            {
                _logger.LogInformation("Exporting optimization results in format: {Format}", format);

                var result = await _optimizerService.FindOptimalCombinationAsync();

                if (result == null || !result.HasResults)
                {
                    TempData["Warning"] = "No optimization results to export. Run optimization first.";
                    return RedirectToAction(nameof(Index));
                }

                switch (format.ToLower())
                {
                    case "json":
                        var jsonResult = new
                        {
                            OptimizationDate = DateTime.UtcNow,
                            MaxPeopleFed = result.MaxPeopleFed,
                            Recipes = result.BestCombination.Select(r => new
                            {
                                r.Name,
                                r.Feeds,
                                r.Description,
                                Ingredients = r.RequiredIngredients
                            }),
                            UsedIngredients = result.UsedIngredients,
                            RemainingIngredients = result.RemainingIngredients
                        };

                        var json = System.Text.Json.JsonSerializer.Serialize(jsonResult, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json",
                            $"optimization-results-{DateTime.Now:yyyy-MM-dd}.json");

                    default:
                        TempData["Error"] = "Unsupported export format.";
                        return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting optimization results");
                TempData["Error"] = "Unable to export results. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Get optimization statistics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            try
            {
                var allRecipes = await _optimizerService.GetAllRecipesAsync();
                var availableIngredients = await _optimizerService.GetAvailableIngredientsAsync();

                var stats = new
                {
                    TotalRecipes = allRecipes?.Count ?? 0,
                    AvailableIngredients = availableIngredients?.Count(kv => kv.Value > 0) ?? 0,
                    TotalIngredients = availableIngredients?.Count ?? 0,
                    MaxPossiblePeople = allRecipes?.Sum(r => r.Feeds) ?? 0,
                    AverageRecipeSize = allRecipes?.Any() == true ? allRecipes.Average(r => r.Feeds) : 0
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimization statistics");
                return Json(new { error = "Unable to load statistics" });
            }
        }
    }
}
