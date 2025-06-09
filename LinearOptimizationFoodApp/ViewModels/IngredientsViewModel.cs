namespace LinearOptimizationFoodApp.ViewModels
{
    public class IngredientsViewModel
    {
        public List<IngredientQuantityViewModel> Ingredients { get; set; } = new();
    }

    public class IngredientQuantityViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}