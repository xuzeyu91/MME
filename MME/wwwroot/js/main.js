// 数据库聊天页面相关功能
window.databaseChatFunctions = {
    // 滚动聊天容器到底部
    scrollChatToBottom: function (element) {
        if (!element) return;
        
        try {
            if (typeof element.scrollTo === 'function') {
                element.scrollTo({
                    top: element.scrollHeight,
                    behavior: 'smooth'
                });
            } else {
                element.scrollTop = element.scrollHeight;
            }
        } catch (e) {
            console.error('滚动失败:', e);
        }
    },
    
    // 复制文本到剪贴板
    copyToClipboard: function (text) {
        if (!text) return Promise.reject('无文本可复制');
        
        return navigator.clipboard.writeText(text)
            .then(() => true)
            .catch(err => {
                console.error('复制失败:', err);
                // 回退方案
                try {
                    const textArea = document.createElement('textarea');
                    textArea.value = text;
                    document.body.appendChild(textArea);
                    textArea.focus();
                    textArea.select();
                    const successful = document.execCommand('copy');
                    document.body.removeChild(textArea);
                    return successful;
                } catch (e) {
                    console.error('回退复制失败:', e);
                    return false;
                }
            });
    },
    
    // 确认函数已加载
    isLoaded: function() {
        return true;
    }
};

// 确保函数已准备就绪
console.log("数据库聊天功能已加载");

// 在Blazor启动后执行
document.addEventListener('DOMContentLoaded', function() {
    // 防止功能未加载情况下的错误
    if (!window.databaseChatFunctions) {
        console.error("警告：数据库聊天功能未正确加载，请检查main.js文件");
        // 提供基本功能以防止页面崩溃
        window.databaseChatFunctions = {
            scrollChatToBottom: function() { console.warn("滚动功能未加载"); },
            copyToClipboard: function() { console.warn("复制功能未加载"); return Promise.resolve(false); },
            isLoaded: function() { return false; }
        };
    }
});
