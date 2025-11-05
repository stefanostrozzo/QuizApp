//author: Stefano Strozzo <strozzostefano@gmail.com>

using Quiz_Task.Models;
using Quiz_Task.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURAZIONE SERVIZI ---

// Add MongoDB configuration from settings.json
builder.Configuration.AddJsonFile("settings.json", optional: false, reloadOnChange: true);

var mongoSettings = new MongoDbSettings();
builder.Configuration.GetSection("MongoDbSettings").Bind(mongoSettings);

// 1. Registra MongoDbSettings come Singleton (corretto)
builder.Services.AddSingleton(mongoSettings);

// 2. Registra i Repository con DI (Corretto)
// Adesso i costruttori accetteranno MongoDbSettings, che il container sa come fornire.
builder.Services.AddSingleton<ITestRepository, MongoTestRepository>();
builder.Services.AddSingleton<IUserSessionRepository, MongoUserSessionRepository>();


// Abilita i Controller, le View (CSHTML) e le funzionalità correlate ad MVC
builder.Services.AddControllersWithViews();

// --- CONFIGURAZIONE PIPELINE HTTP ---

var app = builder.Build();

app.UseHttpsRedirection();

// Abilita il servizio di file statici (necessario per CSS, JS e immagini)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Definisce la rotta di default per l'applicazione MVC (es. /Test/List)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Test}/{action=List}/{id?}"); // Imposta TestController come default

app.Run();