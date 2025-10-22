namespace CallWellbeing.Core.Abstractions;

public interface ICallIdHasher
{
  string Hash(string input);
}
