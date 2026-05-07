using ChatApp.API.Extensions;
using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

[ApiController]
[Authorize]
[Authorize(Policy = "IsVerified")]
[Route("api/v1/contacts")]
public class ContactsController(IUnitOfWork uow) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ContactDto>>>> GetContacts(CancellationToken ct)
    {
        var contacts = await uow.UserContacts.GetUserContactsAsync(User.GetUserId(), ct: ct);
        return Ok(ApiResponse<IEnumerable<ContactDto>>.Ok(contacts.Select(Map)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ContactDto>>> AddContact(AddContactRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var existing = await uow.UserContacts.GetContactAsync(userId, request.ContactUserId, ct);
        if (existing != null) return Ok(ApiResponse<ContactDto>.Ok(Map(existing)));

        var contact = UserContact.Create(userId, request.ContactUserId, request.Nickname);
        await uow.UserContacts.AddAsync(contact, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<ContactDto>.Ok(Map(contact)));
    }

    [HttpGet("blocked")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ContactDto>>>> GetBlocked(CancellationToken ct)
    {
        var contacts = await uow.UserContacts.GetBlockedContactsAsync(User.GetUserId(), ct);
        return Ok(ApiResponse<IEnumerable<ContactDto>>.Ok(contacts.Select(Map)));
    }

    [HttpPost("{userId:guid}/block")]
    public async Task<ActionResult<ApiResponse<object>>> Block(Guid userId, CancellationToken ct)
    {
        var currentUserId = User.GetUserId();
        var contact = await uow.UserContacts.GetContactAsync(currentUserId, userId, ct)
            ?? UserContact.Create(currentUserId, userId);
        contact.Block();
        if (await uow.UserContacts.GetContactAsync(currentUserId, userId, ct) == null)
            await uow.UserContacts.AddAsync(contact, ct);
        else
            uow.UserContacts.Update(contact);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("{userId:guid}/unblock")]
    public async Task<ActionResult<ApiResponse<object>>> Unblock(Guid userId, CancellationToken ct)
    {
        var contact = await uow.UserContacts.GetContactAsync(User.GetUserId(), userId, ct)
            ?? throw new KeyNotFoundException("Contact not found.");
        contact.Unblock();
        uow.UserContacts.Update(contact);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private static ContactDto Map(UserContact contact)
        => new(contact.Id, contact.ContactUserId, contact.Nickname,
            contact.ContactUser == null ? null : new UserProfileDto(
                contact.ContactUser.Id,
                contact.ContactUser.Username,
                contact.ContactUser.AvatarUrl,
                contact.ContactUser.DisplayName,
                contact.ContactUser.Bio,
                contact.ContactUser.Status.ToString(),
                contact.ContactUser.LastSeen,
                contact.ContactUser.IsVerified),
            contact.IsBlocked,
            contact.CreatedAt);
}
