using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using ConfigAndSecurity.Domain;
using Xunit;
using Xunit.Abstractions;

namespace ConfigAndSecurity.Tests;

public class ModeTests : BaseTest
{
    public ModeTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task EducationalMode_ShouldReturn_DetailedErrorMessage()
    {
        Log("Начинаем тест EducationalMode");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("AppSecurity:Mode", "Educational");
            });

        var client = factory.CreateClient();

        Log("Отправка запроса GET /api/error");
        var response = await client.GetAsync("/api/error");
        Log($"Статус ответа: {(int)response.StatusCode}");

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Log($"Сообщение: {errorResponse?.Message}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Тестовая ошибка", errorResponse?.Message);

        Log("Тест пройден успешно");
    }

    [Fact]
    public async Task ProductionMode_ShouldReturn_GenericErrorMessage()
    {
        Log("Начинаем тест ProductionMode");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("AppSecurity:Mode", "Production");
            });

        var client = factory.CreateClient();

        Log("Отправка запроса GET /api/error");
        var response = await client.GetAsync("/api/error");
        Log($"Статус ответа: {(int)response.StatusCode}");

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Log($"Сообщение: {errorResponse?.Message}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Внутренняя ошибка сервера", errorResponse?.Message);

        Log("Тест пройден успешно");
    }
}