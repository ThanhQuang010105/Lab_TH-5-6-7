namespace QTKD.Domain.Exceptions;

public class BusinessRuleException : Exception
{
    public string Code { get; }
    public IReadOnlyList<string> Details { get; }

    public BusinessRuleException(string code, string message, IEnumerable<string>? details = null)
        : base(message)
    {
        Code = code;
        Details = details?.ToList() ?? new List<string>();
    }
}
