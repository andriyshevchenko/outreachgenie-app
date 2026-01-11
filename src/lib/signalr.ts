/**
 * SignalR Hub Connection for Real-time Updates
 * Connects to backend AgentHub at /hubs/agent
 */

import * as signalR from '@microsoft/signalr';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

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

class SignalRHub {
    private connection: signalR.HubConnection | null = null;
    private reconnectAttempts = 0;
    private maxReconnectAttempts = 5;

    /**
     * Connect to SignalR hub
     */
    async connect(): Promise<void> {
        if (this.connection?.state === signalR.HubConnectionState.Connected) {
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
                        return Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
                    }
                    return null; // Stop reconnecting
                },
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.connection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
        });

        this.connection.onreconnected(() => {
            console.log('SignalR reconnected');
            this.reconnectAttempts = 0;
        });

        this.connection.onclose((error) => {
            console.error('SignalR connection closed', error);
        });

        try {
            await this.connection.start();
            console.log('SignalR connected successfully');
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
            console.log('SignalR disconnected');
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
