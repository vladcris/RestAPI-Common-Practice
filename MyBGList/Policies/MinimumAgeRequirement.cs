using Microsoft.AspNetCore.Authorization;

namespace MyBGList.Policies;

public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge {  get; }
    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }
}
