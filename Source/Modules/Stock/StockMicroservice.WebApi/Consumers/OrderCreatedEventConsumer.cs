using MassTransit;
using MessageBroker.Shared.Constants;
using MessageBroker.Shared.Events.Order;
using MessageBroker.Shared.Events.Stock;
using Microsoft.EntityFrameworkCore;
using StockMicroservice.WebApi.Data;

namespace StockMicroservice.WebApi.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{

    private readonly StockDbContext _stockDbContext;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderCreatedEventConsumer(StockDbContext stockDbContext,
                                     ILogger<OrderCreatedEventConsumer> logger,
                                     ISendEndpointProvider sendEndpointProvider,
                                     IPublishEndpoint publishEndpoint)
    {
        _stockDbContext = stockDbContext;
        _logger = logger;
        _sendEndpointProvider = sendEndpointProvider;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        List<bool>? stockResult = new List<bool>();

        foreach (MessageBroker.Shared.Messages.Order.OrderItemMessage? item in context.Message.OrderItems) {
            stockResult.Add(await _stockDbContext.Stocks.AnyAsync(a => a.ProductId == item.ProductId && a.Count > item.Count));
        }

        if (stockResult.All(a => a.Equals(true))) {
            foreach (MessageBroker.Shared.Messages.Order.OrderItemMessage? item in context.Message.OrderItems) {
                Data.Entities.StockEntity? stock = await _stockDbContext.Stocks.FirstOrDefaultAsync(f => f.ProductId == item.ProductId);
                if (stock != null) {
                    stock.Count -= item.Count;
                }
            }

            await _stockDbContext.SaveChangesAsync();

            _logger.LogInformation($"Stock was reserved for Buyer Id : {context.Message.BuyerId}");

            ISendEndpoint? sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"{RabbitMqConstant.Queue}:{RabbitMqConstant.StockReservedEventQueueName}"));
            StockReservedEvent stockReservedEvent = new() {
                Payment = context.Message.Payment,
                BuyerId = context.Message.BuyerId,
                OrderId = context.Message.OrderId,
                OrderItems = context.Message.OrderItems
            };

            await sendEndpoint.Send(stockReservedEvent);
        }
        else {
            _logger.LogInformation($"Not enough stock for Buyer Id : {context.Message.BuyerId}");

            StockNotReservedEvent stockNotReservedEvent = new() {
                OrderId = context.Message.OrderId,
                FailMessage = "Not enough stock"
            };

            await _publishEndpoint.Publish(stockNotReservedEvent);
        }
    }
}