using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LinearOptimizationFoodApp.Repositories;
using LinearOptimizationFoodApp.Models;
using LinearOptimizationFoodApp.ViewModels;

namespace LinearOptimizationFoodApp.Controllers.Admin
{
    [Route("Admin/Recipes")]
    public class RecipesController : Controller
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(
            IRecipeRepository recipeRepository,
            IIngredientRepository ingredientRepository,
            ILogger<RecipesController> logger)
        {
            _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
            _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Display all recipes for management
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading recipes management page");
                var recipes = await _recipeRepository.GetAllRecipesAsync();
                return View(recipes ?? new List<Recipe>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recipes management");
                TempData["Error"] = "Unable to load recipes.";
                return View(new List<Recipe>());
            }
        }

        /// <summary>
        /// Show create recipe form
        /// </summary>
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var ingredients = await _ingredientRepository.GetAllIngredientsAsync();
                var model = new CreateRecipeViewModel
                {
                    AvailableIngredients = ingredients ?? new List<Ingredient>(),
                    RecipeIngredients = new List<RecipeIngredientViewModel>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create recipe form");
                TempData["Error"] = "Unable to load form.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Create a new recipe
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRecipeViewModel model)
        {
            try
            {
                // Reload available ingredients for the view
                model.AvailableIngredients = await _ingredientRepository.GetAllIngredientsAsync() ?? new List<Ingredient>();

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Validate recipe ingredients
                if (model.RecipeIngredients == null || !model.RecipeIngredients.Any())
                {
                    ModelState.AddModelError("RecipeIngredients", "Please add at least one ingredient.");
                    return View(model);
                }

                var validIngredients = model.RecipeIngredients.Where(ri => ri.IngredientId > 0 && ri.Quantity > 0).ToList();
                if (!validIngredients.Any())
                {
                    ModelState.AddModelError("RecipeIngredients", "Please add at least one ingredient with a valid quantity.");
                    return View(model);
                }

                // Check if recipe name already exists
                var existingRecipes = await _recipeRepository.GetAllRecipesAsync();
                if (existingRecipes.Any(r => r.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Name", "A recipe with this name already exists.");
                    return View(model);
                }

                var recipe = new Recipe
                {
                    Name = model.Name.Trim(),
                    Description = model.Description?.Trim() ?? string.Empty,
                    Feeds = model.Feeds,
                    RecipeIngredients = validIngredients.Select(ri => new RecipeIngredient
                    {
                        IngredientId = ri.IngredientId,
                        Quantity = ri.Quantity
                    }).ToList()
                };

                await _recipeRepository.AddRecipeAsync(recipe);

                _logger.LogInformation("Created new recipe: {RecipeName} with {IngredientCount} ingredients",
                    recipe.Name, recipe.RecipeIngredients.Count);
                TempData["Success"] = $"Successfully created recipe '{recipe.Name}'.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recipe");
                TempData["Error"] = "Unable to create recipe. Please try again.";
                return View(model);
            }
        }

        /// <summary>
        /// Show edit recipe form
        /// </summary>
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var recipe = await _recipeRepository.GetRecipeByIdAsync(id);
                if (recipe == null)
                {
                    TempData["Error"] = "Recipe not found.";
                    return RedirectToAction(nameof(Index));
                }

                var ingredients = await _ingredientRepository.GetAllIngredientsAsync();
                var model = new EditRecipeViewModel
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    Description = recipe.Description,
                    Feeds = recipe.Feeds,
                    AvailableIngredients = ingredients ?? new List<Ingredient>(),
                    RecipeIngredients = recipe.RecipeIngredients.Select(ri => new RecipeIngredientViewModel
                    {
                        IngredientId = ri.IngredientId,
                        Quantity = ri.Quantity,
                        // ADD THESE LINES - populate the ingredient details
                        Ingredient = ri.Ingredient,
                        IngredientName = ri.Ingredient?.Name ?? "",
                        IngredientUnit = ri.Ingredient?.Unit ?? ""
                    }).ToList()
                };

                return View("Edit", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recipe for edit: {RecipeId}", id);
                TempData["Error"] = "Unable to load recipe.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Update an existing recipe
        /// </summary>
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditRecipeViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["Error"] = "Invalid request.";
                    return RedirectToAction(nameof(Index));
                }

                // Reload available ingredients for the view
                model.AvailableIngredients = await _ingredientRepository.GetAllIngredientsAsync() ?? new List<Ingredient>();

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var recipe = await _recipeRepository.GetRecipeByIdAsync(id);
                if (recipe == null)
                {
                    TempData["Error"] = "Recipe not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate recipe ingredients
                if (model.RecipeIngredients == null || !model.RecipeIngredients.Any())
                {
                    ModelState.AddModelError("RecipeIngredients", "Please add at least one ingredient.");
                    return View(model);
                }

                var validIngredients = model.RecipeIngredients.Where(ri => ri.IngredientId > 0 && ri.Quantity > 0).ToList();
                if (!validIngredients.Any())
                {
                    ModelState.AddModelError("RecipeIngredients", "Please add at least one ingredient with a valid quantity.");
                    return View(model);
                }

                // Check if name conflicts with another recipe
                var existingRecipes = await _recipeRepository.GetAllRecipesAsync();
                if (existingRecipes.Any(r => r.Id != id && r.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Name", "A recipe with this name already exists.");
                    return View(model);
                }

                recipe.Name = model.Name.Trim();
                recipe.Description = model.Description?.Trim() ?? string.Empty;
                recipe.Feeds = model.Feeds;

                // Update recipe ingredients - remove old ones and add new ones
                recipe.RecipeIngredients.Clear();
                recipe.RecipeIngredients = validIngredients.Select(ri => new RecipeIngredient
                {
                    RecipeId = recipe.Id,
                    IngredientId = ri.IngredientId,
                    Quantity = ri.Quantity
                }).ToList();

                await _recipeRepository.UpdateRecipeAsync(recipe);

                _logger.LogInformation("Updated recipe: {RecipeName} with {IngredientCount} ingredients",
                    recipe.Name, recipe.RecipeIngredients.Count);
                TempData["Success"] = $"Successfully updated recipe '{recipe.Name}'.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recipe: {RecipeId}", id);
                TempData["Error"] = "Unable to update recipe. Please try again.";
                return View(model);
            }
        }

        /// <summary>
        /// Delete a recipe
        /// </summary>
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var recipe = await _recipeRepository.GetRecipeByIdAsync(id);
                if (recipe == null)
                {
                    TempData["Error"] = "Recipe not found.";
                    return RedirectToAction(nameof(Index));
                }

                await _recipeRepository.DeleteRecipeAsync(id);

                _logger.LogInformation("Deleted recipe: {RecipeName}", recipe.Name);
                TempData["Success"] = $"Successfully deleted recipe '{recipe.Name}'.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe: {RecipeId}", id);
                TempData["Error"] = "Unable to delete recipe. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Add ingredient to recipe (AJAX)
        /// </summary>
        [HttpPost("AddIngredient")]
        public async Task<IActionResult> AddIngredient()
        {
            try
            {
                var ingredients = await _ingredientRepository.GetAllIngredientsAsync();
                var model = new RecipeIngredientViewModel();

                return PartialView("_RecipeIngredientRow", new RecipeIngredientRowViewModel
                {
                    RecipeIngredient = model,
                    AvailableIngredients = ingredients ?? new List<Ingredient>(),
                    Index = 0 // This will be updated by JavaScript
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding ingredient row");
                return BadRequest("Unable to add ingredient row");
            }
        }
    }
}