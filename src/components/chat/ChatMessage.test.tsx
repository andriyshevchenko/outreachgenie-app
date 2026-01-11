import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ChatMessage } from './ChatMessage';
import { Message } from '@/types/agent';

describe('ChatMessage', () => {
  it('should render user message', () => {
    const message: Message = {
      id: '1',
      role: 'user',
      content: 'Hello agent',
      timestamp: new Date('2026-01-11T15:00:00Z'),
    };
    
    render(<ChatMessage message={message} />);
    
    expect(screen.getByText('Hello agent')).toBeInTheDocument();
    expect(screen.getByText(/\d{1,2}:\d{2}/)).toBeInTheDocument();
  });

  it('should render assistant message', () => {
    const message: Message = {
      id: '2',
      role: 'assistant',
      content: 'Hello user! How can I help?',
      timestamp: new Date('2026-01-11T15:05:00Z'),
    };
    
    render(<ChatMessage message={message} />);
    
    expect(screen.getByText('Hello user! How can I help?')).toBeInTheDocument();
    expect(screen.getByText(/\d{1,2}:\d{2}/)).toBeInTheDocument();
  });

  it('should render tool calls when present', () => {
    const message: Message = {
      id: '3',
      role: 'assistant',
      content: 'Searching LinkedIn...',
      timestamp: new Date('2026-01-11T15:10:00Z'),
      tools: [
        { id: '1', name: 'linkedin_search', status: 'completed', result: '150 prospects found' },
        { id: '2', name: 'enrich_profiles', status: 'running' },
      ],
    };
    
    render(<ChatMessage message={message} />);
    
    expect(screen.getByText('linkedin_search')).toBeInTheDocument();
    expect(screen.getByText(/150 prospects found/)).toBeInTheDocument();
    expect(screen.getByText('enrich_profiles')).toBeInTheDocument();
  });

  it('should render file attachments when present', () => {
    const message: Message = {
      id: '4',
      role: 'user',
      content: 'Here is my campaign data',
      timestamp: new Date('2026-01-11T15:15:00Z'),
      attachments: [
        { id: '1', name: 'prospects.csv', type: 'text/csv', size: 1024 },
        { id: '2', name: 'template.txt', type: 'text/plain', size: 512 },
      ],
    };
    
    render(<ChatMessage message={message} />);
    
    expect(screen.getByText('prospects.csv')).toBeInTheDocument();
    expect(screen.getByText('template.txt')).toBeInTheDocument();
  });

  it('should format markdown in content', () => {
    const message: Message = {
      id: '5',
      role: 'assistant',
      content: '**Bold text** and *italic text*',
      timestamp: new Date('2026-01-11T15:20:00Z'),
    };
    
    render(<ChatMessage message={message} />);
    
    // Content should be rendered (exact formatting depends on markdown renderer)
    expect(screen.getByText(/bold text/i)).toBeInTheDocument();
    expect(screen.getByText(/italic text/i)).toBeInTheDocument();
  });
});
