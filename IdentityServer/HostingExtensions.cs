using Duende.IdentityServer;
using IdentityServerHost;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;

namespace IdentityServer;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(options => {
                options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
            });
        });

        builder.Services.AddIdentityServer()
            .AddInMemoryIdentityResources(Config.IdentityResources)
            .AddInMemoryApiScopes(Config.ApiScopes)
            .AddInMemoryClients(Config.Clients)
            .AddTestUsers(TestUsers.Users);

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services.AddAuthentication()
            .AddOpenIdConnect("oidc", "Demo IdentityServer", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                options.SaveTokens = true;

                options.Authority = "https://demo.duendesoftware.com";
                options.ClientId = "interactive.confidential";
                options.ClientSecret = "secret";
                options.ResponseType = "code";
                options.CallbackPath = "/signin-oidc";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                options.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        var redirectUri = new Uri(context.ProtocolMessage.RedirectUri);
                        context.ProtocolMessage.RedirectUri = new UriBuilder(context.ProtocolMessage.RedirectUri)
                        {
                            Scheme = "https",
                            Port = redirectUri.IsDefaultPort ? -1 : redirectUri.Port
                        }.ToString();
                        return Task.CompletedTask;
                    },
                };
            });
            // .AddMicrosoftIdentityWebApp(options => {
            //     builder.Configuration.Bind("AzureAd", options);
            //     options.Events = new OpenIdConnectEvents()
            //     {
            //         OnRedirectToIdentityProvider = context =>
            //         {
            //             var redirectUri = new Uri(context.ProtocolMessage.RedirectUri);
            //             context.ProtocolMessage.RedirectUri = new UriBuilder(context.ProtocolMessage.RedirectUri)
            //             {
            //                 Scheme = "https",
            //                 Port = redirectUri.IsDefaultPort ? -1 : redirectUri.Port
            //             }.ToString();
            //             return Task.CompletedTask;
            //         },
            //     };
            //     }, displayName: "Azure");;
            //if running in domain joined windows under httpsys
            // builder.Services.Configure<HttpSysOptions>(o =>
            // {
            //     o.Authentication.AuthenticationDisplayName = "Windows Auth";
            //     o.Authentication.AutomaticAuthentication = false;
            // });

        return builder.Build();
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    { 

        app.UseSerilogRequestLogging();
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseForwardedHeaders();
        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();

        app.UseCors();

        app.UseMiddleware<SameSiteExternalAuthStrictMiddleware>();
            
        app.UseIdentityServer();

        app.UseAuthorization();
        app.MapRazorPages().RequireAuthorization();

        return app;
    }
}