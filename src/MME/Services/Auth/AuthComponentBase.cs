using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace MME.Services.Auth
{
    public class AuthComponentBase : ComponentBase
    {
        [Inject]
        public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        
        [Inject]
        public NavigationManager NavigationManager { get; set; }
        
        public ClaimsPrincipal User { get; set; }
        
        private bool _authenticationChecked = false;

        protected override async Task OnInitializedAsync()
        {
            await GetAuthenticationStateAsync();
        }

        private async Task GetAuthenticationStateAsync()
        {
            if (_authenticationChecked)
                return;

            try
            {
                var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                User = authenticationState.User;
                _authenticationChecked = true;

                if (!User.Identity.IsAuthenticated)
                {
                    // 只有在明确需要认证的页面才重定向
                    var currentPath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
                    if (!currentPath.ToLower().Contains("login") && !currentPath.ToLower().Contains("auth"))
                    {
                        // 等待一点时间让认证状态更新完成，避免竞争条件
                        await Task.Delay(100);
                        
                        // 再次检查认证状态
                        var retryAuthState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                        if (!retryAuthState.User.Identity.IsAuthenticated)
                        {
                            // 使用强制重定向避免页面卡住
                            NavigationManager.NavigateTo("/user/login", true);
                        }
                        else
                        {
                            User = retryAuthState.User;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 认证过程中出现异常，重定向到登录页面
                var currentPath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
                if (!currentPath.ToLower().Contains("login") && !currentPath.ToLower().Contains("auth"))
                {
                    NavigationManager.NavigateTo("/user/login", true);
                }
            }
        }
    }
} 