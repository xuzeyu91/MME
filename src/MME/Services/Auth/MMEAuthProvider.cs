using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Options;
using MME.Domain.Common.Options;
using MME.Models;
using System.Security.Claims;

namespace MME.Services.Auth
{
    public class MMEAuthProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _protectedSessionStore;
        private readonly NavigationManager _navigationManager;
        private readonly AdminOption _adminOption;
        private ClaimsIdentity identity = new ClaimsIdentity();
        private bool _isRedirecting = false;

        public MMEAuthProvider(
            ProtectedSessionStorage protectedSessionStore,
            NavigationManager navigationManager,
            IOptions<AdminOption> adminOption)
        {
            _protectedSessionStore = protectedSessionStore;
            _navigationManager = navigationManager;
            _adminOption = adminOption.Value;
        }

        public async Task<bool> SignIn(string username, string password)
        {
            // 简化的认证逻辑，实际项目中应该连接数据库验证
            if (username == _adminOption.Username && password == _adminOption.Password)
            {
                string adminRole = "MMEAdmin";
                // 管理员认证成功，创建用户的ClaimsIdentity
                var claims = new[] {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, adminRole)
                };
                identity = new ClaimsIdentity(claims, adminRole);
                
                await _protectedSessionStore.SetAsync("UserSession", new UserSession() 
                { 
                    UserName = username, 
                    Role = adminRole,
                    LoginTime = DateTime.Now,
                    Email = "admin@mme.com"
                });
                
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return true;
            }
            
            // 这里可以添加普通用户的认证逻辑
            // 例如从数据库验证用户信息
            
            return false;
        }

        public async Task SignOut()
        {
            try
            {
                // 清除存储的Session数据
                await _protectedSessionStore.DeleteAsync("UserSession");
            }
            catch (Exception ex)
            {
                // 记录日志，但不抛出异常
                Console.WriteLine($"清除Session数据时发生错误: {ex.Message}");
            }

            // 清除身份认证
            identity = new ClaimsIdentity();

            // 通知认证状态已更改
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

            // 强制重定向到登录页面，避免页面卡住
            if (!_isRedirecting)
            {
                _isRedirecting = true;
                _navigationManager.NavigateTo("/user/login", true);
            }
        }

        public ClaimsPrincipal GetCurrentUser()
        {
            var user = new ClaimsPrincipal(identity);
            return user;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userSessionStorageResult = await _protectedSessionStore.GetAsync<UserSession>("UserSession");
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;
                
                if (userSession != null)
                {
                    var claims = new[] {
                        new Claim(ClaimTypes.Name, userSession.UserName),
                        new Claim(ClaimTypes.Role, userSession.Role),
                        new Claim(ClaimTypes.Email, userSession.Email ?? "")
                    };
                    identity = new ClaimsIdentity(claims, userSession.Role);
                }
                else
                {
                    // 没有Session数据，清除身份认证
                    identity = new ClaimsIdentity();
                }
                
                var user = new ClaimsPrincipal(identity);
                return await Task.FromResult(new AuthenticationState(user));
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                Console.WriteLine($"Session校验失败，可能是加密密钥无效: {ex.Message}");

                try
                {
                    // 清除无效的Session数据
                    await _protectedSessionStore.DeleteAsync("UserSession");
                }
                catch (Exception clearEx)
                {
                    Console.WriteLine($"清除无效Session数据时发生错误: {clearEx.Message}");
                }

                // 清除身份并返回未认证状态
                identity = new ClaimsIdentity();
                var user = new ClaimsPrincipal(identity);

                return await Task.FromResult(new AuthenticationState(user));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取认证状态时发生未知错误: {ex.Message}");

                // 清除身份认证
                identity = new ClaimsIdentity();
                var user = new ClaimsPrincipal(identity);

                return await Task.FromResult(new AuthenticationState(user));
            }
        }
    }
} 