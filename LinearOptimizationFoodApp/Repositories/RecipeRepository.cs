using Microsoft.EntityFrameworkCore;
using LinearOptimizationFoodApp.Data;
using LinearOptimizationFoodApp.Models;

namespace LinearOptimizationFoodApp.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly FoodOptimizerContext _context;

        public RecipeRepository(FoodOptimizerContext context)
        {
            _context = context;
        }

        public async Task<List<Recipe>> GetAllRecipesAsync()
        {
            return await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<Recipe?> GetRecipeByIdAsync(int id)
        {
            return await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Recipe> AddRecipeAsync(Recipe recipe)
        {
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            return recipe;
        }

        public async Task<Recipe> UpdateRecipeAsync(Recipe recipe)
        {
            // Remove existing recipe ingredients
            var existingIngredients = await _context.RecipeIngredients
                .Where(ri => ri.RecipeId == recipe.Id)
                .ToListAsync();

            _context.RecipeIngredients.RemoveRange(existingIngredients);

            // Update the recipe
            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync();
            return recipe;
        }

        public async Task DeleteRecipeAsync(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
            }
        }
    }
}