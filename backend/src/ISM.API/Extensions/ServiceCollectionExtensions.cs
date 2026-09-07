using System.Text;
using System.Security.Claims;
using ISM.API.Security;
using ISM.Application.Interfaces;
using ISM.Application.Interfaces.DataImport;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Application.Services;
using ISM.Application.Services.Auth;
using ISM.Application.Services.Menu;
using ISM.Application.Services.Stock;
using ISM.Application.Services.Suppliers;
using ISM.Application.Services.Users;
using ISM.Application.Services.Tenants;
using ISM.Application.Services.DataImport;
using ISM.Application.Services.DataImport.Readers;
using ISM.Infrastructure.Data.Options;
using ISM.Infrastructure.DependencyInjection;
using ISM.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace ISM.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ISM - Intelligence Supply Manager API",
                Version = "v1",
                Description = "API SaaS de gestão inteligente de suprimentos para restaurantes"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        var databaseOptions = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(databaseOptions);
        databaseOptions.ConnectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? databaseOptions.ConnectionString;

        services.AddSingleton(databaseOptions);

        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(IsmPolicies.SuperAdminOnly, policy =>
                policy.RequireRole(IsmRoles.Admin)
                      .RequireAssertion(ctx =>
                          string.IsNullOrEmpty(ctx.User.FindFirstValue("restaurantId"))))
            .AddPolicy(IsmPolicies.RestaurantManagerOrAbove, policy =>
                policy.RequireRole(IsmRoles.Admin, IsmRoles.Manager))
            .AddPolicy(IsmPolicies.RestaurantAnyUser, policy =>
                policy.RequireRole(IsmRoles.Admin, IsmRoles.Manager, IsmRoles.Chef, IsmRoles.Waiter));

        var rawCorsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? configuration["CORS_ALLOWED_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? ["http://localhost:3000", "http://frontend:3000"];

        var allowedOriginSet = new HashSet<string>(rawCorsOrigins, StringComparer.OrdinalIgnoreCase);

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrWhiteSpace(origin))
                        return false;

                    if (allowedOriginSet.Contains(origin))
                        return true;

                    if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    {
                        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                            uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                            uri.Host.Equals("frontend", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return false;
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            });
        });

        services.AddScoped<ICurrentUser, CurrentUserApi>();
        services.AddScoped<IPlanEnforcer, PlanEnforcer>();
        services.AddInfrastructure(databaseOptions);
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IDishService, DishService>();
        services.AddScoped<IRestaurantService, RestaurantService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDevAuthService, DevAuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IImportOrchestrator, ImportOrchestrator>();
        services.AddScoped<IFileImporter, CsvProductImporter>();
        services.AddScoped<IFileImporter, CsvSupplierImporter>();

        // Categorias reconhecidas na análise de planilhas (dry-run / confirmação).
        // Para uma nova categoria: implemente IImportCategoryProfile e registre aqui.
        services.AddScoped<IImportCategoryProfile, StockCategoryProfile>();
        services.AddScoped<IImportCategoryProfile, SuppliersCategoryProfile>();
        services.AddScoped<IImportCategoryProfile, FinanceCategoryProfile>();
        services.AddScoped<ICsvStructureAnalyzer, CsvStructureAnalyzer>();

        // Leitores de formato (CSV, XLSX, XML NF-e, SpreadsheetML, JSON) + resolver
        services.AddScoped<IImportFileReader, CsvImportFileReader>();
        services.AddScoped<IImportFileReader, XlsxImportFileReader>();
        services.AddScoped<IImportFileReader, NFeXmlImportFileReader>();
        services.AddScoped<IImportFileReader, SpreadSheetMlImportFileReader>();
        services.AddScoped<IImportFileReader, JsonWebhookImportFileReader>();
        services.AddScoped<IImportFileReaderResolver, ImportFileReaderResolver>();

        // Importação por foto/PDF escaneado (visão computacional)
        services.Configure<PhotoImportOptions>(configuration.GetSection(PhotoImportOptions.SectionName));
        services.AddHttpClient<IPhotoImportExtractor, LlmVisionPhotoExtractor>();
        services.AddScoped<IPhotoImportService, PhotoImportService>();

        return services;
    }
}
