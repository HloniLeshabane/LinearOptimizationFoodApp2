using System.ComponentModel.DataAnnotations;

namespace LinearOptimizationFoodApp.Models
{
    public class Recipe
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Feeds must be at least 1")]
        public int Feeds { get; set; }

        public string? Instructions { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

        // Computed property that converts RecipeIngredients to Dictionary for optimization logic
        public Dictionary<string, int> RequiredIngredients =>
            RecipeIngredients?.Where(ri => ri.Ingredient != null)
                             .ToDictionary(ri => ri.Ingredient.Name, ri => ri.Quantity)
            ?? new Dictionary<string, int>();
    }
}