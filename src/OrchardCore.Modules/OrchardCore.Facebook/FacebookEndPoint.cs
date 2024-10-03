using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using System.Text.Json.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.Modules;
using OrchardCore.Settings;
using System.Net.Http;
using OrchardCore.Facebook.Settings;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OrchardCore.Facebook;
internal static class FacebookEndPoint
{
    public static IEndpointRouteBuilder GetEndPoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("orchardcore/facebook/sdk", HandleAsync)
            .AllowAnonymous()
            .DisableAntiforgery();

        return builder;
    }

    [Authorize(AuthenticationSchemes = "Api")]
    private static async Task<IResult> HandleAsync(
        ContentItem model,
        ISiteService siteService, //Is this how one works with the dependancy injection framework?
        HttpContext httpContext,
        bool draft = false)
    {
        var script = await getScript(httpContext, siteService);

        if (script != null)
        {
            var bytes = Encoding.UTF8.GetBytes(script);
            await httpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(script).AsMemory(0, bytes.Length), httpContext.RequestAborted);

            return Results.Ok(); //is this the right status codes?
        }
        else
        {
            return Results.NotFound(); //is this the right status codes?
        }
    }

    private static async Task<string> getScript(HttpContext httpContext, ISiteService siteService)
    {
        var script = default(string);
        var settings = await siteService.GetSettingsAsync<FacebookSettings>();

        if (Path.GetFileName(httpContext.Request.Path.Value) == "fbsdk.js")
        {
            var locale = CultureInfo.CurrentUICulture.Name.Replace('-', '_');
            script = $@"(function(d){{
                        var js, id = 'facebook-jssdk'; if (d.getElementById(id)) {{ return; }}
                        js = d.createElement('script'); js.id = id; js.async = true;
                        js.src = ""https://connect.facebook.net/{locale}/{settings.SdkJs}"";
                        d.getElementsByTagName('head')[0].appendChild(js);
                    }} (document));";
        }
        else if (Path.GetFileName(httpContext.Request.Path.Value) == "fb.js")
        {
            if (!string.IsNullOrWhiteSpace(settings?.AppId))
            {
                var options = $"{{ appId:'{settings.AppId}',version:'{settings.Version}'";
                options = string.IsNullOrWhiteSpace(settings.FBInitParams)
                    ? string.Concat(options, "}")
                    : string.Concat(options, ",", settings.FBInitParams, "}");

                script = $"window.fbAsyncInit = function(){{ FB.init({options});}};";
            }
        }

        return script;
    }
}

