const createAssistantTab = (function() {

    let lastUserQuery;

    const assistant = {
        id: 'assistant',
        name: 'Virtual Assistant',
    };

    const user = {
        id: 'user',
    };

    function normalizeAIResponse(text) {
        text = text.replace(/【\d+:\d+†[^\】]+】/g, "");
        let html = marked.parse(text);
        if(/<p>\.\s*<\/p>\s*$/.test(html))
            html = html.replace(/<p>\.\s*<\/p>\s*$/, "")
        return html;
    }

    function copyText(text) {
        navigator.clipboard.writeText(text);
    }

    async function getAIResponse(text, id) {
        const formData = new FormData();
        formData.append('text', text);
        formData.append('chatId', id);
        lastUserQuery = text;
        const response = await fetch(`/AI/GetAnswer`, {
            method: 'POST',
            body: formData
        });
        return await response.text();
    }

    function RenderAssistantMessage(instance, message) {
        instance.option({ typingUsers: [] });
        instance.renderMessage({ timestamp: new Date(), text: message, author: assistant.name, id: assistant.id });
    }

    async function refreshAnswer(instance) {
        const items = instance.option('items');
        const newItems = items.slice(0, -1);
        instance.option({ items: newItems });
        instance.option({ typingUsers: [assistant] });
        const aiResponse = await getAIResponse(lastUserQuery, assistant.id);
        setTimeout(() => {
            instance.option({ typingUsers: [] });
            RenderAssistantMessage(instance, aiResponse);
        }, 200);
    }

    function createAssistantTab(chatId) {
        assistant.id = chatId;
        const model = {
            title: 'AI Assistant',
            showAvatar: false,
            showUserName: false,
            showMessageTimestamp: false,
            user: user,
            messageTemplate: (data, $container) => {
                const { message } = data;
                const container = $container.jquery ? $container.get(0) : $container;
                if(message.author.id && message.author.id !== assistant.id)
                    return message.text;

                const textElement = document.createElement('div');
                textElement.innerHTML = normalizeAIResponse(message.text);
                container.appendChild(textElement)

                const buttonContainer = document.createElement('div');
                buttonContainer.classList.add('dx-bubble-button-container');
                const copyBtnElement = document.createElement('div');
                new DevExpress.ui.dxButton(copyBtnElement, {
                    icon: 'copy',
                    stylingMode: 'text',
                    onClick: () => copyText(textElement.textContent)
                });
                buttonContainer.appendChild(copyBtnElement);
                const refreshBtnElement = document.createElement('div');
                new DevExpress.ui.dxButton(refreshBtnElement, {
                    icon: 'refresh',
                    stylingMode: 'text',
                    onClick: () => refreshAnswer(data.component)
                });
                if(data.component.option('items').at(-1).author === assistant.name) {
                    buttonContainer.appendChild(refreshBtnElement);
                    lastRefreshButton = refreshBtnElement;
                }
                container.appendChild(buttonContainer);
            },
            onMessageEntered: async (e) => {
                lastRefreshButton?.remove();
                const instance = e.component;
                instance.renderMessage(e.message);
                instance.option({ typingUsers: [assistant] });
                const userInput = e.message.text;
                if(!assistant.id && model.chatId) {
                    assistant.id = model.chatId;
                }
                const response = await getAIResponse(instance, userInput, assistant.id);
                RenderAssistantMessage(instance, response);
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

    return createAssistantTab;
})();

window.createAssistantTab = createAssistantTab;