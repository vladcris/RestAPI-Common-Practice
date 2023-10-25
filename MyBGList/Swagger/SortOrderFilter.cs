using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MyBGList.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyBGList.Swagger;

public class SortOrderFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context) {
        var attribute = context.ParameterInfo.GetCustomAttributes(true)
            .Union(context.ParameterInfo.ParameterType.GetProperties()
                .Where(p => p.Name == parameter.Name)
                .SelectMany(p => p.GetCustomAttributes(true))).OfType<SortOrderValidatorAttribute>();


        if (attribute != null) {
            foreach(var attr in attribute) {
                parameter.Schema.Extensions.Add("pattern", 
                    new OpenApiString(string.Join('|', attr.AllowedValues.Select(v => $"^{v}$"))));
            }
        }
    }
}
