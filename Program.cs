using SingleSlotTester.Services; 
using SingleSlotTester.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// 註冊服務，讓 Controller 可以透過建構子注入 _mappingService
builder.Services.AddScoped<FhirMappingService>(); 

var app = builder.Build();

app.UseDefaultFiles(); 
app.UseStaticFiles();
app.MapControllers();
app.Run();