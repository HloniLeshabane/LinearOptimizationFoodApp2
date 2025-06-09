using LinearOptimizationFoodApp.Models;

namespace LinearOptimizationFoodApp.Repositories
{
    public interface IIngredientRepository
    {
        Task<List<Ingredient>> GetAllIngredientsAsync();
        Task<Ingredient?> GetIngredientByIdAsync(int id);
        Task<Ingredient> AddIngredientAsync(Ingredient ingredient);
        Task<Ingredient> UpdateIngredientAsync(Ingredient ingredient);
        Task DeleteIngredientAsync(int id);
        Task<Dictionary<string, int>> GetAvailableIngredientsAsync();
        Task SetAvailableIngredientsAsync(Dictionary<string, int> ingredients);
    }
}