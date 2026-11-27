using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Discussion.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Discussion.Services
{
    public class FarmerForumDashboardDataSource : IFarmerAdvisoryDataSource
    {
        private readonly IRepository<ForumQuestion> _questions;
        private readonly IRepository<ForumAnswer> _answers;
        private readonly IRepository<ForumComment> _comments;

        public FarmerForumDashboardDataSource(IRepository<ForumQuestion> questions, IRepository<ForumAnswer> answers, IRepository<ForumComment> comments)
        {
            _questions = questions;
            _answers = answers;
            _comments = comments;
        }

        public async Task<FarmerAdvisoryDashboardVm> GetAdvisory(long userId, DateTimeOffset from, DateTimeOffset to)
        {
            var questions = _questions.Query().AsNoTracking().Where(x => x.UserId == userId && x.CreatedAt >= from && x.CreatedAt < to);
            var answers = _answers.Query().AsNoTracking().Where(x => x.UserId == userId && x.CreatedAt >= from && x.CreatedAt < to);
            var comments = _comments.Query().AsNoTracking().Where(x => x.UserId == userId && x.CreatedAt >= from && x.CreatedAt < to);
            var result = new FarmerAdvisoryDashboardVm
            {
                KeyMetrics = new List<FarmerMetricVm>
                {
                    Count("forum-questions", "Forum Questions", await questions.CountAsync()),
                    Count("knowledge-shared", "Forum Answers Shared", await answers.CountAsync()),
                    Count("forum-comments", "Forum Comments", await comments.CountAsync())
                }
            };
            var recentQuestions = await questions.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(10)
                .Select(x => new FarmerActivityVm { Title = x.Title, Reference = "question-" + x.Id, Status = x.Status, OccurredOn = x.CreatedAt }).ToListAsync();
            var recentAnswers = await answers.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(10)
                .Select(x => new FarmerActivityVm { Title = "Forum answer", Reference = "answer-" + x.Id, Status = x.IsAccepted ? "Accepted" : "Posted", OccurredOn = x.CreatedAt }).ToListAsync();
            result.RecentActivities = recentQuestions.Concat(recentAnswers).OrderByDescending(x => x.OccurredOn).Take(10).ToList();
            return result;
        }

        private static FarmerMetricVm Count(string code, string label, int count) => new()
        {
            Code = code, Label = label, NumericValue = count, Value = count.ToString(),
            Unit = "count", Source = "discussion-forum", DataStatus = "recorded"
        };
    }
}
