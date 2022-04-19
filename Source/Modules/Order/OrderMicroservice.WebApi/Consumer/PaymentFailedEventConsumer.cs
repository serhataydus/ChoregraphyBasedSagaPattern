using MassTransit;
using MessageBroker.Shared.Events.Payment;
using OrderMicroservice.WebApi.Data;
using OrderMicroservice.WebApi.Enums;

namespace OrderMicroservice.WebApi.Consumer;

public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(OrderDbContext orderDbContext,
                                     ILogger<PaymentFailedEventConsumer> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        Data.Entities.OrderEntity? order = await _orderDbContext.Orders.FindAsync(context.Message.OrderId);
        if (order != null) {
            order.Status = OrderStatus.Fail;
            order.FailMessage = context.Message.FailMessage;
            await _orderDbContext.SaveChangesAsync();

            _logger.LogInformation($"Order (Id={context.Message.OrderId}) status changed : {order.Status}");
        }
        else {
            _logger.LogError($"Order (Id={context.Message.OrderId}) not found");
        }
    }
}
