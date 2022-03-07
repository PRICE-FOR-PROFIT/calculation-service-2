using System.IdentityModel.Tokens.Jwt;
using Insight.Support.Standard.Services.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

const string audience = "ces";
const string schemeSelectorScheme = "SchemeSelector";
const string identityServerScheme = "Identity Server";
const string azureActiveDirectoryScheme = "Azure Active Directory";
const string azureActiveDirectoryB2CScheme = "Azure Active Directory B2C";

var enableSecurity = !builder.Configuration.GetValue<bool>("disableSecurity");

if (enableSecurity)
{
//https://github.com/aspnet/Security/issues/1469
    var identityServerAuthority = builder.Configuration.GetValue<string>("IdentityAuthority");

    var azureB2CTenantId = builder.Configuration.GetValue<string>("AzureB2CTenantId");
    var azureAdAudience = builder.Configuration.GetValue<string>("AzureAdAudience");
    var azureAdAuthority = builder.Configuration.GetValue<string>("AzureAdAuthority");
    var azureB2CMetaDataAddress = builder.Configuration.GetValue<string>("AzureB2CMetaDataAddress");
    var azureClientId = builder.Configuration.GetValue<string>("AzureClientId");

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = identityServerScheme;
            options.DefaultAuthenticateScheme = schemeSelectorScheme;
        })
        .AddPolicyScheme(schemeSelectorScheme, schemeSelectorScheme, options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                try
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(authHeader))
                    {
                        //if (context.Request.Path.ToString().Contains("/health", StringComparison.CurrentCultureIgnoreCase) || context.Request.Path.ToString().Contains("/ping", StringComparison.CurrentCultureIgnoreCase)) return IdentityServerScheme;

                        //var telemetryClient = context.RequestServices.GetService<TelemetryClient>();

                        //var properties = new Dictionary<string, string>
                        //{
                        //    ["RequestHeaders"] = JsonConvert.SerializeObject(context.Request.Headers)
                        //};

                        //telemetryClient.TrackException(new Exception("Authorization was null"), properties);

                        return identityServerScheme;
                    }

                    var jwt = authHeader.Split(" ")[1];

                    var handler = new JwtSecurityTokenHandler();

                    var token = handler.ReadJwtToken(jwt);

                    if (token.Issuer.Contains("https://sts.windows.net") && token.Issuer.Contains(azureB2CTenantId)) return azureActiveDirectoryScheme;

                    if (token.Issuer.Contains(azureB2CTenantId)) return azureActiveDirectoryB2CScheme;

                    return identityServerScheme;
                }
                catch
                {
                    return identityServerScheme;
                }
            };
        })
        .AddJwtBearer(identityServerScheme, options =>
        {
            options.Authority = identityServerAuthority;
            options.Audience = audience;
            options.Events = new JwtBearerEvents
            {
                //OnAuthenticationFailed = async context =>
                //{
                //    await TrackAuthorizationException(IdentityServerScheme, context).ConfigureAwait(false);
                //}
            };
        })
        .AddJwtBearer(azureActiveDirectoryScheme, options => //Client Credential Workflow
        {
            options.Authority = azureAdAuthority;
            options.Audience = azureAdAudience;
            options.Events = new JwtBearerEvents
            {
                //OnAuthenticationFailed = async context =>
                //{
                //    await TrackAuthorizationException(AzureActiveDirectoryScheme, context).ConfigureAwait(false);
                //}
            };
        })
        .AddJwtBearer(azureActiveDirectoryB2CScheme, options => //Azure B2C User Workflow
        {
            options.MetadataAddress = azureB2CMetaDataAddress;
            options.Audience = azureClientId;
            options.Events = new JwtBearerEvents
            {
                //OnAuthenticationFailed = async context =>
                //{
                //    await TrackAuthorizationException(AzureActiveDirectoryB2CScheme, context).ConfigureAwait(false);
                //}
            };
        });

    builder.Services.AddSingleton<IAuthorizationHandler, InsightPermissionHandler>();
}

builder.Services.AddControllers(options =>
{
    if (!enableSecurity) return;

    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "ces.global.execute")
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddHttpContextAccessor();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (enableSecurity)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
