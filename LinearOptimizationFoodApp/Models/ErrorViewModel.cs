using System.Diagnostics;

namespace LinearOptimizationFoodApp.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string Message { get; set; } = "An unexpected error occurred.";
        public int StatusCode { get; set; } = 500;
        public string? Details { get; set; } // Only shown in development

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public bool ShowDetails => !string.IsNullOrEmpty(Details);
    }
}