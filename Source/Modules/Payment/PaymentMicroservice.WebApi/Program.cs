using MassTransit;
using MessageBroker.Shared.Constants;
using PaymentMicroservice.WebApi.Consumer;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(options =>
{
    options.AddConsumer<StockReservedEventConsumer>();
    options.UsingRabbitMq((context, configuration) =>
    {
        configuration.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        configuration.ReceiveEndpoint(RabbitMqConstant.StockReservedEventQueueName, configureEndpoint =>
        {
            configureEndpoint.ConfigureConsumer<StockReservedEventConsumer>(context);
        });
    });
});

builder.Services.AddMassTransitHostedService();

WebApplication? app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
