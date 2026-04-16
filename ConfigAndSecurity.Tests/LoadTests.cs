using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace ConfigAndSecurity.Tests;

public class LoadTests : BaseTest
{
    public LoadTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task RateLimiting_ShouldProtect_AgainstDosAttack()
    {
        Log("НАГРУЗОЧНЫЙ ТЕСТ: Защита от DoS-атак");
        Log("Отправка 200 параллельных запросов");

        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var stopwatch = Stopwatch.StartNew();
        var successfulRequests = 0;
        var blockedRequests = 0;

        var tasks = new List<Task>();
        for (int i = 0; i < 200; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var response = await client.GetAsync("/api/items");
                if (response.IsSuccessStatusCode)
                    Interlocked.Increment(ref successfulRequests);
                else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    Interlocked.Increment(ref blockedRequests);
            }));
        }

        Log("Запуск параллельных запросов...");
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Log($"Успешных запросов: {successfulRequests}");
        Log($"Заблокировано: {blockedRequests}");
        Log($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
        Log($"Процент блокировки: {blockedRequests * 100.0 / 200:F1}%");

        Assert.True(blockedRequests > 0, "Должны быть заблокированы запросы при превышении лимита");
        Assert.True(successfulRequests <= 120, $"Успешных запросов: {successfulRequests}, ожидалось не более 120");

        Log("Нагрузочный тест пройден - Rate limiting защищает от DoS-атак");
    }
}