using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Discussion.Models
{
    public class ForumUserProfile : EntityBase
    {
        public long UserId { get; set; }

        [StringLength(450)]
        public string Avatar { get; set; }

        public string Bio { get; set; }

        public int Reputation { get; set; }

        public int GoldBadges { get; set; }

        public int SilverBadges { get; set; }

        public int BronzeBadges { get; set; }

        [StringLength(450)]
        public string Location { get; set; }

        [StringLength(450)]
        public string Website { get; set; }

        public bool IsOnline { get; set; }

        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    }

    public class ForumTag : EntityBase
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        public string Description { get; set; }

        [StringLength(32)]
        public string Color { get; set; } = "#666666";

        public int QuestionsCount { get; set; }

        public IList<ForumQuestionTag> QuestionTags { get; set; } = new List<ForumQuestionTag>();
    }

    public class ForumQuestion : EntityBase
    {
        [Required]
        [StringLength(450)]
        public string Title { get; set; }

        [Required]
        public string Body { get; set; }

        public long UserId { get; set; }

        public int Votes { get; set; }

        public int Views { get; set; }

        public int AnswersCount { get; set; }

        public long? AcceptedAnswerId { get; set; }

        [StringLength(32)]
        public string Status { get; set; } = ForumQuestionStatuses.Open;

        public bool IsBounty { get; set; }

        public int BountyAmount { get; set; }

        public bool IsClosed { get; set; }

        public bool IsProtected { get; set; }

        public int Favorites { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

        public IList<ForumQuestionTag> QuestionTags { get; set; } = new List<ForumQuestionTag>();

        public IList<ForumAnswer> Answers { get; set; } = new List<ForumAnswer>();

        public IList<ForumComment> Comments { get; set; } = new List<ForumComment>();

        public IList<ForumMedia> Media { get; set; } = new List<ForumMedia>();
    }

    public class ForumQuestionTag
    {
        public long QuestionId { get; set; }

        public ForumQuestion Question { get; set; }

        public long TagId { get; set; }

        public ForumTag Tag { get; set; }
    }

    public class ForumAnswer : EntityBase
    {
        public long QuestionId { get; set; }

        public ForumQuestion Question { get; set; }

        public long UserId { get; set; }

        [Required]
        public string Body { get; set; }

        public int Votes { get; set; }

        public bool IsAccepted { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public IList<ForumComment> Comments { get; set; } = new List<ForumComment>();

        public IList<ForumMedia> Media { get; set; } = new List<ForumMedia>();
    }

    public class ForumComment : EntityBase
    {
        public long UserId { get; set; }

        public long? QuestionId { get; set; }

        public ForumQuestion Question { get; set; }

        public long? AnswerId { get; set; }

        public ForumAnswer Answer { get; set; }

        [Required]
        public string Body { get; set; }

        public int Votes { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class ForumVote : EntityBase
    {
        public long UserId { get; set; }

        [Required]
        [StringLength(32)]
        public string TargetType { get; set; }

        public long TargetId { get; set; }

        public int Value { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class ForumMedia : EntityBase
    {
        public long? QuestionId { get; set; }

        public ForumQuestion Question { get; set; }

        public long? AnswerId { get; set; }

        public ForumAnswer Answer { get; set; }

        [Required]
        [StringLength(32)]
        public string Type { get; set; }

        [Required]
        [StringLength(2048)]
        public string Url { get; set; }

        [StringLength(2048)]
        public string Thumbnail { get; set; }

        [StringLength(450)]
        public string AltText { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        [StringLength(64)]
        public string Duration { get; set; }

        [StringLength(128)]
        public string Platform { get; set; }

        public long? FileSize { get; set; }

        [StringLength(128)]
        public string MimeType { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class ForumBookmark : EntityBase
    {
        public long UserId { get; set; }

        public long QuestionId { get; set; }

        public ForumQuestion Question { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class ForumNotification : EntityBase
    {
        public long UserId { get; set; }

        [Required]
        [StringLength(32)]
        public string Type { get; set; }

        [Required]
        public string Message { get; set; }

        public long? QuestionId { get; set; }

        public long? AnswerId { get; set; }

        public bool IsRead { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class ForumLinkedQuestion
    {
        public long QuestionId { get; set; }

        public ForumQuestion Question { get; set; }

        public long LinkedQuestionId { get; set; }

        public ForumQuestion LinkedQuestion { get; set; }
    }

    public class ForumUserTag
    {
        public long UserId { get; set; }

        public long TagId { get; set; }

        public ForumTag Tag { get; set; }
    }

    public class ForumSearchSuggestion : EntityBase
    {
        [Required]
        [StringLength(256)]
        public string Term { get; set; }

        public int Weight { get; set; } = 1;
    }

    public class ForumQuestionView : EntityBase
    {
        public long QuestionId { get; set; }

        public ForumQuestion Question { get; set; }

        public long UserId { get; set; }

        public DateTimeOffset ViewedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public static class ForumQuestionStatuses
    {
        public const string Open = "open";
        public const string Answered = "answered";
        public const string Closed = "closed";
        public const string Discussion = "discussion";
    }

    public static class ForumTargetTypes
    {
        public const string Question = "question";
        public const string Answer = "answer";
        public const string Comment = "comment";
    }
}
