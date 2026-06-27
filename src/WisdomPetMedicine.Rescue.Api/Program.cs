using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WisdomPetMedicine.Rescue.Api.ApplicationServices;
using WisdomPetMedicine.Rescue.Api.Infrastructure;
using WisdomPetMedicine.Rescue.Api.IntegrationEvents;
using WisdomPetMedicine.Rescue.Domain.Repositories;

namespace WisdomPetMedicine.Rescue.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. Create the builder first
            var builder = WebApplication.CreateBuilder(args);

            // 2. Load the Key Vault URI from appsettings.json
            // Note: Changed the key to "KeyVault:VaultUri" to match your vault's URL, not the connection string itself!
            var vaultUri = builder.Configuration["ServiceBus:ConnectionString"];

            if (!string.IsNullOrEmpty(vaultUri))
            {
                builder.Configuration.AddAzureKeyVault(
                    new Uri(vaultUri),
                    new DefaultAzureCredential()
                );
            }

            // 3. Add services to the dependency injection container
            builder.Services.AddControllers();
            builder.Services.AddRescueDb(builder.Configuration);
            builder.Services.AddScoped<AdopterApplicationService>();
            builder.Services.AddScoped<IRescueRepository, RescueRepository>();
            builder.Services.AddHostedService<PetFlagForAdoptionIntegrationEventHandler>();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WisdomPetMedicine.Rescue.Api", Version = "v1" });
            });


            // 4. Build the application wrapper
            var app = builder.Build();

            // 5. Configure middleware pipelines (Routing, Authorization, etc.)
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WisdomPetMedicine.Rescue.Api v1"));
            }

            app.EnsureRescueDbIsCreated();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // 6. FINALLY, run the application at the very end
            app.Run();
        }




    }
}
