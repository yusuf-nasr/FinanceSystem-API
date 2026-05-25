using Npgsql;
using System.Text;
using System.Text.Json.Serialization;
using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Filters;
using FinanceSystem_Dotnet.Services;
using FinanceSystem_Dotnet.Transformers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FinanceSystem_Dotnet
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ApiExceptionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            var nameTranslator = new Npgsql.NameTranslation.NpgsqlNullNameTranslator();
            dataSourceBuilder.MapEnum<UserPresence>("UserPresence", nameTranslator);
            dataSourceBuilder.MapEnum<Role>("UserRole", nameTranslator);
            dataSourceBuilder.MapEnum<TransactionPriority>("TransactionPriority", nameTranslator);
            dataSourceBuilder.MapEnum<TransactionForwardStatus>("TransactionForwardStatus", nameTranslator);
            dataSourceBuilder.MapEnum<NotificationType>("NotificationType", nameTranslator);
            var dataSource = dataSourceBuilder.Build();

            builder.Services.AddDbContext<FinanceDbContext>(options =>
                options.UseLazyLoadingProxies().UseNpgsql(dataSource, o =>
                {
                    o.MapEnum<UserPresence>("UserPresence", nameTranslator: nameTranslator);
                    o.MapEnum<Role>("UserRole", nameTranslator: nameTranslator);
                    o.MapEnum<TransactionPriority>("TransactionPriority", nameTranslator: nameTranslator);
                    o.MapEnum<TransactionForwardStatus>("TransactionForwardStatus", nameTranslator: nameTranslator);
                    o.MapEnum<NotificationType>("NotificationType", nameTranslator: nameTranslator);
                }));

            // OpenAPI document generation with Bearer auth
            builder.Services.AddOpenApi(options =>
            {
                options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            // CORS — matching Node's ALLOWED_ORIGINS
            var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",") ?? new[] { "*" };
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    if (allowedOrigins.Contains("*"))
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    else
                        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });

            // Configure JWT authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };

                // Support token from query string for SSE connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["token"];
                        if (!string.IsNullOrEmpty(token) && context.Request.Path.StartsWithSegments("/api/v0/sse"))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var response = new
                        {
                            statusCode = 401,
                            message = new { key = "UNAUTHORIZED" },
                            error = "Unauthorized"
                        };
                        await context.Response.WriteAsJsonAsync(response);
                    }
                };
            });

            // Register services
            builder.Services.AddScoped<IFinanceService, Services.Services>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IDepartmentService, DepartmentService>();
            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<ITransactionForwardService, TransactionForwardService>();
            builder.Services.AddScoped<ITransactionTypeService, TransactionTypeService>();
            builder.Services.AddScoped<IBudgetCategoryService, BudgetCategoryService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            // SSE service is singleton (maintains active connections across requests)
            builder.Services.AddSingleton<ISseService, SseService>();

            var app = builder.Build();

            // Reset all users to OFFLINE on startup (matching Node's SseService.onModuleInit)
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    await userService.ResetAllPresenceAsync();

                    var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
                    if (!dbContext.Users.Any(u => u.Name == "admin"))
                    {
                        if (!dbContext.Departments.Any(d => d.Name == "Administration"))
                        {
                            dbContext.Departments.Add(new FinanceSystem_Dotnet.Models.Department
                            {
                                Name = "Administration"
                            });
                            await dbContext.SaveChangesAsync();
                        }

                        dbContext.Users.Add(new FinanceSystem_Dotnet.Models.User
                        {
                            Name = "admin",
                            HashedPassword = Isopoh.Cryptography.Argon2.Argon2.Hash("password"),
                            Role = FinanceSystem_Dotnet.Enums.Role.ADMIN,
                            Active = true,
                            CreatedAt = DateTime.UtcNow,
                            Presence = FinanceSystem_Dotnet.Enums.UserPresence.OFFLINE,
                            DepartmentName = "Administration"
                        });
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Startup seeding/reset failed: {ex.Message}");
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "Finance System API v1");
                });
            }

            app.UseCors();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.RunAsync();
        }
    }
}
