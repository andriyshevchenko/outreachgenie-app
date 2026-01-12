import { TrendingUp, Users, MessageSquare, Calendar, Target, Zap, ArrowUpRight, ArrowDownRight } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, BarChart, Bar } from 'recharts';

const performanceData = [
  { name: 'Mon', connections: 12, messages: 45, responses: 8 },
  { name: 'Tue', connections: 18, messages: 62, responses: 15 },
  { name: 'Wed', connections: 15, messages: 58, responses: 12 },
  { name: 'Thu', connections: 25, messages: 78, responses: 22 },
  { name: 'Fri', connections: 22, messages: 70, responses: 18 },
  { name: 'Sat', connections: 8, messages: 25, responses: 5 },
  { name: 'Sun', connections: 5, messages: 15, responses: 3 },
];

const campaignData = [
  { name: 'Tech Leaders', value: 85 },
  { name: 'SaaS Founders', value: 72 },
  { name: 'VCs', value: 65 },
  { name: 'HR Directors', value: 58 },
];

const metrics = [
  {
    title: 'Total Connections',
    value: '1,234',
    change: '+12.5%',
    trend: 'up',
    icon: Users,
    color: 'text-primary',
    bgColor: 'bg-accent',
  },
  {
    title: 'Messages Sent',
    value: '8,456',
    change: '+8.2%',
    trend: 'up',
    icon: MessageSquare,
    color: 'text-success',
    bgColor: 'bg-success/10',
  },
  {
    title: 'Response Rate',
    value: '24.8%',
    change: '-2.1%',
    trend: 'down',
    icon: Target,
    color: 'text-warning',
    bgColor: 'bg-warning/10',
  },
  {
    title: 'Meetings Booked',
    value: '47',
    change: '+18.3%',
    trend: 'up',
    icon: Calendar,
    color: 'text-primary',
    bgColor: 'bg-accent',
  },
];

const recentCampaigns = [
  { name: 'Q4 Tech Outreach', status: 'active', progress: 78, leads: 156 },
  { name: 'SaaS Decision Makers', status: 'active', progress: 45, leads: 89 },
  { name: 'Series A Founders', status: 'paused', progress: 92, leads: 234 },
  { name: 'HR Tech Buyers', status: 'completed', progress: 100, leads: 312 },
];

export function AnalyticsPage() {
  return (
    <div className="p-6 space-y-6 overflow-y-auto h-full">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-semibold text-foreground">Analytics Dashboard</h2>
          <p className="text-muted-foreground">Track your campaign performance and metrics</p>
        </div>
        <div className="flex items-center gap-2 px-4 py-2 bg-accent rounded-xl text-sm">
          <Calendar className="w-4 h-4 text-primary" />
          <span className="text-foreground font-medium">Last 7 days</span>
        </div>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {metrics.map((metric) => (
          <Card key={metric.title} className="metric-card">
            <CardContent className="p-6">
              <div className="flex items-start justify-between">
                <div className={`w-10 h-10 rounded-xl ${metric.bgColor} flex items-center justify-center`}>
                  <metric.icon className={`w-5 h-5 ${metric.color}`} />
                </div>
                <div className={`flex items-center gap-1 text-sm ${metric.trend === 'up' ? 'text-success' : 'text-destructive'}`}>
                  {metric.trend === 'up' ? (
                    <ArrowUpRight className="w-4 h-4" />
                  ) : (
                    <ArrowDownRight className="w-4 h-4" />
                  )}
                  {metric.change}
                </div>
              </div>
              <div className="mt-4">
                <p className="text-2xl font-bold text-foreground">{metric.value}</p>
                <p className="text-sm text-muted-foreground">{metric.title}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Performance Chart */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-primary" />
              Weekly Performance
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px]">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={performanceData}>
                  <defs>
                    <linearGradient id="colorConnections" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="hsl(210, 100%, 40%)" stopOpacity={0.2} />
                      <stop offset="95%" stopColor="hsl(210, 100%, 40%)" stopOpacity={0} />
                    </linearGradient>
                    <linearGradient id="colorResponses" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="hsl(142, 76%, 36%)" stopOpacity={0.2} />
                      <stop offset="95%" stopColor="hsl(142, 76%, 36%)" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(214, 32%, 91%)" />
                  <XAxis dataKey="name" stroke="hsl(215, 16%, 47%)" fontSize={12} />
                  <YAxis stroke="hsl(215, 16%, 47%)" fontSize={12} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: 'hsl(0, 0%, 100%)',
                      border: '1px solid hsl(214, 32%, 91%)',
                      borderRadius: '12px',
                    }}
                  />
                  <Area
                    type="monotone"
                    dataKey="connections"
                    stroke="hsl(210, 100%, 40%)"
                    fillOpacity={1}
                    fill="url(#colorConnections)"
                    strokeWidth={2}
                  />
                  <Area
                    type="monotone"
                    dataKey="responses"
                    stroke="hsl(142, 76%, 36%)"
                    fillOpacity={1}
                    fill="url(#colorResponses)"
                    strokeWidth={2}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Campaign Performance */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Zap className="w-5 h-5 text-primary" />
              Campaign Performance
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px]">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={campaignData} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(214, 32%, 91%)" />
                  <XAxis type="number" stroke="hsl(215, 16%, 47%)" fontSize={12} />
                  <YAxis dataKey="name" type="category" stroke="hsl(215, 16%, 47%)" fontSize={11} width={100} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: 'hsl(0, 0%, 100%)',
                      border: '1px solid hsl(214, 32%, 91%)',
                      borderRadius: '12px',
                    }}
                  />
                  <Bar dataKey="value" fill="hsl(210, 100%, 40%)" radius={[0, 6, 6, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Campaigns */}
      <Card>
        <CardHeader>
          <CardTitle>Active Campaigns</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {recentCampaigns.map((campaign) => (
              <div
                key={campaign.name}
                className="flex items-center justify-between p-4 rounded-xl bg-muted/50 hover:bg-muted transition-colors"
              >
                <div className="flex items-center gap-4">
                  <div className={`w-2 h-2 rounded-full ${
                    campaign.status === 'active' ? 'bg-success' :
                    campaign.status === 'paused' ? 'bg-warning' : 'bg-muted-foreground'
                  }`} />
                  <div>
                    <p className="font-medium text-foreground">{campaign.name}</p>
                    <p className="text-sm text-muted-foreground capitalize">{campaign.status}</p>
                  </div>
                </div>
                <div className="flex items-center gap-8">
                  <div className="text-right">
                    <p className="font-medium text-foreground">{campaign.leads}</p>
                    <p className="text-sm text-muted-foreground">Leads</p>
                  </div>
                  <div className="w-24">
                    <div className="h-2 bg-muted rounded-full overflow-hidden">
                      <div
                        className="h-full bg-primary rounded-full transition-all"
                        style={{ width: `${campaign.progress}%` }}
                      />
                    </div>
                    <p className="text-xs text-muted-foreground mt-1 text-right">{campaign.progress}%</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
