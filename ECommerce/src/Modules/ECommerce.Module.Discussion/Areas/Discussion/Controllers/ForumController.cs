using System;
using System.Threading.Tasks;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Discussion.Areas.Discussion.ViewModels;
using ECommerce.Module.Discussion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Module.Discussion.Areas.Discussion.Controllers
{
    [Area("Discussion")]
    [ApiController]
    [Authorize]
    [Route("api/forum")]
    public class ForumController : ControllerBase
    {
        private readonly IDiscussionForumService _forumService;
        private readonly IWorkContext _workContext;

        public ForumController(IDiscussionForumService forumService, IWorkContext workContext)
        {
            _forumService = forumService;
            _workContext = workContext;
        }

        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions([FromQuery] ForumQuestionListRequest request)
        {
            return await ExecuteAsync(async () => Ok(await _forumService.GetQuestionsAsync(request)));
        }

        [HttpGet("questions/{id:long}")]
        public async Task<IActionResult> GetQuestion(long id, [FromQuery] long? userId)
        {
            return await ExecuteAsync(async () =>
            {
                var viewerUserId = await ResolveOptionalUserIdAsync(userId);
                return Ok(await _forumService.GetQuestionAsync(id, viewerUserId));
            });
        }

        [HttpPost("questions")]
        public async Task<IActionResult> CreateQuestion([FromBody] ForumQuestionCreateRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                var userId = await ResolveBodyUserIdAsync(request?.UserId);
                var result = await _forumService.CreateQuestionAsync(request, userId);
                return Created(string.Empty, new ForumEnvelope<object> { Data = result });
            });
        }

        [HttpPut("questions/{id:long}")]
        public async Task<IActionResult> UpdateQuestion(long id, [FromBody] ForumQuestionUpdateRequest request)
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<ForumQuestionListItemVm> { Data = await _forumService.UpdateQuestionAsync(id, request) }));
        }

        [HttpDelete("questions/{id:long}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteQuestion(long id)
        {
            return await ExecuteAsync(async () =>
            {
                await _forumService.DeleteQuestionAsync(id);
                return NoContent();
            });
        }

        [HttpGet("questions/{id:long}/views")]
        public async Task<IActionResult> GetViews(long id, [FromQuery] long? userId)
        {
            return await ExecuteAsync(async () =>
            {
                var viewerUserId = await ResolveOptionalUserIdAsync(userId);
                return Ok(new ForumEnvelope<object> { Data = await _forumService.TrackQuestionViewAsync(id, viewerUserId) });
            });
        }

        [HttpGet("questions/{id:long}/viewers")]
        public async Task<IActionResult> GetViewers(long id)
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<object> { Data = await _forumService.GetQuestionViewersAsync(id) }));
        }

        [HttpGet("questions/{id:long}/replies")]
        public async Task<IActionResult> GetReplies(long id, [FromQuery] string sort = "votes", [FromQuery] string order = "DESC")
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<object> { Data = await _forumService.GetQuestionRepliesAsync(id, sort, order) }));
        }

        [HttpGet("questions/{questionId:long}/answers")]
        public async Task<IActionResult> GetAnswers(long questionId, [FromQuery] string sort = "votes", [FromQuery] string order = "DESC")
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<object> { Data = await _forumService.GetAnswersAsync(questionId, sort, order) }));
        }

        [HttpPost("questions/{questionId:long}/answers")]
        public async Task<IActionResult> CreateAnswer(long questionId, [FromBody] ForumAnswerCreateRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                var userId = await ResolveBodyUserIdAsync(request?.UserId);
                var answer = await _forumService.CreateAnswerAsync(questionId, request, userId);
                return Created(string.Empty, new ForumEnvelope<ForumAnswerVm> { Data = answer });
            });
        }

        [HttpPatch("answers/{id:long}/accept")]
        [HttpPatch("questions/{questionId:long}/answers/{id:long}/accept")]
        public async Task<IActionResult> AcceptAnswer(long id)
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<object> { Data = await _forumService.AcceptAnswerAsync(id) }));
        }

        [HttpGet("comments/{id:long}")]
        public async Task<IActionResult> GetComment(long id)
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<ForumCommentVm> { Data = await _forumService.GetCommentAsync(id) }));
        }

        [HttpGet("questions/{questionId:long}/comments")]
        public async Task<IActionResult> GetQuestionComments(long questionId)
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<object> { Data = await _forumService.GetQuestionCommentsAsync(questionId) }));
        }

        [HttpGet("answers/{answerId:long}/comments")]
        public async Task<IActionResult> GetAnswerComments(long answerId)
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<object> { Data = await _forumService.GetAnswerCommentsAsync(answerId) }));
        }

        [HttpPost("comments")]
        public async Task<IActionResult> CreateComment([FromBody] ForumCommentCreateRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                var userId = await ResolveBodyUserIdAsync(request?.UserId);
                var comment = await _forumService.CreateCommentAsync(request, userId);
                return Created(string.Empty, new ForumEnvelope<ForumCommentVm> { Data = comment });
            });
        }

        [HttpPost("questions/{questionId:long}/comments")]
        public async Task<IActionResult> CreateQuestionComment(long questionId, [FromBody] ForumCommentCreateRequest request)
        {
            request ??= new ForumCommentCreateRequest();
            request.QuestionId = questionId;
            request.AnswerId = null;
            return await CreateComment(request);
        }

        [HttpPost("answers/{answerId:long}/comments")]
        public async Task<IActionResult> CreateAnswerComment(long answerId, [FromBody] ForumCommentCreateRequest request)
        {
            request ??= new ForumCommentCreateRequest();
            request.AnswerId = answerId;
            request.QuestionId = null;
            return await CreateComment(request);
        }

        [HttpPost("votes")]
        public async Task<IActionResult> CastVote([FromBody] ForumVoteRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                var userId = await ResolveBodyUserIdAsync(request?.UserId);
                var vote = await _forumService.CastVoteAsync(request, userId);
                return Created(string.Empty, new ForumEnvelope<object> { Data = vote });
            });
        }

        [HttpGet("bookmarks/{userId:long}")]
        public async Task<IActionResult> GetBookmarks(long userId)
        {
            return await ExecuteAsync(async () =>
            {
                var allowedUserId = await ResolveRouteUserIdAsync(userId);
                return Ok(new ForumEnvelope<object> { Data = await _forumService.GetBookmarksAsync(allowedUserId) });
            });
        }

        [HttpPost("bookmarks")]
        public async Task<IActionResult> AddBookmark([FromBody] ForumBookmarkRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                var userId = await ResolveBodyUserIdAsync(request?.UserId);
                var bookmark = await _forumService.AddBookmarkAsync(request, userId);
                return Created(string.Empty, new ForumEnvelope<object> { Data = bookmark });
            });
        }

        [HttpDelete("bookmarks/{userId:long}/{questionId:long}")]
        public async Task<IActionResult> RemoveBookmark(long userId, long questionId)
        {
            return await ExecuteAsync(async () =>
            {
                var allowedUserId = await ResolveRouteUserIdAsync(userId);
                await _forumService.RemoveBookmarkAsync(allowedUserId, questionId);
                return NoContent();
            });
        }

        [HttpPost("media")]
        public async Task<IActionResult> AddMedia([FromBody] ForumMediaRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                var media = await _forumService.AddMediaAsync(request);
                return Created(string.Empty, new ForumEnvelope<ForumMediaVm> { Data = media });
            });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] ForumSearchRequest request)
        {
            return await ExecuteAsync(async () => Ok(await _forumService.SearchAsync(request)));
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] ForumUserListRequest request)
        {
            return await ExecuteAsync(async () => Ok(await _forumService.GetUsersAsync(request)));
        }

        [HttpGet("users/{id:long}")]
        public async Task<IActionResult> GetUser(long id)
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<ForumUserSummaryVm> { Data = await _forumService.GetUserAsync(id) }));
        }

        [HttpGet("tags")]
        public async Task<IActionResult> GetTags([FromQuery] string q)
        {
            return await ExecuteAsync(async () => Ok(await _forumService.GetTagsAsync(q)));
        }

        [HttpGet("notifications/{userId:long}")]
        public async Task<IActionResult> GetNotifications(long userId)
        {
            return await ExecuteAsync(async () =>
            {
                var allowedUserId = await ResolveRouteUserIdAsync(userId);
                return Ok(new ForumEnvelope<object> { Data = await _forumService.GetNotificationsAsync(allowedUserId) });
            });
        }

        [HttpPatch("notifications/{id:long}/read")]
        public async Task<IActionResult> MarkNotificationRead(long id)
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<object> { Data = await _forumService.MarkNotificationReadAsync(id) }));
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            return await ExecuteAsync(async () => Ok(new ForumEnvelope<object> { Data = await _forumService.GetStatsAsync() }));
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> Health()
        {
            return await ExecuteAsync(async () => Ok(await _forumService.HealthCheckAsync()));
        }

        private async Task<long> ResolveBodyUserIdAsync(long? requestedUserId)
        {
            if (requestedUserId.HasValue && User.IsInRole("admin"))
            {
                return requestedUserId.Value;
            }

            var currentUser = await _workContext.GetCurrentUser();
            return currentUser.Id;
        }

        private async Task<long?> ResolveOptionalUserIdAsync(long? requestedUserId)
        {
            if (requestedUserId.HasValue)
            {
                return await ResolveRouteUserIdAsync(requestedUserId.Value);
            }

            var currentUser = await _workContext.GetCurrentUser();
            return currentUser?.Id;
        }

        private async Task<long> ResolveRouteUserIdAsync(long requestedUserId)
        {
            if (User.IsInRole("admin"))
            {
                return requestedUserId;
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser.Id != requestedUserId)
            {
                throw new UnauthorizedAccessException();
            }

            return currentUser.Id;
        }

        private static async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(new { error = ex.Message });
            }
            catch (ForumNotFoundException ex)
            {
                return new NotFoundObjectResult(new { error = ex.Message });
            }
            catch (ForumConflictException ex)
            {
                return new ConflictObjectResult(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return new ForbidResult();
            }
        }
    }
}
