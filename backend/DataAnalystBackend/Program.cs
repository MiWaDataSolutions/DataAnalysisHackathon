using DataAnalystBackend.Hubs;
using DataAnalystBackend.Shared.DataAccess;
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.Services;
using DataAnalystBackend.Shared.Services.RPC;
using Microsoft.AspNetCore.Authentication.Cookies; // Added
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.IO.Compression;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using tusdotnet;
using tusdotnet.Models; // Added for ClaimTypes

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("https://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .WithExposedHeaders(new[] { "Location", "Upload-Offset", "Upload-Length" });
                      });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/google-login"; // Path to trigger Google login
        options.LogoutPath = "/auth/logout";
        // options.Cookie.HttpOnly = true;
        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;

        // Prevent redirects for API requests, return 401 instead
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"];
        options.ClientSecret = builder.Configuration["Google:ClientSecret"];
        // options.CallbackPath = "/signin-google"; // Default is /signin-google
        options.SaveTokens = true; // Save access and refresh tokens in the cookie
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var googleId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
                var name = context.Principal?.FindFirstValue(ClaimTypes.Name);
                var picture = context.Principal?.FindFirstValue("urn:google:picture") ?? context.Principal?.FindFirstValue("picture");

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("GoogleAuth.OnCreatingTicket");

                if (string.IsNullOrEmpty(googleId))
                {
                    logger.LogError("GoogleId (NameIdentifier claim) is missing from Google principal.");
                    context.Fail("GoogleId is missing from claims. Cannot process user.");
                    return;
                }

                var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

                if (existingUser == null)
                {
                    logger.LogInformation("First-time Google login for ID: {GoogleId}. Email: {Email}. Creating user.", googleId, email);
                    var newUser = new User
                    {
                        GoogleId = googleId,
                        Email = email ?? string.Empty,
                        Name = name ?? string.Empty,
                        ProfilePictureUrl = picture ?? string.Empty,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    dbContext.Users.Add(newUser);
                    try
                    {
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation("New user with GoogleID: {GoogleId} created successfully.", googleId);
                    }
                    catch (DbUpdateException ex)
                    {
                        logger.LogError(ex, "Error saving new user with GoogleID: {GoogleId} to the database.", googleId);
                        context.Fail("Failed to save new user to database. Please try again.");
                        return;
                    }
                }
                else
                {
                    logger.LogInformation("Returning Google login for ID: {GoogleId}. Email: {Email}. User found.", googleId, email);
                    // Optional: Update user properties if needed, e.g., last login time or profile picture if changed.
                     existingUser.ProfilePictureUrl = picture ?? existingUser.ProfilePictureUrl; // Example update
                     existingUser.LastLoginAtUtc = DateTime.UtcNow; // If you have such a field
                    if (dbContext.Entry(existingUser).State != EntityState.Unchanged) // Check if there were changes
                    {
                        await dbContext.SaveChangesAsync();
                    }
                }
                // The context.Principal is what gets serialized into the cookie.
                // If you need to add custom claims to your application's cookie from here, you can modify context.Principal.Identity.
            }
        };
    });

builder.Services.AddAuthorization(); // Ensure Authorization services are added

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Data Analyst API",
        Description = "API for the Data Analyst project."
    });

    // Configure XML comments
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    // Define JWT Bearer security scheme
    options.AddSecurityDefinition("BearerAuth", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "BearerAuth"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSignalR();

// Add Services
builder.Services.AddTransient<IDataSessionService, DataSessionService>();
builder.Services.AddScoped<RpcClient>();

var app = builder.Build();

ServiceProviderAccessor.RootServiceProvider = app.Services;
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Data Analysis Hackathon API v1");
    });
}
app.UseCors(MyAllowSpecificOrigins);

app.UseHttpsRedirection();

app.UseAuthentication(); // Authentication middleware before Authorization
app.UseAuthorization();

app.MapTus("/files", async httpContext => new()
{
    // This method is called on each request so different configurations can be returned per user, domain, path etc.
    // Return null to disable tusdotnet for the current request.

    // Where to store data?
    Store = new tusdotnet.Stores.TusDiskStore(@"C:\tusfiles\"),
    Events = new()
    {
        // What to do when file is completely uploaded?
        OnFileCompleteAsync = async eventContext =>
        {
            tusdotnet.Interfaces.ITusFile file = await eventContext.GetFileAsync();
            Dictionary<string, Metadata> metadata = await file.GetMetadataAsync(eventContext.CancellationToken);
            using Stream content = await file.GetContentAsync(eventContext.CancellationToken);
            ApplicationDbContext db = eventContext.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            await ProcessFile(content, metadata, db);
        }
    }
});

app.MapControllers();
app.MapHub<DataSessionHub>("/data-session-hub");


await app.RunAsync();

async Task ProcessFile(Stream content, Dictionary<string, Metadata> metadata, ApplicationDbContext db)
{
    Guid dataSessionId = Guid.Parse(metadata["dataSessionId"].GetString(Encoding.UTF8));
    string userId = metadata["userId"].GetString(Encoding.UTF8);
    string fileName = metadata["filename"].GetString(Encoding.UTF8);
    
    User? user = await db.Users.SingleOrDefaultAsync(o => o.GoogleId == userId);
    if (user == null)
    {
        // Log Somewhere
        return;
    }

    DataSession? dataSession = await db.DataSessions.SingleOrDefaultAsync(o => o.Id == dataSessionId &&  o.UserId == userId);
    if (dataSession == null)
    {
        // Log Somewhere
        return;
    }

    Guid dataSessionFileId = Guid.NewGuid();
    byte[] compressed;
    using (MemoryStream ms = new MemoryStream())
    {
        using (var compressor = new GZipStream(ms, CompressionMode.Compress))
        {
            await content.CopyToAsync(compressor);
                
        }
        compressed = ms.ToArray();
    }

    DataSessionFile dataSessionFile = new DataSessionFile()
    {
        CreatedAt = DateTime.UtcNow,
        DataSessionId = dataSessionId,
        FileData = compressed,
        Filename = fileName,
        UpdatedAt = DateTime.UtcNow,
        Id = dataSessionFileId,
    };

    await db.DataSessionsFiles.AddAsync(dataSessionFile);
    await db.SaveChangesAsync();
}
public static class ServiceProviderAccessor
{
    public static IServiceProvider RootServiceProvider { get; set; }
}