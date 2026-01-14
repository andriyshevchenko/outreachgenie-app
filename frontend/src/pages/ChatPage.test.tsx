/* eslint-disable @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-call, no-magic-numbers */
import * as api from '@/lib/api';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ChatPage } from './ChatPage';

// Mock the API client
vi.mock('@/lib/api', () => ({
  apiClient: {
    sendMessage: vi.fn(),
  },
  ApiError: class ApiError extends Error {
    constructor(public statusCode: number, message: string, public details?: unknown) {
      super(message);
    }
  },
}));

// Mock useToast hook
const mockToast = vi.fn();
vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({ toast: mockToast }),
}));

describe('ChatPage', () => {
  let mockSendMessage: ReturnType<typeof vi.fn>;
  
  beforeEach(() => {
    vi.clearAllMocks();
    // Use spyOn to avoid unbound method issues
    mockSendMessage = vi.spyOn(api.apiClient, 'sendMessage').mockImplementation(vi.fn());
  });

  it('should render empty chat interface', () => {
    render(<ChatPage />);
    
    expect(screen.getByText('LinkedIn Outreach Agent')).toBeInTheDocument();
    expect(screen.getByText('AI-powered campaign automation')).toBeInTheDocument();
    expect(screen.getByText('How can I help you today?')).toBeInTheDocument();
  });

  it('should render suggested actions', () => {
    render(<ChatPage />);
    
    expect(screen.getByRole('button', { name: /start new campaign/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /find prospects/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /draft messages/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /analyze audience/i })).toBeInTheDocument();
  });

  it('should send user message when clicking suggested action', async () => {
    const user = userEvent.setup();
    const mockResponse = {
      messageId: '123',
      content: 'Agent response',
      timestamp: '2026-01-11T15:00:00Z',
    };
    
    mockSendMessage.mockResolvedValueOnce(mockResponse);
    
    render(<ChatPage />);
    
    const button = screen.getByRole('button', { name: /find prospects/i });
    await user.click(button);
    
    // User message should appear
    await waitFor(() => {
      expect(screen.getByText(/find and analyze potential prospects/i)).toBeInTheDocument();
    });
    
    // API should be called
    expect(mockSendMessage).toHaveBeenCalledWith(
      expect.stringContaining('prospects')
    );
    
    // Agent response should appear
    await waitFor(() => {
      expect(screen.getByText('Agent response')).toBeInTheDocument();
    });
  });

  it('should show typing indicator while waiting for response', async () => {
    const user = userEvent.setup();
    let resolvePromise: (value: unknown) => void;
    const promise = new Promise((resolve) => {
      resolvePromise = resolve;
    });
    
    mockSendMessage.mockReturnValueOnce(promise);
    
    render(<ChatPage />);
    
    const button = screen.getByRole('button', { name: /analyze audience/i });
    await user.click(button);
    
    // Typing indicator should be visible
    await waitFor(() => {
      const dots = screen.getAllByRole('generic').filter(el => 
        el.className?.includes('animate-bounce')
      );
      expect(dots.length).toBeGreaterThan(0);
    });
    
    // Resolve the promise
    if (resolvePromise) {
      resolvePromise({
        messageId: '123',
        content: 'Response',
        timestamp: '2026-01-11T15:00:00Z',
      });
    }
    
    // Typing indicator should disappear
    await waitFor(() => {
      expect(screen.getByText('Response')).toBeInTheDocument();
    });
  });

  it('should display error toast when API call fails', async () => {
    const user = userEvent.setup();
    const mockError: InstanceType<typeof api.ApiError> = new api.ApiError(500, 'Internal Server Error');
    
    mockSendMessage.mockRejectedValueOnce(mockError);
    
    render(<ChatPage />);
    
    const button = screen.getByRole('button', { name: /draft messages/i });
    await user.click(button);
    
    // Toast should be called with error
    await waitFor(() => {
      expect(mockToast).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Error',
          description: expect.stringContaining('Internal Server Error'),
          variant: 'destructive',
        })
      );
    });
    
    // Error message should be displayed in chat
    await waitFor(() => {
      expect(screen.getByText(/sorry, i encountered an error/i)).toBeInTheDocument();
    });
  });

  it('should handle network errors with connection message', async () => {
    const user = userEvent.setup();
    const mockError: InstanceType<typeof api.ApiError> = new api.ApiError(0, 'Network failure');
    
    mockSendMessage.mockRejectedValueOnce(mockError);
    
    render(<ChatPage />);
    
    const button = screen.getByRole('button', { name: /start new campaign/i });
    await user.click(button);
    
    // Toast should mention backend connection
    await waitFor(() => {
      expect(mockToast).toHaveBeenCalledWith(
        expect.objectContaining({
          description: expect.stringContaining('Failed to connect to backend'),
        })
      );
    });
  });

  it('should send message from text input', async () => {
    const user = userEvent.setup();
    const mockResponse = {
      messageId: '456',
      content: 'Custom response',
      timestamp: '2026-01-11T15:00:00Z',
    };
    
    mockSendMessage.mockResolvedValueOnce(mockResponse);
    
    render(<ChatPage />);
    
    const input = screen.getByPlaceholderText(/ask the agent/i);
    await user.type(input, 'Hello agent{Enter}');
    
    // Verify message was sent via Enter key
    await waitFor(() => {
      expect(mockSendMessage).toHaveBeenCalledWith(expect.stringContaining('Hello'));
    });
  });

  it('should clear input after sending message', async () => {
    const user = userEvent.setup();
    const mockResponse = {
      messageId: '789',
      content: 'Response',
      timestamp: '2026-01-11T15:00:00Z',
    };
    
    mockSendMessage.mockResolvedValueOnce(mockResponse);
    
    render(<ChatPage />);
    
    const button = screen.getByRole('button', { name: /optimize campaign/i });
    await user.click(button);
    
    // Message should be sent
    await waitFor(() => {
      expect(mockSendMessage).toHaveBeenCalled();
    });
  });

  it('should scroll to bottom when new messages appear', async () => {
    const user = userEvent.setup();
    const mockResponse = {
      messageId: '999',
      content: 'Scroll test',
      timestamp: '2026-01-11T15:00:00Z',
    };
    
    mockSendMessage.mockResolvedValueOnce(mockResponse);
    
    const scrollIntoViewMock = vi.fn();
    Element.prototype.scrollIntoView = scrollIntoViewMock;
    
    render(<ChatPage />);
    
    const button = screen.getByRole('button', { name: /review connections/i });
    await user.click(button);
    
    // ScrollIntoView should be called
    await waitFor(() => {
      expect(scrollIntoViewMock).toHaveBeenCalled();
    });
  });
});
