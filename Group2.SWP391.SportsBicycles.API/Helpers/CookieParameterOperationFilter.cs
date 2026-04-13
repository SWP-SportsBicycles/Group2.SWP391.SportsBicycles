using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Group2.SWP391.SportsBicycles.API.Helpers
{
    public class CookieParameterOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.RelativePath?.Contains("Auth/logout") == true)
            {
                operation.Parameters ??= new List<OpenApiParameter>();
                operation.Parameters.Insert(0, new OpenApiParameter
                {
                    Name = "refreshToken",
                    In = ParameterLocation.Cookie,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "refreshToken (cookie)"
                });
            }
        }
    }
}
