import { useState, useRef, useEffect } from 'react';
import { Sparkles } from 'lucide-react';
import { ChatMessage } from '@/components/chat/ChatMessage';
import { ChatInput } from '@/components/chat/ChatInput';
import { SuggestedActions } from '@/components/chat/SuggestedActions';
import { Message, FileAttachment } from '@/types/agent';
import { ScrollArea } from '@/components/ui/scroll-area';
import { apiClient, ApiError } from '@/lib/api';
import { useToast } from '@/hooks/use-toast';

export function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { toast } = useToast();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const sendMessageToBackend = async (userMessage: string) => {
    setIsTyping(true);

    try {
      const response = await apiClient.sendMessage(userMessage);
      
      const agentMessage: Message = {
        id: response.messageId,
        role: 'assistant',
        content: response.content,
        timestamp: new Date(response.timestamp),
      };

      setMessages((prev) => [...prev, agentMessage]);
    } catch (error) {
      const apiError = error as ApiError;
      const errorMessage = apiError.statusCode === 0 
        ? 'Failed to connect to backend. Make sure the API server is running.'
        : `API Error: ${apiError.message}`;
      
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'destructive',
      });

      const errorMsg: Message = {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: `Sorry, I encountered an error: ${errorMessage}`,
        timestamp: new Date(),
      };

      setMessages((prev) => [...prev, errorMsg]);
    } finally {
      setIsTyping(false);
    }
  };

  const handleSend = (content: string, attachments: FileAttachment[]) => {
    const userMessage: Message = {
      id: crypto.randomUUID(),
      role: 'user',
      content,
      timestamp: new Date(),
      attachments: attachments.length > 0 ? attachments : undefined,
    };

    setMessages((prev) => [...prev, userMessage]);
    sendMessageToBackend(content);
  };

  const handleSuggestion = (prompt: string) => {
    handleSend(prompt, []);
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <header className="px-6 py-4 border-b border-border bg-card/50 backdrop-blur-sm">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-accent flex items-center justify-center">
            <Sparkles className="w-5 h-5 text-primary" />
          </div>
          <div>
            <h2 className="font-semibold text-foreground">LinkedIn Outreach Agent</h2>
            <p className="text-sm text-muted-foreground">AI-powered campaign automation</p>
          </div>
        </div>
      </header>

      {/* Messages Area */}
      <ScrollArea className="flex-1 p-6">
        {messages.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full max-w-xl mx-auto">
            <div className="w-16 h-16 rounded-2xl bg-accent flex items-center justify-center mb-6">
              <Sparkles className="w-8 h-8 text-primary" />
            </div>
            <h3 className="text-xl font-semibold text-foreground mb-2">
              How can I help you today?
            </h3>
            <p className="text-muted-foreground text-center mb-8">
              I can help you create and manage LinkedIn outreach campaigns, find prospects, and craft personalized messages.
            </p>
            <SuggestedActions onSelect={handleSuggestion} />
          </div>
        ) : (
          <div className="space-y-6 max-w-3xl mx-auto">
            {messages.map((message) => (
              <ChatMessage key={message.id} message={message} />
            ))}
            {isTyping && (
              <div className="flex gap-4">
                <div className="w-9 h-9 rounded-full bg-accent flex items-center justify-center">
                  <Sparkles className="w-4 h-4 text-primary animate-pulse-subtle" />
                </div>
                <div className="chat-bubble-agent">
                  <div className="flex gap-1">
                    <div className="w-2 h-2 rounded-full bg-muted-foreground animate-bounce" style={{ animationDelay: '0ms' }} />
                    <div className="w-2 h-2 rounded-full bg-muted-foreground animate-bounce" style={{ animationDelay: '150ms' }} />
                    <div className="w-2 h-2 rounded-full bg-muted-foreground animate-bounce" style={{ animationDelay: '300ms' }} />
                  </div>
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>
        )}
      </ScrollArea>

      {/* Input */}
      <ChatInput onSend={handleSend} disabled={isTyping} />
    </div>
  );
}
