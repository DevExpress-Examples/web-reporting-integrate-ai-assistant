const createAssistantTab = (function() {

    let lastUserQuery;
    let errorList = [];
    const assistant = {
        id: 'assistant',
        name: 'Virtual Assistant',
    };

    const user = {
        id: 'user',
    };


    async function _tryFetch(instance, fetchAction, message) {
        try {
            return await fetchAction();
        } catch(error) {
            _handleError(instance, { message: error.message, code: message });
        }
    }

    function _handleError(instance, error) {
        const id = "id" + Math.random().toString(16).slice(2)
        setTimeout(() => {
            errorList = errorList.filter(err => err.id !== id);
            instance.option('alerts', errorList);
        }, 10000);
        errorList.push({
            id: id,
            message: `${error.code} - ${error.message}`
        });
        instance.option('alerts', errorList);
    }

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

    async function getAIResponse(instance, text, id) {
        const formData = new FormData();
        formData.append('text', text);
        formData.append('chatId', id);
        lastUserQuery = text;
        return _tryFetch(instance, async () => {
            const response = await fetch('/AI/GetAnswer', {
                method: 'POST',
                body: formData
            });

            if(!response.ok) {
                _handleError(instance, { code: `${response.status}`, message: `Internal server error` });
                return;
            }
            return await response.text();
        }, 'GetAnswer');
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
        const aiResponse = await getAIResponse(instance, lastUserQuery, assistant.id);
        setTimeout(() => {
            instance.option({ typingUsers: [] });
            RenderAssistantMessage(instance, aiResponse);
        }, 200);
    }

    function createAssistantTab(chatId) {
        let lastRefreshButton;
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
                lastRefreshButton?.remove();
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
                instance.option('alerts', []);
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