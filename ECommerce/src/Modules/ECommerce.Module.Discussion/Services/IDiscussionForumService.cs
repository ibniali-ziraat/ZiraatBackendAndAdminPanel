using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Discussion.Areas.Discussion.ViewModels;

namespace ECommerce.Module.Discussion.Services
{
    public interface IDiscussionForumService
    {
        Task<ForumPagedEnvelope<ForumQuestionListItemVm>> GetQuestionsAsync(ForumQuestionListRequest request);

        Task<ForumEnvelope<ForumQuestionDetailVm>> GetQuestionAsync(long id, long? viewerUserId);

        Task<object> CreateQuestionAsync(ForumQuestionCreateRequest request, long userId);

        Task<ForumQuestionListItemVm> UpdateQuestionAsync(long id, ForumQuestionUpdateRequest request);

        Task DeleteQuestionAsync(long id);

        Task<object> TrackQuestionViewAsync(long id, long? viewerUserId);

        Task<object> GetQuestionViewersAsync(long id);

        Task<object> GetQuestionRepliesAsync(long id, string sort, string order);

        Task<IList<ForumAnswerVm>> GetAnswersAsync(long questionId, string sort, string order);

        Task<ForumAnswerVm> CreateAnswerAsync(long questionId, ForumAnswerCreateRequest request, long userId);

        Task<object> AcceptAnswerAsync(long answerId);

        Task<ForumCommentVm> GetCommentAsync(long id);

        Task<object> GetQuestionCommentsAsync(long questionId);

        Task<object> GetAnswerCommentsAsync(long answerId);

        Task<ForumCommentVm> CreateCommentAsync(ForumCommentCreateRequest request, long userId);

        Task<object> CastVoteAsync(ForumVoteRequest request, long userId);

        Task<IList<object>> GetBookmarksAsync(long userId);

        Task<object> AddBookmarkAsync(ForumBookmarkRequest request, long userId);

        Task RemoveBookmarkAsync(long userId, long questionId);

        Task<ForumMediaVm> AddMediaAsync(ForumMediaRequest request);

        Task<ForumPagedEnvelope<object>> SearchAsync(ForumSearchRequest request);

        Task<ForumPagedEnvelope<ForumUserSummaryVm>> GetUsersAsync(ForumUserListRequest request);

        Task<ForumUserSummaryVm> GetUserAsync(long id);

        Task<ForumPagedEnvelope<ForumTagVm>> GetTagsAsync(string q);

        Task<IList<object>> GetNotificationsAsync(long userId);

        Task<object> MarkNotificationReadAsync(long id);

        Task<object> GetStatsAsync();

        Task<object> HealthCheckAsync();
    }
}
