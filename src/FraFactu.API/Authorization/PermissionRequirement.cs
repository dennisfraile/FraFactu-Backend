using Microsoft.AspNetCore.Authorization;

namespace FraFactu.API.Authorization
{
    /// <summary>
    /// Requirement para verificar que un usuario tenga un permiso específico
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionCode { get; }

        public PermissionRequirement(string permissionCode)
        {
            PermissionCode = permissionCode;
        }
    }
}
