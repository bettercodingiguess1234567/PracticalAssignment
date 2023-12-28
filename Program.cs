using Ganss.XSS;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using PracticalAssignment.Model;
using PracticalAssignment.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnectionString")));


builder.Services.AddTransient<IEmailSender, SmtpEmailSender>(i =>
    new SmtpEmailSender(
        builder.Configuration["SmtpSettings:Host"],
        builder.Configuration.GetValue<int>("SmtpSettings:Port"),
        builder.Configuration["SmtpSettings:Username"],
        builder.Configuration["SmtpSettings:Password"],

        i.GetRequiredService<ILogger<SmtpEmailSender>>()
    ));


//HandSanitizer for XSS attacks
builder.Services.AddSingleton<HtmlSanitizer>();


builder.Services.AddIdentity<ApplicationUserStuff, IdentityRole>(options =>
{

    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;

    //LOCKOUT
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 3; // Number of failed access attempts before an account gets locked out
}

).AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders(); // Register the 'Default' token provider



builder.Services.ConfigureApplicationCookie(Config =>
{
    Config.LoginPath = "/Login";
});


//SESSION
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddDistributedMemoryCache(); //save session in memory
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(50); //for Testing
    options.Cookie.HttpOnly = true; // Make the session cookie inaccessible to client-side scripts
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Secure cookie should only be transmitted over HTTPS
    options.Cookie.IsEssential = true; // Make the session cookie essential for the application
    options.Cookie.SameSite = SameSiteMode.Strict; // Strictly prevent sending cookies along with cross-site requests
});

//CCOKIES
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true; // Prevent access to cookie via client-side scripts
    options.ExpireTimeSpan = TimeSpan.FromSeconds(50); //same time as the Session timeout
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Only send the cookie over HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict; // Prevent the cookie from being sent in cross-site requests
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.SlidingExpiration = true;

});

// Register IHttpClientFactory for Captcha
builder.Services.AddHttpClient();





var app = builder.Build();


await CleanupStaleSessions(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


//CSP for XSS Attacks
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
                                 "default-src 'self'; " +
                                 "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com https://www.gstatic.com; " +
                                 "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                                 "font-src 'self' https://fonts.gstatic.com; " +
                                 "img-src 'self' data:; " +
                                 "frame-src 'self' https://www.google.com; " +
                                 "connect-src 'self' wss://localhost:44331 wss://localhost:44332 wss://localhost:44356");
    await next();
});


app.MapRazorPages();


app.Run();




async Task CleanupStaleSessions(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var staleSessions = await context.ActiveSessions
        .Where(s => s.ExpiresAt <= DateTime.UtcNow)
        .ToListAsync();
    context.ActiveSessions.RemoveRange(staleSessions);
    await context.SaveChangesAsync();
}