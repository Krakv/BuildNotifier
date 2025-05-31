using Microsoft.AspNetCore.Mvc;
using BuildNotifier.Services;
using BuildNotifier.Data.Models.BambooWebhookPayload;

namespace BuildNotifier.Controllers
{
    [ApiController]
    [Route("api/webhook")]
    public class WebhookController : Controller
    {
        private readonly TelegramNotificationService _notificationService;
        public WebhookController(TelegramNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("bamboo")]
        public async Task<IActionResult> HandleWebhook([FromBody] BuildWebhook payload)
        {
            if (payload.Build.Status == "FAILED")
            {
                _notificationService.NotifyFailedBuildAsync(payload);
            }
            return Accepted();
        }
    }
}
