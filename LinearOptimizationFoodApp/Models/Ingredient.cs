﻿namespace LinearOptimizationFoodApp.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public List<RecipeIngredient> RecipeIngredients { get; set; } = new();
    }
}
