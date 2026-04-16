using System.Text;
using Xunit.Abstractions;

namespace ConfigAndSecurity.Tests;

public abstract class BaseTest
{
    protected readonly ITestOutputHelper Output;

    protected BaseTest(ITestOutputHelper output)
    {
        Output = output;
    }

    protected void Log(string message)
    {
        Output.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    protected void LogTest(string testName, string description, string expected, Func<Task> testAction)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\n");
        sb.AppendLine($"ТЕСТ: {testName}");
        sb.AppendLine($"ОПИСАНИЕ: {description}");
        sb.AppendLine($"ОЖИДАЕТСЯ: {expected}");
        sb.AppendLine($"ВРЕМЯ: {DateTime.Now:HH:mm:ss}");

        Output.WriteLine(sb.ToString());

        try
        {
            testAction().Wait();
            Output.WriteLine($"\nРЕЗУЛЬТАТ: ТЕСТ ПРОЙДЕН\n");
        }
        catch (Exception ex)
        {
            Output.WriteLine($"\nРЕЗУЛЬТАТ: ТЕСТ НЕ ПРОЙДЕН");
            Output.WriteLine($"ОШИБКА: {ex.Message}\n");
            throw;
        }
    }
}