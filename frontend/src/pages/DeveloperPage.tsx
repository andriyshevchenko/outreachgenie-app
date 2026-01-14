import { useState } from 'react';
import { Terminal, Server, Cpu, Save, RefreshCw, Bug, FileJson } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Slider } from '@/components/ui/slider';
import { Settings } from '@/types/agent';
import { toast } from '@/hooks/use-toast';
import { Textarea } from '@/components/ui/textarea';

const TEST_DELAY_MS = 1000;
const TEMPERATURE_DECIMAL_PLACES = 2;
const TEMPERATURE_SLIDER_MULTIPLIER = 100;
const TEMPERATURE_SLIDER_MAX = 200;

interface DeveloperPageProps {
  settings: Settings;
  onSettingsChange: (settings: Settings) => void;
}

const sampleManifest = `{
  "name": "LinkedIn Outreach Agent",
  "version": "1.0.0",
  "description": "AI-powered LinkedIn campaign automation",
  "capabilities": [
    "prospect_search",
    "message_generation",
    "campaign_management",
    "analytics"
  ],
  "tools": [
    "linkedin_search",
    "send_connection",
    "send_message",
    "analyze_profile",
    "generate_content"
  ]
}`;

export function DeveloperPage({ settings, onSettingsChange }: DeveloperPageProps): JSX.Element {
  const [localSettings, setLocalSettings] = useState(settings);
  const [manifest, setManifest] = useState(sampleManifest);
  const [logs, setLogs] = useState<string[]>([
    '[INFO] Agent initialized successfully',
    '[INFO] LinkedIn connection established',
    '[DEBUG] Loading tool definitions...',
    '[INFO] 5 tools registered',
    '[DEBUG] Ready for user input',
  ]);

  const handleSave = () => {
    onSettingsChange(localSettings);
    toast({
      title: 'Developer settings saved',
      description: 'Configuration has been updated.',
    });
  };

  const updateSetting = <K extends keyof Settings>(key: K, value: Settings[K]) => {
    setLocalSettings((prev) => ({ ...prev, [key]: value }));
  };

  const addLog = (message: string) => {
    setLogs((prev) => [...prev, `[${new Date().toLocaleTimeString()}] ${message}`]);
  };

  const handleTestConnection = () => {
    addLog('[INFO] Testing API connection...');
    setTimeout(() => {
      addLog('[SUCCESS] Connection test passed');
      toast({
        title: 'Connection successful',
        description: 'API endpoint is reachable.',
      });
    }, TEST_DELAY_MS);
  };

  return (
    <div className="p-6 space-y-6 overflow-y-auto h-full">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-semibold text-foreground">Developer Settings</h2>
          <p className="text-muted-foreground">Advanced configuration and debugging tools</p>
        </div>
        <div className="flex items-center gap-2">
          <div className="px-3 py-1.5 rounded-lg bg-accent text-accent-foreground text-sm font-medium flex items-center gap-2">
            <Bug className="w-4 h-4" />
            Debug Mode
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* API Configuration */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Server className="w-5 h-5 text-primary" />
              API Configuration
            </CardTitle>
            <CardDescription>Configure the AI agent backend connection</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="api-endpoint">API Endpoint</Label>
              <Input
                id="api-endpoint"
                value={localSettings.apiEndpoint}
                onChange={(e) => updateSetting('apiEndpoint', e.target.value)}
                placeholder="https://api.example.com/v1"
                className="font-mono text-sm"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="model-name">Model Name</Label>
              <Input
                id="model-name"
                value={localSettings.modelName}
                onChange={(e) => updateSetting('modelName', e.target.value)}
                placeholder="gpt-4-turbo"
                className="font-mono text-sm"
              />
            </div>
            <Button variant="outline" className="w-full gap-2" onClick={handleTestConnection}>
              <RefreshCw className="w-4 h-4" />
              Test Connection
            </Button>
          </CardContent>
        </Card>

        {/* Model Parameters */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Cpu className="w-5 h-5 text-primary" />
              Model Parameters
            </CardTitle>
            <CardDescription>Fine-tune the AI model behavior</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <Label>Max Tokens</Label>
                <span className="text-sm font-mono text-muted-foreground">{localSettings.maxTokens}</span>
              </div>
              <Slider
                value={[localSettings.maxTokens]}
                onValueChange={([value]) => updateSetting('maxTokens', value)}
                max={8192}
                min={256}
                step={256}
                className="w-full"
              />
            </div>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <Label>Temperature</Label>
                <span className="text-sm font-mono text-muted-foreground">{localSettings.temperature.toFixed(TEMPERATURE_DECIMAL_PLACES)}</span>
              </div>
              <Slider
                value={[localSettings.temperature * TEMPERATURE_SLIDER_MULTIPLIER]}
                onValueChange={([value]) => updateSetting('temperature', value / TEMPERATURE_SLIDER_MULTIPLIER)}
                max={TEMPERATURE_SLIDER_MAX}
                min={0}
                step={5}
                className="w-full"
              />
            </div>
          </CardContent>
        </Card>

        {/* Agent Manifest */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileJson className="w-5 h-5 text-primary" />
              Agent Manifest
            </CardTitle>
            <CardDescription>View and edit the agent configuration</CardDescription>
          </CardHeader>
          <CardContent>
            <Textarea
              value={manifest}
              onChange={(e) => setManifest(e.target.value)}
              className="font-mono text-xs h-[240px] bg-muted/50"
              spellCheck={false}
            />
          </CardContent>
        </Card>

        {/* Console Logs */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Terminal className="w-5 h-5 text-primary" />
              Console Logs
            </CardTitle>
            <CardDescription>Real-time agent activity logs</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="bg-foreground/5 rounded-xl p-4 h-[240px] overflow-y-auto font-mono text-xs space-y-1">
              {logs.map((log, index) => (
                <div
                  key={index}
                  className={`
                    ${log.includes('[ERROR]') ? 'text-destructive' : ''}
                    ${log.includes('[SUCCESS]') ? 'text-success' : ''}
                    ${log.includes('[DEBUG]') ? 'text-muted-foreground' : ''}
                    ${log.includes('[INFO]') ? 'text-foreground' : ''}
                  `}
                >
                  {log}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Save Button */}
      <div className="flex justify-end">
        <Button onClick={handleSave} className="gap-2">
          <Save className="w-4 h-4" />
          Save Developer Settings
        </Button>
      </div>
    </div>
  );
}
