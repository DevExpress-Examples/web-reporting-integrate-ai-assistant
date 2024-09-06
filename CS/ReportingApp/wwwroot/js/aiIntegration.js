function createAssistantTab(chatId) {
    const model = {
        title: 'AI Assistant',
        chatId: chatId,
        user: {
            id: 1,
        },
        onMessageSend: (e) => {
            const instance = e.component;
            const formData = new FormData();
            formData.append('text', e.message.text);
            formData.append('chatId', model.chatId);
            fetch(`/AI/GetAnswer`, {
                method: 'POST',
                body: formData
            }).then((x) => {
                x.text().then((res) => {
                    instance.renderMessage({
                        text: res,
                        author: { id: 'Assistant' }
                    }, { id: 'Assistant' });
                });
            });
        }
    };
    return new DevExpress.Analytics.Utils.TabInfo({
        text: 'AI Assistant',
        template: 'dxrd-ai-panel',
        imageTemplateName: 'dxrd-ai-icon',
        imageClassName: 'aitab',
        model: model
    });
}
