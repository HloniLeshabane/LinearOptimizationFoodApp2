using System.ComponentModel.DataAnnotations;
using LinearOptimizationFoodApp.Models;

namespace LinearOptimizationFoodApp.ViewModels
{
    // Ingredient ViewModels
    public class CreateIngredientViewModel
    {
        [Required(ErrorMessage = "Ingredient name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Ingredient Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit is required")]
        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; } = string.Empty;
    }

    public class EditIngredientViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingredient name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Ingredient Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit is required")]
        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; } = string.Empty;
    }

    // Recipe ViewModels
    public class CreateRecipeViewModel
    {
        [Required(ErrorMessage = "Recipe name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        [Display(Name = "Recipe Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of people served is required")]
        [Range(1, 100, ErrorMessage = "Must serve between 1 and 100 people")]
        [Display(Name = "Number of People Served")]
        public int Feeds { get; set; } = 1;

        public List<RecipeIngredientViewModel> RecipeIngredients { get; set; } = new();
        public List<Ingredient> AvailableIngredients { get; set; } = new();
    }

    public class EditRecipeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Recipe name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        [Display(Name = "Recipe Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of people served is required")]
        [Range(1, 100, ErrorMessage = "Must serve between 1 and 100 people")]
        [Display(Name = "Number of People Served")]
        public int Feeds { get; set; } = 1;

        public List<RecipeIngredientViewModel> RecipeIngredients { get; set; } = new();
        public List<Ingredient> AvailableIngredients { get; set; } = new();
    }

    public class RecipeIngredientViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select an ingredient")]
        [Display(Name = "Ingredient")]
        public int IngredientId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.1, 1000, ErrorMessage = "Quantity must be between 0.1 and 1000")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        // Add these properties to support the views
        public string IngredientName { get; set; } = string.Empty;
        public string IngredientUnit { get; set; } = string.Empty;

        // Or add the full ingredient object
        public Ingredient? Ingredient { get; set; }
    }

    public class RecipeIngredientRowViewModel
    {
        public RecipeIngredientViewModel RecipeIngredient { get; set; } = new();
        public List<Ingredient> AvailableIngredients { get; set; } = new();
        public int Index { get; set; }
    }
}