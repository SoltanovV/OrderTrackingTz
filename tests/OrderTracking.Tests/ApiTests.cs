using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderTracking.Application.Models.Events;
using OrderTracking.Application.Models.Response;
using OrderTracking.Domain.Models.Enums;
using OrderTracking.Infrastructure.Models.Settings;
using OrderTracking.Infrastructure.Persistence;
using OrderTracking.Infrastructure.Services;
using OrderTracking.Infrastructure.Utilities;
using Xunit;

namespace OrderTracking.Tests;

/// <summary>HTTP API, сохранение данных и SSE на тестовом сервере.</summary>
public sealed class ApiTests(TestHost host) : IClassFixture<TestHost>
{
    private readonly HttpClient client = host.CreateClient();
    private static readonly JsonSerializerOptions Json = EventJson.Options;

    /// <summary>Создаёт заказ для теста и проверяет ответ 201 с адресом его карточки.</summary>
    private async Task<OrderDto> Create(string? number = null)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { orderNumber = number ?? Guid.NewGuid().ToString(), description = "  Laptop and accessories  " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderDto>(Json))!;
        Assert.EndsWith($"/api/orders/{order.Id}", response.Headers.Location!.ToString());
        return order;
    }

    /// <summary>Проверяет нормализацию ввода, чтение заказа, новую версию и сохранение двух событий в outbox.</summary>
    [Fact(DisplayName = "Создание и смена статуса сохраняют заказ, историю и события")]
    public async Task CreateGetAndChangeStatusPersistHistoryAndOutboxAtomically()
    {
        var order = await Create("  api-lifecycle  ");
        Assert.Equal("API-LIFECYCLE", order.OrderNumber);
        Assert.Equal("Laptop and accessories", order.Description);
        Assert.Equal(OrderStatus.Created, order.Status);
        var response = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Shipped", version = 1 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}", Json))!;
        Assert.Equal(OrderStatus.Shipped, updated.Status);
        Assert.Equal(2, updated.Version);
        Assert.Equal(2, updated.History.Count);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var events = (await db.OutboxMessages.AsNoTracking().ToListAsync()).Select(x => JsonSerializer.Deserialize<OrderChanged>(x.Payload, Json)!)
            .Where(x => x.Order.Id == order.Id).ToList();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, x => x.Order.Status == OrderStatus.Shipped && x.Order.Version == 2);
    }

    /// <summary>Проверяет, что регистр букв и пробелы по краям не позволяют обойти уникальность номера.</summary>
    [Fact(DisplayName = "Повторный номер заказа возвращает 409")]
    public async Task DuplicateOrderNumberIsCaseInsensitive()
    {
        await Create("Duplicate-Number");
        var response = await client.PostAsJsonAsync("/api/orders", new { orderNumber = " duplicate-number ", description = "Test" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Проверяет обязательность номера и описания отдельно: каждое поле должно содержать текст.</summary>
    [Theory(DisplayName = "Номер и описание из одних пробелов возвращают 400")]
    [InlineData("  ", "Test")]
    [InlineData("A", "   ")]
    public async Task RequiredFieldsRejectWhitespace(string number, string description)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { orderNumber = number, description });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Проверяет отклонение запроса с номером длиннее 64 символов и описанием длиннее 2000 символов.</summary>
    [Fact(DisplayName = "Слишком длинные номер и описание возвращают 400")]
    public async Task LengthLimitsAreEnforced()
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { orderNumber = new string('x', 65), description = new string('x', 2001) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Проверяет запрет доставки без отправки, устаревшую версию и отсутствие новых записей при повторе статуса.</summary>
    [Fact(DisplayName = "Конфликт версии и запрещённый переход не создают лишних записей")]
    public async Task StaleVersionAndInvalidTransitionDoNotWriteHistoryOrEvents()
    {
        var order = await Create();
        var skipped = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Delivered", version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, skipped.StatusCode);
        var changed = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Shipped", version = 1 });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var stale = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Cancelled", version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var same = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "Shipped", version = 2 });
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        var latest = (await client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}", Json))!;
        Assert.Equal(2, latest.History.Count);
        Assert.Equal(OrderStatus.Shipped, latest.Status);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        Assert.Equal(2, await db.OutboxMessages.CountAsync(x => x.Payload.Contains(order.Id.ToString())));
    }

    /// <summary>Проверяет пропущенный или неизвестный статус и нулевую версию: состояние и история заказа не меняются.</summary>
    [Theory(DisplayName = "Некорректный запрос смены статуса возвращает 400")]
    [InlineData("{\"version\":1}")]
    [InlineData("{\"status\":\"Unknown\",\"version\":1}")]
    [InlineData("{\"status\":\"Shipped\",\"version\":0}")]
    public async Task InvalidStatusPayloadReturns400(string body)
    {
        var order = await Create();
        var response = await client.PatchAsync($"/api/orders/{order.Id}/status", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var latest = (await client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}", Json))!;
        Assert.Equal(OrderStatus.Created, latest.Status);
        Assert.Equal(1, latest.Version);
        Assert.Single(latest.History);
    }

    /// <summary>Проверяет поиск по номеру, фильтр статуса, размер страницы, выборку по идентификаторам и неверные параметры.</summary>
    [Fact(DisplayName = "Поиск, фильтр, пагинация и отслеживаемые заказы")]
    public async Task SearchFilterPagingAndTrackedIdsAreApplied()
    {
        var a = await Create("SEARCH-ONE");
        var b = await Create("SEARCH-TWO");
        await client.PatchAsJsonAsync($"/api/orders/{b.Id}/status", new { status = "Shipped", version = 1 });
        var page = (await client.GetFromJsonAsync<OrderPage>("/api/orders?search=search-&pageSize=1", Json))!;
        Assert.Equal(2, page.Total); Assert.Single(page.Items);
        var filtered = (await client.GetFromJsonAsync<OrderPage>("/api/orders?search=search-&status=Shipped", Json))!;
        Assert.Equal(b.Id, Assert.Single(filtered.Items).Id);
        var tracked = (await client.GetFromJsonAsync<OrderPage>($"/api/orders?ids={a.Id}", Json))!;
        Assert.Equal(a.Id, Assert.Single(tracked.Items).Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?status=99")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orders?pageSize=101")).StatusCode);
    }

    /// <summary>Проверяет неизвестный заказ, работоспособность процесса и неготовность приложения без брокера.</summary>
    [Fact(DisplayName = "Отсутствующий заказ — 404; недоступный брокер — 503")]
    public async Task MissingOrderReturns404AndBrokerOutageIsVisibleInHealth()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/orders/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health")).StatusCode);
    }

    /// <summary>Проверяет передачу изменений доступности брокера через тот же SSE-поток в пределах пяти секунд.</summary>
    [Fact(DisplayName = "SSE сообщает отключение и восстановление брокера")]
    public async Task SseReportsBrokerDisconnectAndRecoveryWithoutReconnect()
    {
        var broker = host.Services.GetRequiredService<BrokerStatus>();
        var original = broker.Connected;
        try
        {
            broker.Connected = true;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var response = await client.GetAsync("/api/events", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var reader = new StreamReader(stream);
            Assert.Equal("retry: 3000", await reader.ReadLineAsync(timeout.Token));
            Assert.Equal("event: ready", await reader.ReadLineAsync(timeout.Token));
            Assert.Contains("\"brokerConnected\":true", await reader.ReadLineAsync(timeout.Token));
            Assert.Equal("", await reader.ReadLineAsync(timeout.Token));

            broker.Connected = false;
            Assert.Equal("event: heartbeat", await reader.ReadLineAsync(timeout.Token));
            Assert.Contains("\"brokerConnected\":false", await reader.ReadLineAsync(timeout.Token));
            Assert.Equal("", await reader.ReadLineAsync(timeout.Token));

            broker.Connected = true;
            Assert.Equal("event: heartbeat", await reader.ReadLineAsync(timeout.Token));
            Assert.Contains("\"brokerConnected\":true", await reader.ReadLineAsync(timeout.Token));
        }
        finally
        {
            broker.Connected = original;
        }
    }

    /// <summary>Проверяет формат потока: начальное событие ready и последующее order-changed с идентификатором заказа.</summary>
    [Fact(DisplayName = "SSE передаёт готовность и событие изменения заказа")]
    public async Task SseStreamsReadyThenOrderEvent()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var response = await client.GetAsync("/api/events", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        Assert.Equal("retry: 3000", await reader.ReadLineAsync(timeout.Token));
        Assert.Equal("event: ready", await reader.ReadLineAsync(timeout.Token));
        Assert.Contains("brokerConnected", await reader.ReadLineAsync(timeout.Token));
        Assert.Equal("", await reader.ReadLineAsync(timeout.Token));
        var order = await Create();
        host.Services.GetRequiredService<EventHub>().Publish(new OrderChanged(Guid.NewGuid(), order));
        while (await reader.ReadLineAsync(timeout.Token) is { } line && line != "event: order-changed") { }
        Assert.Contains(order.Id.ToString(), await reader.ReadLineAsync(timeout.Token));
    }
}
