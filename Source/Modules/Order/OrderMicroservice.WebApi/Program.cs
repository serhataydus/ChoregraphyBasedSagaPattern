using MassTransit;
using MessageBroker.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using OrderMicroservice.WebApi.Consumer;
using OrderMicroservice.WebApi.Data;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<IOrderDbContext, OrderDbContext>(options =>
         options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")),
         ServiceLifetime.Transient);

builder.Services.AddMassTransit(options =>
{
    options.AddConsumer<PaymentCompletedEventConsumer>();
    options.AddConsumer<PaymentFailedEventConsumer>();
    options.AddConsumer<StockNotReservedEventConsumer>();
    options.UsingRabbitMq((context, configuration) =>
    {
        configuration.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        configuration.ReceiveEndpoint(RabbitMqConstant.OrderPaymentCompletedEventQueueName, configureEndpoint =>
        {
            configureEndpoint.ConfigureConsumer<PaymentCompletedEventConsumer>(context);
        });
        configuration.ReceiveEndpoint(RabbitMqConstant.OrderPaymentFailedEventQueueName, configureEndpoint =>
        {
            configureEndpoint.ConfigureConsumer<PaymentFailedEventConsumer>(context);
        });
        configuration.ReceiveEndpoint(RabbitMqConstant.OrderStockNotReservedEventQueueName, configureEndpoint =>
        {
            configureEndpoint.ConfigureConsumer<StockNotReservedEventConsumer>(context);
        });
    });

});

builder.Services.AddMassTransitHostedService();

WebApplication? app = builder.Build();

using (IServiceScope? scope = app.Services.CreateScope()) {
    OrderDbContext? dataContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dataContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
