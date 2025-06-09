using LinearOptimizationFoodApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LinearOptimizationFoodApp.Data
{
    public class FoodOptimizerContext : DbContext
    {
        public FoodOptimizerContext(DbContextOptions<FoodOptimizerContext> options) : base(options) { }

        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<AvailableIngredient> AvailableIngredients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ingredient configuration
            modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.HasIndex(e => e.Name)
                    .IsUnique();
            });

            // Recipe configuration
            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(e => e.Feeds)
                    .IsRequired();
                entity.HasIndex(e => e.Name)
                    .IsUnique();
            });

            // RecipeIngredient configuration (Many-to-Many)
            modelBuilder.Entity<RecipeIngredient>(entity =>
            {
                entity.HasKey(ri => new { ri.RecipeId, ri.IngredientId });

                entity.HasOne(ri => ri.Recipe)
                    .WithMany(r => r.RecipeIngredients)
                    .HasForeignKey(ri => ri.RecipeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ri => ri.Ingredient)
                    .WithMany(i => i.RecipeIngredients)
                    .HasForeignKey(ri => ri.IngredientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(ri => ri.Quantity)
                    .IsRequired();
            });

            // AvailableIngredient configuration
            modelBuilder.Entity<AvailableIngredient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.HasOne(ai => ai.Ingredient)
                    .WithMany()
                    .HasForeignKey(ai => ai.IngredientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
