import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { SuggestedActions } from './SuggestedActions';

describe('SuggestedActions', () => {
  it('should render all suggested action buttons', () => {
    const onSelect = vi.fn();
    render(<SuggestedActions onSelect={onSelect} />);
    
    expect(screen.getByText('Start new campaign')).toBeInTheDocument();
    expect(screen.getByText('Find prospects')).toBeInTheDocument();
    expect(screen.getByText('Draft messages')).toBeInTheDocument();
    expect(screen.getByText('Analyze audience')).toBeInTheDocument();
    expect(screen.getByText('Review connections')).toBeInTheDocument();
    expect(screen.getByText('Optimize campaign')).toBeInTheDocument();
  });

  it('should call onSelect with prompt when button is clicked', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    
    render(<SuggestedActions onSelect={onSelect} />);
    
    const button = screen.getByText('Find prospects');
    await user.click(button);
    
    expect(onSelect).toHaveBeenCalledWith(expect.stringContaining('prospects'));
  });

  it('should have correct icons for each action', () => {
    const onSelect = vi.fn();
    render(<SuggestedActions onSelect={onSelect} />);
    
    // All buttons should have icons (SVG elements from lucide-react)
    const buttons = screen.getAllByRole('button');
    const EXPECTED_ACTION_COUNT = 6;
    expect(buttons.length).toBe(EXPECTED_ACTION_COUNT);
    
    buttons.forEach(button => {
      const svg = button.querySelector('svg');
      expect(svg).toBeTruthy();
    });
  });

  it('should be accessible', () => {
    const onSelect = vi.fn();
    render(<SuggestedActions onSelect={onSelect} />);
    
    const buttons = screen.getAllByRole('button');
    buttons.forEach(button => {
      expect(button).toBeVisible();
    });
  });
});
