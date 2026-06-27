using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WisdomPetMedicine.Rescue.Api.IntegrationEvents;

public class PetFlagForAdoptionIntegrationEventHandler : BackgroundService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<PetFlagForAdoptionIntegrationEventHandler> logger;
    private readonly ServiceBusClient serviceBusClient;
    private readonly ServiceBusProcessor serviceBusProcessor;

    public PetFlagForAdoptionIntegrationEventHandler(IConfiguration configuration, 
        ILogger<PetFlagForAdoptionIntegrationEventHandler> logger)
    {
        this.configuration = configuration;
        this.logger = logger;

        serviceBusClient = new ServiceBusClient(configuration["sb-pet-flag-for-adoption"]);
        serviceBusProcessor = serviceBusClient.CreateProcessor(configuration["ServiceBus:Adoption:QueueName"]);
        serviceBusProcessor.ProcessMessageAsync += ProcessMessageAsync;
        serviceBusProcessor.ProcessErrorAsync += ProcessErrorAsync;
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
    {
        logger.LogError(arg.Exception, "Error processing message: {ErrorSource}", arg.ErrorSource);
        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs arg)
    {
        var messageBody = arg.Message.Body.ToString();
        var theEvent = JsonConvert.DeserializeObject<PetFlagForAdoptionIntegrationEvent>(messageBody);
        logger.LogInformation("Received message: {MessageBody}", messageBody);
        // Here you can deserialize the message and handle it accordingly
        // For example, you might want to call a service to process the adoption request
        await arg.CompleteMessageAsync(arg.Message);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await serviceBusProcessor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await serviceBusProcessor.StopProcessingAsync(cancellationToken);
        await serviceBusProcessor.DisposeAsync();
        await serviceBusClient.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
