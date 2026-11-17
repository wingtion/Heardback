using JobTrail.Shared.Results;

namespace JobTrail.UnitTests.Shared;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.NotFound("sample.not_found", "Sample not found.");

    [Fact]
    public void ImplicitConversion_FromValue_IsSuccessWithValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_IsFailureWithError()
    {
        Result<int> result = SampleError;

        Assert.False(result.IsSuccess);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void Value_OnFailure_ThrowsWithErrorCode()
    {
        Result<int> result = SampleError;

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);

        Assert.Contains(SampleError.Code, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_WithField_PopulatesFieldErrors()
    {
        var error = Error.Validation("auth.invalid_invite", "inviteCode", "Invite code is invalid.");

        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Equal(["Invite code is invalid."], error.FieldErrors["inviteCode"]);
    }
}
