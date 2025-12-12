// ALAN Chat App using AG-UI Protocol
// This implementation uses vanilla JavaScript to communicate with the AG-UI endpoint

class AGUIChatApp {
    constructor(rootElementId, chatApiBaseUrl) {
        this.root = document.getElementById(rootElementId);
        this.chatApiBaseUrl = chatApiBaseUrl;
        this.messages = [];
        this.sessionId = this.getOrCreateSessionId();
        this.threadId = crypto.randomUUID();
        this.isLoading = false;
        
        this.render();
        this.attachEventListeners();
    }

    getOrCreateSessionId() {
        let sessionId = localStorage.getItem('alanChatSessionId');
        if (!sessionId) {
            sessionId = crypto.randomUUID();
            localStorage.setItem('alanChatSessionId', sessionId);
        }
        return sessionId;
    }

    render() {
        this.root.innerHTML = `
            <div class="agui-chat-container">
                <div id="agui-messages" class="agui-messages">
                    <div class="text-muted text-center" style="margin-top: 2rem;">
                        <p>Start a conversation with ALAN...</p>
                        <small>Using AG-UI protocol for standardized agent communication</small>
                    </div>
                </div>
                <div class="agui-input-container">
                    <input 
                        type="text" 
                        id="agui-input" 
                        class="form-control" 
                        placeholder="Type your message..."
                    />
                    <button 
                        id="agui-send-btn" 
                        class="btn btn-primary"
                    >
                        📤 Send
                    </button>
                </div>
            </div>
        `;

        // Add CSS
        const style = document.createElement('style');
        style.textContent = `
            .agui-chat-container {
                display: flex;
                flex-direction: column;
                height: 100%;
            }
            .agui-messages {
                flex: 1;
                overflow-y: auto;
                padding: 1rem;
                background-color: #f8f9fa;
                border: 1px solid #dee2e6;
                border-radius: 0.25rem;
                margin-bottom: 1rem;
            }
            .agui-input-container {
                display: flex;
                gap: 0.5rem;
            }
            .agui-message {
                margin-bottom: 1rem;
            }
            .agui-message.human {
                text-align: right;
            }
            .agui-message-content {
                display: inline-block;
                max-width: 80%;
                padding: 0.5rem;
                border-radius: 0.25rem;
                margin-top: 0.25rem;
            }
            .agui-message.human .agui-message-content {
                background-color: #0d6efd;
                color: white;
            }
            .agui-message.agent .agui-message-content {
                background-color: white;
                color: black;
                border: 1px solid #dee2e6;
            }
            .agui-badge {
                display: inline-block;
                padding: 0.25rem 0.5rem;
                border-radius: 0.25rem;
                font-size: 0.875rem;
                font-weight: bold;
                margin-bottom: 0.25rem;
                color: white;
            }
            .agui-badge.human {
                background-color: #0d6efd;
            }
            .agui-badge.agent {
                background-color: #198754;
            }
            .agui-timestamp {
                display: block;
                margin-bottom: 0.25rem;
                opacity: 0.7;
                font-size: 0.75rem;
            }
        `;
        document.head.appendChild(style);
    }

    attachEventListeners() {
        const input = document.getElementById('agui-input');
        const sendBtn = document.getElementById('agui-send-btn');

        sendBtn.addEventListener('click', () => this.sendMessage());
        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
    }

    async sendMessage() {
        const input = document.getElementById('agui-input');
        const message = input.value.trim();
        
        if (!message || this.isLoading) return;

        // Add user message
        this.addMessage('human', message);
        input.value = '';
        this.isLoading = true;
        document.getElementById('agui-send-btn').disabled = true;

        try {
            // Call AG-UI endpoint using Server-Sent Events for streaming
            const response = await this.callAGUIEndpoint(message);
            
            if (response) {
                this.addMessage('agent', response);
            }
        } catch (error) {
            console.error('AGUI Chat error:', error);
            this.addMessage('agent', 'Sorry, I could not connect to the agent.');
        } finally {
            this.isLoading = false;
            document.getElementById('agui-send-btn').disabled = false;
            input.focus();
        }
    }

    async callAGUIEndpoint(message) {
        // AG-UI protocol returns Server-Sent Events (SSE) with JSON events
        const endpoint = `${this.chatApiBaseUrl}/agui`;
        
        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'text/event-stream'
                },
                body: JSON.stringify({
                    threadId: this.threadId,
                    sessionId: this.sessionId,
                    input: message
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            // Parse SSE stream to extract text content
            const reader = response.body.getReader();
            const decoder = new TextDecoder('utf-8');
            let buffer = '';
            let accumulatedText = '';

            while (true) {
                const { value, done } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                
                // Split into events (each event ends with \n\n)
                const events = buffer.split('\n\n');
                buffer = events.pop() || ''; // Keep incomplete event in buffer

                for (const eventText of events) {
                    if (!eventText.trim()) continue;

                    // Parse SSE event lines
                    const lines = eventText.split('\n');
                    let data = '';
                    
                    for (const line of lines) {
                        if (line.startsWith('data:')) {
                            data = line.slice(5).trim();
                        }
                    }

                    if (!data || data === '[DONE]') continue;

                    try {
                        const event = JSON.parse(data);
                        
                        // Extract text from TEXT_MESSAGE_CONTENT events
                        if (event.type === 'TEXT_MESSAGE_CONTENT' && event.delta) {
                            accumulatedText += event.delta;
                        }
                    } catch (e) {
                        // Skip malformed JSON
                        console.debug('Skipping non-JSON event data:', data);
                    }
                }
            }

            return accumulatedText || 'Agent responded (no text content)';
        } catch (error) {
            console.error('Error calling AG-UI endpoint:', error);
            throw error;
        }
    }

    addMessage(role, content) {
        const messagesContainer = document.getElementById('agui-messages');
        
        // Remove placeholder if present
        const placeholder = messagesContainer.querySelector('.text-muted');
        if (placeholder) {
            placeholder.remove();
        }

        const timestamp = new Date().toLocaleTimeString();
        const messageDiv = document.createElement('div');
        messageDiv.className = `agui-message ${role}`;
        messageDiv.innerHTML = `
            <div>
                <span class="agui-badge ${role}">${role === 'human' ? 'You' : 'ALAN'}</span>
                <div class="agui-message-content">
                    <small class="agui-timestamp">${timestamp}</small>
                    ${this.escapeHtml(content)}
                </div>
            </div>
        `;

        messagesContainer.appendChild(messageDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
        
        this.messages.push({ role, content, timestamp: new Date() });
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeAGUIChat);
} else {
    initializeAGUIChat();
}

function initializeAGUIChat() {
    const rootElement = document.getElementById('chat-root');
    if (rootElement) {
        const chatApiUrl = rootElement.getAttribute('data-chatapi-url');
        new AGUIChatApp('chat-root', chatApiUrl);
    }
}
