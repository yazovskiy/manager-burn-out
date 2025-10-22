using System.Security.Cryptography;
using System.Text;
using CallWellbeing.Core.Abstractions;

namespace CallWellbeing.Infra.Security;

public sealed class CallIdHasher : ICallIdHasher
{
  public string Hash(string input)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(input);

    using var sha = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(input);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToHexString(hash);
  }
}
