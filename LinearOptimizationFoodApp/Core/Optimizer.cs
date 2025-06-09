namespace LinearOptimizationFoodApp.Core
{
    public class Optimizer
    {
        private readonly List<LinearOptimizationFoodApp.Models.Recipe> _allRecipes;
        private readonly Dictionary<string, int> _initialIngredients;
        private List<LinearOptimizationFoodApp.Models.Recipe> _bestCombination;
        private int _maxPeopleFed;

        public Optimizer(List<LinearOptimizationFoodApp.Models.Recipe> allRecipes, Dictionary<string, int> initialIngredients)
        {
            _allRecipes = allRecipes.OrderByDescending(r => r.Feeds).ToList();
            _initialIngredients = initialIngredients;
            _bestCombination = new List<LinearOptimizationFoodApp.Models.Recipe>();
            _maxPeopleFed = 0;
        }

        public (List<LinearOptimizationFoodApp.Models.Recipe> BestCombination, int MaxPeopleFed) FindOptimalCombination()
        {
            _bestCombination.Clear();
            _maxPeopleFed = 0;
            RecursiveSolve(new Dictionary<string, int>(_initialIngredients), new List<LinearOptimizationFoodApp.Models.Recipe>(), 0);
            return (_bestCombination, _maxPeopleFed);
        }

        private void RecursiveSolve(Dictionary<string, int> currentAvailableIngredients, List<LinearOptimizationFoodApp.Models.Recipe> currentPath, int currentPeopleFed)
        {
            bool canMakeAnyMoreInThisPath = false;

            foreach (var recipe in _allRecipes)
            {
                if (CanMake(recipe, currentAvailableIngredients))
                {
                    canMakeAnyMoreInThisPath = true;
                    var nextIngredients = new Dictionary<string, int>(currentAvailableIngredients);
                    foreach (var req in recipe.RequiredIngredients)
                    {
                        nextIngredients[req.Key] -= req.Value;
                    }
                    currentPath.Add(recipe);
                    RecursiveSolve(nextIngredients, currentPath, currentPeopleFed + recipe.Feeds);
                    currentPath.RemoveAt(currentPath.Count - 1);
                }
            }

            if (!canMakeAnyMoreInThisPath)
            {
                if (currentPeopleFed > _maxPeopleFed)
                {
                    _maxPeopleFed = currentPeopleFed;
                    _bestCombination = new List<LinearOptimizationFoodApp.Models.Recipe>(currentPath);
                }
            }
        }

        private bool CanMake(LinearOptimizationFoodApp.Models.Recipe recipe, Dictionary<string, int> availableIngredients)
        {
            foreach (var requiredIngredient in recipe.RequiredIngredients)
            {
                if (!availableIngredients.ContainsKey(requiredIngredient.Key) ||
                    availableIngredients[requiredIngredient.Key] < requiredIngredient.Value)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
