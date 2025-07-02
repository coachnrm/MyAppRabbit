using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyApp.Api.Services;
using MyApp.Shared;

namespace MyApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
    private readonly IMessagePublisher _bus;
    public ChatController(IMessagePublisher bus) => _bus = bus;

        [HttpPost]
        public IActionResult Post(ChatMessage dto)
        {
            if (string.IsNullOrWhiteSpace(dto.User) || string.IsNullOrWhiteSpace(dto.Text))
                return BadRequest("User and Text required.");

            var enriched = dto with { Timestamp = DateTime.UtcNow };
            _bus.PublishChat(enriched);

            return Accepted();
        }
    }
}
