using Microsoft.AspNetCore.Mvc;
using LinearOptimizationFoodApp.Services;
using Microsoft.Extensions.Logging;

using LinearOptimizationFoodApp.ViewModels;

namespace LinearOptimizationFoodApp.Controllers
{
    public class IngredientsController : Controller
    {
        private readonly IOptimizerService _optimizerService;
        private readonly ILogger<IngredientsController> _logger;

        public IngredientsController(IOptimizerService optimizerService, ILogger<IngredientsController> logger)
        {
            _optimizerService = optimizerService ?? throw new ArgumentNullException(nameof(optimizerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        
        /// Display ingredient management page
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] 
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading ingredients management page");

                var allIngredients = await _optimizerService.GetAllIngredientsAsync();
                var availableIngredients = await _optimizerService.GetAvailableIngredientsAsync();

                if (allIngredients == null)
                {
                    _logger.LogWarning("GetAllIngredientsAsync returned null");
                    allIngredients = new List<LinearOptimizationFoodApp.Models.Ingredient>();
                }

                if (availableIngredients == null)
                {
                    _logger.LogWarning("GetAvailableIngredientsAsync returned null");
                    availableIngredients = new Dictionary<string, int>();
                }

                var viewModel = new IngredientsViewModel
                {
                    Ingredients = allIngredients.Select(i => new IngredientQuantityViewModel
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Unit = i.Unit,
                        Quantity = availableIngredients.GetValueOrDefault(i.Name, 0)
                    }).OrderBy(i => i.Name).ToList()
                };

                _logger.LogInformation("Successfully loaded {IngredientCount} ingredients for management",
                    viewModel.Ingredients.Count);

                // Add informational message if no ingredients are set
                var totalQuantity = viewModel.Ingredients.Sum(i => i.Quantity);
                if (totalQuantity == 0)
                {
                    TempData["Info"] = "Set your ingredient quantities to start optimizing your meals!";
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ingredients management page");
                TempData["Error"] = "Unable to load ingredients. Please try again later.";

                // Return empty view model to prevent crashes
                return View(new IngredientsViewModel());
            }
        }

         
        /// Update ingredient quantitiy
        /// <param name="model">Ingredients view model with updated quantities</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)] // 1 minute
        public async Task<IActionResult> Update(IngredientsViewModel model)
        {
            try
            {
                _logger.LogInformation("Updating ingredient quantities");

                if (model?.Ingredients == null)
                {
                    _logger.LogWarning("Model or Ingredients is null");
                    TempData["Error"] = "Invalid data received. Please try again.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate the model
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["Error"] = "Please correct the following errors: " + string.Join(", ", errors);
                    return View("Index", model);
                }

                // Convert to dictionary, filtering out negative quantities and null/empty names
                var ingredientQuantities = model.Ingredients
                    .Where(i => !string.IsNullOrWhiteSpace(i.Name) && i.Quantity >= 0)
                    .ToDictionary(i => i.Name, i => i.Quantity);

                await _optimizerService.SetAvailableIngredientsAsync(ingredientQuantities);

                // Count how many ingredients were set
                var setIngredientsCount = ingredientQuantities.Count(kv => kv.Value > 0);
                var totalIngredientsCount = ingredientQuantities.Count;

                _logger.LogInformation("Successfully updated {SetCount} out of {TotalCount} ingredient quantities",
                    setIngredientsCount, totalIngredientsCount);

                if (setIngredientsCount == 0)
                {
                    TempData["Warning"] = "All ingredient quantities are set to 0. Set some quantities to start optimizing!";
                }
                else if (setIngredientsCount == 1)
                {
                    TempData["Success"] = $"Successfully updated ingredients! You have {setIngredientsCount} ingredient available.";
                }
                else
                {
                    TempData["Success"] = $"Successfully updated ingredients! You have {setIngredientsCount} ingredients available.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ingredient quantities");
                TempData["Error"] = "Unable to save ingredient quantities. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Clear all ingredient quantities
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                _logger.LogInformation("Clearing all ingredient quantities");

                await _optimizerService.SetAvailableIngredientsAsync(new Dictionary<string, int>());

                TempData["Success"] = "All ingredient quantities have been cleared.";
                _logger.LogInformation("Successfully cleared all ingredient quantities");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing ingredient quantities");
                TempData["Error"] = "Unable to clear ingredient quantities. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Get current ingredient quantities as JSON (for AJAX requests)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetQuantities()
        {
            try
            {
                var availableIngredients = await _optimizerService.GetAvailableIngredientsAsync();
                return Json(availableIngredients ?? new Dictionary<string, int>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ingredient quantities");
                return Json(new Dictionary<string, int>());
            }
        }

        /// <summary>
        /// Quick set common ingredient combinations
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickSet(string preset)
        {
            try
            {
                _logger.LogInformation("Setting quick preset: {Preset}", preset);

                Dictionary<string, int> quantities = preset?.ToLower() switch
                {
                    "basic" => new Dictionary<string, int>
                    {
                        ["Flour"] = 5,
                        ["Eggs"] = 12,
                        ["Milk"] = 3,
                        ["Butter"] = 10,
                        ["Sugar"] = 2
                    },
                    "asian" => new Dictionary<string, int>
                    {
                        ["Rice"] = 8,
                        ["Eggs"] = 6,
                        ["Vegetables"] = 4,
                        ["Chicken"] = 2
                    },
                    "italian" => new Dictionary<string, int>
                    {
                        ["Pasta"] = 4,
                        ["Cheese"] = 2,
                        ["Milk"] = 2,
                        ["Butter"] = 5
                    },
                    _ => new Dictionary<string, int>()
                };

                if (quantities.Any())
                {
                    await _optimizerService.SetAvailableIngredientsAsync(quantities);
                    TempData["Success"] = $"Successfully applied {preset} ingredient preset!";
                    _logger.LogInformation("Successfully applied preset: {Preset}", preset);
                }
                else
                {
                    TempData["Error"] = "Unknown preset selected.";
                    _logger.LogWarning("Unknown preset requested: {Preset}", preset);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying quick preset: {Preset}", preset);
                TempData["Error"] = "Unable to apply preset. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}