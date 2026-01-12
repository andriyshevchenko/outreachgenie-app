export interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  tools?: ToolCall[];
  attachments?: FileAttachment[];
}

export interface ToolCall {
  id: string;
  name: string;
  status: 'pending' | 'running' | 'completed' | 'error';
  result?: string;
}

export interface FileAttachment {
  id: string;
  name: string;
  type: string;
  size: number;
  url?: string;
}

export interface Campaign {
  id: string;
  name: string;
  status: 'draft' | 'active' | 'paused' | 'completed';
  targetAudience: string;
  messagesCount: number;
  responseRate: number;
  createdAt: Date;
}

export interface CampaignMetrics {
  totalCampaigns: number;
  activeCampaigns: number;
  totalMessages: number;
  responseRate: number;
  connectionsGained: number;
  meetingsBooked: number;
}

export interface Settings {
  developerMode: boolean;
  apiEndpoint: string;
  modelName: string;
  maxTokens: number;
  temperature: number;
  linkedInCookie: string;
  autoSave: boolean;
  notifications: boolean;
}
