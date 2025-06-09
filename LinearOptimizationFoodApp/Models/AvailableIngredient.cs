namespace LinearOptimizationFoodApp.Models
{
    public class AvailableIngredient
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; } = null!;
        public int Quantity { get; set; }
    }
}
