using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Catchlogr.Web.Pages
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ConfirmEmailModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public bool Success { get; private set; }

        public async Task OnGetAsync(string? userId, string? code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                Success = false;
                return;
            }

            var client = _httpClientFactory.CreateClient("CatchlogrApi");

            var url = "/api/auth/confirmEmail" +
                $"?userId={Uri.EscapeDataString(userId)}" +
                $"&code={Uri.EscapeDataString(code)}";

            var response = await client.GetAsync(url);

            Success = response.IsSuccessStatusCode;
        }
    }
}
