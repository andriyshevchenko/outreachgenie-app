import { useState, useRef } from 'react';
import { Send, Paperclip, X, Mic } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { FileAttachment } from '@/types/agent';
import { cn } from '@/lib/utils';

interface ChatInputProps {
  onSend: (message: string, attachments: FileAttachment[]) => void;
  disabled?: boolean;
}

export function ChatInput({ onSend, disabled }: ChatInputProps): JSX.Element {
  const [message, setMessage] = useState('');
  const [attachments, setAttachments] = useState<FileAttachment[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const handleSubmit = () => {
    if (message.trim() || attachments.length > 0) {
      onSend(message.trim(), attachments);
      setMessage('');
      setAttachments([]);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files) {
      const newAttachments: FileAttachment[] = Array.from(files).map((file) => ({
        id: crypto.randomUUID(),
        name: file.name,
        type: file.type,
        size: file.size,
      }));
      setAttachments((prev) => [...prev, ...newAttachments]);
    }
    e.target.value = '';
  };

  const removeAttachment = (id: string) => {
    setAttachments((prev) => prev.filter((a) => a.id !== id));
  };

  return (
    <div className="p-4">
      <div className="floating-panel p-4">
        {/* Attachments Preview */}
        {attachments.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-3 pb-3 border-b border-border">
            {attachments.map((file) => (
              <div
                key={file.id}
                className="flex items-center gap-2 px-3 py-1.5 bg-muted rounded-lg text-sm group"
              >
                <Paperclip className="w-3.5 h-3.5 text-muted-foreground" />
                <span className="text-foreground max-w-[120px] truncate">{file.name}</span>
                <button
                  onClick={() => removeAttachment(file.id)}
                  className="opacity-0 group-hover:opacity-100 transition-opacity"
                >
                  <X className="w-3.5 h-3.5 text-muted-foreground hover:text-destructive" />
                </button>
              </div>
            ))}
          </div>
        )}

        {/* Input Area */}
        <div className="flex items-end gap-3">
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileChange}
            className="hidden"
            multiple
          />

          <Button
            variant="ghost"
            size="icon"
            onClick={() => fileInputRef.current?.click()}
            className="flex-shrink-0 text-muted-foreground hover:text-foreground"
          >
            <Paperclip className="w-5 h-5" />
          </Button>

          <div className="flex-1 relative">
            <textarea
              ref={textareaRef}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Ask the agent to start a campaign, draft messages, or analyze prospects..."
              className={cn(
                'w-full resize-none bg-transparent border-0 focus:ring-0 focus:outline-none text-sm',
                'placeholder:text-muted-foreground min-h-[24px] max-h-[120px]'
              )}
              rows={1}
              disabled={disabled}
            />
          </div>

          <Button
            variant="ghost"
            size="icon"
            className="flex-shrink-0 text-muted-foreground hover:text-foreground"
          >
            <Mic className="w-5 h-5" />
          </Button>

          <Button
            variant="copilot"
            size="icon-lg"
            onClick={handleSubmit}
            disabled={disabled || (!message.trim() && attachments.length === 0)}
            className="flex-shrink-0"
          >
            <Send className="w-5 h-5" />
          </Button>
        </div>
      </div>
    </div>
  );
}
