using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Discussion.Areas.Discussion.ViewModels;
using ECommerce.Module.Discussion.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Discussion.Services
{
    public class DiscussionForumService : IDiscussionForumService
    {
        private readonly SimplDbContext _dbContext;

        public DiscussionForumService(SimplDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ForumPagedEnvelope<ForumQuestionListItemVm>> GetQuestionsAsync(ForumQuestionListRequest request)
        {
            request ??= new ForumQuestionListRequest();
            var query = _dbContext.Set<ForumQuestion>()
                .Include(x => x.QuestionTags).ThenInclude(x => x.Tag)
                .Include(x => x.Media)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x => x.Status == request.Status.Trim());
            }

            if (request.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == request.UserId.Value);
            }

            if (request.IsBounty.HasValue)
            {
                query = query.Where(x => x.IsBounty == request.IsBounty.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Q))
            {
                var search = request.Q.Trim();
                query = query.Where(x => x.Title.Contains(search) || x.Body.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(request.Tag))
            {
                var tag = request.Tag.Trim();
                query = query.Where(x => x.QuestionTags.Any(t => t.Tag.Name == tag));
            }

            var total = await query.CountAsync();
            var items = await ApplyQuestionSort(query, request.Sort, request.Order)
                .Skip((NormalizePage(request.Page) - 1) * NormalizeLimit(request.Limit))
                .Take(NormalizeLimit(request.Limit))
                .ToListAsync();

            var userMap = await GetUserMapAsync(items.Select(x => x.UserId));
            return Paged(items.Select(x => MapQuestionListItem(x, userMap)).ToList(), request.Page, request.Limit, total);
        }

        public async Task<ForumEnvelope<ForumQuestionDetailVm>> GetQuestionAsync(long id, long? viewerUserId)
        {
            var question = await _dbContext.Set<ForumQuestion>()
                .Include(x => x.QuestionTags).ThenInclude(x => x.Tag)
                .Include(x => x.Media)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (question == null)
            {
                throw new ForumNotFoundException("Question not found");
            }

            var isNewView = false;
            if (viewerUserId.HasValue)
            {
                var existingView = await _dbContext.Set<ForumQuestionView>()
                    .AnyAsync(x => x.QuestionId == id && x.UserId == viewerUserId.Value);

                if (!existingView)
                {
                    _dbContext.Set<ForumQuestionView>().Add(new ForumQuestionView
                    {
                        QuestionId = id,
                        UserId = viewerUserId.Value,
                        ViewedAt = DateTimeOffset.UtcNow
                    });
                    question.Views++;
                    isNewView = true;
                    await _dbContext.SaveChangesAsync();
                }
            }
            else
            {
                question.Views++;
                await _dbContext.SaveChangesAsync();
            }

            var comments = await _dbContext.Set<ForumComment>()
                .Where(x => x.QuestionId == id)
                .OrderBy(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var linked = await _dbContext.Set<ForumLinkedQuestion>()
                .Where(x => x.QuestionId == id)
                .Select(x => x.LinkedQuestion)
                .AsNoTracking()
                .ToListAsync();

            var userIds = new List<long> { question.UserId };
            userIds.AddRange(comments.Select(x => x.UserId));
            var userMap = await GetUserMapAsync(userIds);

            var model = MapQuestionDetail(question, userMap);
            model.Comments = comments.Select(x => MapComment(x, userMap)).ToList();
            model.LinkedQuestions = linked.Select(x => new ForumLinkedQuestionVm
            {
                LinkedQuestionId = x.Id,
                Title = x.Title,
                Votes = x.Votes,
                AnswersCount = x.AnswersCount
            }).ToList();
            model.ViewTrackingInfo = new
            {
                isNewView,
                userId = viewerUserId.HasValue ? viewerUserId.Value : (object)"anonymous"
            };

            return new ForumEnvelope<ForumQuestionDetailVm> { Data = model };
        }

        public async Task<object> CreateQuestionAsync(ForumQuestionCreateRequest request, long userId)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            {
                throw new ArgumentException("title and body required");
            }

            await EnsureUserExistsAsync(userId);
            await EnsureForumProfileAsync(userId);

            var now = DateTimeOffset.UtcNow;
            var question = new ForumQuestion
            {
                Title = request.Title.Trim(),
                Body = request.Body.Trim(),
                UserId = userId,
                IsBounty = request.IsBounty,
                BountyAmount = request.BountyAmount,
                CreatedAt = now,
                UpdatedAt = now,
                LastActivityAt = now
            };

            _dbContext.Set<ForumQuestion>().Add(question);
            await _dbContext.SaveChangesAsync();

            var tagNames = NormalizeTagNames(request.Tags);
            foreach (var name in tagNames)
            {
                var tag = await _dbContext.Set<ForumTag>().FirstOrDefaultAsync(x => x.Name == name);
                if (tag == null)
                {
                    tag = new ForumTag { Name = name, Color = "#666666", QuestionsCount = 0 };
                    _dbContext.Set<ForumTag>().Add(tag);
                    await _dbContext.SaveChangesAsync();
                }

                tag.QuestionsCount++;
                _dbContext.Set<ForumQuestionTag>().Add(new ForumQuestionTag { QuestionId = question.Id, TagId = tag.Id });
            }

            await AddSearchSuggestionAsync(question.Title);
            await _dbContext.SaveChangesAsync();

            return new
            {
                id = question.Id,
                title = question.Title,
                tags = tagNames
            };
        }

        public async Task<ForumQuestionListItemVm> UpdateQuestionAsync(long id, ForumQuestionUpdateRequest request)
        {
            var question = await _dbContext.Set<ForumQuestion>()
                .Include(x => x.QuestionTags).ThenInclude(x => x.Tag)
                .Include(x => x.Media)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (question == null)
            {
                throw new ForumNotFoundException("Question not found");
            }

            if (!string.IsNullOrWhiteSpace(request?.Title))
            {
                question.Title = request.Title.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request?.Body))
            {
                question.Body = request.Body.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request?.Status))
            {
                var status = request.Status.Trim();
                if (!IsValidStatus(status))
                {
                    throw new ArgumentException("status must be open, answered, closed, or discussion");
                }

                question.Status = status;
                question.IsClosed = status == ForumQuestionStatuses.Closed;
            }

            question.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();

            var userMap = await GetUserMapAsync(new[] { question.UserId });
            return MapQuestionListItem(question, userMap);
        }

        public async Task DeleteQuestionAsync(long id)
        {
            var question = await _dbContext.Set<ForumQuestion>()
                .Include(x => x.QuestionTags).ThenInclude(x => x.Tag)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (question == null)
            {
                return;
            }

            var answerIds = await _dbContext.Set<ForumAnswer>()
                .Where(x => x.QuestionId == id)
                .Select(x => x.Id)
                .ToListAsync();

            var commentIds = await _dbContext.Set<ForumComment>()
                .Where(x => x.QuestionId == id || (x.AnswerId.HasValue && answerIds.Contains(x.AnswerId.Value)))
                .Select(x => x.Id)
                .ToListAsync();

            _dbContext.Set<ForumLinkedQuestion>().RemoveRange(_dbContext.Set<ForumLinkedQuestion>().Where(x => x.QuestionId == id || x.LinkedQuestionId == id));
            _dbContext.Set<ForumQuestionView>().RemoveRange(_dbContext.Set<ForumQuestionView>().Where(x => x.QuestionId == id));
            _dbContext.Set<ForumBookmark>().RemoveRange(_dbContext.Set<ForumBookmark>().Where(x => x.QuestionId == id));
            _dbContext.Set<ForumNotification>().RemoveRange(_dbContext.Set<ForumNotification>().Where(x => x.QuestionId == id));
            _dbContext.Set<ForumVote>().RemoveRange(_dbContext.Set<ForumVote>().Where(x => x.TargetType == ForumTargetTypes.Question && x.TargetId == id));
            _dbContext.Set<ForumMedia>().RemoveRange(_dbContext.Set<ForumMedia>().Where(x => x.QuestionId == id || (x.AnswerId.HasValue && answerIds.Contains(x.AnswerId.Value))));
            _dbContext.Set<ForumComment>().RemoveRange(_dbContext.Set<ForumComment>().Where(x => x.QuestionId == id || (x.AnswerId.HasValue && answerIds.Contains(x.AnswerId.Value))));
            _dbContext.Set<ForumVote>().RemoveRange(_dbContext.Set<ForumVote>().Where(x => x.TargetType == ForumTargetTypes.Answer && answerIds.Contains(x.TargetId)));
            _dbContext.Set<ForumVote>().RemoveRange(_dbContext.Set<ForumVote>().Where(x => x.TargetType == ForumTargetTypes.Comment && commentIds.Contains(x.TargetId)));
            _dbContext.Set<ForumAnswer>().RemoveRange(_dbContext.Set<ForumAnswer>().Where(x => x.QuestionId == id));

            foreach (var questionTag in question.QuestionTags)
            {
                if (questionTag.Tag.QuestionsCount > 0)
                {
                    questionTag.Tag.QuestionsCount--;
                }
            }

            _dbContext.Set<ForumQuestionTag>().RemoveRange(question.QuestionTags);
            _dbContext.Set<ForumQuestion>().Remove(question);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<object> TrackQuestionViewAsync(long id, long? viewerUserId)
        {
            var question = await _dbContext.Set<ForumQuestion>().FirstOrDefaultAsync(x => x.Id == id);
            if (question == null)
            {
                throw new ForumNotFoundException("Question not found");
            }

            if (!viewerUserId.HasValue)
            {
                return new { views = question.Views, isNewView = (bool?)null, message = "Provide userId query param to track unique views" };
            }

            var existingView = await _dbContext.Set<ForumQuestionView>().AnyAsync(x => x.QuestionId == id && x.UserId == viewerUserId.Value);
            if (existingView)
            {
                return new { views = question.Views, isNewView = false, userId = viewerUserId.Value };
            }

            _dbContext.Set<ForumQuestionView>().Add(new ForumQuestionView
            {
                QuestionId = id,
                UserId = viewerUserId.Value,
                ViewedAt = DateTimeOffset.UtcNow
            });
            question.Views++;
            await _dbContext.SaveChangesAsync();
            return new { views = question.Views, isNewView = true, userId = viewerUserId.Value };
        }

        public async Task<object> GetQuestionViewersAsync(long id)
        {
            var question = await _dbContext.Set<ForumQuestion>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (question == null)
            {
                throw new ForumNotFoundException("Question not found");
            }

            var views = await _dbContext.Set<ForumQuestionView>()
                .Where(x => x.QuestionId == id)
                .OrderByDescending(x => x.ViewedAt)
                .AsNoTracking()
                .ToListAsync();

            var userMap = await GetUserMapAsync(views.Select(x => x.UserId));
            var viewers = views.Where(x => userMap.ContainsKey(x.UserId)).Select(x => new
            {
                userId = x.UserId,
                username = userMap[x.UserId].Username,
                displayName = userMap[x.UserId].DisplayName,
                avatar = userMap[x.UserId].Avatar,
                reputation = userMap[x.UserId].Reputation,
                viewedAt = x.ViewedAt
            }).ToList();

            return new
            {
                questionId = id,
                totalViews = question.Views,
                uniqueViewers = viewers.Count,
                viewers
            };
        }

        public async Task<object> GetQuestionRepliesAsync(long id, string sort, string order)
        {
            if (!await _dbContext.Set<ForumQuestion>().AnyAsync(x => x.Id == id))
            {
                throw new ForumNotFoundException("Question not found");
            }

            var replies = await GetAnswersAsync(id, sort, order);
            return new
            {
                questionId = id,
                count = replies.Count,
                replies
            };
        }

        public async Task<IList<ForumAnswerVm>> GetAnswersAsync(long questionId, string sort, string order)
        {
            var query = _dbContext.Set<ForumAnswer>()
                .Include(x => x.Comments)
                .Include(x => x.Media)
                .Where(x => x.QuestionId == questionId)
                .AsNoTracking();

            var answers = await ApplyAnswerSort(query, sort, order).ToListAsync();
            var userIds = answers.Select(x => x.UserId).Concat(answers.SelectMany(x => x.Comments.Select(c => c.UserId)));
            var userMap = await GetUserMapAsync(userIds);
            return answers.Select(x => MapAnswer(x, userMap)).ToList();
        }

        public async Task<ForumAnswerVm> CreateAnswerAsync(long questionId, ForumAnswerCreateRequest request, long userId)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
            {
                throw new ArgumentException("body required");
            }

            var question = await _dbContext.Set<ForumQuestion>().FirstOrDefaultAsync(x => x.Id == questionId);
            if (question == null)
            {
                throw new ForumNotFoundException("Question not found");
            }

            await EnsureUserExistsAsync(userId);
            await EnsureForumProfileAsync(userId);

            var now = DateTimeOffset.UtcNow;
            var answer = new ForumAnswer
            {
                QuestionId = questionId,
                UserId = userId,
                Body = request.Body.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Set<ForumAnswer>().Add(answer);
            question.AnswersCount++;
            question.LastActivityAt = now;
            await AddNotificationAsync(question.UserId, "answer", "A new answer was added to your question.", question.Id, null, userId);
            await _dbContext.SaveChangesAsync();

            var userMap = await GetUserMapAsync(new[] { userId });
            return MapAnswer(answer, userMap);
        }

        public async Task<object> AcceptAnswerAsync(long answerId)
        {
            var answer = await _dbContext.Set<ForumAnswer>().FirstOrDefaultAsync(x => x.Id == answerId);
            if (answer == null)
            {
                throw new ForumNotFoundException("Answer not found");
            }

            var question = await _dbContext.Set<ForumQuestion>().FirstOrDefaultAsync(x => x.Id == answer.QuestionId);
            if (question == null)
            {
                throw new ForumNotFoundException("Question not found");
            }

            var answers = await _dbContext.Set<ForumAnswer>().Where(x => x.QuestionId == answer.QuestionId).ToListAsync();
            foreach (var item in answers)
            {
                item.IsAccepted = item.Id == answerId;
            }

            question.AcceptedAnswerId = answerId;
            question.Status = ForumQuestionStatuses.Answered;
            question.LastActivityAt = DateTimeOffset.UtcNow;
            await AddNotificationAsync(answer.UserId, "accepted", "Your answer was accepted.", question.Id, answer.Id, question.UserId);
            await _dbContext.SaveChangesAsync();

            return new { message = "Accepted", answerId };
        }

        public async Task<ForumCommentVm> GetCommentAsync(long id)
        {
            var comment = await _dbContext.Set<ForumComment>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (comment == null)
            {
                throw new ForumNotFoundException("Comment not found");
            }

            var userMap = await GetUserMapAsync(new[] { comment.UserId });
            return MapComment(comment, userMap);
        }

        public async Task<object> GetQuestionCommentsAsync(long questionId)
        {
            var comments = await _dbContext.Set<ForumComment>()
                .Where(x => x.QuestionId == questionId)
                .OrderByDescending(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var userMap = await GetUserMapAsync(comments.Select(x => x.UserId));
            return new { questionId, count = comments.Count, comments = comments.Select(x => MapComment(x, userMap)).ToList() };
        }

        public async Task<object> GetAnswerCommentsAsync(long answerId)
        {
            var comments = await _dbContext.Set<ForumComment>()
                .Where(x => x.AnswerId == answerId)
                .OrderByDescending(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var userMap = await GetUserMapAsync(comments.Select(x => x.UserId));
            return new { answerId, count = comments.Count, comments = comments.Select(x => MapComment(x, userMap)).ToList() };
        }

        public async Task<ForumCommentVm> CreateCommentAsync(ForumCommentCreateRequest request, long userId)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Body) || (request.QuestionId.HasValue == request.AnswerId.HasValue))
            {
                throw new ArgumentException("body and exactly one of questionId or answerId required");
            }

            await EnsureUserExistsAsync(userId);
            await EnsureForumProfileAsync(userId);

            ForumQuestion question = null;
            ForumAnswer answer = null;
            if (request.QuestionId.HasValue)
            {
                question = await _dbContext.Set<ForumQuestion>().FirstOrDefaultAsync(x => x.Id == request.QuestionId.Value);
                if (question == null)
                {
                    throw new ForumNotFoundException("Question not found");
                }
            }
            else
            {
                answer = await _dbContext.Set<ForumAnswer>().FirstOrDefaultAsync(x => x.Id == request.AnswerId.Value);
                if (answer == null)
                {
                    throw new ForumNotFoundException("Answer not found");
                }

                question = await _dbContext.Set<ForumQuestion>().FirstOrDefaultAsync(x => x.Id == answer.QuestionId);
            }

            var comment = new ForumComment
            {
                UserId = userId,
                QuestionId = request.QuestionId,
                AnswerId = request.AnswerId,
                Body = request.Body.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Set<ForumComment>().Add(comment);
            if (question != null)
            {
                question.LastActivityAt = DateTimeOffset.UtcNow;
                await AddNotificationAsync(question.UserId, "comment", "A new comment was added.", question.Id, answer?.Id, userId);
            }

            await _dbContext.SaveChangesAsync();
            var userMap = await GetUserMapAsync(new[] { userId });
            return MapComment(comment, userMap);
        }

        public async Task<object> CastVoteAsync(ForumVoteRequest request, long userId)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TargetType) || request.TargetId <= 0 || (request.Value != 1 && request.Value != -1))
            {
                throw new ArgumentException("targetType, targetId, and value (1 or -1) required");
            }

            var targetType = request.TargetType.Trim().ToLowerInvariant();
            if (!IsValidTargetType(targetType))
            {
                throw new ArgumentException("targetType must be question, answer, or comment");
            }

            var existing = await _dbContext.Set<ForumVote>().AnyAsync(x => x.UserId == userId && x.TargetType == targetType && x.TargetId == request.TargetId);
            if (existing)
            {
                throw new ForumConflictException("Already voted");
            }

            await ApplyVoteToTargetAsync(targetType, request.TargetId, request.Value);
            var vote = new ForumVote
            {
                UserId = userId,
                TargetType = targetType,
                TargetId = request.TargetId,
                Value = request.Value,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Set<ForumVote>().Add(vote);
            await _dbContext.SaveChangesAsync();
            return new { voteId = vote.Id, userId, targetType, targetId = request.TargetId, value = request.Value };
        }

        public async Task<IList<object>> GetBookmarksAsync(long userId)
        {
            var rows = await _dbContext.Set<ForumBookmark>()
                .Include(x => x.Question)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return rows.Select(x => (object)new
            {
                x.Id,
                x.UserId,
                x.QuestionId,
                x.CreatedAt,
                title = x.Question.Title,
                votes = x.Question.Votes,
                answersCount = x.Question.AnswersCount,
                status = x.Question.Status
            }).ToList();
        }

        public async Task<object> AddBookmarkAsync(ForumBookmarkRequest request, long userId)
        {
            if (request == null || request.QuestionId <= 0)
            {
                throw new ArgumentException("questionId required");
            }

            var question = await _dbContext.Set<ForumQuestion>().FirstOrDefaultAsync(x => x.Id == request.QuestionId);
            if (question == null)
            {
                throw new ForumNotFoundException("Question not found");
            }

            var exists = await _dbContext.Set<ForumBookmark>().AnyAsync(x => x.UserId == userId && x.QuestionId == request.QuestionId);
            if (exists)
            {
                throw new ForumConflictException("Already bookmarked");
            }

            _dbContext.Set<ForumBookmark>().Add(new ForumBookmark
            {
                UserId = userId,
                QuestionId = request.QuestionId,
                CreatedAt = DateTimeOffset.UtcNow
            });
            question.Favorites++;
            await _dbContext.SaveChangesAsync();

            return new { userId, questionId = request.QuestionId };
        }

        public async Task RemoveBookmarkAsync(long userId, long questionId)
        {
            var bookmark = await _dbContext.Set<ForumBookmark>().FirstOrDefaultAsync(x => x.UserId == userId && x.QuestionId == questionId);
            if (bookmark != null)
            {
                _dbContext.Set<ForumBookmark>().Remove(bookmark);
            }

            var question = await _dbContext.Set<ForumQuestion>().FirstOrDefaultAsync(x => x.Id == questionId);
            if (question != null && question.Favorites > 0)
            {
                question.Favorites--;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<ForumMediaVm> AddMediaAsync(ForumMediaRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Url) || (request.QuestionId.HasValue == request.AnswerId.HasValue))
            {
                throw new ArgumentException("type, url, and exactly one of questionId or answerId required");
            }

            var type = request.Type.Trim().ToLowerInvariant();
            if (type != "image" && type != "video" && type != "attachment")
            {
                throw new ArgumentException("type must be image, video, or attachment");
            }

            if (request.QuestionId.HasValue && !await _dbContext.Set<ForumQuestion>().AnyAsync(x => x.Id == request.QuestionId.Value))
            {
                throw new ForumNotFoundException("Question not found");
            }

            if (request.AnswerId.HasValue && !await _dbContext.Set<ForumAnswer>().AnyAsync(x => x.Id == request.AnswerId.Value))
            {
                throw new ForumNotFoundException("Answer not found");
            }

            var media = new ForumMedia
            {
                QuestionId = request.QuestionId,
                AnswerId = request.AnswerId,
                Type = type,
                Url = request.Url.Trim(),
                Thumbnail = request.Thumbnail,
                AltText = request.AltText,
                Width = request.Width,
                Height = request.Height,
                Duration = request.Duration,
                Platform = request.Platform,
                FileSize = request.FileSize,
                MimeType = request.MimeType,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Set<ForumMedia>().Add(media);
            await _dbContext.SaveChangesAsync();
            return MapMedia(media);
        }

        public async Task<ForumPagedEnvelope<object>> SearchAsync(ForumSearchRequest request)
        {
            request ??= new ForumSearchRequest();
            if (string.IsNullOrWhiteSpace(request.Q))
            {
                var suggestions = await _dbContext.Set<ForumSearchSuggestion>()
                    .OrderByDescending(x => x.Weight)
                    .Take(50)
                    .AsNoTracking()
                    .Select(x => (object)new { x.Term })
                    .ToListAsync();

                return Paged(suggestions, 1, 50, suggestions.Count);
            }

            var search = request.Q.Trim();
            var query = _dbContext.Set<ForumQuestion>()
                .Include(x => x.QuestionTags).ThenInclude(x => x.Tag)
                .Where(x => x.Title.Contains(search) || x.Body.Contains(search))
                .AsNoTracking();

            var total = await query.CountAsync();
            var rows = await query
                .OrderByDescending(x => x.Votes)
                .Skip((NormalizePage(request.Page) - 1) * NormalizeLimit(request.Limit))
                .Take(NormalizeLimit(request.Limit))
                .ToListAsync();

            var userMap = await GetUserMapAsync(rows.Select(x => x.UserId));
            var data = rows.Select(x => (object)new
            {
                id = x.Id,
                title = x.Title,
                votes = x.Votes,
                answersCount = x.AnswersCount,
                views = x.Views,
                status = x.Status,
                createdAt = x.CreatedAt,
                displayName = userMap.TryGetValue(x.UserId, out var user) ? user.DisplayName : null,
                avatar = userMap.TryGetValue(x.UserId, out var userForAvatar) ? userForAvatar.Avatar : null,
                tags = x.QuestionTags.Select(t => t.Tag.Name).ToList()
            }).ToList();

            return Paged(data, request.Page, request.Limit, total);
        }

        public async Task<ForumPagedEnvelope<ForumUserSummaryVm>> GetUsersAsync(ForumUserListRequest request)
        {
            request ??= new ForumUserListRequest();
            var users = await _dbContext.Users.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync();
            var profiles = await _dbContext.Set<ForumUserProfile>().AsNoTracking().ToListAsync();
            var profileMap = profiles.ToDictionary(x => x.UserId);
            var mapped = users.Select(x => MapUser(x, profileMap.TryGetValue(x.Id, out var profile) ? profile : null)).ToList();

            mapped = (request.Sort ?? string.Empty).ToLowerInvariant() switch
            {
                "joined" => mapped.OrderByDescending(x => x.JoinedAt).ToList(),
                "name" => mapped.OrderBy(x => x.DisplayName).ToList(),
                _ => mapped.OrderByDescending(x => x.Reputation).ToList()
            };

            var total = mapped.Count;
            var data = mapped.Skip((NormalizePage(request.Page) - 1) * NormalizeLimit(request.Limit)).Take(NormalizeLimit(request.Limit)).ToList();
            return Paged(data, request.Page, request.Limit, total);
        }

        public async Task<ForumUserSummaryVm> GetUserAsync(long id)
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (user == null)
            {
                throw new ForumNotFoundException("User not found");
            }

            var profile = await _dbContext.Set<ForumUserProfile>().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id);
            return MapUser(user, profile);
        }

        public async Task<ForumPagedEnvelope<ForumTagVm>> GetTagsAsync(string q)
        {
            var query = _dbContext.Set<ForumTag>().AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x => x.Name.Contains(search));
            }

            var tags = await query.OrderByDescending(x => x.QuestionsCount).Take(50).ToListAsync();
            return Paged(tags.Select(MapTag).ToList(), 1, 50, tags.Count);
        }

        public async Task<IList<object>> GetNotificationsAsync(long userId)
        {
            var notifications = await _dbContext.Set<ForumNotification>()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return notifications.Select(x => (object)new
            {
                x.Id,
                x.UserId,
                x.Type,
                x.Message,
                x.QuestionId,
                x.AnswerId,
                x.IsRead,
                x.CreatedAt
            }).ToList();
        }

        public async Task<object> MarkNotificationReadAsync(long id)
        {
            var notification = await _dbContext.Set<ForumNotification>().FirstOrDefaultAsync(x => x.Id == id);
            if (notification == null)
            {
                throw new ForumNotFoundException("Notification not found");
            }

            notification.IsRead = true;
            await _dbContext.SaveChangesAsync();
            return new { message = "Read" };
        }

        public async Task<object> GetStatsAsync()
        {
            return new
            {
                totalQuestions = await _dbContext.Set<ForumQuestion>().CountAsync(),
                totalAnswers = await _dbContext.Set<ForumAnswer>().CountAsync(),
                totalUsers = await _dbContext.Users.CountAsync(x => !x.IsDeleted),
                totalTags = await _dbContext.Set<ForumTag>().CountAsync(),
                totalComments = await _dbContext.Set<ForumComment>().CountAsync(),
                activeUsers = await _dbContext.Set<ForumUserProfile>().CountAsync(x => x.IsOnline)
            };
        }

        public async Task<object> HealthCheckAsync()
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            return new { status = canConnect ? "healthy" : "unhealthy", db = _dbContext.Database.ProviderName };
        }

        private async Task EnsureUserExistsAsync(long userId)
        {
            if (!await _dbContext.Users.AnyAsync(x => x.Id == userId && !x.IsDeleted))
            {
                throw new ForumNotFoundException("User not found");
            }
        }

        private async Task EnsureForumProfileAsync(long userId)
        {
            if (await _dbContext.Set<ForumUserProfile>().AnyAsync(x => x.UserId == userId))
            {
                return;
            }

            _dbContext.Set<ForumUserProfile>().Add(new ForumUserProfile
            {
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow,
                LastSeen = DateTimeOffset.UtcNow
            });
        }

        private async Task AddNotificationAsync(long userId, string type, string message, long? questionId, long? answerId, long actorUserId)
        {
            if (userId == actorUserId)
            {
                return;
            }

            _dbContext.Set<ForumNotification>().Add(new ForumNotification
            {
                UserId = userId,
                Type = type,
                Message = message,
                QuestionId = questionId,
                AnswerId = answerId,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await Task.CompletedTask;
        }

        private async Task AddSearchSuggestionAsync(string title)
        {
            var words = (title ?? string.Empty)
                .Split(new[] { ' ', ',', '.', '?', '!', ':', ';', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().ToLowerInvariant())
                .Where(x => x.Length > 2)
                .Distinct()
                .Take(8)
                .ToList();

            foreach (var word in words)
            {
                var suggestion = await _dbContext.Set<ForumSearchSuggestion>().FirstOrDefaultAsync(x => x.Term == word);
                if (suggestion == null)
                {
                    _dbContext.Set<ForumSearchSuggestion>().Add(new ForumSearchSuggestion { Term = word, Weight = 1 });
                }
                else
                {
                    suggestion.Weight++;
                }
            }
        }

        private async Task ApplyVoteToTargetAsync(string targetType, long targetId, int value)
        {
            if (targetType == ForumTargetTypes.Question)
            {
                var question = await _dbContext.Set<ForumQuestion>().FirstOrDefaultAsync(x => x.Id == targetId);
                if (question == null)
                {
                    throw new ForumNotFoundException("Question not found");
                }

                question.Votes += value;
                return;
            }

            if (targetType == ForumTargetTypes.Answer)
            {
                var answer = await _dbContext.Set<ForumAnswer>().FirstOrDefaultAsync(x => x.Id == targetId);
                if (answer == null)
                {
                    throw new ForumNotFoundException("Answer not found");
                }

                answer.Votes += value;
                return;
            }

            var comment = await _dbContext.Set<ForumComment>().FirstOrDefaultAsync(x => x.Id == targetId);
            if (comment == null)
            {
                throw new ForumNotFoundException("Comment not found");
            }

            comment.Votes += value;
        }

        private async Task<Dictionary<long, ForumUserSummaryVm>> GetUserMapAsync(IEnumerable<long> userIds)
        {
            var ids = userIds.Where(x => x > 0).Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<long, ForumUserSummaryVm>();
            }

            var users = await _dbContext.Users.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync();
            var profiles = await _dbContext.Set<ForumUserProfile>().AsNoTracking().Where(x => ids.Contains(x.UserId)).ToListAsync();
            var profileMap = profiles.ToDictionary(x => x.UserId);
            return users.ToDictionary(x => x.Id, x => MapUser(x, profileMap.TryGetValue(x.Id, out var profile) ? profile : null));
        }

        private static ForumUserSummaryVm MapUser(User user, ForumUserProfile profile)
        {
            return new ForumUserSummaryVm
            {
                Id = user.Id,
                Username = user.UserName,
                DisplayName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName,
                Email = user.Email,
                Avatar = profile?.Avatar,
                Bio = profile?.Bio,
                Reputation = profile?.Reputation ?? 0,
                GoldBadges = profile?.GoldBadges ?? 0,
                SilverBadges = profile?.SilverBadges ?? 0,
                BronzeBadges = profile?.BronzeBadges ?? 0,
                Location = profile?.Location,
                Website = profile?.Website,
                IsOnline = profile?.IsOnline ?? false,
                JoinedAt = profile?.JoinedAt ?? user.CreatedOn,
                LastSeen = profile?.LastSeen ?? user.LatestUpdatedOn
            };
        }

        private static ForumQuestionListItemVm MapQuestionListItem(ForumQuestion question, IDictionary<long, ForumUserSummaryVm> userMap)
        {
            return new ForumQuestionListItemVm
            {
                Id = question.Id,
                Title = question.Title,
                Body = question.Body,
                UserId = question.UserId,
                Votes = question.Votes,
                Views = question.Views,
                AnswersCount = question.AnswersCount,
                AcceptedAnswerId = question.AcceptedAnswerId,
                Status = question.Status,
                IsBounty = question.IsBounty,
                BountyAmount = question.BountyAmount,
                Favorites = question.Favorites,
                IsProtected = question.IsProtected,
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt,
                LastActivityAt = question.LastActivityAt,
                Tags = question.QuestionTags.Select(x => x.Tag.Name).ToList(),
                Media = question.Media.Select(MapMedia).ToList(),
                User = userMap.TryGetValue(question.UserId, out var user) ? user : null
            };
        }

        private static ForumQuestionDetailVm MapQuestionDetail(ForumQuestion question, IDictionary<long, ForumUserSummaryVm> userMap)
        {
            var item = MapQuestionListItem(question, userMap);
            return new ForumQuestionDetailVm
            {
                Id = item.Id,
                Title = item.Title,
                Body = item.Body,
                UserId = item.UserId,
                Votes = item.Votes,
                Views = item.Views,
                AnswersCount = item.AnswersCount,
                AcceptedAnswerId = item.AcceptedAnswerId,
                Status = item.Status,
                IsBounty = item.IsBounty,
                BountyAmount = item.BountyAmount,
                Favorites = item.Favorites,
                IsProtected = item.IsProtected,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                LastActivityAt = item.LastActivityAt,
                Tags = item.Tags,
                Media = item.Media,
                User = item.User
            };
        }

        private static ForumAnswerVm MapAnswer(ForumAnswer answer, IDictionary<long, ForumUserSummaryVm> userMap)
        {
            return new ForumAnswerVm
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                Body = answer.Body,
                Votes = answer.Votes,
                IsAccepted = answer.IsAccepted,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt,
                Comments = answer.Comments.Select(x => MapComment(x, userMap)).ToList(),
                Media = answer.Media.Select(MapMedia).ToList(),
                User = userMap.TryGetValue(answer.UserId, out var user) ? user : null
            };
        }

        private static ForumCommentVm MapComment(ForumComment comment, IDictionary<long, ForumUserSummaryVm> userMap)
        {
            return new ForumCommentVm
            {
                Id = comment.Id,
                UserId = comment.UserId,
                QuestionId = comment.QuestionId,
                AnswerId = comment.AnswerId,
                Body = comment.Body,
                Votes = comment.Votes,
                CreatedAt = comment.CreatedAt,
                User = userMap.TryGetValue(comment.UserId, out var user) ? user : null
            };
        }

        private static ForumMediaVm MapMedia(ForumMedia media)
        {
            return new ForumMediaVm
            {
                Id = media.Id,
                QuestionId = media.QuestionId,
                AnswerId = media.AnswerId,
                Type = media.Type,
                Url = media.Url,
                Thumbnail = media.Thumbnail,
                AltText = media.AltText,
                Width = media.Width,
                Height = media.Height,
                Duration = media.Duration,
                Platform = media.Platform,
                FileSize = media.FileSize,
                MimeType = media.MimeType,
                CreatedAt = media.CreatedAt
            };
        }

        private static ForumTagVm MapTag(ForumTag tag)
        {
            return new ForumTagVm
            {
                Id = tag.Id,
                Name = tag.Name,
                Description = tag.Description,
                Color = tag.Color,
                QuestionsCount = tag.QuestionsCount
            };
        }

        private static IOrderedQueryable<ForumQuestion> ApplyQuestionSort(IQueryable<ForumQuestion> query, string sort, string order)
        {
            var asc = string.Equals(order, "ASC", StringComparison.OrdinalIgnoreCase);
            return (sort ?? string.Empty).ToLowerInvariant() switch
            {
                "votes" => asc ? query.OrderBy(x => x.Votes) : query.OrderByDescending(x => x.Votes),
                "views" => asc ? query.OrderBy(x => x.Views) : query.OrderByDescending(x => x.Views),
                "activity" => asc ? query.OrderBy(x => x.LastActivityAt) : query.OrderByDescending(x => x.LastActivityAt),
                "answers" => asc ? query.OrderBy(x => x.AnswersCount) : query.OrderByDescending(x => x.AnswersCount),
                _ => asc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt)
            };
        }

        private static IOrderedQueryable<ForumAnswer> ApplyAnswerSort(IQueryable<ForumAnswer> query, string sort, string order)
        {
            var asc = string.Equals(order, "ASC", StringComparison.OrdinalIgnoreCase);
            var ordered = query.OrderByDescending(x => x.IsAccepted);
            return string.Equals(sort, "created", StringComparison.OrdinalIgnoreCase)
                ? asc ? ordered.ThenBy(x => x.CreatedAt) : ordered.ThenByDescending(x => x.CreatedAt)
                : asc ? ordered.ThenBy(x => x.Votes) : ordered.ThenByDescending(x => x.Votes);
        }

        private static ForumPagedEnvelope<T> Paged<T>(IList<T> items, int page, int limit, int total)
        {
            var safePage = NormalizePage(page);
            var safeLimit = NormalizeLimit(limit);
            return new ForumPagedEnvelope<T>
            {
                Data = items,
                Pagination = new ForumPagination
                {
                    Page = safePage,
                    Limit = safeLimit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling(total / (double)safeLimit),
                    HasMore = safePage * safeLimit < total
                }
            };
        }

        private static int NormalizePage(int page)
        {
            return page <= 0 ? 1 : page;
        }

        private static int NormalizeLimit(int limit)
        {
            if (limit <= 0)
            {
                return 15;
            }

            return Math.Min(limit, 100);
        }

        private static IList<string> NormalizeTagNames(IEnumerable<string> tags)
        {
            return (tags ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().TrimStart('#').ToLowerInvariant())
                .Where(x => x.Length > 0)
                .Distinct()
                .Take(10)
                .ToList();
        }

        private static bool IsValidStatus(string status)
        {
            return status == ForumQuestionStatuses.Open
                || status == ForumQuestionStatuses.Answered
                || status == ForumQuestionStatuses.Closed
                || status == ForumQuestionStatuses.Discussion;
        }

        private static bool IsValidTargetType(string targetType)
        {
            return targetType == ForumTargetTypes.Question
                || targetType == ForumTargetTypes.Answer
                || targetType == ForumTargetTypes.Comment;
        }
    }
}
