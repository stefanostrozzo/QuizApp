using Microsoft.AspNetCore.Builder; // Aggiunto per chiarezza
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection; // Aggiunto per chiarezza
using Quiz_Task.DataAccess;
using Quiz_Task.Models;
using MongoDB.Bson.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURAZIONE SERVIZI -- -

// Add MongoDB configuration from settings.json
builder.Configuration.AddJsonFile("settings.json", optional: false, reloadOnChange: true);

var mongoSettings = new MongoDbSettings();
builder.Configuration.GetSection("MongoDbSettings").Bind(mongoSettings);

builder.Services.AddSingleton(mongoSettings);

// 🛠️ Ottimizzazione DI 1 & 2: Registro i repository in modo pulito.
// Il container DI è in grado di iniettare MongoDbSettings direttamente nel costruttore.
builder.Services.AddSingleton<ITestRepository, MongoTestRepository>();
builder.Services.AddSingleton<IUserSessionRepository, MongoUserSessionRepository>();


// 🔑 CORREZIONE CHIAVE 1: Abilita il supporto per le View (CSHTML) e le funzionalità correlate ad MVC
// Sostituisce builder.Services.AddControllers();
builder.Services.AddControllersWithViews();


// --- CONFIGURAZIONE PIPELINE HTTP -- -

if (!BsonClassMap.IsClassMapRegistered(typeof(Option)))
{
    BsonClassMap.RegisterClassMap<Option>(cm =>
    {
        //cm.AutoMap(); // Mappa automaticamente tutte le altre proprietà (Text, IsCorrect)
        cm.SetIgnoreExtraElements(true);
        // Mappa esplicitamente la proprietà Id al campo BSON "Id"
        cm.MapProperty(c => c.Id).SetElementName("Id");
        cm.MapProperty(c => c.Text).SetElementName("Text");
        cm.MapProperty(c => c.IsCorrect).SetElementName("IsCorrect");
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Abilita il servizio di file statici (necessario per caricare CSS, JS)
app.UseStaticFiles();

// Abilita il routing
app.UseRouting();

app.UseAuthorization();

// 🔑 CORREZIONE CHIAVE 2: Definisce la rotta di default per l'applicazione MVC
// In questo modo, l'app sa come mappare l'URL base al tuo TestController e all'azione List.
app.MapControllerRoute(
    name: "default",
    // Imposta Test come controller di default e List come action di default (la tua Homepage)
    pattern: "{controller=Test}/{action=List}/{id?}");

// Mantieni app.MapControllers() se hai anche API Controller (per le rotte con [ApiController] come le tue /api/...)
app.MapControllers();

app.Run();