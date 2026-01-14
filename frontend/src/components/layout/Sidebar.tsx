import { cn } from '@/lib/utils';
import { BarChart3, Code2, FolderKanban, Linkedin, MessageSquare, Settings, Sparkles } from 'lucide-react';

interface SidebarProps {
  currentPage: string;
  onNavigate: (page: string) => void;
  developerMode: boolean;
}

const navItems = [
  { id: 'chat', icon: MessageSquare, label: 'Agent Chat' },
  { id: 'campaigns', icon: FolderKanban, label: 'Campaigns' },
  { id: 'analytics', icon: BarChart3, label: 'Analytics' },
  { id: 'settings', icon: Settings, label: 'Settings' },
];

export function Sidebar({ currentPage, onNavigate, developerMode }: SidebarProps): JSX.Element {
  return (
    <aside className="w-72 bg-sidebar border-r border-sidebar-border flex flex-col h-screen">
      {/* Logo */}
      <div className="p-6 border-b border-sidebar-border">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-primary flex items-center justify-center shadow-soft">
            <Linkedin className="w-5 h-5 text-primary-foreground" />
          </div>
          <div>
            <h1 className="font-semibold text-foreground">LinkedIn Agent</h1>
            <p className="text-xs text-muted-foreground">AI Outreach Platform</p>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 space-y-1">
        {navItems.map((item) => (
          <button
            key={item.id}
            onClick={() => onNavigate(item.id)}
            className={cn(
              'nav-item w-full',
              currentPage === item.id && 'nav-item-active'
            )}
          >
            <item.icon className="w-5 h-5" />
            <span>{item.label}</span>
          </button>
        ))}

        {developerMode && (
          <button
            onClick={() => onNavigate('developer')}
            className={cn(
              'nav-item w-full mt-4 border-t border-sidebar-border pt-4',
              currentPage === 'developer' && 'nav-item-active'
            )}
          >
            <Code2 className="w-5 h-5" />
            <span>Developer</span>
          </button>
        )}
      </nav>

      {/* Agent Status */}
      <div className="p-4 border-t border-sidebar-border">
        <div className="glass-panel p-4">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-full bg-accent flex items-center justify-center">
              <Sparkles className="w-4 h-4 text-primary" />
            </div>
            <div className="flex-1">
              <p className="text-sm font-medium text-foreground">Agent Ready</p>
              <p className="text-xs text-muted-foreground">Connected</p>
            </div>
            <div className="w-2 h-2 rounded-full bg-success animate-pulse-subtle" />
          </div>
        </div>
      </div>
    </aside>
  );
}
