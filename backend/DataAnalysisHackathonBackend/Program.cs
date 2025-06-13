using Microsoft.AspNetCore.Authentication.Cookies; // Added
using Microsoft.AspNetCore.Authentication.Google; // Added
// using Microsoft.AspNetCore.Authentication.JwtBearer; // Commented out/Removed
// using Microsoft.IdentityModel.Tokens; // Commented out/Removed (related to JWT Bearer)
using Microsoft.EntityFrameworkCore;
using DataAnalysisHackathonBackend.Data;
using DataAnalysisHackathonBackend.Models; // Added for User model
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims; // Added for ClaimTypes
using Microsoft.AspNetCore.Authentication.OAuth; // Added for OAuthEvents
using Microsoft.Extensions.Logging; // Added for ILoggerFactory

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Comment out or remove old JWT Bearer authentication if it was solely for direct Google ID token validation
/*
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://accounts.google.com";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://accounts.google.com",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Google:ClientId"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });
*/

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/google-login"; // Path to trigger Google login
        options.LogoutPath = "/auth/logout";
        // options.Cookie.HttpOnly = true;
        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // options.Cookie.SameSite = SameSiteMode.Lax;
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
                    // existingUser.ProfilePictureUrl = picture ?? existingUser.ProfilePictureUrl; // Example update
                    // existingUser.LastLoginAtUtc = DateTime.UtcNow; // If you have such a field
                    // if(dbContext.Entry(existingUser).State != EntityState.Unchanged) // Check if there were changes
                    // {
                    //    await dbContext.SaveChangesAsync();
                    // }
                }
                // The context.Principal is what gets serialized into the cookie.
                // If you need to add custom claims to your application's cookie from here, you can modify context.Principal.Identity.
            }
        };
    });

// using Microsoft.OpenApi.Models; // Moved to top
// using System.Reflection; // Moved to top

builder.Services.AddAuthorization(); // Ensure Authorization services are added

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Data Analysis Hackathon API",
        Description = "API for the Data Analysis Hackathon project."
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Data Analysis Hackathon API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Authentication middleware before Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();
