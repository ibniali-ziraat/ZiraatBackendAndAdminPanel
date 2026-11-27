using System;
using System.Collections.Generic;

namespace ECommerce.Module.Discussion.Areas.Discussion.ViewModels
{
    public class ForumEnvelope<T>
    {
        public T Data { get; set; }

        public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    }

    public class ForumPagedEnvelope<T>
    {
        public IList<T> Data { get; set; } = new List<T>();

        public ForumPagination Pagination { get; set; }

        public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    }

    public class ForumPagination
    {
        public int Page { get; set; }

        public int Limit { get; set; }

        public int Total { get; set; }

        public int TotalPages { get; set; }

        public bool HasMore { get; set; }
    }

    public class ForumUserSummaryVm
    {
        public long Id { get; set; }

        public string Username { get; set; }

        public string DisplayName { get; set; }

        public string Email { get; set; }

        public string Avatar { get; set; }

        public string Bio { get; set; }

        public int Reputation { get; set; }

        public int GoldBadges { get; set; }

        public int SilverBadges { get; set; }

        public int BronzeBadges { get; set; }

        public string Location { get; set; }

        public string Website { get; set; }

        public bool IsOnline { get; set; }

        public DateTimeOffset JoinedAt { get; set; }

        public DateTimeOffset LastSeen { get; set; }
    }

    public class ForumQuestionListItemVm
    {
        public long Id { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public long UserId { get; set; }

        public int Votes { get; set; }

        public int Views { get; set; }

        public int AnswersCount { get; set; }

        public long? AcceptedAnswerId { get; set; }

        public string Status { get; set; }

        public bool IsBounty { get; set; }

        public int BountyAmount { get; set; }

        public int Favorites { get; set; }

        public bool IsProtected { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset LastActivityAt { get; set; }

        public IList<string> Tags { get; set; } = new List<string>();

        public IList<ForumMediaVm> Media { get; set; } = new List<ForumMediaVm>();

        public ForumUserSummaryVm User { get; set; }
    }

    public class ForumQuestionDetailVm : ForumQuestionListItemVm
    {
        public IList<ForumCommentVm> Comments { get; set; } = new List<ForumCommentVm>();

        public IList<ForumLinkedQuestionVm> LinkedQuestions { get; set; } = new List<ForumLinkedQuestionVm>();

        public object ViewTrackingInfo { get; set; }
    }

    public class ForumAnswerVm
    {
        public long Id { get; set; }

        public long QuestionId { get; set; }

        public long UserId { get; set; }

        public string Body { get; set; }

        public int Votes { get; set; }

        public bool IsAccepted { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public IList<ForumCommentVm> Comments { get; set; } = new List<ForumCommentVm>();

        public IList<ForumMediaVm> Media { get; set; } = new List<ForumMediaVm>();

        public ForumUserSummaryVm User { get; set; }
    }

    public class ForumCommentVm
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public long? QuestionId { get; set; }

        public long? AnswerId { get; set; }

        public string Body { get; set; }

        public int Votes { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ForumUserSummaryVm User { get; set; }
    }

    public class ForumMediaVm
    {
        public long Id { get; set; }

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

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class ForumTagVm
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Color { get; set; }

        public int QuestionsCount { get; set; }
    }

    public class ForumLinkedQuestionVm
    {
        public long LinkedQuestionId { get; set; }

        public string Title { get; set; }

        public int Votes { get; set; }

        public int AnswersCount { get; set; }
    }
}
