/* eslint-disable @typescript-eslint/no-unsafe-assignment */
import { apiClient, ApiError, ArtifactSource, CampaignStatus, TaskStatus } from '@/lib/api';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// Mock fetch globally
global.fetch = vi.fn();

describe('ApiClient', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    describe('request method', () => {
        it('should make successful GET request and return JSON', async () => {
            const mockData = { id: '123', name: 'Test Campaign' };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockData,
            });

            const result: unknown = await apiClient['request']('/api/test');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/test',
                expect.objectContaining({
                    headers: expect.objectContaining({
                        'Content-Type': 'application/json',
                    }),
                })
            );
            expect(result).toEqual(mockData);
        });

        it('should handle non-JSON responses', async () => {
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'text/plain' }),
            });

            await expect(apiClient['request']('/api/test')).rejects.toThrow('Expected JSON response but received text/plain');
        });

        it('should throw ApiError on HTTP error', async () => {
            const errorData = { message: 'Not found', code: 'NOT_FOUND' };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: false,
                status: 404,
                statusText: 'Not Found',
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => errorData,
            });

            await expect(apiClient['request']('/api/test')).rejects.toMatchObject({
                statusCode: 404,
                message: 'Not found',
                details: errorData,
            } as ApiError);
        });

        it('should handle network errors', async () => {
            (global.fetch as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network failure'));

            await expect(apiClient['request']('/api/test')).rejects.toMatchObject({
                statusCode: 0,
                message: 'Network failure',
            } as ApiError);
        });
    });

    describe('Campaign endpoints', () => {
        it('should fetch campaigns list', async () => {
            const mockCampaigns = [
                { id: '1', name: 'Campaign 1', status: CampaignStatus.Active, targetAudience: 'CTOs', createdAt: '2026-01-11T00:00:00Z', updatedAt: '2026-01-11T00:00:00Z' },
                { id: '2', name: 'Campaign 2', status: CampaignStatus.Paused, targetAudience: 'CEOs', createdAt: '2026-01-11T00:00:00Z', updatedAt: '2026-01-11T00:00:00Z' },
            ];
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockCampaigns,
            });

            const result = await apiClient.getCampaigns();

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/campaign',
                expect.any(Object)
            );
            expect(result).toEqual(mockCampaigns);
        });

        it('should get single campaign by ID', async () => {
            const mockCampaign = { id: '123', name: 'Test', status: CampaignStatus.Active, targetAudience: 'VPs', createdAt: '2026-01-11T00:00:00Z', updatedAt: '2026-01-11T00:00:00Z' };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockCampaign,
            });

            const result = await apiClient.getCampaign('123');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/campaign/123',
                expect.any(Object)
            );
            expect(result).toEqual(mockCampaign);
        });

        it('should create campaign with POST request', async () => {
            const request = { name: 'New Campaign', targetAudience: 'Developers' };
            const mockResponse = { id: '456', ...request, status: CampaignStatus.Draft, createdAt: '2026-01-11T00:00:00Z', updatedAt: '2026-01-11T00:00:00Z' };

            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockResponse,
            });

            const result = await apiClient.createCampaign(request);

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/campaign',
                expect.objectContaining({
                    method: 'POST',
                    body: JSON.stringify(request),
                })
            );
            expect(result).toEqual(mockResponse);
        });

        it('should pause campaign', async () => {
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => ({}),
            });

            await apiClient.pauseCampaign('123');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/campaign/123/pause',
                expect.objectContaining({
                    method: 'POST',
                })
            );
        });

        it('should resume campaign', async () => {
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => ({}),
            });

            await apiClient.resumeCampaign('123');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/campaign/123/resume',
                expect.objectContaining({
                    method: 'POST',
                })
            );
        });

        it('should delete campaign', async () => {
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => ({}),
            });

            await apiClient.deleteCampaign('123');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/campaign/123',
                expect.objectContaining({
                    method: 'DELETE',
                })
            );
        });
    });

    describe('Chat endpoints', () => {
        it('should send message with default campaign ID', async () => {
            const mockResponse = {
                messageId: '789',
                content: 'Agent response',
                timestamp: '2026-01-11T15:00:00Z',
            };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockResponse,
            });

            const result = await apiClient.sendMessage('Hello');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/chat/send',
                expect.objectContaining({
                    method: 'POST',
                    body: JSON.stringify({
                        campaignId: '00000000-0000-0000-0000-000000000000',
                        message: 'Hello',
                    }),
                })
            );
            expect(result).toEqual(mockResponse);
        });

        it('should send message with specific campaign ID', async () => {
            const mockResponse = {
                messageId: '789',
                content: 'Agent response',
                timestamp: '2026-01-11T15:00:00Z',
            };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockResponse,
            });

            await apiClient.sendMessage('Hello', '123-456');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/chat/send',
                expect.objectContaining({
                    body: JSON.stringify({
                        campaignId: '123-456',
                        message: 'Hello',
                    }),
                })
            );
        });

        it('should get chat history', async () => {
            const mockHistory = [
                { id: '1', campaignId: '123', role: 'user', content: 'Hello', timestamp: '2026-01-11T15:00:00Z' },
                { id: '2', campaignId: '123', role: 'assistant', content: 'Hi there', timestamp: '2026-01-11T15:00:01Z' },
            ];
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockHistory,
            });

            const result = await apiClient.getChatHistory('123');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/chat/history?campaignId=123',
                expect.any(Object)
            );
            expect(result).toEqual(mockHistory);
        });
    });

    describe('Task endpoints', () => {
        it('should get tasks for campaign', async () => {
            const mockTasks = [
                { id: '1', campaignId: '123', description: 'Task 1', status: TaskStatus.Pending, type: 'Test', input: null, output: null, retryCount: 0, createdAt: '2026-01-11T00:00:00Z', updatedAt: '2026-01-11T00:00:00Z' },
            ];
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockTasks,
            });

            const result = await apiClient.getTasks('123');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/task/list/123',
                expect.any(Object)
            );
            expect(result).toEqual(mockTasks);
        });

        it('should get single task by ID', async () => {
            const mockTask = { id: '1', campaignId: '123', description: 'Task 1', status: TaskStatus.Done, type: 'Test', input: null, output: null, retryCount: 0, createdAt: '2026-01-11T00:00:00Z', updatedAt: '2026-01-11T00:00:00Z' };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockTask,
            });

            const result = await apiClient.getTask('1');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/task/get/1',
                expect.any(Object)
            );
            expect(result).toEqual(mockTask);
        });
    });

    describe('Artifact endpoints', () => {
        it('should get artifacts for campaign', async () => {
            const mockArtifacts = [
                { id: '1', campaignId: '123', type: 'context', key: 'main', content: '{}', source: ArtifactSource.User, version: 1, createdAt: '2026-01-11T00:00:00Z' },
            ];
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockArtifacts,
            });

            const result = await apiClient.getArtifacts('123');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/artifact/list/123',
                expect.any(Object)
            );
            expect(result).toEqual(mockArtifacts);
        });

        it('should get artifacts filtered by type', async () => {
            const mockArtifacts = [
                { id: '1', campaignId: '123', type: 'leads', key: 'main', content: '[]', source: ArtifactSource.Agent, version: 1, createdAt: '2026-01-11T00:00:00Z' },
            ];
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockArtifacts,
            });

            await apiClient.getArtifacts('123', 'leads');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/artifact/list/123?type=leads',
                expect.any(Object)
            );
        });

        it('should get specific artifact', async () => {
            const mockArtifact = { id: '1', campaignId: '123', type: 'context', key: 'main', content: '{}', source: ArtifactSource.User, version: 1, createdAt: '2026-01-11T00:00:00Z' };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockArtifact,
            });

            const result = await apiClient.getArtifact('123', 'context', 'main');

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/artifact/get/123/context/main',
                expect.any(Object)
            );
            expect(result).toEqual(mockArtifact);
        });

        it('should create artifact', async () => {
            const request = {
                campaignId: '123',
                type: 'leads',
                key: 'main',
                content: '[]',
                source: ArtifactSource.Agent,
            };
            const mockResponse = { id: '1', ...request, version: 1, createdAt: '2026-01-11T00:00:00Z' };

            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockResponse,
            });

            const result = await apiClient.createArtifact(request);

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/artifact/create',
                expect.objectContaining({
                    method: 'POST',
                    body: JSON.stringify(request),
                })
            );
            expect(result).toEqual(mockResponse);
        });
    });

    describe('Settings endpoints', () => {
        it('should get settings', async () => {
            const mockSettings = {
                apiEndpoint: 'http://localhost:5104',
                modelName: 'gpt-4',
                maxTokens: 4096,
                temperature: 0.7,
            };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => mockSettings,
            });

            const result = await apiClient.getSettings();

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/settings/get',
                expect.any(Object)
            );
            expect(result).toEqual(mockSettings);
        });

        it('should update settings', async () => {
            const settings = {
                apiEndpoint: 'http://localhost:5104',
                modelName: 'gpt-4-turbo',
                maxTokens: 8192,
                temperature: 0.8,
            };
            (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
                ok: true,
                headers: new Headers({ 'content-type': 'application/json' }),
                json: () => ({}),
            });

            await apiClient.updateSettings(settings);

            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/v1/settings/update',
                expect.objectContaining({
                    method: 'POST',
                    body: JSON.stringify(settings),
                })
            );
        });
    });
});
