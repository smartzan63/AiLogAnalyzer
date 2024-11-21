namespace Common.Tests;

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

[ExcludeFromCodeCoverage]
public static class LogAnalysisServiceResponseValidator
{
    public static void ValidateBasicResponse(string response)
    {
        response.Should().NotBeNullOrEmpty();

        response.Should().Contain("WHAT HAPPENED:");
        response.Should().Contain("EXPECTED RESULT:");
        response.Should().Contain("ACTUAL RESULT:");
        response.Should().Contain("HOW TO FIX IT:");

        Console.WriteLine("Response from OpenAI: " + response);
    }
    
    public static void ValidateAdditionalResponse(string response)
    {
        response.Should().NotBeNullOrEmpty();
        Console.WriteLine("Response from OpenAI: " + response);
    }
}