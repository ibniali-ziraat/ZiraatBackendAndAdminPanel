using System.Collections.Generic;

namespace ECommerce.Module.Discussion.Areas.Discussion.ViewModels
{
    public class ForumQuestionListRequest
    {
        public int Page { get; set; } = 1;

        public int Limit { get; set; } = 15;

        public string Sort { get; set; } = "created";

        public string Order { get; set; } = "DESC";

        public string Status { get; set; }

        public string Tag { get; set; }

        public long? UserId { get; set; }

        public bool? IsBounty { get; set; }

        public string Q { get; set; }
    }

    public class ForumQuestionCreateRequest
    {
        public string Title { get; set; }

        public string Body { get; set; }

        public long? UserId { get; set; }

        public IList<string> Tags { get; set; } = new List<string>();

        public bool IsBounty { get; set; }

        public int BountyAmount { get; set; }
    }

    public class ForumQuestionUpdateRequest
    {
        public string Title { get; set; }

        public string Body { get; set; }

        public string Status { get; set; }
    }

    public class ForumAnswerCreateRequest
    {
        public long? UserId { get; set; }

        public string Body { get; set; }
    }

    public class ForumCommentCreateRequest
    {
        public long? UserId { get; set; }

        public long? QuestionId { get; set; }

        public long? AnswerId { get; set; }

        public string Body { get; set; }
    }

    public class ForumVoteRequest
    {
        public long? UserId { get; set; }

        public string TargetType { get; set; }

        public long TargetId { get; set; }

        public int Value { get; set; }
    }

    public class ForumBookmarkRequest
    {
        public long? UserId { get; set; }

        public long QuestionId { get; set; }
    }

    public class ForumMediaRequest
    {
        public long? QuestionId { get; set; }

        public long? AnswerId { get; set; }

        public string Type { get; set; }

        public string Url { get; set; }

        public string Thumbnail { get; set; }

        public string AltText { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public string Duration { get; set; }

        public string Platform { get; set; }

        public long? FileSize { get; set; }

        public string MimeType { get; set; }
    }

    public class ForumSearchRequest
    {
        public string Q { get; set; }

        public int Page { get; set; } = 1;

        public int Limit { get; set; } = 15;
    }

    public class ForumUserListRequest
    {
        public int Page { get; set; } = 1;

        public int Limit { get; set; } = 15;

        public string Sort { get; set; } = "reputation";
    }
}
