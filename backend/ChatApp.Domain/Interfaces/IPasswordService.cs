using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
namespace ChatApp.Domain.Interfaces;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
