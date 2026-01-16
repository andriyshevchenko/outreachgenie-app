import { ChatInput } from '@/components/chat/ChatInput';
import { ChatMessage } from '@/components/chat/ChatMessage';
import { SuggestedActions } from '@/components/chat/SuggestedActions';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useToast } from '@/hooks/use-toast';
import { AgentRunUpdate, aguiClient } from '@/lib/agui';
import { Message } from '@/types/agent';
import { Sparkles } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

export function ChatPage(): JSX.Element {
  const [messages, setMessages] = useState<Message[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const [currentAssistantMessage, setCurrentAssistantMessage] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { toast } = useToast();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, currentAssistantMessage]);

  useEffect(() => {
    return () => {
      aguiClient.disconnect();
    };
  }, []);

  const sendMessageToBackend = async (userMessage: string) => {
    // Add user message immediately
    const userMsg: Message = {
      id: crypto.randomUUID(),
      role: 'user',
      content: userMessage,
      timestamp: new Date(),
    };
    setMessages((prev) => [...prev, userMsg]);
    setIsTyping(true);
    setCurrentAssistantMessage('');

    try {
      await aguiClient.sendMessage(
        userMessage,
        undefined,
        (update: AgentRunUpdate) => {
          // Handle AG-UI streaming updates
          if (update.type === 'TEXT_MESSAGE_CONTENT' && update.delta) {
            // Accumulate text deltas
            setCurrentAssistantMessage((prev) => prev + update.delta);
          } else if (update.type === 'TEXT_MESSAGE_START') {
            // Starting a new message
            setCurrentAssistantMessage('');
          } else if (update.type === 'TEXT_MESSAGE_END') {
            // Message complete - will be finalized in complete callback
            // Don't add message here to avoid duplicates
          } else if (update.type === 'TOOL_CALL_START') {
            // Show tool call in progress
            const toolMsg = `🔧 Using tool: ${update.toolName ?? 'unknown'}`;
            setCurrentAssistantMessage((prev) => prev + '\n' + toolMsg);
          } else if (update.type === 'TOOL_CALL_END' && update.toolResult) {
            // Show tool result
            setCurrentAssistantMessage((prev) => prev + '\n✅ Tool completed');
          } else if (update.type === 'RUN_COMPLETED') {
            // Run complete
            if (currentAssistantMessage) {
              const agentMessage: Message = {
                id: crypto.randomUUID(),
                role: 'assistant',
                content: currentAssistantMessage,
                timestamp: new Date(),
              };
              setMessages((prev) => [...prev, agentMessage]);
              setCurrentAssistantMessage('');
            }
            setIsTyping(false);
          } else if (update.type === 'RUN_FAILED' || update.type === 'ERROR') {
            // Handle errors
            const errorText = update.error ?? update.message ?? 'Unknown error';
            const errorMsg: Message = {
              id: crypto.randomUUID(),
              role: 'assistant',
              content: `Sorry, I encountered an error: ${errorText}`,
              timestamp: new Date(),
            };
            setMessages((prev) => [...prev, errorMsg]);
            setCurrentAssistantMessage('');
            setIsTyping(false);
          }
        },
        (error: Error) => {
          toast({
            title: 'Error',
            description: error.message,
            variant: 'destructive',
          });

          const errorMsg: Message = {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: `Sorry, I encountered an error: ${error.message}`,
            timestamp: new Date(),
          };
          setMessages((prev) => [...prev, errorMsg]);
          setIsTyping(false);
          setCurrentAssistantMessage('');
        },
        () => {
          // Complete callback
          if (currentAssistantMessage) {
            const agentMessage: Message = {
              id: crypto.randomUUID(),
              role: 'assistant',
              content: currentAssistantMessage,
              timestamp: new Date(),
            };
            setMessages((prev) => [...prev, agentMessage]);
            setCurrentAssistantMessage('');
          }
          setIsTyping(false);
        }
      );
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      
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
      setIsTyping(false);
      setCurrentAssistantMessage('');
    }
  };

  const handleSend = (content: string) => {
    void sendMessageToBackend(content);
  };

  const handleSuggestion = (prompt: string) => {
    handleSend(prompt);
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
            {isTyping && currentAssistantMessage && (
              <div className="flex gap-4">
                <div className="w-9 h-9 rounded-full bg-accent flex items-center justify-center">
                  <Sparkles className="w-4 h-4 text-primary animate-pulse-subtle" />
                </div>
                <div className="chat-bubble-agent whitespace-pre-wrap">
                  {currentAssistantMessage}
                </div>
              </div>
            )}
            {isTyping && !currentAssistantMessage && (
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
