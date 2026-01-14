import { useState } from 'react';
import { Bell, Shield, User, Code2, Linkedin, Save } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Settings } from '@/types/agent';
import { toast } from '@/hooks/use-toast';

interface SettingsPageProps {
  settings: Settings;
  onSettingsChange: (settings: Settings) => void;
}

export function SettingsPage({ settings, onSettingsChange }: SettingsPageProps): JSX.Element {
  const [localSettings, setLocalSettings] = useState(settings);

  const handleSave = () => {
    onSettingsChange(localSettings);
    toast({
      title: 'Settings saved',
      description: 'Your preferences have been updated successfully.',
    });
  };

  const updateSetting = <K extends keyof Settings>(key: K, value: Settings[K]) => {
    setLocalSettings((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <div className="p-6 space-y-6 overflow-y-auto h-full max-w-4xl">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-semibold text-foreground">Settings</h2>
        <p className="text-muted-foreground">Manage your agent configuration and preferences</p>
      </div>

      {/* Profile Section */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <User className="w-5 h-5 text-primary" />
            Profile Settings
          </CardTitle>
          <CardDescription>Configure your LinkedIn connection and profile</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="linkedin-cookie">LinkedIn Session Cookie</Label>
              <Input
                id="linkedin-cookie"
                type="password"
                value={localSettings.linkedInCookie}
                onChange={(e) => updateSetting('linkedInCookie', e.target.value)}
                placeholder="li_at cookie value"
              />
            </div>
          </div>
          <div className="flex items-center gap-3 p-4 rounded-xl bg-accent/50">
            <Linkedin className="w-5 h-5 text-primary" />
            <div className="flex-1">
              <p className="text-sm font-medium text-foreground">LinkedIn Connection</p>
              <p className="text-xs text-muted-foreground">Status: Connected</p>
            </div>
            <div className="w-2 h-2 rounded-full bg-success" />
          </div>
        </CardContent>
      </Card>

      {/* Notifications */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Bell className="w-5 h-5 text-primary" />
            Notifications
          </CardTitle>
          <CardDescription>Configure how you receive updates</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium text-foreground">Push Notifications</p>
              <p className="text-sm text-muted-foreground">Receive alerts for campaign updates</p>
            </div>
            <Switch
              checked={localSettings.notifications}
              onCheckedChange={(checked) => updateSetting('notifications', checked)}
            />
          </div>
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium text-foreground">Auto-save Conversations</p>
              <p className="text-sm text-muted-foreground">Automatically save chat history</p>
            </div>
            <Switch
              checked={localSettings.autoSave}
              onCheckedChange={(checked) => updateSetting('autoSave', checked)}
            />
          </div>
        </CardContent>
      </Card>

      {/* Security */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="w-5 h-5 text-primary" />
            Security & Privacy
          </CardTitle>
          <CardDescription>Manage your security preferences</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium text-foreground">Developer Mode</p>
              <p className="text-sm text-muted-foreground">Enable advanced settings and debugging tools</p>
            </div>
            <Switch
              checked={localSettings.developerMode}
              onCheckedChange={(checked) => updateSetting('developerMode', checked)}
            />
          </div>
        </CardContent>
      </Card>

      {/* Developer Settings Preview */}
      {localSettings.developerMode && (
        <Card className="border-primary/20 bg-accent/30">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Code2 className="w-5 h-5 text-primary" />
              Developer Settings Preview
            </CardTitle>
            <CardDescription>These settings are available in the Developer section</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-4">
              <div className="p-3 rounded-lg bg-card">
                <p className="text-xs text-muted-foreground mb-1">API Endpoint</p>
                <p className="text-sm font-mono text-foreground truncate">{localSettings.apiEndpoint}</p>
              </div>
              <div className="p-3 rounded-lg bg-card">
                <p className="text-xs text-muted-foreground mb-1">Model</p>
                <p className="text-sm font-mono text-foreground">{localSettings.modelName}</p>
              </div>
              <div className="p-3 rounded-lg bg-card">
                <p className="text-xs text-muted-foreground mb-1">Max Tokens</p>
                <p className="text-sm font-mono text-foreground">{localSettings.maxTokens}</p>
              </div>
              <div className="p-3 rounded-lg bg-card">
                <p className="text-xs text-muted-foreground mb-1">Temperature</p>
                <p className="text-sm font-mono text-foreground">{localSettings.temperature}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Save Button */}
      <div className="flex justify-end">
        <Button onClick={handleSave} className="gap-2">
          <Save className="w-4 h-4" />
          Save Settings
        </Button>
      </div>
    </div>
  );
}
