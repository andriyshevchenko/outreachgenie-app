/**
 * AG-UI Client for Microsoft Agent Framework
 * Connects to /api/agent endpoint using Server-Sent Events (SSE)
 */

const API_BASE_URL: string = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5063';

export interface AgentMessage {
    role: 'user' | 'assistant' | 'system';
    content: string;
    timestamp?: Date;
}

export interface AgentRunUpdate {
    // AG-UI standard types
    type: 'RUN_STARTED' | 'TEXT_MESSAGE_START' | 'TEXT_MESSAGE_CONTENT' | 'TEXT_MESSAGE_END' |
    'TOOL_CALL_START' | 'TOOL_CALL_CONTENT' | 'TOOL_CALL_END' |
    'RUN_COMPLETED' | 'RUN_FAILED' | 'ERROR';

    // Common fields
    threadId?: string;
    runId?: string;
    messageId?: string;

    // Text content
    content?: string;
    delta?: string;

    // Tool call fields
    toolCallId?: string;
    toolName?: string;
    toolArgs?: Record<string, unknown>;
    toolResult?: unknown;

    // Error
    error?: string;
    message?: string;
}

export type MessageHandler = (update: AgentRunUpdate) => void;
export type ErrorHandler = (error: Error) => void;
export type CompleteHandler = () => void;

export class AGUIClient {
    private eventSource: EventSource | null = null;
    private messageHandlers: MessageHandler[] = [];
    private errorHandlers: ErrorHandler[] = [];
    private completeHandlers: CompleteHandler[] = [];

    /**
     * Send a message to the agent and stream the response
     */
    async sendMessage(
        message: string,
        campaignId?: string,
        onMessage?: MessageHandler,
        onError?: ErrorHandler,
        onComplete?: CompleteHandler
    ): Promise<void> {
        // Close any existing connection
        this.disconnect();

        // Register handlers
        if (onMessage) this.messageHandlers.push(onMessage);
        if (onError) this.errorHandlers.push(onError);
        if (onComplete) this.completeHandlers.push(onComplete);

        try {
            // Use fetch with streaming response for AG-UI endpoint
            const response = await fetch(`${API_BASE_URL}/api/agent`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'text/event-stream',
                },
                body: JSON.stringify({ message, campaignId }),
            });

            if (!response.ok || !response.body) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Read SSE stream
            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop() || '';

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        try {
                            const update = JSON.parse(data) as AgentRunUpdate;
                            this.handleMessage(update);

                            if (update.type === 'RUN_COMPLETED' || update.type === 'RUN_FAILED') {
                                this.handleComplete();
                                return;
                            }
                        } catch (error) {
                            console.error('Failed to parse SSE data:', error, 'Raw:', data);
                        }
                    }
                }
            }

            this.handleComplete();
        } catch (error) {
            this.handleError(error as Error);
        }
    }

    private handleMessage(update: AgentRunUpdate): void {
        for (const handler of this.messageHandlers) {
            try {
                handler(update);
            } catch (error) {
                console.error('Message handler error:', error);
            }
        }
    }

    private handleError(error: Error): void {
        for (const handler of this.errorHandlers) {
            try {
                handler(error);
            } catch (err) {
                console.error('Error handler error:', err);
            }
        }
    }

    private handleComplete(): void {
        for (const handler of this.completeHandlers) {
            try {
                handler();
            } catch (error) {
                console.error('Complete handler error:', error);
            }
        }
    }

    /**
     * Disconnect from the agent stream
     */
    disconnect(): void {
        if (this.eventSource) {
            this.eventSource.close();
            this.eventSource = null;
        }
        this.messageHandlers = [];
        this.errorHandlers = [];
        this.completeHandlers = [];
    }

    /**
     * Check if currently connected
     */
    get isConnected(): boolean {
        return this.eventSource !== null && this.eventSource.readyState === EventSource.OPEN;
    }
}

// Export singleton instance
export const aguiClient = new AGUIClient();
