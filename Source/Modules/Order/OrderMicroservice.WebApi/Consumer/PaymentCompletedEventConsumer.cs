using MassTransit;
using MessageBroker.Shared.Events.Payment;
using OrderMicroservice.WebApi.Data;
using OrderMicroservice.WebApi.Enums;

namespace OrderMicroservice.WebApi.Consumer;

public class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<PaymentCompletedEventConsumer> _logger;

    public PaymentCompletedEventConsumer(OrderDbContext orderDbContext,
                                     ILogger<PaymentCompletedEventConsumer> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        Data.Entities.OrderEntity? order = await _orderDbContext.Orders.FindAsync(context.Message.OrderId);
        if (order != null) {
            order.Status = OrderStatus.Complete;
            await _orderDbContext.SaveChangesAsync();

            _logger.LogInformation($"Order (Id={context.Message.OrderId}) status changed : {order.Status}");
        }
        else {
            _logger.LogError($"Order (Id={context.Message.OrderId}) not found");
        }
    }
}
