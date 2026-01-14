/**
 * API Client for OutreachGenie Backend
 * Provides typed interfaces matching C# backend models
 */

/* eslint-disable max-lines */
// Note: This file exceeds 150 lines but would require substantial refactoring to split into smaller modules
// while maintaining cohesion of the API client logic and type definitions.

const API_BASE_URL: string = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5000';

class ApiError extends Error {
    statusCode: number;
    details?: unknown;
    
    constructor(statusCode: number, message: string, details?: unknown) {
        super(message);
        this.name = 'ApiError';
        this.statusCode = statusCode;
        this.details = details;
    }
}

class ApiClient {
    private baseUrl: string;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    private async handleErrorResponse(response: Response): Promise<never> {
        let message = response.statusText;
        let details: unknown;

        try {
            const errorData: unknown = await response.json();
            details = errorData;
            if (errorData && typeof errorData === 'object' && 'message' in errorData && typeof errorData.message === 'string') {
                message = errorData.message || message;
            }
        } catch {
            // Response body might not be JSON
        }

        throw new ApiError(response.status, message, details);
    }

    private async parseResponse<T>(response: Response): Promise<T> {
        const contentType = response.headers.get('content-type');
        if (contentType?.includes('application/json')) {
            return (await response.json()) as T;
        }
        throw new Error(`Expected JSON response but received ${contentType ?? 'no content-type'}`);
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
                return await this.handleErrorResponse(response);
            }

            return await this.parseResponse<T>(response);
        } catch (error) {
            if (error instanceof Error && !('statusCode' in error)) {
                const apiError = new ApiError(0, error.message, error);
                throw apiError;
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
        // Backend requires a CampaignId (Guid). For now, use a default/null GUID if not provided
        // In production, we should either create a campaign first or allow nullable CampaignId
        const payload = {
            campaignId: campaignId || '00000000-0000-0000-0000-000000000000',
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
