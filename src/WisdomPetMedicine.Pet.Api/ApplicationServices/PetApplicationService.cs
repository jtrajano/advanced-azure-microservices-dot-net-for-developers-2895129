using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using WisdomPetMedicine.Common;
using WisdomPetMedicine.Pet.Api.Commands;
using WisdomPetMedicine.Pet.Api.IntegrationEvents;
using WisdomPetMedicine.Pet.Domain.Events;
using WisdomPetMedicine.Pet.Domain.Repositories;
using WisdomPetMedicine.Pet.Domain.Services;
using WisdomPetMedicine.Pet.Domain.ValueObjects;
using System.Text.Json;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Net.Mime;
using System;
using Newtonsoft.Json;

namespace WisdomPetMedicine.Pet.Api.ApplicationServices
{
    public class PetApplicationService
    {
        private readonly IPetRepository petRepository;
        private readonly IBreedService breedService;

        public PetApplicationService(IPetRepository petRepository,
                                     IBreedService breedService,
                                     IConfiguration configuration
                                     )
        {
            this.petRepository = petRepository;
            this.breedService = breedService;

            // Register the domain event handler for PetFlaggedForAdoption
            DomainEvents.PetFlaggedForAdoption.Register(async c =>
            {
                var integrationEvent = new PetFlaggedForAdoptionIntegrationEvent()
                {
                    Id = c.Id,
                    Name = c.Name,
                    Breed = c.Breed,
                    Sex = c.Sex,
                    Color = c.Color,
                    DateOfBirth = c.DateOfBirth,
                    Species = c.Species
                };

                await PublishIntegrationEventAsync(integrationEvent
                    , configuration["sb-pet-flag-for-adoption"]
                    , configuration["ServiceBus:Adoption:QueueName"]);
            });
        }

        public async Task HandleCommandAsync(CreatePetCommand command)
        {
            var pet = new Domain.Entities.Pet(PetId.Create(command.Id));
            pet.SetName(PetName.Create(command.Name));
            pet.SetBreed(PetBreed.Create(command.Breed, breedService));
            pet.SetSex(SexOfPet.Create((SexesOfPets)command.Sex));
            pet.SetColor(PetColor.Create(command.Color));
            pet.SetDateOfBirth(PetDateOfBirth.Create(command.DateOfBirth));
            pet.SetSpecies(PetSpecies.Get(command.Species));
            await petRepository.AddAsync(pet);
        }

        public async Task HandleCommandAsync(SetNameCommand command)
        {
            var pet = await petRepository.GetAsync(PetId.Create(command.Id));
            pet.SetName(PetName.Create(command.Name));
            await petRepository.UpdateAsync(pet);
        }

        public async Task HandleCommandAsync(SetBreedCommand command)
        {
            var pet = await petRepository.GetAsync(PetId.Create(command.Id));
            pet.SetBreed(PetBreed.Create(command.Breed, new FakeBreedService()));
            await petRepository.UpdateAsync(pet);
        }

        public async Task HandleCommandAsync(SetColorCommand command)
        {
            var pet = await petRepository.GetAsync(PetId.Create(command.Id));
            pet.SetColor(PetColor.Create(command.Color));
            await petRepository.UpdateAsync(pet);
        }

        public async Task HandleCommandAsync(FlagForAdoptionCommand command)
        {
            var pet = await petRepository.GetAsync(PetId.Create(command.Id));
            pet.FlagForAdoption();            
        }

        public async Task HandleCommandAsync(TransferToHospitalCommand command)
        {
            var pet = await petRepository.GetAsync(PetId.Create(command.Id));
            pet.TransferToHospital();
        }

        private async Task PublishIntegrationEventAsync(IIntegrationEvent integrationEvent, string connectionString, string queueName)
        {
            // Here you would publish the integration event to your message bus or event stream
            // For example, using a message broker like RabbitMQ, Kafka, etc.
            // This is a placeholder for the actual implementation
            var jsonMessage = JsonConvert.SerializeObject(integrationEvent);
            var body =  Encoding.UTF8.GetBytes(jsonMessage);
            var client = new ServiceBusClient(connectionString);
            await using ServiceBusSender sender = client.CreateSender(queueName);
            var message = new ServiceBusMessage()
            {
                Body = new BinaryData(body),
                ContentType = MediaTypeNames.Application.Json,
                Subject = integrationEvent.GetType().FullName,
                MessageId = Guid.NewGuid().ToString()
            };

            await sender.SendMessageAsync(message);


        }
    }
}