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
using WisdomPetMedicine.Hospital.Api.ApplicationServices;
using WisdomPetMedicine.Hospital.Api.Infrastructure;
using WisdomPetMedicine.Hospital.Api.IntegrationEvents;

namespace WisdomPetMedicine.Hospital.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {

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
            builder.Services.AddHospitalDb(builder.Configuration);
            builder.Services.AddHostedService<PetTransferredToHospitalIntegrationEventHandler>();
            builder.Services.AddScoped<HospitalApplicationService>();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WisdomPetMedicine.HospitalApi", Version = "v1" });
            });

            // 4. Build the application wrapper
            var app = builder.Build();

            // 5. Configure middleware pipelines (Routing, Authorization, etc.)


            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WisdomPetMedicine.Hospital.Api v1"));
            }

            app.EnsureHospitalDbIsCreated();
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
