using BrickForge.Core.Results;

namespace BrickForge.Core.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_ReturnsSuccessResult_WithValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_ReturnsFailureResult_WithErrorMessage()
    {
        const string error = "something went wrong";
        var result = Result<int>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.ErrorMessage);
    }

    [Fact]
    public void NonGenericSuccess_ReturnsSuccessResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void NonGenericFailure_ReturnsFailureResult_WithErrorMessage()
    {
        const string error = "export failed";
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.ErrorMessage);
    }
}
