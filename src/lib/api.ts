/**
 * API Client for OutreachGenie Backend
 * Provides typed interfaces matching C# backend models
 */

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

interface ApiError {
    message: string;
    statusCode: number;
    details?: unknown;
}

class ApiClient {
    private baseUrl: string;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    private async request<T>(
        endpoint: string,
        options: RequestInit = {}
    ): Promise<T> {
        const url = `${this.baseUrl}${endpoint}`;

        try {
            const response = await fetch(url, {
                ...options,
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers,
                },
            });

            if (!response.ok) {
                const error: ApiError = {
                    message: response.statusText,
                    statusCode: response.status,
                };

                try {
                    const errorData = await response.json();
                    error.details = errorData;
                    error.message = errorData.message || error.message;
                } catch {
                    // Response body might not be JSON
                }

                throw error;
            }

            const contentType = response.headers.get('content-type');
            if (contentType?.includes('application/json')) {
                return await response.json();
            }

            return {} as T;
        } catch (error) {
            if (error instanceof Error && !(error as ApiError).statusCode) {
                throw {
                    message: error.message,
                    statusCode: 0,
                    details: error,
                } as ApiError;
            }
            throw error;
        }
    }

    // Campaign endpoints
    async getCampaigns() {
        return this.request<Campaign[]>('/api/v1/campaign');
    }

    async getCampaign(id: string) {
        return this.request<Campaign>(`/api/v1/campaign/${id}`);
    }

    async createCampaign(request: CreateCampaignRequest) {
        return this.request<Campaign>('/api/v1/campaign', {
            method: 'POST',
            body: JSON.stringify(request),
        });
    }

    async pauseCampaign(id: string) {
        return this.request<void>(`/api/v1/campaign/${id}/pause`, {
            method: 'POST',
        });
    }

    async resumeCampaign(id: string) {
        return this.request<void>(`/api/v1/campaign/${id}/resume`, {
            method: 'POST',
        });
    }

    async deleteCampaign(id: string) {
        return this.request<void>(`/api/v1/campaign/${id}`, {
            method: 'DELETE',
        });
    }

    // Chat endpoints
    async sendMessage(message: string, campaignId?: string) {
        // If no campaignId provided, fetch the first available campaign
        let actualCampaignId = campaignId;
        
        if (!actualCampaignId) {
            const campaigns = await this.getCampaigns();
            if (campaigns.length === 0) {
                throw new Error('No campaigns available. Please create a campaign first.');
            }
            actualCampaignId = campaigns[0].id;
        }

        const payload = {
            campaignId: actualCampaignId,
            message,
        };

        return this.request<ChatResponse>('/api/v1/chat/send', {
            method: 'POST',
            body: JSON.stringify(payload),
        });
    }

    async getChatHistory(campaignId?: string) {
        const query = campaignId ? `?campaignId=${campaignId}` : '';
        return this.request<ChatMessageDto[]>(`/api/v1/chat/history${query}`);
    }

    // Task endpoints
    async getTasks(campaignId: string) {
        return this.request<CampaignTask[]>(`/api/v1/task/list/${campaignId}`);
    }

    async getTask(taskId: string) {
        return this.request<CampaignTask>(`/api/v1/task/get/${taskId}`);
    }

    // Artifact endpoints
    async getArtifacts(campaignId: string, type?: string) {
        const query = type ? `?type=${type}` : '';
        return this.request<Artifact[]>(`/api/v1/artifact/list/${campaignId}${query}`);
    }

    async getArtifact(campaignId: string, type: string, key: string) {
        return this.request<Artifact>(`/api/v1/artifact/get/${campaignId}/${type}/${key}`);
    }

    async createArtifact(request: CreateArtifactRequest) {
        return this.request<Artifact>('/api/v1/artifact/create', {
            method: 'POST',
            body: JSON.stringify(request),
        });
    }

    // Settings endpoints
    async getSettings() {
        return this.request<SettingsDto>('/api/v1/settings/get');
    }

    async updateSettings(settings: SettingsDto) {
        return this.request<void>('/api/v1/settings/update', {
            method: 'POST',
            body: JSON.stringify(settings),
        });
    }
}

// Type definitions matching C# backend models

export interface Campaign {
    id: string;
    name: string;
    status: CampaignStatus;
    targetAudience: string;
    createdAt: string;
    updatedAt: string;
}

export enum CampaignStatus {
    Initializing = 'Initializing',
    Draft = 'Draft',
    Active = 'Active',
    Paused = 'Paused',
    Completed = 'Completed',
    Cancelled = 'Cancelled'
}

export interface CreateCampaignRequest {
    name: string;
    targetAudience: string;
}

export interface CampaignTask {
    id: string;
    campaignId: string;
    description: string;
    status: TaskStatus;
    type: string;
    input: string | null;
    output: string | null;
    retryCount: number;
    createdAt: string;
    updatedAt: string;
}

export enum TaskStatus {
    Pending = 'Pending',
    InProgress = 'InProgress',
    Done = 'Done',
    Failed = 'Failed',
    Retrying = 'Retrying'
}

export interface Artifact {
    id: string;
    campaignId: string;
    type: string;
    key: string;
    content: string;
    source: ArtifactSource;
    version: number;
    createdAt: string;
}

export enum ArtifactSource {
    User = 'User',
    Agent = 'Agent'
}

export interface CreateArtifactRequest {
    campaignId: string;
    type: string;
    key: string;
    content: string;
    source: ArtifactSource;
}

export interface ChatResponse {
    messageId: string;
    content: string;
    timestamp: string;
}

export interface ChatMessageDto {
    id: string;
    campaignId: string | null;
    role: string;
    content: string;
    timestamp: string;
}

export interface SettingsDto {
    apiEndpoint: string;
    modelName: string;
    maxTokens: number;
    temperature: number;
}

export type { ApiError };

// Export singleton instance
export const apiClient = new ApiClient(API_BASE_URL);
