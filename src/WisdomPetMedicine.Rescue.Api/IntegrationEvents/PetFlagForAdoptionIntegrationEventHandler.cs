using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WisdomPetMedicine.Rescue.Api.Infrastructure;
using WisdomPetMedicine.Rescue.Domain.Entities;
using WisdomPetMedicine.Rescue.Domain.Repositories;
using WisdomPetMedicine.Rescue.Domain.ValueObjects;

namespace WisdomPetMedicine.Rescue.Api.IntegrationEvents;

public class PetFlagForAdoptionIntegrationEventHandler : BackgroundService
{
    private readonly IConfiguration configuration;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<PetFlagForAdoptionIntegrationEventHandler> logger;
    private readonly ServiceBusClient serviceBusClient;
    private readonly ServiceBusProcessor serviceBusProcessor;

    public PetFlagForAdoptionIntegrationEventHandler(IConfiguration configuration, 
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PetFlagForAdoptionIntegrationEventHandler> logger)
    {
        this.configuration = configuration;
        this.serviceScopeFactory = serviceScopeFactory;
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
        await arg.CompleteMessageAsync(arg.Message);


        using var scope = this.serviceScopeFactory.CreateScope();
        var repo =scope.ServiceProvider.GetRequiredService<IRescueRepository>();
        using var dbContext = scope.ServiceProvider.GetRequiredService<RescueDbContext>();
        var rescuedAnimalMetadata = dbContext.RescuedAnimalsMetadata.Add(theEvent);
        var rescuedAnimal = new RescuedAnimal(RescuedAnimalId.Create(theEvent.Id));
        await repo.AddRescuedAnimalAsync(rescuedAnimal);

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
