using ChatApp.Application.DTOs;
using ChatApp.Domain.Interfaces;
using MediatR;
namespace ChatApp.Application.Queries;

public class GetKeyBundleHandler(IUnitOfWork uow) : IRequestHandler<GetKeyBundleQuery, KeyBundleDto>
{
    public async Task<KeyBundleDto> Handle(GetKeyBundleQuery q, CancellationToken ct)
    {
        var bundle = await uow.KeyBundles.GetByUserIdAsync(q.TargetUserId, ct)
            ?? throw new KeyNotFoundException("Key bundle not found for user.");

        var otpk = await uow.KeyBundles.ClaimOneTimePreKeyAsync(q.TargetUserId, ct);

        return new KeyBundleDto(
            q.TargetUserId, bundle.IdentityKey,
            bundle.SignedPreKeyId, bundle.SignedPreKey, bundle.SignedPreKeySig,
            otpk == null ? null : new OneTimePreKeyDto(otpk.KeyId, otpk.PublicKey));
    }
}
