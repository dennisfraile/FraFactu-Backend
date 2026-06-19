using Microsoft.AspNetCore.Authorization;

namespace FraFactu.API.Authorization
{
    /// <summary>
    /// Attribute para requerir un permiso específico en un endpoint
    /// Uso: [RequirePermission("factura.crear")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permissionCode)
        {
            Policy = $"Permission:{permissionCode}";
        }
    }
}
