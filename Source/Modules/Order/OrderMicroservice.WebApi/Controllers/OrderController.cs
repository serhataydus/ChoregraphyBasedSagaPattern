using MassTransit;
using MessageBroker.Shared.Events.Order;
using MessageBroker.Shared.Messages.Order;
using Microsoft.AspNetCore.Mvc;
using OrderMicroservice.WebApi.Data;
using OrderMicroservice.WebApi.Data.Entities;
using OrderMicroservice.WebApi.Enums;
using OrderMicroservice.WebApi.Models.Dtos.Order;

namespace OrderMicroservice.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly OrderDbContext _orderDbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderController(OrderDbContext orderDbContext, IPublishEndpoint publishEndpoint)
    {
        _orderDbContext = orderDbContext;
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<IActionResult> Get(OrderCreateDto orderCreate, CancellationToken cancellationToken)
    {
        OrderEntity newOrder = new() {
            BuyerId = orderCreate.BuyerId,
            Status = OrderStatus.Suspend,
            Address = new() {
                District = orderCreate.Address.District,
                Line = orderCreate.Address.Line,
                Province = orderCreate.Address.Province
            },
            CreationDate = DateTime.UtcNow,
            Items = new List<OrderItemEntity>()
        };

        orderCreate.OrderItems.ForEach(item =>
        {
            newOrder.Items.Add(new() {
                Price = item.Price,
                ProductId = item.ProductId,
                Count = item.Count
            });
        });

        await _orderDbContext.Orders.AddAsync(newOrder, cancellationToken);
        await _orderDbContext.SaveChangesAsync(cancellationToken);

        OrderCreatedEvent orderCreatedEvent = new() {
            BuyerId = orderCreate.BuyerId,
            OrderId = newOrder.Id,
            Payment = new() {
                CardName = orderCreate.Payment.CardName,
                CardNumber = orderCreate.Payment.CardNumber,
                CVV = orderCreate.Payment.CVV,
                Expiration = orderCreate.Payment.Expiration,
                TotalAmount = orderCreate.OrderItems.Sum(s => s.Price * s.Count)
            },
            OrderItems = orderCreate.OrderItems.Select(s => new OrderItemMessage { Count = s.Count, ProductId = s.ProductId }).ToList()
        };

        await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);

        return Ok();
    }
}
