using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using MME.Domain;
using MME.Domain.Common.Extensions;
using MME.Domain.Common.Options;
using MME.Domain.Middlewares;
using MME.Domain.Services;
using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components.Authorization;
using MME.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAntDesign();
builder.Services.AddHttpClient();

// 添加认证服务 - 配置Cookie认证作为默认方案
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/user/login";
        options.LogoutPath = "/user/logout";
        options.AccessDeniedPath = "/user/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// 添加自定义认证状态提供程序
builder.Services.AddScoped<AuthenticationStateProvider, MMEAuthProvider>();

// 配置授权策略 - 移除全局策略，改为页面级控制
builder.Services.AddAuthorizationCore(options =>
{
    // 可以在这里添加自定义策略，但不设置FallbackPolicy
    options.AddPolicy("RequireAuthenticated", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(sp.GetService<NavigationManager>()!.BaseUri)
});

// 配置ProSettings
builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));

// 配置管理员选项
builder.Services.Configure<AdminOption>(builder.Configuration.GetSection("Admin"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加Yarp反向代理
builder.Services.AddReverseProxy();

// 添加 SqlSugar 数据库服务
builder.Services.AddSqlSugar(builder.Configuration);

builder.Services.AddServicesFromAssemblies("MME", "MME.Domain");

// 绑定配置选项
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

// 添加认证和授权中间件
app.UseAuthentication();
app.UseAuthorization();

// 添加代理日志中间件
app.UseMiddleware<ProxyLoggingMiddleware>();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();

app.CodeFirst();

app.Run();
