using System;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Library.Models
{
    public class SwaggerExcludeAttribute : Attribute, ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties.ContainsKey("books"))
            {
                var property = schema.Properties["books"];
                property.Type = "null";
                property.Example = new OpenApiNull();
            }
            if (schema.Properties.ContainsKey("categories"))
            {
                var property = schema.Properties["categories"];
                property.Type = "null";
                property.Example = new OpenApiNull();
            }
            if (schema.Properties.ContainsKey("users"))
            {
                var property = schema.Properties["users"];
                property.Type = "null";
                property.Example = new OpenApiNull();
            }
            if (schema.Properties.ContainsKey("publishers"))
            {
                var property = schema.Properties["publishers"];
                property.Type = "null";
                property.Example = new OpenApiNull();
            }
            if (schema.Properties.ContainsKey("authors"))
            {
                var property = schema.Properties["authors"];
                property.Type = "null";
                property.Example = new OpenApiNull();
            }
            if (schema.Properties.ContainsKey("image"))
            {
                var property = schema.Properties["image"];
                property.Type = "string";
                property.Example = new OpenApiNull();
            }
        }
    }
}
