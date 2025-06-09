using LinearOptimizationFoodApp.Models;

namespace LinearOptimizationFoodApp.ViewModels
{
    public class OptimizationResultViewModel
    {
        public List<Recipe> BestCombination { get; set; } = new List<Recipe>();

        public int MaxPeopleFed { get; set; }

        public Dictionary<string, int> UsedIngredients { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, int> RemainingIngredients { get; set; } = new Dictionary<string, int>();

        // Computed property to check if optimization found any results
        public bool HasResults => BestCombination.Any() && MaxPeopleFed > 0;

        // Additional helpful properties
        public decimal IngredientUtilizationPercentage
        {
            get
            {
                var totalAvailable = UsedIngredients.Values.Sum() + RemainingIngredients.Values.Sum();
                if (totalAvailable == 0) return 0;

                var totalUsed = UsedIngredients.Values.Sum();
                return Math.Round((decimal)totalUsed / totalAvailable * 100, 2);
            }
        }

        public int TotalRecipesUsed => BestCombination.Count;

        public string OptimizationSummary
        {
            get
            {
                if (!HasResults)
                    return "No optimal combination found with available ingredients.";

                return $"Found optimal combination: {TotalRecipesUsed} recipe(s) feeding {MaxPeopleFed} people " +
                       $"with {IngredientUtilizationPercentage}% ingredient utilization.";
            }
        }
    }
}