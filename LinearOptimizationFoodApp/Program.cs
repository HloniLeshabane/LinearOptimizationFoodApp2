using Microsoft.EntityFrameworkCore;
using LinearOptimizationFoodApp.Data;
using LinearOptimizationFoodApp.Services;
using LinearOptimizationFoodApp.Repositories;
using LinearOptimizationFoodApp.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Memory Caching
builder.Services.AddMemoryCache();

// Add Response Caching
builder.Services.AddResponseCaching();

// Database
builder.Services.AddDbContext<FoodOptimizerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository pattern - MAKE SURE THESE ARE REGISTERED
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IOptimizerService, OptimizerService>();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<FoodOptimizerContext>();
        await context.Database.MigrateAsync();
        await SeedData(context);
        await SeedAvailableIngredients(context);
        // OPTIONAL: Uncomment this line if you want to pre-populate available ingredients
        // await SeedAvailableIngredients(context);
    }
    catch (Exception ex)
    {
        // Log seeding errors
        Console.WriteLine($"Error seeding database: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Replace default exception handler with our global middleware
    // app.UseExceptionHandler("/Home/Error"); // Commented out - replaced by GlobalExceptionMiddleware
    app.UseHsts();
}

// Global Exception Middleware - should be early in pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Response Caching Middleware
app.UseResponseCaching();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Seed data method with error handling
static async Task SeedData(FoodOptimizerContext context)
{
    try
    {
        if (await context.Ingredients.AnyAsync()) return;

        // Seed ingredients based on your image
        var ingredients = new List<LinearOptimizationFoodApp.Models.Ingredient>
        {
            new() { Name = "Meat", Unit = "pieces" },
            new() { Name = "Lettuce", Unit = "pieces" },
            new() { Name = "Tomato", Unit = "pieces" },
            new() { Name = "Cheese", Unit = "pieces" },
            new() { Name = "Dough", Unit = "pieces" },
            new() { Name = "Cucumber", Unit = "pieces" },
            new() { Name = "Olives", Unit = "pieces" }
        };

        context.Ingredients.AddRange(ingredients);
        await context.SaveChangesAsync();

        var savedIngredients = await context.Ingredients.ToListAsync();
        var ingredientLookup = savedIngredients.ToDictionary(i => i.Name, i => i.Id);

        // Seed recipes based on your image
        var recipes = new List<LinearOptimizationFoodApp.Models.Recipe>
        {
            // Burger (Feeds 1)
            new()
            {
                Name = "Burger",
                Feeds = 1,
                Description = "Classic burger with meat, lettuce, tomato, cheese and dough",
                RecipeIngredients = new List<LinearOptimizationFoodApp.Models.RecipeIngredient>
                {
                    new() { IngredientId = ingredientLookup["Meat"], Quantity = 1 },
                    new() { IngredientId = ingredientLookup["Lettuce"], Quantity = 1 },
                    new() { IngredientId = ingredientLookup["Tomato"], Quantity = 1 },
                    new() { IngredientId = ingredientLookup["Cheese"], Quantity = 1 },
                    new() { IngredientId = ingredientLookup["Dough"], Quantity = 1 }
                }
            },

            // Pie (Feeds 1)
            new()
            {
                Name = "Pie",
                Feeds = 1,
                Description = "Savory pie with dough and meat",
                RecipeIngredients = new List<LinearOptimizationFoodApp.Models.RecipeIngredient>
                {
                    new() { IngredientId = ingredientLookup["Dough"], Quantity = 2 },
                    new() { IngredientId = ingredientLookup["Meat"], Quantity = 2 }
                }
            },

            // Sandwich (Feeds 1)
            new()
            {
                Name = "Sandwich",
                Feeds = 1,
                Description = "Fresh sandwich with dough and cucumber",
                RecipeIngredients = new List<LinearOptimizationFoodApp.Models.RecipeIngredient>
                {
                    new() { IngredientId = ingredientLookup["Dough"], Quantity = 1 },
                    new() { IngredientId = ingredientLookup["Cucumber"], Quantity = 1 }
                }
            },

            // Pasta (Feeds 2)
            new()
            {
                Name = "Pasta",
                Feeds = 2,
                Description = "Delicious pasta with dough, tomato, cheese and meat",
                RecipeIngredients = new List<LinearOptimizationFoodApp.Models.RecipeIngredient>
                {
                    new() { IngredientId = ingredientLookup["Dough"], Quantity = 2 },
                    new() { IngredientId = ingredientLookup["Tomato"], Quantity = 1 },
                    new() { IngredientId = ingredientLookup["Cheese"], Quantity = 2 },
                    new() { IngredientId = ingredientLookup["Meat"], Quantity = 1 }
                }
            },

            // Salad (Feeds 3)
            new()
            {
                Name = "Salad",
                Feeds = 3,
                Description = "Fresh salad with lettuce, tomato, cucumber, cheese and olives",
                RecipeIngredients = new List<LinearOptimizationFoodApp.Models.RecipeIngredient>
                {
                    new() { IngredientId = ingredientLookup["Lettuce"], Quantity = 2 },
                    new() { IngredientId = ingredientLookup["Tomato"], Quantity = 2 },
                    new() { IngredientId = ingredientLookup["Cucumber"], Quantity = 1 },
                    new() { IngredientId = ingredientLookup["Cheese"], Quantity = 2 },
                    new() { IngredientId = ingredientLookup["Olives"], Quantity = 1 }
                }
            },

            // Pizza (Feeds 4)
            new()
            {
                Name = "Pizza",
                Feeds = 4,
                Description = "Classic pizza with dough, tomato, cheese and olives",
                RecipeIngredients = new List<LinearOptimizationFoodApp.Models.RecipeIngredient>
                {
                    new() { IngredientId = ingredientLookup["Dough"], Quantity = 3 },
                    new() { IngredientId = ingredientLookup["Tomato"], Quantity = 2 },
                    new() { IngredientId = ingredientLookup["Cheese"], Quantity = 3 },
                    new() { IngredientId = ingredientLookup["Olives"], Quantity = 1 }
                }
            }
        };

        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();



        Console.WriteLine("Database seeded successfully with 6 recipes and 7 ingredients!");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in SeedData: {ex.Message}");
        throw;
    }
}

static async Task SeedAvailableIngredients(FoodOptimizerContext context)
{
    try
    {
        // Check if available ingredients already exist
        if (await context.AvailableIngredients.AnyAsync()) return;

        // Get all ingredients
        var ingredients = await context.Ingredients.ToListAsync();
        var ingredientLookup = ingredients.ToDictionary(i => i.Name, i => i.Id);

        // Add the available ingredients shown in your image
        var availableIngredients = new List<LinearOptimizationFoodApp.Models.AvailableIngredient>
        {
            new() { IngredientId = ingredientLookup["Cucumber"], Quantity = 2 },
            new() { IngredientId = ingredientLookup["Olives"], Quantity = 2 },
            new() { IngredientId = ingredientLookup["Lettuce"], Quantity = 3 },
            new() { IngredientId = ingredientLookup["Meat"], Quantity = 6 },
            new() { IngredientId = ingredientLookup["Tomato"], Quantity = 6 },
            new() { IngredientId = ingredientLookup["Cheese"], Quantity = 6 },
            new() { IngredientId = ingredientLookup["Dough"], Quantity = 10 }
        };

        context.AvailableIngredients.AddRange(availableIngredients);
        await context.SaveChangesAsync();

        Console.WriteLine("Available ingredients seeded successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding available ingredients: {ex.Message}");
    }
}