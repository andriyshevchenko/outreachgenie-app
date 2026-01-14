import { Rocket, Users, MessageSquare, Search, Target, Zap } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface SuggestedActionsProps {
  onSelect: (action: string) => void;
}

const suggestions = [
  {
    icon: Rocket,
    label: 'Start new campaign',
    prompt: 'Start a new LinkedIn outreach campaign for B2B SaaS decision makers',
  },
  {
    icon: Search,
    label: 'Find prospects',
    prompt: 'Find and analyze potential prospects in the tech industry',
  },
  {
    icon: MessageSquare,
    label: 'Draft messages',
    prompt: 'Draft personalized connection request messages',
  },
  {
    icon: Target,
    label: 'Analyze audience',
    prompt: 'Analyze my target audience and suggest improvements',
  },
  {
    icon: Users,
    label: 'Review connections',
    prompt: 'Review my recent connections and engagement rates',
  },
  {
    icon: Zap,
    label: 'Optimize campaign',
    prompt: 'Analyze my current campaigns and suggest optimizations',
  },
];

export function SuggestedActions({ onSelect }: SuggestedActionsProps): JSX.Element {
  return (
    <div className="grid grid-cols-2 gap-3">
      {suggestions.map((suggestion) => (
        <Button
          key={suggestion.label}
          variant="tool"
          className="h-auto py-4 px-4 justify-start gap-3 text-left"
          onClick={() => onSelect(suggestion.prompt)}
        >
          <div className="w-8 h-8 rounded-lg bg-accent flex items-center justify-center flex-shrink-0">
            <suggestion.icon className="w-4 h-4 text-primary" />
          </div>
          <span className="text-sm font-medium">{suggestion.label}</span>
        </Button>
      ))}
    </div>
  );
}
