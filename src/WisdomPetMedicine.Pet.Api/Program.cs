using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using System;
using WisdomPetMedicine.Pet.Api.ApplicationServices;
using WisdomPetMedicine.Pet.Api.Infrastructure;
using WisdomPetMedicine.Pet.Domain.Repositories;
using WisdomPetMedicine.Pet.Domain.Services;

namespace WisdomPetMedicine.Pet.Api
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
            builder.Services.AddPetDb(builder.Configuration);
            builder.Services.AddScoped<IPetRepository, PetRepository>();
            builder.Services.AddScoped<PetApplicationService>();
            builder.Services.AddScoped<IBreedService, FakeBreedService>();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WisdomPetMedicine.Api", Version = "v1" });
            });

            // 4. Build the application wrapper
            var app = builder.Build();

            // 5. Configure middleware pipelines (Routing, Authorization, etc.)


            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WisdomPetMedicine.Api v1"));
            }

            app.EnsurePetDbIsCreated();
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