using System;

using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class OperationResultTests
{
    [Fact]
    public void SuccessFactory_SetsIsSuccessAndValue()
    {
        var r = OperationResult.Success(42);

        Assert.True(r.IsSuccess);
        Assert.Equal(42, r.Value);
        Assert.Equal(ResponseStatus.Success, r.Status);
        Assert.Null(r.ErrorMessage);
        Assert.Null(r.Diagnostic);
    }

    [Fact]
    public void FailureFactory_SetsIsFailureAndErrorMessage()
    {
        var ex = new InvalidOperationException("boom");
        var r = OperationResult.Failure<int>("bad", ResponseStatus.Error, ex);

        Assert.False(r.IsSuccess);
        Assert.Equal(default(int), r.Value);
        Assert.Equal(ResponseStatus.Error, r.Status);
        Assert.Equal("bad", r.ErrorMessage);
        Assert.Same(ex, r.Diagnostic);
    }

    [Fact]
    public void UnitSuccess_Factory_IsConcise()
    {
        var r = OperationResult.Success();

        Assert.True(r.IsSuccess);
        Assert.Equal(Unit.Value, r.Value);
    }
}
