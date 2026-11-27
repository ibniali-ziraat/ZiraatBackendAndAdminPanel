using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Reviews.Areas.Reviews.ViewModels;

namespace ECommerce.Module.Reviews.Areas.Reviews.Controllers
{
    [Area("Reviews")]
    [ApiController]
    [Route("api/reviews")]
    public class ReplyController :ControllerBase
    {
        private readonly IRepository<Models.Reply> _replyRepository;
        private readonly IWorkContext _workContext;

        public ReplyController(IRepository<Models.Reply> replyRepository, IWorkContext workContext)
        {
            _replyRepository = replyRepository;
            _workContext = workContext;
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> AddReply(ReplyForm model)
        {
            if (ModelState.IsValid)
            {
                var user = await _workContext.GetCurrentUser();
                model.ReplierName = user.FullName;

                var reply = new Models.Reply
                {
                    ReviewId = model.ReviewId,
                    UserId = user.Id,
                    Comment = model.Comment,
                    ReplierName = user.FullName
                };

                _replyRepository.Add(reply);
                _replyRepository.SaveChanges();

                return Ok(model);
            }

            return Ok(model);
        }
    }
}
