using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
namespace CleanArchitecture.Application.Common.Models;

public class OperationResult
{
    private const string SuccessMessage = "عملیات با موفقیت انجام شد";
    private const string FailedMessage = "عملیات انجام نشد";

    [JsonProperty("status")]
    public int Status { get; set; }
    public string Message { get; set; }
    public bool IsSucceded { get; set; }
    public OperationResult()
    {
        IsSucceded = false;
        Message = "عملیات با موفقیت انجام نشد";
    }
    public OperationResult(OperationResult op)
    {
        this.Status = op.Status;
        this.Message = op.Message;
        this.IsSucceded = op.IsSucceded;
    }
    public OperationResult succedded(string message = SuccessMessage)
    {
        Status = 200;
        IsSucceded = true;
        Message = message;
        return this;
    }
    public OperationResult Failed(string message)
    {
        Status = 201;
        IsSucceded = false;
        Message = message;
        return this;
    }
    public OperationResult BadRequest(string message)
    {
        Status = 400;
        IsSucceded = false;
        Message = message;
        return this;
    }
    public OperationResult NotFound(string message)
    {
        Status = 404;
        IsSucceded = false;
        Message = message;
        return this;
    }
    public OperationResult Logic(string message)
    {
        Status = 500;
        IsSucceded = false;
        Message = message;
        return this;
    }
    public OperationResult Forbiden(string message)
    {
        Status = 403;
        IsSucceded = false;
        Message = message;
        return this;
    }
    public static OperationResult<TValue> Success<TValue>(TValue value,int count=1) => new (value, new OperationResult().succedded(), count);
    public static OperationResult<TValue> Failure<TValue>(OperationResult operationResult) => new(default, operationResult);
    public static OperationResult<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(new OperationResult().Failed(FailedMessage));
}
public class OperationResult<TValue> : OperationResult
{
    private readonly TValue? _value;
    protected internal OperationResult(TValue? value,OperationResult operation,int count=1):base(operation)
    {
        _value = value;
        Count = count;
    }
    public int Count { get; set; }
    [NotNull]
    public TValue result => IsSucceded ? _value!
         : _value;
    public static implicit operator OperationResult<TValue>(TValue? value) => Create(value);
}