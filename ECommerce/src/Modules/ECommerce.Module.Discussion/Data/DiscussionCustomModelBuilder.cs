using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Discussion.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Discussion.Data
{
    public class DiscussionCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ForumUserProfile>(b =>
            {
                b.ToTable("Forum_UserProfile");
                b.HasIndex(x => x.UserId).IsUnique();
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumTag>(b =>
            {
                b.ToTable("Forum_Tag");
                b.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<ForumQuestion>(b =>
            {
                b.ToTable("Forum_Question");
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => x.Status);
                b.HasIndex(x => x.Votes);
                b.HasIndex(x => x.CreatedAt);
                b.HasIndex(x => x.LastActivityAt);
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumQuestionTag>(b =>
            {
                b.ToTable("Forum_QuestionTag");
                b.HasKey(x => new { x.QuestionId, x.TagId });
                b.HasOne(x => x.Question).WithMany(x => x.QuestionTags).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Tag).WithMany(x => x.QuestionTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumAnswer>(b =>
            {
                b.ToTable("Forum_Answer");
                b.HasIndex(x => x.QuestionId);
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => x.Votes);
                b.HasOne(x => x.Question).WithMany(x => x.Answers).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumComment>(b =>
            {
                b.ToTable("Forum_Comment");
                b.HasIndex(x => x.QuestionId);
                b.HasIndex(x => x.AnswerId);
                b.HasOne(x => x.Question).WithMany(x => x.Comments).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Answer).WithMany(x => x.Comments).HasForeignKey(x => x.AnswerId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumVote>(b =>
            {
                b.ToTable("Forum_Vote");
                b.HasIndex(x => new { x.UserId, x.TargetType, x.TargetId }).IsUnique();
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumMedia>(b =>
            {
                b.ToTable("Forum_Media");
                b.HasIndex(x => x.QuestionId);
                b.HasIndex(x => x.AnswerId);
                b.HasOne(x => x.Question).WithMany(x => x.Media).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Answer).WithMany(x => x.Media).HasForeignKey(x => x.AnswerId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumBookmark>(b =>
            {
                b.ToTable("Forum_Bookmark");
                b.HasIndex(x => new { x.UserId, x.QuestionId }).IsUnique();
                b.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumNotification>(b =>
            {
                b.ToTable("Forum_Notification");
                b.HasIndex(x => new { x.UserId, x.IsRead });
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumLinkedQuestion>(b =>
            {
                b.ToTable("Forum_LinkedQuestion");
                b.HasKey(x => new { x.QuestionId, x.LinkedQuestionId });
                b.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.LinkedQuestion).WithMany().HasForeignKey(x => x.LinkedQuestionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumUserTag>(b =>
            {
                b.ToTable("Forum_UserTag");
                b.HasKey(x => new { x.UserId, x.TagId });
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ForumSearchSuggestion>(b =>
            {
                b.ToTable("Forum_SearchSuggestion");
                b.HasIndex(x => x.Term).IsUnique();
            });

            modelBuilder.Entity<ForumQuestionView>(b =>
            {
                b.ToTable("Forum_QuestionView");
                b.HasIndex(x => x.QuestionId);
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => new { x.QuestionId, x.UserId }).IsUnique();
                b.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
