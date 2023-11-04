namespace MyBGList.Abstractions;

public interface IRequestValidator<T> where T : class
{
    bool isValid(T request);

    ValidationResult Validate(T request) {
        throw new NotImplementedException();
    }
}


public class ValidationResult 
{
    private bool _success;
    private Dictionary<string, string>? _erroMessage;    
    public bool Success => _success;
    public Dictionary<string, string>? ErrorMessage => _erroMessage;
    public ValidationResult(bool success)
    {
        _success = success;
    }


    public static ValidationResult Succeeded => new ValidationResult(true);
}