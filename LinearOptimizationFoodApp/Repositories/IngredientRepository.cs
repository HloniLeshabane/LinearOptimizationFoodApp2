using Microsoft.EntityFrameworkCore;
using LinearOptimizationFoodApp.Data;
using LinearOptimizationFoodApp.Models;

namespace LinearOptimizationFoodApp.Repositories
{
    public class IngredientRepository : IIngredientRepository
    {
        private readonly FoodOptimizerContext _context;

        public IngredientRepository(FoodOptimizerContext context)
        {
            _context = context;
        }

        public async Task<List<Ingredient>> GetAllIngredientsAsync()
        {
            return await _context.Ingredients
                .Include(i => i.RecipeIngredients)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<Ingredient?> GetIngredientByIdAsync(int id)
        {
            return await _context.Ingredients
                .Include(i => i.RecipeIngredients)
                .ThenInclude(ri => ri.Recipe)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Ingredient> AddIngredientAsync(Ingredient ingredient)
        {
            _context.Ingredients.Add(ingredient);
            await _context.SaveChangesAsync();
            return ingredient;
        }

        public async Task<Ingredient> UpdateIngredientAsync(Ingredient ingredient)
        {
            _context.Ingredients.Update(ingredient);
            await _context.SaveChangesAsync();
            return ingredient;
        }

        public async Task DeleteIngredientAsync(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient != null)
            {
                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Dictionary<string, int>> GetAvailableIngredientsAsync()
        {
            var available = await _context.AvailableIngredients
                .Include(ai => ai.Ingredient)
                .ToListAsync();

            return available.ToDictionary(ai => ai.Ingredient.Name, ai => ai.Quantity);
        }

        public async Task SetAvailableIngredientsAsync(Dictionary<string, int> ingredients)
        {
            var existing = await _context.AvailableIngredients.ToListAsync();
            _context.AvailableIngredients.RemoveRange(existing);

            foreach (var kvp in ingredients)
            {
                var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.Name == kvp.Key);
                if (ingredient != null && kvp.Value > 0)
                {
                    _context.AvailableIngredients.Add(new AvailableIngredient
                    {
                        IngredientId = ingredient.Id,
                        Quantity = kvp.Value
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}