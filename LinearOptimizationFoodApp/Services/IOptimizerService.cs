using LinearOptimizationFoodApp.Models;
using LinearOptimizationFoodApp.ViewModels;

namespace LinearOptimizationFoodApp.Services
{
    public interface IOptimizerService
    {
        Task<OptimizationResultViewModel> FindOptimalCombinationAsync();
        Task<List<Recipe>> GetAllRecipesAsync();
        Task<List<Ingredient>> GetAllIngredientsAsync();
        Task<Dictionary<string, int>> GetAvailableIngredientsAsync();
        Task SetAvailableIngredientsAsync(Dictionary<string, int> ingredients);
    }
}