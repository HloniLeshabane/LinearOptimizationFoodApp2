# Linear Optimization Food App

A web application that solves the optimal food recipe combination problem using linear optimization algorithms. Given a set of available ingredients, the application determines the best combination of recipes to cook that maximizes the number of people fed.

## 🎯 Problem Statement

The application addresses the classic resource optimization problem in cooking:
- **Input**: Available ingredients with specific quantities
- **Goal**: Find the optimal combination of recipes to maximize people fed
- **Constraint**: Cannot exceed available ingredient quantities

## 🏗️ Architecture Overview

### Technology Stack
- **Backend**: ASP.NET Core MVC (.NET 6+)
- **Database**: SQL Server with Entity Framework Core
- **Caching**: In-Memory Caching
- **Frontend**: Razor Views with Bootstrap
- **Architecture Pattern**: Repository Pattern with Dependency Injection

### Core Components

#### 1. **Optimization Engine** (`Core/Optimizer.cs`)
- Implements recursive backtracking algorithm
- Explores all possible recipe combinations
- Tracks optimal solution (maximum people fed)
- Time complexity: O(2^n) where n is number of recipes

#### 2. **Data Layer**
- **Models**: Recipe, Ingredient, RecipeIngredient, AvailableIngredient
- **DbContext**: Entity Framework configuration with proper relationships
- **Repository Pattern**: Abstraction layer for data access

#### 3. **Service Layer** (`Services/OptimizerService.cs`)
- Business logic orchestration
- Caching strategy implementation
- Integration between data layer and optimization engine

#### 4. **Web Layer**
- MVC Controllers with proper error handling
- Global exception middleware
- Response caching for performance

## 🚀 Getting Started

### Prerequisites
- .NET 6.0 SDK or later
- SQL Server (LocalDB acceptable for development)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
```bash
git clone <repository-url>
cd LinearOptimizationFoodApp
```

2. **Update Connection String**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FoodOptimizerDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

3. **Run the application**
```bash
dotnet restore
dotnet run
```

The application will:
- Automatically create and migrate the database
- Seed initial data (recipes and ingredients)
- Launch on `https://localhost:5001`

## 📊 Sample Data

### Pre-loaded Recipes
| Recipe | Feeds | Ingredients |
|--------|-------|-------------|
| Burger | 1 | Meat(1), Lettuce(1), Tomato(1), Cheese(1), Dough(1) |
| Pie | 1 | Dough(2), Meat(2) |
| Sandwich | 1 | Dough(1), Cucumber(1) |
| Pasta | 2 | Dough(2), Tomato(1), Cheese(2), Meat(1) |
| Salad | 3 | Lettuce(2), Tomato(2), Cucumber(1), Cheese(2), Olives(1) |
| Pizza | 4 | Dough(3), Tomato(2), Cheese(3), Olives(1) |

### Default Available Ingredients
- Cucumber: 2 pieces
- Olives: 2 pieces  
- Lettuce: 3 pieces
- Meat: 6 pieces
- Tomato: 6 pieces
- Cheese: 6 pieces
- Dough: 10 pieces

## 🔧 Key Features

### 1. **Optimization Algorithm**
- Recursive backtracking with pruning
- Explores all feasible combinations
- Guarantees optimal solution
- Tracks ingredient usage and remaining quantities

### 2. **Performance Optimizations**
- Multi-level caching strategy:
  - Recipes: 30-minute cache
  - Ingredients: 1-hour cache  
  - Available ingredients: 5-minute cache
- Response caching on controllers
- Efficient database queries with EF Core

### 3. **Error Handling**
- Global exception middleware
- Graceful degradation on errors
- Comprehensive logging
- User-friendly error pages

### 4. **Data Management**
- Automatic database seeding
- Repository pattern for testability
- Proper entity relationships
- Migration support

## 🎨 User Interface

### Main Features
- **Recipe Display**: Shows all available recipes with ingredients
- **Optimization Results**: Displays optimal combination and people fed
- **Ingredient Management**: View and modify available ingredients
- **Usage Tracking**: Shows used vs. remaining ingredients

### Admin Features
- Add/Edit recipes
- Manage ingredient inventory
- View optimization statistics
- Cache management

## 🧪 Algorithm Details

### Optimization Strategy
```csharp
// Pseudo-code for the optimization algorithm
function FindOptimal(availableIngredients, currentPath, peopleFed):
    for each recipe in recipes:
        if CanMake(recipe, availableIngredients):
            UpdateIngredients(availableIngredients, recipe)
            AddToPath(currentPath, recipe)
            FindOptimal(availableIngredients, currentPath, peopleFed + recipe.Feeds)
            RemoveFromPath(currentPath, recipe)
            RestoreIngredients(availableIngredients, recipe)
    
    if currentPath.PeopleFed > bestSolution.PeopleFed:
        bestSolution = currentPath
```

### Complexity Analysis
- **Time Complexity**: O(2^n) - explores all recipe combinations
- **Space Complexity**: O(n) - recursive call stack depth
- **Optimization**: Early pruning when no more recipes can be made

## 📁 Project Structure

```
LinearOptimizationFoodApp/
├── Controllers/
│   ├── HomeController.cs
│   ├── Admin/
│   └── OptimizerController.cs
├── Core/
│   └── Optimizer.cs              # Core optimization algorithm
├── Data/
│   ├── FoodOptimizerContext.cs   # EF DbContext
│   └── Migrations/
├── Models/                       # Data models
├── Repositories/                 # Data access layer
├── Services/                     # Business logic
├── ViewModels/                   # View-specific models
├── Views/                        # Razor views
├── Middleware/                   # Global exception handling
└── Program.cs                    # Application startup
```

## 🔍 Testing the Application

### Sample Test Cases

1. **Default Scenario**
   - Input: Default ingredient quantities
   - Expected: Optimal combination maximizing people fed

2. **Limited Ingredients**
   - Modify available ingredients to test constraint handling
   - Verify algorithm respects ingredient limits

3. **No Valid Combinations**
   - Set very low ingredient quantities
   - Test graceful handling of no-solution scenarios

## 🚀 Deployment Considerations

### Production Checklist
- [ ] Update connection strings for production database
- [ ] Configure proper logging (Serilog, NLog)
- [ ] Set up application insights/monitoring
- [ ] Configure HTTPS certificates
- [ ] Set up database backup strategy
- [ ] Configure caching for scale (Redis)

### Performance Tuning
- Database indexing on frequently queried columns
- Connection pooling configuration
- Output caching for static content
- Consider async/await patterns for I/O operations

## 🤝 Contributing

### Code Standards
- Follow C# naming conventions
- Use dependency injection
- Implement proper error handling
- Add unit tests for new features
- Document public APIs

### Adding New Features
1. Create feature branch
2. Implement with tests
3. Update documentation
4. Submit pull request

## 📜 License

This project is part of a technical assessment and is for evaluation purposes.

---

**Technical Assessment Completed By**: [Your Name]  
**Date**: [Current Date]  
**Framework**: ASP.NET Core MVC  
**Algorithm**: Recursive Backtracking Optimization
