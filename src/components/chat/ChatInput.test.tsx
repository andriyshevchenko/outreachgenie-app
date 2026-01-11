import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ChatInput } from './ChatInput';

describe('ChatInput', () => {
  it('should render text input', () => {
    const onSend = vi.fn();
    render(<ChatInput onSend={onSend} disabled={false} />);
    
    const input = screen.getByPlaceholderText(/ask the agent/i);
    expect(input).toBeInTheDocument();
    expect(input).not.toBeDisabled();
  });

  it('should be disabled when prop is true', () => {
    const onSend = vi.fn();
    render(<ChatInput onSend={onSend} disabled={true} />);
    
    const input = screen.getByPlaceholderText(/ask the agent/i);
    expect(input).toBeDisabled();
  });

  it('should call onSend with message when Enter is pressed', async () => {
    const user = userEvent.setup();
    const onSend = vi.fn();
    
    render(<ChatInput onSend={onSend} disabled={false} />);
    
    const input = screen.getByPlaceholderText(/ask the agent/i);
    await user.type(input, 'Test message{Enter}');
    
    expect(onSend).toHaveBeenCalledWith('Test message', []);
  });

  it('should not send empty messages', async () => {
    const user = userEvent.setup();
    const onSend = vi.fn();
    
    render(<ChatInput onSend={onSend} disabled={false} />);
    
    const input = screen.getByPlaceholderText(/ask the agent/i);
    await user.type(input, '{Enter}');
    
    expect(onSend).not.toHaveBeenCalled();
  });

  it('should clear input after sending', async () => {
    const user = userEvent.setup();
    const onSend = vi.fn();
    
    render(<ChatInput onSend={onSend} disabled={false} />);
    
    const input = screen.getByPlaceholderText(/ask the agent/i) as HTMLInputElement;
    await user.type(input, 'Another message{Enter}');
    
    expect(input.value).toBe('');
  });

  it('should show send button', () => {
    const onSend = vi.fn();
    render(<ChatInput onSend={onSend} disabled={false} />);
    
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(0);
  });

  it('should handle file attachments button', () => {
    const onSend = vi.fn();
    render(<ChatInput onSend={onSend} disabled={false} />);
    
    // Attachment button should be present
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThanOrEqual(2); // Send button + attachment button
  });
});
