using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MobileProviderApi.Data;
using MobileProviderApi.Models;
using System.Text;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. SERVICES (Add services to the container)
// ---------------------------------------------------------

builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mobile Provider API", Version = "v1" });

    // Swagger Authorize Button (JWT)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Connection -------------------------------------
// SQLite kullanıyoruz - macOS'ta SQL Server LocalDB çalışmıyor
var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "MobileProviderDB.db");
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<MobileProviderContext>(options =>
    options.UseSqlite(connectionString));

// JWT Authentication ---------------------------------------
var jwtKey = builder.Configuration["JwtSettings:Key"] ?? "dev-secret-key-change-me";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "MobileProviderApiIssuer";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "MobileProviderClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// ---------------------------------------------------------
// 2. HTTP Pipeline
// ---------------------------------------------------------

// ● Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication → Authorization (correct order)
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Veritabanını otomatik oluştur (Development ortamında)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MobileProviderContext>();
        try
        {
            context.Database.EnsureCreated();
            // Örnek veri ekle
            if (!context.Subscribers.Any())
            {
                // Aboneler
                context.Subscribers.Add(new MobileProviderApi.Models.Subscriber 
                { 
                    SubscriberNo = 1, 
                    Name = "Ahmet", 
                    Surname = "Yılmaz" 
                });
                context.Subscribers.Add(new MobileProviderApi.Models.Subscriber 
                { 
                    SubscriberNo = 2, 
                    Name = "Ayşe", 
                    Surname = "Demir" 
                });
                context.Subscribers.Add(new MobileProviderApi.Models.Subscriber 
                { 
                    SubscriberNo = 3, 
                    Name = "Mehmet", 
                    Surname = "Kaya" 
                });
                context.SaveChanges();

                // Faturalar - SubscriberNo 1 (Ahmet Yılmaz)
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 1, 
                    Month = "2024-01", 
                    BillTotal = 250.75m, 
                    BillDetails = "Ocak ayı faturası - Data: 5GB, Konuşma: 500dk, SMS: 200", 
                    PaidStatus = "Unpaid", 
                    RemainingAmount = 250.75m 
                });
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 1, 
                    Month = "2024-02", 
                    BillTotal = 280.50m, 
                    BillDetails = "Şubat ayı faturası - Data: 6GB, Konuşma: 600dk, SMS: 250", 
                    PaidStatus = "Unpaid", 
                    RemainingAmount = 280.50m 
                });
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 1, 
                    Month = "2024-03", 
                    BillTotal = 195.25m, 
                    BillDetails = "Mart ayı faturası - Data: 4GB, Konuşma: 400dk, SMS: 150", 
                    PaidStatus = "Unpaid", 
                    RemainingAmount = 195.25m 
                });

                // Faturalar - SubscriberNo 2 (Ayşe Demir)
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 2, 
                    Month = "2024-01", 
                    BillTotal = 320.00m, 
                    BillDetails = "Ocak ayı faturası - Data: 8GB, Konuşma: 800dk, SMS: 300", 
                    PaidStatus = "Paid", 
                    RemainingAmount = 0m 
                });
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 2, 
                    Month = "2024-02", 
                    BillTotal = 350.00m, 
                    BillDetails = "Şubat ayı faturası - Data: 9GB, Konuşma: 900dk, SMS: 350", 
                    PaidStatus = "Partial", 
                    RemainingAmount = 150.00m 
                });
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 2, 
                    Month = "2024-03", 
                    BillTotal = 310.50m, 
                    BillDetails = "Mart ayı faturası - Data: 7GB, Konuşma: 700dk, SMS: 280", 
                    PaidStatus = "Unpaid", 
                    RemainingAmount = 310.50m 
                });

                // Faturalar - SubscriberNo 3 (Mehmet Kaya)
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 3, 
                    Month = "2024-01", 
                    BillTotal = 180.00m, 
                    BillDetails = "Ocak ayı faturası - Data: 3GB, Konuşma: 300dk, SMS: 100", 
                    PaidStatus = "Paid", 
                    RemainingAmount = 0m 
                });
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 3, 
                    Month = "2024-02", 
                    BillTotal = 200.00m, 
                    BillDetails = "Şubat ayı faturası - Data: 4GB, Konuşma: 350dk, SMS: 120", 
                    PaidStatus = "Paid", 
                    RemainingAmount = 0m 
                });
                context.Bills.Add(new Bill 
                { 
                    SubscriberNo = 3, 
                    Month = "2024-03", 
                    BillTotal = 190.75m, 
                    BillDetails = "Mart ayı faturası - Data: 3.5GB, Konuşma: 320dk, SMS: 110", 
                    PaidStatus = "Unpaid", 
                    RemainingAmount = 190.75m 
                });

                context.SaveChanges();
                Console.WriteLine("Örnek veriler başarıyla eklendi!");
            }
        }
        catch (Exception ex)
        {
            // Veritabanı oluşturma hatası - loglama yapılabilir
            Console.WriteLine($"Veritabanı oluşturma hatası: {ex.Message}");
        }
    }
}

app.Run();