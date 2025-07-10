using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SM_BE.Data;
using SM_BE.Services;
using SM_BE.Hubs;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Add JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // For regular HTTP requests, read token from cookie
                var token = context.Request.Cookies["access_token"];
                
                // For SignalR connections, also check cookies
                if (string.IsNullOrEmpty(token) && 
                    (context.Request.Path.StartsWithSegments("/zero-blast") || 
                     context.Request.Headers["Connection"].ToString().Contains("Upgrade")))
                {
                    // Try to get token from cookie for SignalR
                    token = context.Request.Cookies["access_token"];
                    
                    // Fallback: check query string (if frontend sends it)
                    if (string.IsNullOrEmpty(token))
                    {
                        token = context.Request.Query["access_token"];
                    }
                }
                
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for cookies
    });
});

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Use CORS before authorization and endpoints
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<GameHub>("/zero-blast");

app.Run();
