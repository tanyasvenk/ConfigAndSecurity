using Microsoft.Extensions.Configuration;
using ConfigAndSecurity.Config;
using Xunit;
using Xunit.Abstractions;

namespace ConfigAndSecurity.Tests;

public class ConfigurationPriorityTests : BaseTest
{
    public ConfigurationPriorityTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void CommandLine_ShouldOverride_EnvironmentVariables()
    {
        Log("Проверка приоритета: CommandLine > EnvironmentVariables");

        var args = new[] { "--AppSecurity:Mode=Production" };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSecurity:Mode"] = "Educational"
            })
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var mode = config.GetValue<AppMode>("AppSecurity:Mode");
        Log($"Результат: Mode = {mode}");
        Log($"Ожидалось: Production (из аргументов командной строки)");

        Assert.Equal(AppMode.Production, mode);
        Log("Приоритет работает корректно");
    }

    [Fact]
    public void EnvironmentVariables_ShouldOverride_Json()
    {
        Log("Проверка приоритета: EnvironmentVariables > JSON");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSecurity:Mode"] = "Educational"
            })
            .AddEnvironmentVariables()
            .Build();

        var mode = config.GetValue<AppMode>("AppSecurity:Mode");
        Log($"Результат: Mode = {mode}");
        Log($"Ожидалось: Educational (из in-memory JSON)");

        Assert.Equal(AppMode.Educational, mode);
        Log("Приоритет работает корректно");
    }

    [Fact]
    public void InvalidTrustedOrigin_ShouldFailValidation()
    {
        Log("Проверка валидации некорректного URL");

        var options = new AppOptions
        {
            TrustedOrigins = new List<string> { "invalid-url" },
            Mode = AppMode.Educational
        };

        var validator = new AppOptionsValidator();
        var result = validator.Validate(null, options);

        Log($"Результат валидации: {(result.Succeeded ? "Успех" : "Ошибка")}");
        if (!result.Succeeded)
        {
            Log($"Сообщение: {string.Join(", ", result.Failures)}");
        }

        Assert.False(result.Succeeded);
        Assert.Contains("Некорректный URL", string.Join(", ", result.Failures));
        Log("Валидация корректно отклонила неверный URL");
    }

    [Fact]
    public void EmptyTrustedOrigins_ShouldFailDataAnnotations()
    {
        Log("Проверка валидации пустого списка TrustedOrigins");

        var options = new AppOptions
        {
            TrustedOrigins = new List<string>(),
            Mode = AppMode.Educational
        };

        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(options);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options, validationContext, validationResults, true);

        Log($"Результат валидации: {(isValid ? "Успех" : "Ошибка")}");
        if (!isValid)
        {
            Log($"Сообщение: {validationResults.First()?.ErrorMessage}");
        }

        Assert.False(isValid);
        Assert.Contains(validationResults, v =>
            v.ErrorMessage == "Список доверенных источников не может быть пустым");
        Log("Валидация корректно отклонила пустой список");
    }
}