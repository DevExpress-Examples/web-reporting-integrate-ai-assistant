
let lastUserQuery;

const assistant = {
    id: 'assistant',
    name: 'Virtual Assistant',
};

const user = {
    id: 'user',
};

function normilizeAIResponse(text) {
    text = text.replace(/【\d+:\d+†[^\】]+】/g, "");
    let html = marked.parse(text);
    if (/<p>\.\s*<\/p>\s*$/.test(html))
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
        messageTemplate: (data, container) => {
            const { message } = data;

            if (message.author.id && message.author.id !== assistant.id)
                return message.text;

            const $textElement = $('<div>')
                .html(normilizeAIResponse(message.text))
                .appendTo(container);
            const $buttonContainer = $('<div>')
                .addClass('dx-bubble-button-containder');
            $('<div>')
                .dxButton({
                    icon: 'copy',
                    stylingMode: 'text',
                    onClick: () => {
                        const text = $textElement.text();
                        copyText(text);
                    }
                })
                .appendTo($buttonContainer);
            $('<div>')
                .dxButton({
                    icon: 'refresh',
                    stylingMode: 'text',
                    onClick: () => {
                        refreshAnswer(data.component);
                    },
                })
                .appendTo($buttonContainer);
            $buttonContainer.appendTo(container);
        },
        onMessageEntered: async (e) => {
            const instance = e.component;
            instance.renderMessage(e.message);
            instance.option({ typingUsers: [assistant] });
            const userInput = e.message.text;

            var response = await getAIResponse(userInput, assistant.id);
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
