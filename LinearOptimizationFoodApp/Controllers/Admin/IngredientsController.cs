using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LinearOptimizationFoodApp.Models;
using LinearOptimizationFoodApp.ViewModels;
using LinearOptimizationFoodApp.Repositories;

namespace LinearOptimizationFoodApp.Controllers.Admin
{
    [Route("Admin/Ingredients")]
    public class IngredientsController : Controller
    {
        private readonly IIngredientRepository _ingredientRepository;
        private readonly ILogger<IngredientsController> _logger;

        public IngredientsController(IIngredientRepository ingredientRepository, ILogger<IngredientsController> logger)
        {
            _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Display all ingredients for management
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading ingredients management page");
                var ingredients = await _ingredientRepository.GetAllIngredientsAsync();
                return View(ingredients ?? new List<Ingredient>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ingredients management");
                TempData["Error"] = "Unable to load ingredients.";
                return View(new List<Ingredient>());
            }
        }

        /// <summary>
        /// Show create ingredient form
        /// </summary>
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new CreateIngredientViewModel());
        }

        /// <summary>
        /// Create a new ingredient
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateIngredientViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Check if ingredient already exists
                var existingIngredients = await _ingredientRepository.GetAllIngredientsAsync();
                if (existingIngredients.Any(i => i.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Name", "An ingredient with this name already exists.");
                    return View(model);
                }

                var ingredient = new Ingredient
                {
                    Name = model.Name.Trim(),
                    Unit = model.Unit.Trim()
                };

                await _ingredientRepository.AddIngredientAsync(ingredient);

                _logger.LogInformation("Created new ingredient: {IngredientName}", ingredient.Name);
                TempData["Success"] = $"Successfully created ingredient '{ingredient.Name}'.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ingredient");
                TempData["Error"] = "Unable to create ingredient. Please try again.";
                return View(model);
            }
        }

        /// <summary>
        /// Show edit ingredient form
        /// </summary>
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var ingredient = await _ingredientRepository.GetIngredientByIdAsync(id);
                if (ingredient == null)
                {
                    TempData["Error"] = "Ingredient not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new EditIngredientViewModel
                {
                    Id = ingredient.Id,
                    Name = ingredient.Name,
                    Unit = ingredient.Unit
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ingredient for edit: {IngredientId}", id);
                TempData["Error"] = "Unable to load ingredient.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Update an existing ingredient
        /// </summary>
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditIngredientViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["Error"] = "Invalid request.";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var ingredient = await _ingredientRepository.GetIngredientByIdAsync(id);
                if (ingredient == null)
                {
                    TempData["Error"] = "Ingredient not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if name conflicts with another ingredient
                var existingIngredients = await _ingredientRepository.GetAllIngredientsAsync();
                if (existingIngredients.Any(i => i.Id != id && i.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Name", "An ingredient with this name already exists.");
                    return View(model);
                }

                ingredient.Name = model.Name.Trim();
                ingredient.Unit = model.Unit.Trim();

                await _ingredientRepository.UpdateIngredientAsync(ingredient);

                _logger.LogInformation("Updated ingredient: {IngredientName}", ingredient.Name);
                TempData["Success"] = $"Successfully updated ingredient '{ingredient.Name}'.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ingredient: {IngredientId}", id);
                TempData["Error"] = "Unable to update ingredient. Please try again.";
                return View(model);
            }
        }

        /// <summary>
        /// Delete an ingredient
        /// </summary>
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ingredient = await _ingredientRepository.GetIngredientByIdAsync(id);
                if (ingredient == null)
                {
                    TempData["Error"] = "Ingredient not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if ingredient is used in any recipes
                if (ingredient.RecipeIngredients?.Any() == true)
                {
                    TempData["Error"] = $"Cannot delete '{ingredient.Name}' because it's used in existing recipes.";
                    return RedirectToAction(nameof(Index));
                }

                await _ingredientRepository.DeleteIngredientAsync(id);

                _logger.LogInformation("Deleted ingredient: {IngredientName}", ingredient.Name);
                TempData["Success"] = $"Successfully deleted ingredient '{ingredient.Name}'.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ingredient: {IngredientId}", id);
                TempData["Error"] = "Unable to delete ingredient. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}