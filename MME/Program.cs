using MME.Domain;
using MME.Domain.Common.Extensions;
using MME.Domain.Common.Options;
using MME.Domain.Services;
using MME.Domain.Middlewares;
using Microsoft.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAntDesign();
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(sp.GetService<NavigationManager>()!.BaseUri)
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加Yarp反向代理
builder.Services.AddReverseProxy();

builder.Services.AddServicesFromAssemblies("MME", "MME.Domain");

builder.Configuration.GetSection("DBConnection").Get<DBConnectionOption>();
builder.Configuration.GetSection("OpenAI").Get<OpenAIOption>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseRouting();

// 添加代理日志中间件
app.UseMiddleware<ProxyLoggingMiddleware>();

app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();

app.CodeFirst();

app.Run();
