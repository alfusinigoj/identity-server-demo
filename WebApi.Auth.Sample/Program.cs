using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using WebApi.Auth.Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        string AuthorityUrl = "https://identity-server.agreeablepond-3914c6b0.canadacentral.azurecontainerapps.io";
        string AuthorizationUrl = $"{AuthorityUrl}/connect/authorize";
        string TokenUrl = $"{AuthorityUrl}/connect/token";
        string scopes = "openid,profile,verification";

        var builder = WebApplication.CreateBuilder(args);

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(options =>
            {
                options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
            });
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            //==========================
            // Jwt Bearer 
            //==========================
            // c.OperationFilter<SecurityRequirementsOperationFilter>();
            // c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            // {

            //     Type = SecuritySchemeType.OAuth2,
            //     Flows = new OpenApiOAuthFlows()
            //     {
            //         AuthorizationCode = new OpenApiOAuthFlow()
            //         {
            //             AuthorizationUrl = new Uri(AuthorizationUrl),
            //             TokenUrl = new Uri(TokenUrl),
            //             Scopes = scopes.Split(',')?.ToDictionary(scope => scope, scope => scope)
            //         }
            //     },
            // });
            //==========================
        });

        //==========================
        // Jwt Bearer 
        //==========================
        // builder.Services.AddAuthentication(options =>
        // {
        //     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        // })
        // .AddJwtBearer("Bearer", options =>
        // {
        //     options.SaveToken = true;
        //     options.Authority = AuthorityUrl;
        //     options.TokenValidationParameters = new TokenValidationParameters
        //     {
        //         ValidateAudience = false,
        //     };
        // });
        //==========================

        //==========================
        // OpenId Connect
        //==========================
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie("Cookies")
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = AuthorityUrl;
            options.ClientId = "web";
            options.ClientSecret = "secret";
            options.ResponseType = "code";
            options.Scope.Clear();
            foreach (var scope in scopes.Split(','))
                options.Scope.Add(scope);
            options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;
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
        //==========================

        var app = builder.Build();

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = string.Empty;
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi.Auth.Sample");

            //==========================
            // Jwt Bearer 
            //==========================
            // options.OAuthClientId("web");
            // options.OAuthClientSecret("secret");
            // options.OAuthScopes(scopes.Split(','));
            //==========================

            //==========================
            // OpenId Connect
            //==========================
            app.Use(async (context, next) =>
            {
                if (!context.User.Identity.IsAuthenticated)
                    await context.ChallengeAsync();
                else
                    await next();
            });
            //==========================
        });

        app.Run();
    }
}