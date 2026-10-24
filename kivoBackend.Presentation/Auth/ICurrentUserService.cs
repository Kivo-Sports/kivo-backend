using System.Security.Claims;

namespace kivoBackend.Presentation.Auth
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        Guid? UserId { get; }
        bool IsAdmin { get; }
        bool IsInRole(string role);
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public Guid? UserId
        {
            get
            {
                var id = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(id, out var parsed) ? parsed : null;
            }
        }

        public bool IsAdmin => IsInRole("Administrador") || IsInRole("Admin");

        public bool IsInRole(string role) => User?.IsInRole(role) == true;
    }
}
