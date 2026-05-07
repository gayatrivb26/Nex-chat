using System.Security.Claims;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ChatApp.API.Authorization;

public sealed class GroupRoleRequirement(MemberRole minimumRole) : IAuthorizationRequirement
{
    public MemberRole MinimumRole { get; } = minimumRole;
}

public sealed class MessageSenderRequirement : IAuthorizationRequirement;
public sealed class NotBlockedRequirement : IAuthorizationRequirement;
public sealed class ConversationAccessRequirement : IAuthorizationRequirement;

public sealed class GroupRoleAuthorizationHandler(
    IUnitOfWork uow,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<GroupRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GroupRoleRequirement requirement)
    {
        var userId = AuthorizationContextReader.GetUserId(context.User);
        var conversationId = AuthorizationContextReader.TryGetGuid(httpContextAccessor.HttpContext, "conversationId");
        if (userId == null || conversationId == null) return;

        var member = await uow.Conversations.GetMemberAsync(conversationId.Value, userId.Value);
        if (member == null) return;

        if (member.Role == MemberRole.Owner ||
            (requirement.MinimumRole == MemberRole.Admin && member.Role == MemberRole.Admin))
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class MessageSenderAuthorizationHandler(
    IUnitOfWork uow,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<MessageSenderRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MessageSenderRequirement requirement)
    {
        var userId = AuthorizationContextReader.GetUserId(context.User);
        var messageId = AuthorizationContextReader.TryGetGuid(httpContextAccessor.HttpContext, "messageId");
        if (userId == null || messageId == null) return;

        var message = await uow.Messages.GetByIdAsync(messageId.Value);
        if (message?.SenderId == userId)
            context.Succeed(requirement);
    }
}

public sealed class NotBlockedAuthorizationHandler(
    IUnitOfWork uow,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<NotBlockedRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NotBlockedRequirement requirement)
    {
        var currentUserId = AuthorizationContextReader.GetUserId(context.User);
        var targetUserId = AuthorizationContextReader.TryGetGuid(httpContextAccessor.HttpContext, "userId")
            ?? AuthorizationContextReader.TryGetGuid(httpContextAccessor.HttpContext, "contactUserId")
            ?? AuthorizationContextReader.TryGetGuid(httpContextAccessor.HttpContext, "otherUserId");

        if (currentUserId == null || targetUserId == null) return;

        var blocked = await uow.UserContacts.IsBlockedAsync(currentUserId.Value, targetUserId.Value)
            || await uow.UserContacts.IsBlockedAsync(targetUserId.Value, currentUserId.Value);

        if (!blocked)
            context.Succeed(requirement);
    }
}

public sealed class ConversationAccessAuthorizationHandler(
    IUnitOfWork uow,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<ConversationAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ConversationAccessRequirement requirement)
    {
        var userId = AuthorizationContextReader.GetUserId(context.User);
        var conversationId = AuthorizationContextReader.TryGetGuid(httpContextAccessor.HttpContext, "conversationId");
        if (userId == null || conversationId == null) return;

        if (await uow.Conversations.IsUserMemberAsync(conversationId.Value, userId.Value))
            context.Succeed(requirement);
    }
}

internal static class AuthorizationContextReader
{
    public static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static Guid? TryGetGuid(HttpContext? context, string key)
    {
        if (context == null) return null;
        if (context.Request.RouteValues.TryGetValue(key, out var value) &&
            Guid.TryParse(value?.ToString(), out var parsed))
            return parsed;
        if (context.Request.Query.TryGetValue(key, out var queryValue) &&
            Guid.TryParse(queryValue.ToString(), out parsed))
            return parsed;
        return null;
    }
}
