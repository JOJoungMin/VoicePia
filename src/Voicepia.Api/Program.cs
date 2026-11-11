using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Voicepia.Api.Gateways;
using Voicepia.Api.Models.Users;
using Voicepia.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();

// 로그인 쿠키 인증
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "voicepia.auth";
        options.Cookie.HttpOnly = true;                              // JavaScript에서 쿠키를 읽지 못하게
        options.Cookie.SameSite = SameSiteMode.Lax;                  // 다른 사이트에서 보낸 요청에는 쿠키를 안 붙임
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // 개발(http)에선 http, 배포(https)에선 https 전용
        options.ExpireTimeSpan = TimeSpan.FromDays(14);              // 14일 동안 로그인 유지
        options.SlidingExpiration = true;                            // 쓰는 동안은 만료가 계속 연장됨

        // API라서 로그인 페이지로 보내지 않고 상태 코드만 돌려준다
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// Gateway (데이터 계층)
builder.Services.AddScoped<UserGateway>();

// Service (업무 계층)
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // 요청의 쿠키를 읽어서 "누구인지" 확인
app.UseAuthorization();    // [Authorize]가 붙은 곳에 들어갈 수 있는지 확인

app.MapControllers();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
