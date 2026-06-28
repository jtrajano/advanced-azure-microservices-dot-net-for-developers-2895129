using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using WisdomPetMedicine.Hospital.Api.Infrastructure;

namespace WisdomPetMedicine.Hospital.Api.IntegrationEvents;

public class PetTransferredToHospitalIntegrationEventHandler : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PetTransferredToHospitalIntegrationEventHandler> _logger;
    private readonly ServiceBusClient serviceBusClient;
    private readonly ServiceBusProcessor serviceBusProcessor;

    public PetTransferredToHospitalIntegrationEventHandler(
        IConfiguration configuration, 
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PetTransferredToHospitalIntegrationEventHandler> logger)
    {
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        serviceBusClient = new ServiceBusClient(_configuration["sb-pet-flag-for-adoption"]);
        serviceBusProcessor = serviceBusClient.CreateProcessor(_configuration["ServiceBus:Transfer:QueueName"]);
        serviceBusProcessor.ProcessMessageAsync += ProcessMessageAsync;
        serviceBusProcessor.ProcessErrorAsync += ProcessErrorAsync;

    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();
        var theEvent = JsonConvert.DeserializeObject<PetTransferredToHospitalIntegrationEvent>(messageBody);
        await args.CompleteMessageAsync(args.Message);


        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();

        var existingPatientsMetadata = await dbContext.PatientsMetadata.FindAsync(theEvent.Id);
        if (existingPatientsMetadata == null)
        {
            dbContext.PatientsMetadata.Add(theEvent);
            await dbContext.SaveChangesAsync();
        }

    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error processing message: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await serviceBusProcessor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await serviceBusProcessor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
