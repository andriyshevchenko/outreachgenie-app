import { useState, useEffect } from 'react';
import { Plus, Play, Pause, Trash2, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { apiClient, Campaign, CampaignStatus, ApiError } from '@/lib/api';
import { signalRHub, CampaignStateChangedEvent } from '@/lib/signalr';
import { useToast } from '@/hooks/use-toast';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';

export function CampaignsPage() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [newCampaignName, setNewCampaignName] = useState('');
  const [newCampaignAudience, setNewCampaignAudience] = useState('');
  const { toast } = useToast();

  const loadCampaigns = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getCampaigns();
      setCampaigns(data);
    } catch (error) {
      const apiError = error as ApiError;
      toast({
        title: 'Error',
        description: `Failed to load campaigns: ${apiError.message}`,
        variant: 'destructive',
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCampaigns();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const handleCampaignStateChanged = (event: CampaignStateChangedEvent) => {
      setCampaigns((prev) =>
        prev.map((c) =>
          c.id === event.campaignId
            ? { ...c, status: event.status as CampaignStatus, updatedAt: event.timestamp }
            : c
        )
      );
    };

    if (signalRHub.connectionState !== null) {
      signalRHub.onCampaignStateChanged(handleCampaignStateChanged);
    }

    return () => {
      if (signalRHub.connectionState !== null) {
        signalRHub.offAll();
      }
    };
  }, []);

  const handleCreateCampaign = async () => {
    if (!newCampaignName.trim() || !newCampaignAudience.trim()) {
      toast({
        title: 'Validation Error',
        description: 'Campaign name and target audience are required',
        variant: 'destructive',
      });
      return;
    }

    try {
      const campaign = await apiClient.createCampaign({
        name: newCampaignName,
        targetAudience: newCampaignAudience,
      });
      setCampaigns((prev) => [campaign, ...prev]);
      setIsCreateDialogOpen(false);
      setNewCampaignName('');
      setNewCampaignAudience('');
      toast({
        title: 'Success',
        description: 'Campaign created successfully',
      });
    } catch (error) {
      const apiError = error as ApiError;
      toast({
        title: 'Error',
        description: `Failed to create campaign: ${apiError.message}`,
        variant: 'destructive',
      });
    }
  };

  const handlePauseCampaign = async (id: string) => {
    try {
      await apiClient.pauseCampaign(id);
      toast({
        title: 'Success',
        description: 'Campaign paused',
      });
    } catch (error) {
      const apiError = error as ApiError;
      toast({
        title: 'Error',
        description: `Failed to pause campaign: ${apiError.message}`,
        variant: 'destructive',
      });
    }
  };

  const handleResumeCampaign = async (id: string) => {
    try {
      await apiClient.resumeCampaign(id);
      toast({
        title: 'Success',
        description: 'Campaign resumed',
      });
    } catch (error) {
      const apiError = error as ApiError;
      toast({
        title: 'Error',
        description: `Failed to resume campaign: ${apiError.message}`,
        variant: 'destructive',
      });
    }
  };

  const handleDeleteCampaign = async (id: string) => {
    if (!confirm('Are you sure you want to delete this campaign?')) {
      return;
    }

    try {
      await apiClient.deleteCampaign(id);
      setCampaigns((prev) => prev.filter((c) => c.id !== id));
      toast({
        title: 'Success',
        description: 'Campaign deleted',
      });
    } catch (error) {
      const apiError = error as ApiError;
      toast({
        title: 'Error',
        description: `Failed to delete campaign: ${apiError.message}`,
        variant: 'destructive',
      });
    }
  };

  const getStatusBadgeVariant = (status: CampaignStatus): 'default' | 'secondary' | 'destructive' | 'outline' => {
    switch (status) {
      case CampaignStatus.Initializing:
        return 'secondary';
      case CampaignStatus.Active:
        return 'default';
      case CampaignStatus.Paused:
        return 'secondary';
      case CampaignStatus.Completed:
        return 'outline';
      case CampaignStatus.Draft:
        return 'secondary';
      default:
        return 'outline';
    }
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <header className="px-6 py-4 border-b border-border bg-card/50 backdrop-blur-sm">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="font-semibold text-foreground">Campaigns</h2>
            <p className="text-sm text-muted-foreground">Manage your LinkedIn outreach campaigns</p>
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={loadCampaigns}
              disabled={loading}
            >
              <RefreshCw className={`w-4 h-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
            <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
              <DialogTrigger asChild>
                <Button size="sm">
                  <Plus className="w-4 h-4 mr-2" />
                  New Campaign
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Create New Campaign</DialogTitle>
                  <DialogDescription>
                    Set up a new LinkedIn outreach campaign with target audience and goals.
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="name">Campaign Name</Label>
                    <Input
                      id="name"
                      placeholder="e.g., Q1 2026 SaaS Founders Outreach"
                      value={newCampaignName}
                      onChange={(e) => setNewCampaignName(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="audience">Target Audience</Label>
                    <Textarea
                      id="audience"
                      placeholder="e.g., CTOs and VPs of Engineering at mid-size SaaS companies (50-500 employees) in North America"
                      value={newCampaignAudience}
                      onChange={(e) => setNewCampaignAudience(e.target.value)}
                      rows={4}
                    />
                  </div>
                </div>
                <DialogFooter>
                  <Button variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
                    Cancel
                  </Button>
                  <Button onClick={handleCreateCampaign}>
                    Create Campaign
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          </div>
        </div>
      </header>

      {/* Campaigns List */}
      <div className="flex-1 overflow-auto p-6">
        {loading && campaigns.length === 0 ? (
          <div className="flex items-center justify-center h-full">
            <div className="text-center">
              <RefreshCw className="w-8 h-8 mx-auto mb-4 animate-spin text-muted-foreground" />
              <p className="text-muted-foreground">Loading campaigns...</p>
            </div>
          </div>
        ) : campaigns.length === 0 ? (
          <div className="flex items-center justify-center h-full">
            <div className="text-center max-w-md">
              <h3 className="text-lg font-semibold mb-2">No campaigns yet</h3>
              <p className="text-muted-foreground mb-4">
                Create your first LinkedIn outreach campaign to get started.
              </p>
              <Button onClick={() => setIsCreateDialogOpen(true)}>
                <Plus className="w-4 h-4 mr-2" />
                Create Campaign
              </Button>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {campaigns.map((campaign) => (
              <Card key={campaign.id} data-campaign-card data-campaign-id={campaign.id}>
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <CardTitle className="text-base">{campaign.name}</CardTitle>
                      <CardDescription className="mt-1">
                        {campaign.targetAudience}
                      </CardDescription>
                    </div>
                    <Badge variant={getStatusBadgeVariant(campaign.status)}>
                      {campaign.status}
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="flex items-center justify-between text-xs text-muted-foreground mb-4">
                    <span>Created: {new Date(campaign.createdAt).toLocaleDateString()}</span>
                    <span>Updated: {new Date(campaign.updatedAt).toLocaleDateString()}</span>
                  </div>
                  <div className="flex gap-2">
                    {campaign.status === CampaignStatus.Active && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handlePauseCampaign(campaign.id)}
                        className="flex-1"
                      >
                        <Pause className="w-4 h-4 mr-1" />
                        Pause
                      </Button>
                    )}
                    {(campaign.status === CampaignStatus.Paused || campaign.status === CampaignStatus.Draft) && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleResumeCampaign(campaign.id)}
                        className="flex-1"
                      >
                        <Play className="w-4 h-4 mr-1" />
                        {campaign.status === CampaignStatus.Draft ? 'Start' : 'Resume'}
                      </Button>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleDeleteCampaign(campaign.id)}
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
