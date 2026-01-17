import { ChatInput } from '@/components/chat/ChatInput';
import { ChatMessage } from '@/components/chat/ChatMessage';
import { SuggestedActions } from '@/components/chat/SuggestedActions';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useToast } from '@/hooks/use-toast';
import { AgentChatClient, ChatMessage as ChatMsg } from '@/lib/agent-chat';
import { Message } from '@/types/agent';
import { Sparkles } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

const chatClient = new AgentChatClient(''); // Use relative URL - proxied by Vite

export function ChatPage(): JSX.Element {
  const [messages, setMessages] = useState<Message[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const [currentAssistantMessage, setCurrentAssistantMessage] = useState('');
  const currentAssistantMessageRef = useRef<string>('');
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { toast } = useToast();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, currentAssistantMessage]);

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
    currentAssistantMessageRef.current = '';

    try {
      // Convert messages to agent format
      const agentMessages: ChatMsg[] = [
        ...messages.map((m) => ({
          role: m.role,
          content: m.content,
        })),
        {
          role: 'user' as const,
          content: userMessage,
        },
      ];

      // Stream responses from agent
      for await (const event of chatClient.streamChat(agentMessages)) {
        console.log('SSE Event:', event.type, event.data);
        
        if (event.type === 'start') {
          // Start event
          currentAssistantMessageRef.current = '';
          setCurrentAssistantMessage('');
        } else if (event.type === 'update') {
          // Raw update from agent - extract text from AgentRunResponseUpdate
          const data = event.data as any;
          if (data && data.Contents && Array.isArray(data.Contents)) {
            // Extract text from Contents array
            for (const content of data.Contents) {
              if (content.type === 'text' && content.Text && content.Text.length > 0) {
                console.log('Adding text:', content.Text);
                currentAssistantMessageRef.current += content.Text;
                setCurrentAssistantMessage(currentAssistantMessageRef.current);
              }
            }
          }
        } else if (event.type === 'message') {
          // Text content
          const content = event.data.content || '';
          currentAssistantMessageRef.current += content;
          setCurrentAssistantMessage(currentAssistantMessageRef.current);
        } else if (event.type === 'tool_call') {
          // Tool invocation
          const toolMsg = `\n🔧 Using tool: ${event.data.name}`;
          currentAssistantMessageRef.current += toolMsg;
          setCurrentAssistantMessage(currentAssistantMessageRef.current);
        } else if (event.type === 'tool_result') {
          // Tool completed
          currentAssistantMessageRef.current += '\n✅ Tool completed';
          setCurrentAssistantMessage(currentAssistantMessageRef.current);
        } else if (event.type === 'done') {
          // Stream complete
          if (currentAssistantMessageRef.current) {
            const agentMessage: Message = {
              id: crypto.randomUUID(),
              role: 'assistant',
              content: currentAssistantMessageRef.current,
              timestamp: new Date(),
            };
            setMessages((prev) => [...prev, agentMessage]);
            setCurrentAssistantMessage('');
            currentAssistantMessageRef.current = '';
          }
          setIsTyping(false);
        } else if (event.type === 'error') {
          // Error occurred
          const errorMsg: Message = {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: `Sorry, I encountered an error: ${event.data.message}`,
            timestamp: new Date(),
          };
          setMessages((prev) => [...prev, errorMsg]);
          setCurrentAssistantMessage('');
          setIsTyping(false);
        }
      }
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
      currentAssistantMessageRef.current = '';
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
