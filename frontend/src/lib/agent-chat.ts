/**
 * Agent chat client that uses the REST API endpoint instead of AG-UI.
 * This works around the MapAGUI bug where agent tools are ignored.
 */

export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

export interface ChatStreamEvent {
  type: 'start' | 'update' | 'message' | 'tool_call' | 'tool_result' | 'done' | 'error';
  data: any;
}

export class AgentChatClient {
  private baseUrl: string;

  constructor(baseUrl: string = 'http://localhost:5063') {
    this.baseUrl = baseUrl;
  }

  /**
   * Streams chat responses from the agent with tool support.
   */
  async* streamChat(messages: ChatMessage[]): AsyncGenerator<ChatStreamEvent> {
    const response = await fetch(`${this.baseUrl}/api/agentchat/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ messages }),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    if (!response.body) {
      throw new Error('Response body is null');
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let currentEventType = '';

    try {
      while (true) {
        const { done, value } = await reader.read();

        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || ''; // Keep incomplete line in buffer

        for (const line of lines) {
          if (line.startsWith('event: ')) {
            currentEventType = line.substring(7).trim();
          } else if (line.startsWith('data: ')) {
            const dataStr = line.substring(6).trim();
            if (dataStr && currentEventType) {
              try {
                const data = JSON.parse(dataStr);
                const event: ChatStreamEvent = {
                  type: currentEventType as ChatStreamEvent['type'],
                  data,
                };
                yield event;
              } catch (e) {
                console.error('Failed to parse SSE data:', dataStr, e);
              }
            }
          }
        }
      }
    } finally {
      reader.releaseLock();
    }
  }

  private inferEventType(data: any): ChatStreamEvent['type'] {
    if (data.status === 'running') return 'start';
    if (data.status === 'completed') return 'done';
    if (data.message) return 'error';
    if (data.name) return 'tool_call';
    if (data.result) return 'tool_result';
    return 'message';
  }
}
