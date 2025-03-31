using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace YoutubeDownloader.Configurations;

public class EnumSchemaFiler : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Enables string enum options in swagger ui
        if (context.Type.IsEnum)
        {
            schema.Enum = context.Type
                .GetEnumNames()
                .Select(name => new OpenApiString(name))
                .ToList<IOpenApiAny>();
            schema.Type = "string";
        }
    }
}