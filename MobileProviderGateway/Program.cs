using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MobileProviderGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

//-------------------------------------------------------------
// 1. SERVİSLERİN EKLEMESİ (Add Services)
//-------------------------------------------------------------

builder.Services.AddControllers();

// HttpClient Factory - Downstream API istekleri için
builder.Services.AddHttpClient();

// JWT Kimlik Doğrulama Servislerini Gateway'e Ekleme
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Önemli Not: Bu ayarlar MobileProviderApi/appsettings.json ile aynı olmalıdır.
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, 
        ValidateAudience = true, 
        ValidateLifetime = true, 
        ValidateIssuerSigningKey = true, 

        ValidIssuer = "MobileProviderApiIssuer",
        ValidAudience = "MobileProviderClients",
        // Anahtar: MobileProviderApi'deki key ile aynı olmalıdır!
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MobileProviderProject_JwtSigningKey_EnAz256BitOlmalidir"))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

//-------------------------------------------------------------
// 2. HTTP İSTEK İŞLEME HATTININ YAPILANDIRILMASI (HTTP Pipeline)
//-------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Authentication → Authorization (correct order)
// ÖNEMLİ: Authentication rate limiting'den önce olmalı ki JWT token doğrulansın
app.UseAuthentication();
app.UseAuthorization();

// Custom Middleware'ler
// 1. Rate Limiting Middleware (authentication'dan sonra çalışmalı)
app.UseMiddleware<RateLimitingMiddleware>();

// 2. API Gateway Middleware (request forwarding)
app.UseMiddleware<ApiGatewayMiddleware>();

// Controllers (eğer gateway'de kendi endpoint'lerimiz olursa)
app.MapControllers();

app.Run();
