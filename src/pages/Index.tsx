import { useState } from 'react';
import { Sidebar } from '@/components/layout/Sidebar';
import { ChatPage } from '@/pages/ChatPage';
import { AnalyticsPage } from '@/pages/AnalyticsPage';
import { SettingsPage } from '@/pages/SettingsPage';
import { DeveloperPage } from '@/pages/DeveloperPage';
import { Settings } from '@/types/agent';

const defaultSettings: Settings = {
  developerMode: false,
  apiEndpoint: 'https://api.linkedin-agent.ai/v1',
  modelName: 'gpt-4-turbo',
  maxTokens: 4096,
  temperature: 0.7,
  linkedInCookie: '',
  autoSave: true,
  notifications: true,
};

const Index = () => {
  const [currentPage, setCurrentPage] = useState('chat');
  const [settings, setSettings] = useState<Settings>(defaultSettings);

  const renderPage = () => {
    switch (currentPage) {
      case 'chat':
        return <ChatPage />;
      case 'analytics':
        return <AnalyticsPage />;
      case 'settings':
        return <SettingsPage settings={settings} onSettingsChange={setSettings} />;
      case 'developer':
        return <DeveloperPage settings={settings} onSettingsChange={setSettings} />;
      default:
        return <ChatPage />;
    }
  };

  return (
    <div className="flex h-screen bg-background overflow-hidden">
      <Sidebar
        currentPage={currentPage}
        onNavigate={setCurrentPage}
        developerMode={settings.developerMode}
      />
      <main className="flex-1 flex flex-col overflow-hidden">
        {renderPage()}
      </main>
    </div>
  );
};

export default Index;
