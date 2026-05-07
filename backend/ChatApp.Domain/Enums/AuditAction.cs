namespace ChatApp.Domain.Enums;

public enum AuditAction
{
	UserRegistered, UserLoggedIn, UserLoggedOut, UserDeleted,
	PasswordChanged, TwoFactorEnabled, TwoFactorDisabled,
	MessageSent, MessageDeleted, MessageEdited,
	GroupCreated, GroupDeleted, MemberAdded, MemberRemoved, MemberRoleChanged,
	FileUploaded, CallInitiated, CallEnded,
	ContactBlocked, ContactUnblocked,
	TokenRevoked, SuspiciousActivity
}
