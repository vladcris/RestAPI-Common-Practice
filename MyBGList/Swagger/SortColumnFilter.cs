using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MyBGList.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace MyBGList.Swagger;

public class SortColumnFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context) {
        var attribute = context.ParameterInfo.GetCustomAttributes(true).OfType<SortColumnValidatorAttribute>();

        if (attribute != null) {
            foreach (var attr in attribute) {
                var pattern = attr.EntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => p.Name);

                parameter.Schema.Extensions.Add("pattern",
                    new OpenApiString(string.Join("|", pattern.Select(v => $"^{v}$"))));
            }

        }
    }
}
