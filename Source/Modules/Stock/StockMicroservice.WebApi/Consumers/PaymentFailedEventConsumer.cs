using MassTransit;
using MessageBroker.Shared.Events.Payment;
using Microsoft.EntityFrameworkCore;
using StockMicroservice.WebApi.Data;

namespace StockMicroservice.WebApi.Consumers;

public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly StockDbContext _stockDbContext;
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(StockDbContext stockDbContext,
                                     ILogger<PaymentFailedEventConsumer> logger)
    {
        _stockDbContext = stockDbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        foreach (MessageBroker.Shared.Messages.Order.OrderItemMessage? item in context.Message.OrderItems) {
            Data.Entities.StockEntity? stock = await _stockDbContext.Stocks.FirstOrDefaultAsync(f => f.ProductId == item.ProductId);
            if (stock != null) {
                stock.Count += item.Count;
            }
        }

        await _stockDbContext.SaveChangesAsync();

        _logger.LogInformation($"Stock was released for Order Id : ({context.Message.OrderId})");
    }
}
