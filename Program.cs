using EcomerceBE.Data;
using EcomerceBE.Service;
using EcomerceBE.Service.auth;
using EcomerceBE.Service.user;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OfficeOpenXml;
using EcomerceBE.Service.flashSale;
using EcomerceBE.Service.ModelAI;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Cấu hình DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ✅ JWT Auth
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// ✅ CORS - cấu hình cẩn thận
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });

    //// Nếu muốn dùng policy riêng cho localhost:3000, kích hoạt
    //options.AddPolicy("AllowLocalhost3000", policy =>
    //{
    //    policy.WithOrigins("http://localhost:3000")
    //          .AllowAnyHeader()
    //          .AllowAnyMethod();
    //});
});

// ✅ DI Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFlashSaleService, FlashSaleService>();
builder.Services.AddHostedService<EcomerceBE.Background.FlashSaleCleanupService>();
builder.Services.AddHostedService<EcomerceBE.Background.EmbeddingGenerationService>();

// ✅ ModelAI Services
builder.Services.AddHttpClient(); // For EmbeddingService to call AI Model API
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// ✅ Payment Services
builder.Services.AddScoped<EcomerceBE.Service.Payment.IMoMoPaymentService, EcomerceBE.Service.Payment.MoMoPaymentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Dùng FullName để tránh trùng schemaId cho các DTO lồng nhau (OrderController+UpdateOrderStatusDto vs CheckOutController+UpdateOrderStatusDto)
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    // Cấu hình Bearer token cho Swagger UI
    // Sử dụng chuẩn HTTP Bearer để Swagger tự thêm tiền tố "Bearer "
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Nhập access token (không cần gõ 'Bearer ')",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
builder.Services.Configure<AppUrls>(builder.Configuration.GetSection("AppUrls")); /// url of frontend
ExcelPackage.License.SetNonCommercialPersonal("ecomercebe");
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.SeedAdminUsers();
    context.EnsureReviewImagesTable(); // Auto-create ReviewImages table if it doesn't exist
}


// ✅ Middleware gọi theo thứ tự chính xác
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseStaticFiles(); // serve wwwroot (e.g. /assets/logo/logo-email.png)

// Chỉ gọi 1 lần UseCors:
// Nếu dùng chính sách mặc định:
app.UseCors();

// Nếu dùng chính sách "AllowLocalhost3000":
// app.UseCors("AllowLocalhost3000");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine($"[DEBUG] JWT Key in use: {builder.Configuration["Jwt:Key"]}");

app.Run();
