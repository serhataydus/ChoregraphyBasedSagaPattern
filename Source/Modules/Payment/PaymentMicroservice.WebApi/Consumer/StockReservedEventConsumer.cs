using MassTransit;
using MessageBroker.Shared.Events.Payment;
using MessageBroker.Shared.Events.Stock;

namespace PaymentMicroservice.WebApi.Consumer;

public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
{

    private readonly ILogger<StockReservedEventConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public StockReservedEventConsumer(ILogger<StockReservedEventConsumer> logger,
                                     IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        decimal balance = 3000m;
        if (balance > context.Message.Payment.TotalAmount) {
            _logger.LogInformation($"{context.Message.Payment.TotalAmount} TL was withdrawn from credit card for user id = {context.Message.BuyerId}");

            PaymentCompletedEvent paymentCompletedEvent = new() {
                OrderId = context.Message.OrderId,
                BuyerId = context.Message.BuyerId
            };

            await _publishEndpoint.Publish(paymentCompletedEvent);
        }
        else {
            _logger.LogInformation($"{context.Message.Payment.TotalAmount} TL was not withdrawn from credit card for user id = {context.Message.BuyerId}");

            PaymentFailedEvent paymentFailedEvent = new() {
                OrderId = context.Message.OrderId,
                BuyerId = context.Message.BuyerId,
                OrderItems = context.Message.OrderItems,
                FailMessage = "not enough balance"
            };

            await _publishEndpoint.Publish(paymentFailedEvent);
        }
    }
}
