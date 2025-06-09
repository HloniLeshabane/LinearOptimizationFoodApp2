# Linear Optimization Food App - Test Suite

This document provides comprehensive information about the test suite for the Linear Optimization Food App, built with .NET 8.

## 📋 Table of Contents

- [Overview](#overview)
- [Test Architecture](#test-architecture)
- [Test Categories](#test-categories)
- [Getting Started](#getting-started)
- [Running Tests](#running-tests)
- [Test Structure](#test-structure)
- [Testing Frameworks & Tools](#testing-frameworks--tools)
- [Code Coverage](#code-coverage)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## 🎯 Overview

The test suite provides comprehensive coverage for the Linear Optimization Food App, ensuring reliability and maintainability of the codebase. The tests are organized into three main layers:

- **Unit Tests**: Testing individual components in isolation
- **Integration Tests**: Testing component interactions, particularly with Entity Framework
- **Controller Tests**: Testing MVC controllers with mocked dependencies

## 🏗️ Test Architecture

```
LinearOptimizationFoodAppTests/
├── Controllers/
│   ├── HomeControllerTests.cs
│   ├── IngredientsControllerTests.cs
│   ├── OptimizerControllerTests.cs
│   └── RecipesControllerTests.cs
├── Core/
│   ├── IngredientRepositoryTests.cs
│   └── OptimizerServiceTests.cs
└── RepositoryTests/
    ├── IngredientRepositoryTests.cs
    └── RecipeRepositoryTests.cs
```

## 📊 Test Categories

### Controller Tests
- **HomeControllerTests**: Tests for the main landing page controller
- **IngredientsControllerTests**: Tests for ingredient management functionality
- **OptimizerControllerTests**: Tests for optimization algorithms and results
- **RecipesControllerTests**: Tests for recipe browsing and search functionality

### Service Tests
- **OptimizerServiceTests**: Tests for the core optimization logic and caching

### Repository Tests
- **IngredientRepositoryTests**: Tests for ingredient data access layer
- **RecipeRepositoryTests**: Tests for recipe data access layer

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Git

### Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd LinearOptimizationFoodApp
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the solution:
```bash
dotnet build
```

## 🧪 Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Tests with Detailed Output
```bash
dotnet test --verbosity normal
```

### Run Tests in a Specific Category
```bash
# Run only controller tests
dotnet test --filter "FullyQualifiedName~Controllers"

# Run only repository tests
dotnet test --filter "FullyQualifiedName~RepositoryTests"

# Run only service tests
dotnet test --filter "FullyQualifiedName~OptimizerServiceTests"
```

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Tests in Watch Mode
```bash
dotnet watch test
```

## 📁 Test Structure

### Controller Tests Pattern
Each controller test class follows this structure:
- Constructor tests (dependency injection validation)
- Action method tests (happy path and edge cases)
- Exception handling tests
- Model validation tests

Example test categories for controllers:
```csharp
#region Constructor Tests
#region Index Action Tests  
#region Update Action Tests
#region Integration Tests
```

### Repository Tests Pattern
Repository tests include:
- CRUD operations testing
- Entity Framework integration
- Data validation
- Error handling
- Full lifecycle tests

### Service Tests Pattern
Service tests cover:
- Business logic validation
- Caching mechanisms
- Error handling and resilience
- Integration with repositories

## 🛠️ Testing Frameworks & Tools

### Core Testing Stack
- **xUnit**: Primary testing framework
- **FluentAssertions**: Enhanced assertion library for better readability
- **Moq**: Mocking framework for dependencies
- **Entity Framework In-Memory**: Database testing without external dependencies

### Key Testing Libraries
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.4.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
```

## 📈 Code Coverage

### Coverage Goals
- **Minimum Coverage**: 80%
- **Target Coverage**: 90%+
- **Critical Components**: 95%+ (Services, Repositories)

### Generating Coverage Reports
```bash
# Install coverage tools
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### Coverage Exclusions
The following are excluded from coverage requirements:
- Auto-generated code
- Program.cs and startup files
- Migration files
- Models with only properties

## ✅ Best Practices

### Test Naming Convention
```csharp
[Fact]
public async Task MethodName_WithSpecificCondition_ExpectedBehavior()
{
    // Arrange
    // Act  
    // Assert
}
```

### AAA Pattern
All tests follow the **Arrange-Act-Assert** pattern:
```csharp
[Fact]
public async Task GetAllRecipesAsync_WithValidData_ReturnsAllRecipes()
{
    // Arrange
    var expectedRecipes = CreateTestRecipes();
    
    // Act
    var result = await _repository.GetAllRecipesAsync();
    
    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(expectedRecipes.Count);
}
```

### Mock Setup Best Practices
- Use descriptive variable names for mocks
- Setup mocks in the Arrange section
- Verify mock interactions when testing behavior
- Use `It.IsAny<T>()` for flexible parameter matching

### Test Data Management
- Use helper methods for creating test data
- Implement `IDisposable` for cleanup in integration tests
- Use unique database names for parallel test execution

## 🔧 Troubleshooting

### Common Issues

#### Tests Failing Due to Database Conflicts
**Problem**: Tests interfere with each other when using shared database contexts.

**Solution**: Use unique database names:
```csharp
var options = new DbContextOptionsBuilder<FoodOptimizerContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
```

#### Mock Setup Not Working
**Problem**: Mocked methods returning default values instead of expected results.

**Solution**: Ensure proper setup syntax:
```csharp
_mockService.Setup(x => x.GetDataAsync())
    .ReturnsAsync(expectedData);
```

#### Async Test Issues
**Problem**: Tests hanging or not completing.

**Solution**: Always use `await` with async operations and `Task` return types:
```csharp
[Fact]
public async Task TestMethod()
{
    var result = await _service.GetDataAsync();
    // assertions...
}
```

### Debug Mode
To debug tests in Visual Studio:
1. Set breakpoints in test methods
2. Right-click the test and select "Debug Test"
3. Use Test Explorer for managing test execution

### Performance Considerations
- Repository tests using Entity Framework In-Memory are faster than SQL Server
- Controller tests with mocked dependencies execute quickly
- Service tests balance speed with realistic behavior testing

## 📞 Support

For questions about the test suite:
1. Check existing test patterns for similar scenarios
2. Review this documentation
3. Create an issue in the project repository
4. Follow the established testing conventions when adding new tests

## 🔄 Continuous Integration

The test suite is designed to run in CI/CD pipelines:
- All tests must pass before merging
- Coverage reports are generated automatically
- Performance regression tests included
- Parallel test execution supported

---

**Last Updated**: [Current Date]
**Test Framework Version**: xUnit 2.4.2
**.NET Version**: .NET 8
