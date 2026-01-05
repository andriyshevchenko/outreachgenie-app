import { useState, useRef, useEffect } from 'react';
import { Sparkles } from 'lucide-react';
import { ChatMessage } from '@/components/chat/ChatMessage';
import { ChatInput } from '@/components/chat/ChatInput';
import { SuggestedActions } from '@/components/chat/SuggestedActions';
import { Message, FileAttachment } from '@/types/agent';
import { ScrollArea } from '@/components/ui/scroll-area';

export function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const simulateAgentResponse = async (userMessage: string) => {
    setIsTyping(true);

    // Simulate tool calls
    const toolCalls = [];
    if (userMessage.toLowerCase().includes('campaign')) {
      toolCalls.push(
        { id: '1', name: 'analyze_target_audience', status: 'completed' as const, result: '250 prospects found' },
        { id: '2', name: 'generate_message_templates', status: 'completed' as const, result: '3 templates created' }
      );
    } else if (userMessage.toLowerCase().includes('prospect')) {
      toolCalls.push(
        { id: '1', name: 'linkedin_search', status: 'completed' as const, result: '180 matches' },
        { id: '2', name: 'enrich_profiles', status: 'completed' as const, result: 'Data enriched' }
      );
    }

    await new Promise((resolve) => setTimeout(resolve, 1500));

    const responses = [
      "I've analyzed your request and prepared a comprehensive outreach strategy. Based on the target audience analysis, I've identified 250 high-potential prospects in your niche. Here's what I've done:\n\n1. **Audience Segmentation**: Divided prospects into 3 tiers based on engagement likelihood\n2. **Message Templates**: Created personalized connection requests with 85%+ acceptance rate potential\n3. **Schedule Optimization**: Identified best times for outreach based on prospect activity patterns",
      "I've found and enriched 180 prospect profiles matching your criteria. The data includes:\n\n• **Decision Makers**: 120 C-level executives\n• **Influencers**: 45 Senior Directors\n• **Champions**: 15 high-engagement users\n\nWould you like me to draft personalized messages for each segment?",
      "Great question! I can help you optimize your LinkedIn outreach strategy. Based on best practices and your account history, I recommend:\n\n1. **Personalization**: Include specific company mentions\n2. **Value-First**: Lead with insights, not asks\n3. **Timing**: Schedule messages for Tuesday-Thursday, 9-11 AM recipient time",
    ];

    const agentMessage: Message = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: responses[Math.floor(Math.random() * responses.length)],
      timestamp: new Date(),
      tools: toolCalls.length > 0 ? toolCalls : undefined,
    };

    setMessages((prev) => [...prev, agentMessage]);
    setIsTyping(false);
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
    simulateAgentResponse(content);
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
