using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace ConfigAndSecurity.Tests;

public class IntegrationSecurityTests : BaseTest
{
    public IntegrationSecurityTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task Cors_ShouldBlock_UntrustedOrigin()
    {
        Log("Проверка блокировки CORS для недоверенных источников");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("AppSecurity:TrustedOrigins:0", "https://trusted.com");
            });

        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/items");
        request.Headers.Add("Origin", "http://malicious-site.net");

        Log("Отправка запроса с Origin: http://malicious-site.net");
        var response = await client.SendAsync(request);

        var hasHeader = response.Headers.Contains("Access-Control-Allow-Origin");
        Log($"Заголовок Access-Control-Allow-Origin присутствует: {hasHeader}");

        Assert.False(hasHeader);
        Log("Запрос успешно заблокирован");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresent()
    {
        Log("Проверка наличия заголовков безопасности");

        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/items");

        var frameHeader = response.Headers.Contains("X-Frame-Options");
        var contentTypeHeader = response.Headers.Contains("X-Content-Type-Options");

        Log($"X-Frame-Options присутствует: {frameHeader}");
        Log($"X-Content-Type-Options присутствует: {contentTypeHeader}");

        if (frameHeader)
            Log($"   Значение: {response.Headers.GetValues("X-Frame-Options").First()}");
        if (contentTypeHeader)
            Log($"   Значение: {response.Headers.GetValues("X-Content-Type-Options").First()}");

        Assert.True(frameHeader, "X-Frame-Options должен присутствовать");
        Assert.True(contentTypeHeader, "X-Content-Type-Options должен присутствовать");

        Log("Все заголовки безопасности на месте");
    }

    [Fact]
    public async Task RateLimiting_ShouldBlock_ExcessiveRequests()
    {
        Log("Проверка ограничения частоты запросов");

        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var blockedCount = 0;

        Log("Отправка 110 запросов...");
        for (int i = 1; i <= 110; i++)
        {
            var response = await client.GetAsync("/api/items");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                blockedCount++;

            if (i % 20 == 0)
                Log($"   Выполнено {i} запросов, заблокировано: {blockedCount}");
        }

        Log($"Итого заблокировано: {blockedCount} из 110 запросов");
        Assert.True(blockedCount > 0, "Должны быть заблокированные запросы");
        Log("Rate limiting работает корректно");
    }

    [Fact]
    public async Task StrictRateLimiting_ShouldBlockPostRequests_AfterLimit()
    {
        Log("Проверка строгого лимита для POST запросов");

        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var results = new List<(int status, int count)>();

        Log("Отправка 10 POST запросов...");
        for (int i = 1; i <= 10; i++)
        {
            var response = await client.PostAsync("/api/items", null);
            var status = (int)response.StatusCode;
            results.Add((status, i));

            if (status == 429)
                Log($"   Запрос {i}: ЗАБЛОКИРОВАН (429)");
            else
                Log($"   Запрос {i}: УСПЕШЕН (200)");

            await Task.Delay(50);
        }

        var successCount = results.Count(r => r.status == 200);
        var blockedCount = results.Count(r => r.status == 429);

        Log($"Успешных: {successCount}, Заблокировано: {blockedCount}");
        Assert.True(successCount <= 5, $"Успешных запросов {successCount}, должно быть не более 5");
        Assert.True(blockedCount >= 4, $"Заблокировано {blockedCount}, должно быть не менее 4");
        Log("Строгий лимит работает корректно");
    }

    [Fact]
    public async Task RequestId_ShouldBeUnique_ForEachRequest()
    {
        Log("Проверка уникальности RequestId");

        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var requestIds = new HashSet<string>();

        Log("Отправка 10 запросов...");
        for (int i = 1; i <= 10; i++)
        {
            var response = await client.GetAsync("/api/items");
            if (response.Headers.TryGetValues("X-Request-Id", out var values))
            {
                var requestId = values.First();
                requestIds.Add(requestId);
                Log($"   Запрос {i}: RequestId = {requestId}");
            }
        }

        Log($"Уникальных ID: {requestIds.Count} из 10");
        Assert.Equal(10, requestIds.Count);
        Log("Все RequestId уникальны");
    }
}