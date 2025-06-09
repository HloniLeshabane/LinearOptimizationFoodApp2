using Microsoft.AspNetCore.Mvc;
using LinearOptimizationFoodApp.Services;
using LinearOptimizationFoodApp.Models;
using LinearOptimizationFoodApp.Controllers.Admin;
using Microsoft.Extensions.Logging;
using System.Diagnostics;


namespace LinearOptimizationFoodApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IOptimizerService _optimizerService;
        private readonly ILogger<HomeController> _logger; // Fixed logger type

        public HomeController(IOptimizerService optimizerService, ILogger<HomeController> logger) // Added logger parameter
        {
            _optimizerService = optimizerService ?? throw new ArgumentNullException(nameof(optimizerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Initialize logger
        }

        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // 5 minutes
        public async Task<IActionResult> Index()
        {
            try
            {
                var recipes = await _optimizerService.GetAllRecipesAsync();
                // Ensure we never pass null to the view
                if (recipes == null)
                {
                    recipes = new List<Recipe>();
                }
                return View(recipes);
            }
            catch (Exception ex)
            {
                // Log the error if we have logging configured
                _logger?.LogError(ex, "Error loading recipes for home page");
                // Return empty list to prevent null reference
                return View(new List<Recipe>());
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorMessage = TempData["ErrorMessage"] as string ?? "An unexpected error occurred.";
            var statusCode = TempData["ErrorStatusCode"] as int? ?? 500;
            var errorDetails = TempData["ErrorDetails"] as string;

            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = errorMessage,
                StatusCode = statusCode,
                Details = errorDetails
            };

            return View(errorViewModel);
        }
    }
}