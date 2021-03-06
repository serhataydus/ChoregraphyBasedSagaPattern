using MassTransit;
using MessageBroker.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using StockMicroservice.WebApi.Consumers;
using StockMicroservice.WebApi.Data;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<IStockDbContext, StockDbContext>(options =>
         options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")),
         ServiceLifetime.Transient);

builder.Services.AddMassTransit(options =>
{
    options.AddConsumer<OrderCreatedEventConsumer>();
    options.AddConsumer<PaymentFailedEventConsumer>();
    options.UsingRabbitMq((context, configuration) =>
    {
        configuration.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        configuration.ReceiveEndpoint(RabbitMqConstant.StockOrderCreatedEventQueueName, configureEndpoint =>
        {
            configureEndpoint.ConfigureConsumer<OrderCreatedEventConsumer>(context);
        });
        configuration.ReceiveEndpoint(RabbitMqConstant.StockPaymentFailedEventQueueName, configureEndpoint =>
        {
            configureEndpoint.ConfigureConsumer<PaymentFailedEventConsumer>(context);
        });
    });
});

builder.Services.AddMassTransitHostedService();

WebApplication? app = builder.Build();

using (IServiceScope? scope = app.Services.CreateScope()) {
    StockDbContext? dataContext = scope.ServiceProvider.GetRequiredService<StockDbContext>();
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
