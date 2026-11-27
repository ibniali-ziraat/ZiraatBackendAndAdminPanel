using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MediatR;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Core.Models;
using ECommerce.Infrastructure;
using System;

namespace ECommerce.Module.Core.Services;

public interface IProfileService
{
    Task<Profile> GetByIdAsync(long id);
    Task<Profile> GetByUserIdAsync(long userId);
    Task<IEnumerable<Profile>> GetAllAsync();
    Task<Result<long>> CreateAsync(UserProfile model, long userId);
    Task UpdateAsync(long id, UserProfile model);
    Task DeleteAsync(long id);
}

public class ProfileService : IProfileService
{
    private readonly IRepository<Profile> _profileRepository;
    private readonly IMediator _mediator;

    public ProfileService(IRepository<Profile> profileRepository, IMediator mediator)
    {
        _profileRepository = profileRepository;
        _mediator = mediator;
    }

    public async Task<Profile> GetByIdAsync(long id)
    {
        return await _profileRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<Profile> GetByUserIdAsync(long userId)
    {
        return await _profileRepository.Query()
            .FirstOrDefaultAsync(p => p.CoreUserId == userId)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Profile>> GetAllAsync()
    {
        return await _profileRepository.Query()
            .OrderByDescending(p => p.CreatedOn)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a user profile. Password is NOT stored in Profile table.
    /// Password is handled by UserManager.CreateAsync() in UserProfileController.
    /// </summary>
    public async Task<Result<long>> CreateAsync(UserProfile model, long userId)
    {
        if (model == null)
        {
            return Result.Fail<long>("Model cannot be null");
        }

        var entity = new Profile
        {
            CoreUserId = userId,
            FullName = model.Fullname,
            Email = model.Email,
            Phone = model.Phone,
            Age = model.Age,
            Gender = model.Gender,
            ProfilePic = model.Profilepic,
            CreatedOn = DateTime.UtcNow
        };

        // Password is managed by UserManager and stored in User.PasswordHash
        // Profile table only stores non-sensitive profile data

        _profileRepository.Add(entity);
        await _profileRepository.SaveChangesAsync().ConfigureAwait(false);

        if (_mediator != null)
        {
            await _mediator.Publish(new UserSignedIn { UserId = userId }).ConfigureAwait(false);
        }

        return Result.Ok(userId);
    }

    public async Task UpdateAsync(long id, UserProfile model)
    {
        var entity = await _profileRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);

        if (entity == null)
            return;

        entity.FullName = model.Fullname;
        entity.Email = model.Email;
        entity.Phone = model.Phone;
        entity.Age = model.Age;
        entity.Gender = model.Gender;
        entity.ProfilePic = model.Profilepic;

        // Password NOT updated here - use ChangePassword endpoint in UserProfileController
        // ChangePassword uses UserManager.ChangePasswordAsync()

        await _profileRepository.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _profileRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);

        if (entity == null)
            return;

        _profileRepository.Remove(entity);
        await _profileRepository.SaveChangesAsync().ConfigureAwait(false);
    }
}
