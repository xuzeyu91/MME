using AntDesign;
using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MME.Models;
using MME.Services.Auth;

namespace MME.Components
{
    public partial class RightContent
    {
        private CurrentUser _currentUser = new CurrentUser();
        private NoticeIconData[] _notifications = { };
        private NoticeIconData[] _messages = { };
        private NoticeIconData[] _events = { };
        private int _count = 0;

        private List<AutoCompleteDataItem<string>> DefaultOptions { get; set; } = new List<AutoCompleteDataItem<string>>
        {
            new AutoCompleteDataItem<string>
            {
                Label = "umi ui",
                Value = "umi ui"
            },
            new AutoCompleteDataItem<string>
            {
                Label = "Pro Table",
                Value = "Pro Table"
            },
            new AutoCompleteDataItem<string>
            {
                Label = "Pro Layout",
                Value = "Pro Layout"
            }
        };

        public AvatarMenuItem[] AvatarMenuItems { get; set; } = new AvatarMenuItem[]
        {
            new() { Key = "center", IconType = "user", Option = "个人中心"},
            new() { Key = "setting", IconType = "setting", Option = "个人设置"},
            new() { IsDivider = true },
            new() { Key = "logout", IconType = "logout", Option = "退出登录"}
        };

        [Inject] protected NavigationManager NavigationManager { get; set; }

        [Inject] protected MessageService MessageService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            SetClassMap();

        }

        /// <summary>
        /// 设置组件的CSS类映射
        /// </summary>
        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .Add("right");
        }

        /// <summary>
        /// 处理用户菜单项选择事件
        /// </summary>
        /// <param name="item">选中的菜单项</param>
        public void HandleSelectUser(MenuItem item)
        {
            switch (item.Key)
            {
                case "center":
                    NavigationManager.NavigateTo("/account/center");
                    break;
                case "setting":
                    NavigationManager.NavigateTo("/account/settings");
                    break;
                case "logout":
                    NavigationManager.NavigateTo("/user/login");
                    break;
            }
        }

        /// <summary>
        /// 处理语言选择菜单项事件
        /// </summary>
        /// <param name="item">选中的语言菜单项</param>
        public void HandleSelectLang(MenuItem item)
        {
        }

        /// <summary>
        /// 处理清空通知、消息或事件列表的事件
        /// </summary>
        /// <param name="key">要清空的项目类型（notification/message/event）</param>
        /// <returns>异步任务</returns>
        public async Task HandleClear(string key)
        {
            switch (key)
            {
                case "notification":
                    _notifications = new NoticeIconData[] { };
                    break;
                case "message":
                    _messages = new NoticeIconData[] { };
                    break;
                case "event":
                    _events = new NoticeIconData[] { };
                    break;
            }
            _= MessageService.Success($"清空了{key}");
        }

        /// <summary>
        /// 处理查看更多的事件
        /// </summary>
        /// <param name="key">要查看更多的项目类型</param>
        /// <returns>异步任务</returns>
        public async Task HandleViewMore(string key)
        {
            _ = MessageService.Info("Click on view more");
        }

        private async Task HandleLogout()
        {
            var authProvider = (MMEAuthProvider)AuthenticationStateProvider;
            await authProvider.SignOut();
        }
    }
}