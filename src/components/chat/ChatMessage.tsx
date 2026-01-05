import { User, Sparkles, Wrench, CheckCircle2, Loader2, AlertCircle, Paperclip } from 'lucide-react';
import { Message } from '@/types/agent';
import { cn } from '@/lib/utils';

interface ChatMessageProps {
  message: Message;
}

export function ChatMessage({ message }: ChatMessageProps) {
  const isUser = message.role === 'user';

  return (
    <div
      className={cn(
        'flex gap-4 animate-slide-up',
        isUser ? 'flex-row-reverse' : 'flex-row'
      )}
    >
      {/* Avatar */}
      <div
        className={cn(
          'w-9 h-9 rounded-full flex items-center justify-center flex-shrink-0',
          isUser ? 'bg-primary' : 'bg-accent'
        )}
      >
        {isUser ? (
          <User className="w-4 h-4 text-primary-foreground" />
        ) : (
          <Sparkles className="w-4 h-4 text-primary" />
        )}
      </div>

      {/* Message Content */}
      <div className={cn('max-w-[70%] space-y-2', isUser ? 'items-end' : 'items-start')}>
        {/* Attachments */}
        {message.attachments && message.attachments.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-2">
            {message.attachments.map((file) => (
              <div
                key={file.id}
                className="flex items-center gap-2 px-3 py-1.5 bg-muted rounded-lg text-sm"
              >
                <Paperclip className="w-3.5 h-3.5 text-muted-foreground" />
                <span className="text-foreground">{file.name}</span>
                <span className="text-muted-foreground text-xs">
                  ({(file.size / 1024).toFixed(1)}KB)
                </span>
              </div>
            ))}
          </div>
        )}

        {/* Message Bubble */}
        <div className={isUser ? 'chat-bubble-user' : 'chat-bubble-agent'}>
          <p className="text-sm leading-relaxed whitespace-pre-wrap">{message.content}</p>
        </div>

        {/* Tool Calls */}
        {message.tools && message.tools.length > 0 && (
          <div className="space-y-2 mt-2">
            {message.tools.map((tool) => (
              <div
                key={tool.id}
                className="flex items-center gap-2 px-3 py-2 bg-accent/50 rounded-xl border border-border/50"
              >
                <div
                  className={cn(
                    'w-6 h-6 rounded-full flex items-center justify-center',
                    tool.status === 'completed' && 'bg-success/10',
                    tool.status === 'running' && 'bg-primary/10',
                    tool.status === 'error' && 'bg-destructive/10',
                    tool.status === 'pending' && 'bg-muted'
                  )}
                >
                  {tool.status === 'completed' && (
                    <CheckCircle2 className="w-3.5 h-3.5 text-success" />
                  )}
                  {tool.status === 'running' && (
                    <Loader2 className="w-3.5 h-3.5 text-primary animate-spin" />
                  )}
                  {tool.status === 'error' && (
                    <AlertCircle className="w-3.5 h-3.5 text-destructive" />
                  )}
                  {tool.status === 'pending' && (
                    <Wrench className="w-3.5 h-3.5 text-muted-foreground" />
                  )}
                </div>
                <span className="text-xs font-medium text-foreground">{tool.name}</span>
                {tool.result && (
                  <span className="text-xs text-muted-foreground ml-2">
                    → {tool.result}
                  </span>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Timestamp */}
        <p className="text-xs text-muted-foreground mt-1">
          {message.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </p>
      </div>
    </div>
  );
}
