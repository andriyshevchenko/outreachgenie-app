/**
 * SignalR Hub Connection for Real-time Updates
 * Connects to backend AgentHub at /hubs/agent
 */

import * as signalR from '@microsoft/signalr';

const API_BASE_URL: string = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5000';

export interface TaskStatusChangedEvent {
    taskId: string;
    status: string;
    timestamp: string;
}

export interface ChatMessageReceivedEvent {
    messageId: string;
    content: string;
    role: 'user' | 'assistant';
    timestamp: string;
}

export interface CampaignStateChangedEvent {
    campaignId: string;
    status: string;
    timestamp: string;
}

export interface ArtifactCreatedEvent {
    type: string;
    key: string;
    campaignId: string;
    timestamp: string;
}

const RECONNECT_BASE_DELAY_MS = 1000;
const RECONNECT_EXPONENT_BASE = 2;
const MAX_RECONNECT_DELAY_MS = 30000;
const MAX_RECONNECT_ATTEMPTS = 5;

class SignalRHub {
    private connection: signalR.HubConnection | null = null;
    private reconnectAttempts = 0;
    private readonly maxReconnectAttempts = MAX_RECONNECT_ATTEMPTS;

    /**
     * Connect to SignalR hub
     */
    async connect(): Promise<void> {
        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            // Info level logging is allowed
            // eslint-disable-next-line no-console
            console.log('SignalR already connected');
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/agent`, {
                withCredentials: true,
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: () => {
                    if (this.reconnectAttempts < this.maxReconnectAttempts) {
                        this.reconnectAttempts++;
                        return Math.min(RECONNECT_BASE_DELAY_MS * Math.pow(RECONNECT_EXPONENT_BASE, this.reconnectAttempts), MAX_RECONNECT_DELAY_MS);
                    }
                    return null; // Stop reconnecting
                },
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.connection.onreconnecting(() => {
            // Connection is reconnecting - allowed console method
            console.warn('SignalR reconnecting...');
        });

        this.connection.onreconnected(() => {
            // eslint-disable-next-line no-console
            console.log('SignalR reconnected');
            this.reconnectAttempts = 0;
        });

        this.connection.onclose((error) => {
            console.error('SignalR connection closed', error);
        });

        try {
            await this.connection.start();
            this.reconnectAttempts = 0;
        } catch (error) {
            console.error('SignalR connection failed', error);
            throw error;
        }
    }

    /**
     * Disconnect from SignalR hub
     */
    async disconnect(): Promise<void> {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
        }
    }

    /**
     * Subscribe to TaskStatusChanged events
     */
    onTaskStatusChanged(callback: (event: TaskStatusChangedEvent) => void): void {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        this.connection.on('TaskStatusChanged', callback);
    }

    /**
     * Subscribe to ChatMessageReceived events
     */
    onChatMessageReceived(callback: (event: ChatMessageReceivedEvent) => void): void {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        this.connection.on('ChatMessageReceived', callback);
    }

    /**
     * Subscribe to CampaignStateChanged events
     */
    onCampaignStateChanged(callback: (event: CampaignStateChangedEvent) => void): void {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        this.connection.on('CampaignStateChanged', callback);
    }

    /**
     * Subscribe to ArtifactCreated events
     */
    onArtifactCreated(callback: (event: ArtifactCreatedEvent) => void): void {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        this.connection.on('ArtifactCreated', callback);
    }

    /**
     * Unsubscribe from all events
     */
    offAll(): void {
        if (this.connection) {
            this.connection.off('TaskStatusChanged');
            this.connection.off('ChatMessageReceived');
            this.connection.off('CampaignStateChanged');
            this.connection.off('ArtifactCreated');
        }
    }

    /**
     * Get current connection state
     */
    get connectionState(): signalR.HubConnectionState | null {
        return this.connection?.state || null;
    }
}

// Singleton instance
export const signalRHub = new SignalRHub();
