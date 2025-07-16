using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MME.Models;
using MME.Services.Auth;

namespace MME.Pages.User
{
    public partial class Login : ComponentBase
    {
        private readonly LoginParamsType _model = new LoginParamsType() { LoginType = "1" };
        private bool _isRedirecting = false;
        private bool _isLoading = false;

        [Inject] public NavigationManager NavigationManager { get; set; }

        [Inject] public MessageService Message { get; set; }

        [Inject] public ProtectedSessionStorage ProtectedSessionStore { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            // 检查用户是否已经登录，如果已登录则重定向到主页
            try
            {
                var userSessionStorageResult = await ProtectedSessionStore.GetAsync<UserSession>("UserSession");
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;

                if (userSession != null && !string.IsNullOrEmpty(userSession.UserName) && !_isRedirecting)
                {
                    // 用户已登录，重定向到主页
                    _isRedirecting = true;
                    NavigationManager.NavigateTo("/", true);
                    return;
                }
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // Session数据损坏，清理它
                try
                {
                    await ProtectedSessionStore.DeleteAsync("UserSession");
                }
                catch
                {
                    // 忽略清理错误
                }
            }
            catch
            {
                // 其他错误，不清理Session，让用户正常登录
            }
        }

        public async Task HandleSubmit()
        {
            if (_isRedirecting || _isLoading)
                return;

            _isLoading = true;
            StateHasChanged();

            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(_model.UserName) || string.IsNullOrWhiteSpace(_model.Password))
                {
                    _= Message.Error("请输入用户名和密码", 2);
                    return;
                }

                // 调用认证服务进行登录
                var authProvider = (MMEAuthProvider)AuthenticationStateProvider;
                var loginSuccess = await authProvider.SignIn(_model.UserName, _model.Password);
                
                if (loginSuccess)
                {
                    _isRedirecting = true;
                    _= Message.Success("登录成功", 1);
                    
                    // 等待认证状态完全更新后再重定向
                    await Task.Delay(50);
                    NavigationManager.NavigateTo("/", true);
                    return;
                }
                else
                {
                    _= Message.Error("用户名或密码错误", 2);
                }
            }
            catch (Exception ex)
            {
                _= Message.Error($"登录过程中发生错误: {ex.Message}", 3);
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }
    }
} 