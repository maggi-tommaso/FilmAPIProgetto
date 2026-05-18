var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com https://cdnjs.cloudflare.com https://kit.fontawesome.com; style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.tailwindcss.com; img-src 'self' data: https:; font-src 'self' https://cdnjs.cloudflare.com https://kit.fontawesome.com; connect-src 'self' http://localhost:* https:; frame-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'";
    headers["X-Frame-Options"] = "DENY";
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), interest-cohort=()";

    await next();
});

app.UseStaticFiles();
app.UseDefaultFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
